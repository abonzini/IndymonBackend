using System.Text;

namespace IndymonBackend
{
    public class TournamentManager
    {
        Tournament _ongoingTournament;
        DataContainers _backEndData;
        Random _rng = new Random();
        public TournamentManager(DataContainers backEndData)
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
                        _ongoingTournament = new Tournament();
                        break;
                    default:
                        validSelection = false;
                        break;
                }
            } while (!validSelection);
            Console.WriteLine("How many players will participate?");
            _ongoingTournament.NPlayers = int.Parse(Console.ReadLine());
            Console.WriteLine("How many pokemon each team?");
            _ongoingTournament.NMons = int.Parse(Console.ReadLine());
            // Finally, player selection
            List<TrainerData> trainers = _backEndData.TrainerData.Values.ToList();
            List<TrainerData> npcs = _backEndData.NpcData.Values.ToList();
            List<TrainerData> namedNpcs = _backEndData.NamedNpcData.Values.ToList();
            List<TrainerData> currentChosenTrainers = null;
            int remainingPlayersNeeded = _ongoingTournament.NPlayers;
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
                    _ongoingTournament.Participants.Add(nextTrainer);
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
            int n = _ongoingTournament.Participants.Count;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = _rng.Next(n + 1);
                (_ongoingTournament.Participants[k], _ongoingTournament.Participants[n]) = (_ongoingTournament.Participants[n], _ongoingTournament.Participants[k]); // Swap
            }
            // Ok not bad, next step is to update participant team sheet if needed, and generate the import pokepaste
            StringBuilder pokepasteBuilder = new StringBuilder();
            foreach (TrainerData participant in _ongoingTournament.Participants)
            {
                participant.DefineSets(_backEndData, _ongoingTournament.NMons); // Gets the team for everyone
                pokepasteBuilder.AppendLine($"=== {participant.Name} ===");
                pokepasteBuilder.AppendLine(participant.GetPokepaste(_backEndData, _ongoingTournament.NMons));
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
            _ongoingTournament.PlayTournament();
        }
    }
    public class Tournament
    {
        public int NPlayers = 0;
        public int NMons = 3;
        public List<TrainerData> Participants = new List<TrainerData>();
        class TournamentMatch()
        {
            public string player1 = "";
            public string player2 = "";
            public bool isBye = false;
            public int score1 = 0;
            public int score2 = 0;
            public string winner = "";
            public override string ToString()
            {
                return $"\t{player1} {((player1 == winner) ? "(v)" : "")} {score1}-{score2} {((player2 == winner) ? "(v)" : "")} {player2}";
            }
        }
        List<List<TournamentMatch>> _roundHistory = new List<List<TournamentMatch>>();
        /// <summary>
        /// Will play the tournament, organising the bracket and asking for results
        /// </summary>
        public void PlayTournament()
        {
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
                thisMatch.player1 = Participants[i].Name;
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
                    thisMatch.player2 = Participants[i].Name;
                    thisMatch.isBye = false;
                }
                thisRound.Add(thisMatch);
            }
            // Ok now the tournament begins
            bool finished = false;
            while (!finished)
            {
                _roundHistory.Add(thisRound); // Add the round to match history
                int playerProcessed = 0;
                List<TournamentMatch> futureRound = new List<TournamentMatch>();
                foreach (TournamentMatch match in thisRound)
                {
                    if (match.isBye)
                    {
                        Console.WriteLine($"{match.player1} has a bye");
                        match.winner = match.player1;
                    }
                    else
                    {
                        Console.WriteLine($"Score for match between {match.player1} and {match.player2}? 0 if randomized");
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
                        Console.WriteLine($"\t{match.ToString()}");
                    }
                    if ((playerProcessed % 2) == 0) // Even players, first of the next match
                    {
                        TournamentMatch nextMatch = new TournamentMatch();
                        nextMatch.player1 = match.winner;
                        futureRound.Add(nextMatch);
                    }
                    else // It's the player 2
                    {
                        TournamentMatch nextMatch = futureRound.Last();
                        nextMatch.player2 = match.winner;
                    }
                    playerProcessed++;
                }
                // Finally, now here's the next round, unless tournament was finished
                if (thisRound.Count == 1) // was the finals...
                {
                    finished = true;
                    Console.WriteLine($"\tTournament won by {thisRound.Last().winner}");
                }
                else
                {
                    thisRound = futureRound; // Otherwise move forward
                }
            }
        }
    }
}
