using System.Text;

namespace IndymonBackend
{
    public class TournamentManager
    {
        public Tournament OngoingTournament { get; set; }
        DataContainers _backEndData = null;
        Random _rng = new Random();
        public TournamentManager(DataContainers backEndData)
        {
            _backEndData = backEndData;
        }
        public TournamentManager()
        {

        }
        public void SetBackEndData(DataContainers backEndData)
        {
            _backEndData = backEndData;
        }
        /// <summary>
        /// Generates a new tournament, dialog asking for tpy, n players, n mons, and which participants
        /// </summary>
        /// <param name="backEndData"></param>
        public void GenerateNewTournament()
        {
            Console.WriteLine("Creation of a new tournament. Which type of tournament? [elim, swiss]");
            string inputString;
            bool validSelection = false;
            do
            {
                inputString = Console.ReadLine();
                switch (inputString.ToLower())
                {
                    case "elim":
                        validSelection = true;
                        OngoingTournament = new Tournament();
                        break;
                    default:
                        validSelection = false;
                        break;
                }
            } while (!validSelection);
            Console.WriteLine("How many players will participate?");
            OngoingTournament.NPlayers = int.Parse(Console.ReadLine());
            Console.WriteLine("How many pokemon each team?");
            OngoingTournament.NMons = int.Parse(Console.ReadLine());
            // Finally, player selection
            List<TrainerData> trainers = _backEndData.TrainerData.Values.ToList();
            List<TrainerData> npcs = _backEndData.NpcData.Values.ToList();
            List<TrainerData> namedNpcs = _backEndData.NamedNpcData.Values.ToList();
            List<TrainerData> currentChosenTrainers = null;
            int remainingPlayersNeeded = OngoingTournament.NPlayers;
            bool randomizeFill = false;
            while (remainingPlayersNeeded > 0) // Will do addition loop until all players are selected
            {
                TrainerData nextTrainer = null;
                if (currentChosenTrainers == null)
                {
                    Console.WriteLine("Which group of trainers to load? 1-Players, 2-NPCs, 3-Named NPCs. 4-Fill with random NPCs");
                    inputString = Console.ReadLine().ToLower();
                    switch (inputString)
                    {
                        case "1":
                            currentChosenTrainers = trainers;
                            break;
                        case "2":
                            currentChosenTrainers = npcs;
                            break;
                        case "3":
                            currentChosenTrainers = namedNpcs;
                            break;
                        case "4":
                            currentChosenTrainers = npcs;
                            randomizeFill = true;
                            break;
                        default:
                            break;
                    }
                }
                if (randomizeFill)
                {
                    int nextTrainerIndex = _rng.Next(currentChosenTrainers.Count); // Will pick one of them
                    nextTrainer = currentChosenTrainers[nextTrainerIndex];
                }
                else
                {
                    Console.WriteLine("Choose next trainer to add. 0 to go back");
                    for (int idx = 1; idx <= currentChosenTrainers.Count; idx++)
                    {
                        Console.Write($"{idx}-{currentChosenTrainers[idx - 1].Name} ");
                    }
                    Console.WriteLine("");
                    int trainerIdx = int.Parse(Console.ReadLine());
                    if (trainerIdx > 0 && trainerIdx <= currentChosenTrainers.Count)
                    {
                        // A valid trainer was chosen
                        nextTrainer = currentChosenTrainers[trainerIdx - 1];
                    }
                    else
                    {
                        currentChosenTrainers = null;
                    }
                }
                if (nextTrainer != null)
                {
                    currentChosenTrainers.Remove(nextTrainer);
                    remainingPlayersNeeded--;
                    OngoingTournament.Participants.Add(nextTrainer.Name);
                    Console.WriteLine($"{nextTrainer.Name} added");
                }
            }
        }
        /// <summary>
        /// Updates the tournament teams, meaning a team randomization and updating team sheets if needed
        /// </summary>
        public void UpdateTournamentTeams()
        {
            // First, shuffle the participants
            Random _rng = new Random();
            int n = OngoingTournament.Participants.Count;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = _rng.Next(n + 1);
                (OngoingTournament.Participants[k], OngoingTournament.Participants[n]) = (OngoingTournament.Participants[n], OngoingTournament.Participants[k]); // Swap
            }
            // Ok not bad, next step is to update participant team sheet if needed, and generate the import pokepaste
            StringBuilder pokepasteBuilder = new StringBuilder();
            foreach (string participantName in OngoingTournament.Participants)
            {
                // Try to find the participant
                TrainerData participant;
                if (_backEndData.TrainerData.ContainsKey(participantName)) participant = _backEndData.TrainerData[participantName];
                else if (_backEndData.NpcData.ContainsKey(participantName)) participant = _backEndData.NpcData[participantName];
                else if (_backEndData.NamedNpcData.ContainsKey(participantName)) participant = _backEndData.NamedNpcData[participantName];
                else throw new Exception("Trainer not found!?");
                participant.DefineSets(_backEndData, OngoingTournament.NMons); // Gets the team for everyone
                pokepasteBuilder.AppendLine($"=== {participant.Name} ===");
                pokepasteBuilder.AppendLine(participant.GetPokepaste(_backEndData, OngoingTournament.NMons));
            }
            // Finally, ready to save the pokepaste
            string exportFile = Path.Combine(_backEndData.MasterDirectory, "importable-pokepaste.txt");
            File.WriteAllText(exportFile, pokepasteBuilder.ToString()); // Save the export
        }
        /// <summary>
        /// Starts the tourn proper, will ask for input of scores
        /// </summary>
        public void ExecuteTournament()
        {
            OngoingTournament.PlayTournament();
        }
    }
    public class TournamentMatch()
    {
        public string player1 { get; set; } = "";
        public string player2 { get; set; } = "";
        public bool isBye { get; set; } = false;
        public int score1 { get; set; } = 0;
        public int score2 { get; set; } = 0;
        public string winner { get; set; } = "";
        public override string ToString()
        {
            return $"{player1} ({score1}-{score2}) {player2}";
        }
    }
    public class Tournament
    {
        public int NPlayers { get; set; } = 0;
        public int NMons { get; set; } = 3;
        public List<string> Participants { get; set; } = new List<string>();
        public List<List<TournamentMatch>> RoundHistory { get; set; } = null;
        /// <summary>
        /// Will play the tournament, organising the bracket and asking for results
        /// </summary>
        public void PlayTournament()
        {
            if (RoundHistory == null) // Brand new tournament
            {
                RoundHistory = new List<List<TournamentMatch>>();
                int closestPowerOf2 = 1;
                while (closestPowerOf2 < NPlayers) closestPowerOf2 *= 2; // Find the closest po2 above player name (will add byes)
                int numberOfByes = closestPowerOf2 - NPlayers;
                int byesBeginning = (numberOfByes / 2) + (numberOfByes % 2); // The beginning ones may have 1 more bye
                int byesEnd = numberOfByes / 2; // The beginning ones may have 1 more bye
                // Ok will start doing the matchups, first round
                List<TournamentMatch> thisRound = new List<TournamentMatch>();
                for (int i = 0; i < Participants.Count; i++)
                {
                    TournamentMatch thisMatch = new TournamentMatch();
                    thisMatch.player1 = Participants[i];
                    if (i < byesBeginning)
                    {
                        thisMatch.isBye = true;
                    }
                    else if (i >= (Participants.Count - byesEnd))
                    {
                        thisMatch.isBye = true;
                    }
                    else // Not a bye in either side...
                    {
                        i++; // Get next player (opp)
                        thisMatch.player2 = Participants[i];
                        thisMatch.isBye = false;
                    }
                    thisRound.Add(thisMatch);
                }
                RoundHistory.Add(thisRound);
            }
            // This is the part that loads a tournament, visually it prints all matches and prompts user one by one
            bool finished = false;
            while (!finished)
            {
                List<TournamentMatch> currentRound = RoundHistory.Last();
                List<TournamentMatch> nextRound = new List<TournamentMatch>();
                int playerProcessed = 0;
                Console.Clear();
                Console.WriteLine("Insert scores for each match. 0 if you want it randomized, q to stop input temporarily");
                int consoleEndPosition = currentRound.Count + 1; // Where the cursor will be after data entry
                int maxStringLength = 0;
                foreach (TournamentMatch match in currentRound) // Print all rounds first (so that they can be simulated if needed
                {
                    string matchString;
                    if (match.isBye)
                    {
                        matchString = $"{match.player1} gets a bye";
                    }
                    else
                    {
                        matchString = $"{match.player1} v {match.player2}";
                    }
                    maxStringLength = Math.Max(maxStringLength, matchString.Length);
                    Console.WriteLine(matchString);
                }
                for (int i = 0; i < currentRound.Count; i++)
                {
                    TournamentMatch match = currentRound[i];
                    if (match.isBye)
                    {
                        match.winner = match.player1;
                    }
                    else
                    {
                        Console.SetCursorPosition(maxStringLength + 1, i + 1); // Put the cursor on the right, and starting from 1 (to avoid message string)
                        string scoreString = Console.ReadLine();
                        if (scoreString == "0")
                        {
                            Random _rng = new Random(); // Will randomize result
                            if (_rng.Next(2) == 0) // Winner was 1
                            {
                                match.score1 = _rng.Next(1, NMons + 1);
                                match.score2 = 0;
                            }
                            else // Winner was 2
                            {
                                match.score1 = 0;
                                match.score2 = _rng.Next(1, NMons + 1);
                            }
                        }
                        else if (scoreString.ToLower() == "q") // If q, just stop here we'll need to restart after
                        {
                            finished = true;
                            break; // Stops iteration
                        }
                        else
                        {
                            string[] scores = scoreString.Split('-');
                            match.score1 = int.Parse(scores[0]);
                            match.score2 = int.Parse(scores[1]);
                        }
                        if (match.score1 > match.score2)
                        {
                            match.winner = match.player1;
                        }
                        else
                        {
                            match.winner = match.player2;
                        }
                    }
                    if ((playerProcessed % 2) == 0) // Even players, first of the next match
                    {
                        TournamentMatch nextMatch = new TournamentMatch();
                        nextMatch.player1 = match.winner;
                        nextRound.Add(nextMatch);
                    }
                    else // It's the player 2
                    {
                        TournamentMatch nextMatch = nextRound.Last();
                        nextMatch.player2 = match.winner;
                    }
                    playerProcessed++;
                }
                // Finally, now here's the next round, unless tournament was finished
                if (finished || currentRound.Count == 1) // If all's finished (either manually or because this was the last round)
                {
                    finished = true;
                    Console.SetCursorPosition(0, consoleEndPosition); // Sets the console in the right place
                    Console.WriteLine($"Tournament data entry done");
                }
                else
                {
                    RoundHistory.Add(nextRound); // Adds the next round to the pile
                }
            }
        }
    }
}
