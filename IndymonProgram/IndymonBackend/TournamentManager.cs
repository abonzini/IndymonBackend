using ParsersAndData;
using ShowdownBot;

namespace IndymonBackendProgram
{
    public class IndividualMu
    {
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public float Winrate { get { return (float)Wins / (float)(Losses + Wins); } }
    }
    public class PlayerAndStats
    {
        public string Name { get; set; }
        public Dictionary<string, IndividualMu> EachMuWr { get; set; } = null; // Contains each matchup
        public int TournamentWins { get; set; } = 0;
        public int TournamentsPlayed { get; set; } = 1;
        public float Winrate { get { return (float)TournamentWins / (float)TournamentsPlayed; } }
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Diff { get { return Kills - Deaths; } }
        public override string ToString()
        {
            return $"{Name}: {TournamentWins}/{TournamentsPlayed})";
        }
    }
    public class TournamentHistory
    {
        public List<PlayerAndStats> PlayerStats { get; set; } = new List<PlayerAndStats>();
        public List<PlayerAndStats> NpcStats { get; set; } = new List<PlayerAndStats>();
    }
    public class TournamentManager
    {
        public Tournament OngoingTournament { get; set; }
        DataContainers _backEndData = null;
        TournamentHistory _leaderboard = null;
        readonly Random _rng = new Random();
        public TournamentManager(DataContainers backEndData, TournamentHistory leaderboard)
        {
            _backEndData = backEndData;
            _leaderboard = leaderboard;
        }
        public TournamentManager()
        {

        }
        public void SetBackEndData(DataContainers backEndData, TournamentHistory leaderboard)
        {
            _backEndData = backEndData;
            _leaderboard = leaderboard;
        }
        /// <summary>
        /// Generates a new tournament, dialog asking for tpy, n players, n mons, and which participants
        /// </summary>
        /// <param name="backEndData"></param>
        public void GenerateNewTournament()
        {
            Console.WriteLine("Creation of a new tournament. Which type of tournament? [elim, king, group]");
            string inputString;
            bool validSelection = false;
            do
            {
                inputString = Console.ReadLine();
                switch (inputString.ToLower())
                {
                    case "elim":
                        validSelection = true;
                        OngoingTournament = new ElimTournament();
                        break;
                    case "king":
                        validSelection = true;
                        OngoingTournament = new KingOfTheHillTournament();
                        break;
                    case "group":
                        validSelection = true;
                        OngoingTournament = new GroupStageTournament();
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
            OngoingTournament.RequestAdditionalInfo(); // Request tournament-specific info (if needed)
            // Finally, player selection, pre-filter traines whether they can participate in this event
            List<TrainerData> trainers = [.. _backEndData.TrainerData.Values.Where(t => t.GetValidTeamComps(_backEndData, OngoingTournament.NMons, OngoingTournament.TeamBuildSettings).Count > 0)];
            List<TrainerData> npcs = [.. _backEndData.NpcData.Values.Where(t => t.GetValidTeamComps(_backEndData, OngoingTournament.NMons, OngoingTournament.TeamBuildSettings).Count > 0)];
            List<TrainerData> namedNpcs = [.. _backEndData.NamedNpcData.Values.Where(t => t.GetValidTeamComps(_backEndData, OngoingTournament.NMons, OngoingTournament.TeamBuildSettings).Count > 0)];
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
                    if (currentChosenTrainers.Count > 0)
                    {
                        int nextTrainerIndex = _rng.Next(currentChosenTrainers.Count); // Will pick one of them
                        nextTrainer = currentChosenTrainers[nextTrainerIndex];
                    }
                    else
                    {
                        currentChosenTrainers = null;
                        randomizeFill = false; // Have to end randomized fill
                    }
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
        /// <param name="Seeds">Potential seed list if some players are too good and need to done last (this is mostly for special tournaments)</param>
        public void UpdateTournamentTeams()
        {
            // First, shuffle the participants (use seed if needed)
            List<string> Seeds = new List<string>();
            Console.WriteLine("Want to add specific seeding? y/N");
            string seedInput = Console.ReadLine();
            if (seedInput.Trim().ToLower() == "y") // One last seeding step
            {
                List<string> seedOptions = [.. OngoingTournament.Participants];
                bool seedingFinished = false;
                while (!seedingFinished && Seeds.Count < OngoingTournament.Participants.Count) // Continue seeding until finished or all players seeded
                {
                    Console.WriteLine("Choose next seed, or anything if finished seeding:");
                    for (int i = 0; i < seedOptions.Count; i++)
                    {
                        Console.Write($"{i + 1}: " + seedOptions[i] + ",");
                    }
                    if (int.TryParse(Console.ReadLine(), out int seedChoice)) // If user chose one...
                    {
                        Seeds.Add(seedOptions[seedChoice - 1]);
                        seedOptions.RemoveAt(seedChoice - 1);
                    }
                    else // Finish here
                    {
                        seedingFinished = true;
                    }
                }
            }
            OngoingTournament.ShuffleWithSeeds(Seeds);
            // Reset the tournament if one was already in progress
            OngoingTournament.ResetTournament();
            // Ok not bad, next step is to update participant team sheet if needed
            foreach (string participantName in OngoingTournament.Participants)
            {
                // Try to find the participant in the place where located
                if (_backEndData.TrainerData.TryGetValue(participantName, out TrainerData participant)) { }
                else if (_backEndData.NpcData.TryGetValue(participantName, out participant)) { }
                else if (_backEndData.NamedNpcData.TryGetValue(participantName, out participant)) { }
                else throw new Exception("Trainer not found!?");
                participant.ConfirmSets(_backEndData, OngoingTournament.NMons, OngoingTournament.TeamBuildSettings); // Gets the team for everyone with the settings needed for the tournament
            }
        }
        /// <summary>
        /// Starts the tourn proper, will ask for input of scores
        /// </summary>
        public void ExecuteTournament()
        {
            OngoingTournament.PlayTournament(_backEndData);
        }
        /// <summary>
        /// Does the animation and stuff
        /// </summary>
        public void FinaliseTournament()
        {
            OngoingTournament.FinaliseTournament();
            // Also, ask the tournament to update the sheets
            OngoingTournament.UpdateLeaderboard(_leaderboard, _backEndData);
        }
    }
    public class TournamentMatch()
    {
        public string Player1 { get; set; } = "";
        public string Player2 { get; set; } = "";
        public bool IsBye { get; set; } = false;
        public int Score1 { get; set; } = 0;
        public int Score2 { get; set; } = 0;
        public string Winner { get; set; } = "";
        public int DrawHelper1 { get; set; } = 0; // For the bracket drawing
        public int DrawHelper2 { get; set; } = 0;
        public override string ToString()
        {
            return $"{Player1} ({Score1}-{Score2}) {Player2}";
        }
    }
    public abstract class Tournament
    {
        public bool Official { get; set; } = true;
        public bool FirstInstallment { get; set; } = true;
        public TeambuildSettings TeamBuildSettings { get; set; } = TeambuildSettings.SMART; // Teams will be smart always in tournaments (human v human)
        public int NPlayers { get; set; } = 0;
        public int NMons { get; set; } = 3;
        public List<string> Participants { get; set; } = new List<string>();
        /// <summary>
        /// Duting tournament init, asks for extra info if needed
        /// </summary>
        public abstract void RequestAdditionalInfo();
        /// <summary>
        /// Resets the tournament internally so that it begins anew
        /// </summary>
        public abstract void ResetTournament();
        /// <summary>
        /// From a tournament, it shuffles players, but also has a list of top seeds (from best to worst) if needed in some tournament
        /// </summary>
        /// <param name="Seeds">Seed list to be used in tournament</param>
        public abstract void ShuffleWithSeeds(List<string> Seeds);
        /// <summary>
        /// Will play the tournament, organising the bracket and asking for results or simulating it
        /// </summary>
        /// <param name="_backEndData">Back end just in case needs to simulate bot</param>
        public abstract void PlayTournament(DataContainers _backEndData);
        /// <summary>
        /// Performs tournament animation once complete
        /// </summary>
        public abstract void FinaliseTournament();
        /// <summary>
        /// Asks tournament to update leaderboard according to match history
        /// </summary>
        /// <param name="leaderboard">The leaderboard to update</param>
        /// <param name="backend">Backend, to determine whether a character is added into leaderboard</param>
        public abstract void UpdateLeaderboard(TournamentHistory leaderboard, DataContainers backend);
        /// <summary>
        /// Resolves a match, including the possibility of creating a bot battle, or manual score input. This sometimes writes into console current cursor.
        /// </summary>
        /// <param name="match">Match to evaluate</param>
        /// <param name="backendData">Backend used to find players, etc</param>
        /// <returns>True if match succesfully concluded, false otherwise</returns>
        public bool ResolveMatch(TournamentMatch match, DataContainers backendData)
        {
            if (match.IsBye)
            {
                match.Winner = match.Player1;
            }
            else
            {
                (int cursorX, int cursorY) = Console.GetCursorPosition(); // Just in case I need to write in same place
                string scoreString = Console.ReadLine();
                if (scoreString == "0")
                {
                    Random _rng = new Random(); // Will randomize result
                    if (_rng.Next(2) == 0) // Winner was 1
                    {
                        match.Score1 = _rng.Next(1, NMons + 1);
                        match.Score2 = 0;
                    }
                    else // Winner was 2
                    {
                        match.Score1 = 0;
                        match.Score2 = _rng.Next(1, NMons + 1);
                    }
                    Console.Write($"{match.Score1}-{match.Score2}");
                }
                else if (scoreString.ToLower() == "q") // If q, just stop here we'll need to restart after
                {
                    return false;
                }
                else if (scoreString.ToLower() == "b") // If b, do battle bots
                {
                    TrainerData p1 = Utilities.GetTrainerByName(match.Player1, backendData);
                    TrainerData p2 = Utilities.GetTrainerByName(match.Player2, backendData);
                    BotBattle automaticBattle = new BotBattle(backendData);
                    string challengeString = "gen9customgame@@@OHKO Clause,Evasion Moves Clause,Moody Clause";
                    (match.Score1, match.Score2) = automaticBattle.SimulateBotBattle(p1, p2, NMons, NMons, challengeString);
                    Console.SetCursorPosition(cursorX, cursorY);
                    Console.Write($"{match.Score1}-{match.Score2} GET THE REPLAY");
                }
                else
                {
                    string[] scores = scoreString.Split('-');
                    match.Score1 = int.Parse(scores[0]);
                    match.Score2 = int.Parse(scores[1]);
                }
                // Determine winner
                if (match.Score1 > match.Score2)
                {
                    match.Winner = match.Player1;
                }
                else
                {
                    match.Winner = match.Player2;
                }
            }
            return true;
        }
        /// <summary>
        /// Register all participant's tournament played count, also adds them if not there before
        /// </summary>
        /// <param name="leaderboard">Leaderboard to add</param>
        /// <param name="backend">Backend for data</param>
        protected void RegisterTournamentParticipation(TournamentHistory leaderboard, DataContainers backend)
        {
            if (!FirstInstallment) return; // Dont do anything if tournament had already begun
            foreach (string participant in Participants)
            {
                List<PlayerAndStats> participantLocation = null;
                if (backend.TrainerData.ContainsKey(participant)) participantLocation = leaderboard.PlayerStats; // Is it a trainer?
                else if (backend.TrainerData.ContainsKey(participant)) participantLocation = leaderboard.NpcStats; // Is it NPC?
                else { } // Never mind then (leave null)
                if (participantLocation != null) // Add participant if relevant
                {
                    PlayerAndStats playerStats = participantLocation.FirstOrDefault(p => (p.Name == participant)); // Get data for player
                    if (playerStats != null) // Player wasn't there, need to add
                    {
                        PlayerAndStats newPlayer = new PlayerAndStats()
                        {
                            Name = participant,
                            TournamentsPlayed = 1,
                            Deaths = 0,
                            Kills = 0,
                            TournamentWins = 0,
                            EachMuWr = new Dictionary<string, IndividualMu>()
                        };
                        participantLocation.Add(newPlayer);
                    }
                    else
                    {
                        playerStats.TournamentsPlayed++;
                    }
                }
            }
        }
        /// <summary>
        /// Processes the standings for a match
        /// </summary>
        /// <param name="match">The match</param>
        /// <param name="leaderboard">Leaderboard to update</param>
        /// <param name="backend">Backend for extra data</param>
        protected void ProcessMatchStandings(TournamentMatch match, TournamentHistory leaderboard, DataContainers backend)
        {
            // Try to find where P1 is
            List<PlayerAndStats> p1Location = null;
            if (backend.TrainerData.ContainsKey(match.Player1)) p1Location = leaderboard.PlayerStats; // Is it a trainer?
            else if (backend.TrainerData.ContainsKey(match.Player1)) p1Location = leaderboard.NpcStats; // Is it NPC?
            else { } // Never mind then
            List<PlayerAndStats> p2Location = null;
            if (!match.IsBye) // If there's actually a p2...
            {
                if (backend.TrainerData.ContainsKey(match.Player2)) p2Location = leaderboard.PlayerStats; // Is it a trainer?
                else if (backend.TrainerData.ContainsKey(match.Player2)) p2Location = leaderboard.NpcStats; // Is it NPC?
                else { } // Never mind then
            }
            // After that is done, guaranteed everything is in place, just update each match stats individually
            if (!match.IsBye) // A bye doesn't have additional stats as no one fought anyone
            {
                // These 2 should exist unless they're not players or npc
                PlayerAndStats p1Stats = p1Location?.First(p => (p.Name == match.Player1));
                PlayerAndStats p2Stats = p2Location?.First(p => (p.Name == match.Player2));
                // How many mons each player killed
                int p1Kills = (NMons - match.Score2);
                int p2Kills = (NMons - match.Score1);
                // Update all remaining stats
                if (p1Stats != null)
                {
                    p1Stats.Kills += p1Kills;
                    p1Stats.Deaths += p2Kills;
                    if (!p1Stats.EachMuWr.TryGetValue(match.Player2, out IndividualMu mu))
                    {
                        mu = new IndividualMu();
                        p1Stats.EachMuWr.Add(match.Player2, mu);
                    }
                    bool playerWon = (match.Winner.Trim().ToLower() == p1Stats.Name.Trim().ToLower());
                    if (playerWon)
                    {
                        mu.Wins++;
                    }
                    else
                    {
                        mu.Losses++;
                    }
                }
                if (p2Stats != null)
                {
                    p2Stats.Kills += p2Kills;
                    p2Stats.Deaths += p1Kills;
                    if (!p2Stats.EachMuWr.TryGetValue(match.Player1, out IndividualMu mu))
                    {
                        mu = new IndividualMu();
                        p2Stats.EachMuWr.Add(match.Player1, mu);
                    }

                    bool playerWon = (match.Winner.Trim().ToLower() == p2Stats.Name.Trim().ToLower());
                    if (playerWon)
                    {
                        mu.Wins++;
                    }
                    else
                    {
                        mu.Losses++;
                    }
                }
            }
        }
        /// <summary>
        /// Sets tournament winner
        /// </summary>
        /// <param name="winner">Who won</param>
        /// <param name="leaderboard">Leaderboard to update</param>
        /// <param name="backend">Backend for extra data</param>
        protected void SetTournamentWinner(string winnerName, TournamentHistory leaderboard, DataContainers backend)
        {
            if (!Official) return; // Non official tournaments are not tallied
            List<PlayerAndStats> winnerLocation = null;
            if (backend.TrainerData.ContainsKey(winnerName)) winnerLocation = leaderboard.PlayerStats; // Is it a trainer?
            else if (backend.TrainerData.ContainsKey(winnerName)) winnerLocation = leaderboard.NpcStats; // Is it NPC?
            else { } // Never mind then
            PlayerAndStats winnerStats = winnerLocation?.FirstOrDefault(p => (p.Name == winnerName));
            if (winnerStats != null) winnerStats.TournamentWins++;
        }
        /// <summary>
        /// Checks if certain tournaments are official or not (just to set official tournament wins)
        /// </summary>
        protected void AskIfOfficial()
        {
            Console.WriteLine("Is this an official (sanctioned) tournament? Y/n");
            string response = Console.ReadLine();
            if (response.Trim().ToLower() == "n")
            {
                Official = false;
            }
            else
            {
                Official = true;
            }
        }
        /// <summary>
        /// Checks if tournament is a 2nd part (e.g. elim after groups) so it doesnt increment counter twice
        /// </summary>
        protected void AskIf2ndPart()
        {
            Console.WriteLine("Is this the first installment of a tournament? Y/n (Otherwise a 2nd part of an already started tournament)");
            string response = Console.ReadLine();
            if (response.Trim().ToLower() == "n")
            {
                FirstInstallment = false;
            }
            else
            {
                FirstInstallment = true;
            }
        }
        /// <summary>
        /// Checks if certain tournaments are monotype or not
        /// </summary>
        protected void AskSpecialRulesets()
        {
            Console.WriteLine("Is this a monotype tournament? y/N");
            string response = Console.ReadLine();
            if (response.Trim().ToLower() == "y")
            {
                TeamBuildSettings |= TeambuildSettings.MONOTYPE; // If so, then this is monotype
            }
            Console.WriteLine("Is this a dance-off tournament? y/N");
            response = Console.ReadLine();
            if (response.Trim().ToLower() == "y")
            {
                TeamBuildSettings |= TeambuildSettings.DANCERS; // If so, then this is dance-off
            }
        }
    }
    public class ElimTournament : Tournament
    {
        // Internal draw helpers
        const float DRAW_RYTHM_PERIOD = 1.0f;
        const float BLINK_TOGGLE_PERIOD = 0.5f;
        const int NUMBER_OF_BLINKS = 3;
        public List<List<TournamentMatch>> RoundHistory { get; set; } = null;
        // Seed helper
        public int[] SeedOrder { get; set; }
        public override void RequestAdditionalInfo()
        {
            AskIfOfficial();
            AskIf2ndPart();
            AskSpecialRulesets();
        }
        public override void ResetTournament()
        {
            RoundHistory = null;
        }
        public override void ShuffleWithSeeds(List<string> Seeds)
        {
            // First, assemble a list of seeds, for all players get seed order from that to the closest power of 2
            int closestPowerOf2 = 1;
            while (closestPowerOf2 < NPlayers) closestPowerOf2 *= 2;
            SeedOrder = new int[closestPowerOf2]; // Create space for all seeds
            int stage = 0;
            while (stage < closestPowerOf2) // Continue until i reach closest power of 2
            {
                if (stage == 0) // First stage there's no logic, just add 1
                {
                    SeedOrder[0] = 0;
                    stage = 1; // Start with power of 2
                }
                else // Otherwise need to expand seed list, stage also contains how many players need to be there
                {
                    // First stage is to move all entries and leave one space between
                    for (int i = stage - 1; i >= 0; i--)
                    {
                        SeedOrder[2 * i] = SeedOrder[i]; // Move to next even
                    }
                    // Then, go on twos, fill the next one with the complement
                    int complement = (2 * stage) - 1;
                    for (int i = 0; i < stage; i++)
                    {
                        int mu = complement - SeedOrder[2 * i]; // Calculate MU
                        SeedOrder[(2 * i) + 1] = mu; // Put it in the array
                    }
                    // Next stage
                    stage *= 2;
                }
            }
            // Ok now shuffle everything
            Utilities.ShuffleList(Participants, 0, Participants.Count);
            // Seeding will involve putting the best first
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = Participants.IndexOf(Seeds[seed]); // Find where seed is currently
                // Perform switch
                if (currentIndex != seed)
                {
                    (Participants[currentIndex], Participants[seed]) = (Participants[seed], Participants[currentIndex]); // Swap
                }
            }
        }
        public override void PlayTournament(DataContainers backEndData)
        {
            Console.CursorVisible = true;
            if (RoundHistory == null) // Brand new tournament
            {
                RoundHistory = new List<List<TournamentMatch>>();
                // Ok will start doing the matchups, first round
                List<TournamentMatch> thisRound = new List<TournamentMatch>();
                // Need to find seed by seed, and the ones that are higher than the number of player involve a bye
                // Due to how algorithm was created, p1 will always be valid
                int drawHelper = 0; // Where next player will be drawn
                for (int i = 0; i < SeedOrder.Length; i += 2) // Go in pairs
                {
                    TournamentMatch thisMatch = new TournamentMatch();
                    // Find both players
                    int p1Index = SeedOrder[i];
                    int p2Index = SeedOrder[i + 1];
                    thisMatch.Player1 = Participants[p1Index];
                    thisMatch.DrawHelper1 = drawHelper * 2; // Go to the next even (leave a space between names)
                    drawHelper++; // Draw next
                    if (p2Index < NPlayers) // Meaning the next seed MU is valid
                    {
                        thisMatch.Player2 = Participants[p2Index]; // Got the second player
                        thisMatch.IsBye = false;
                        thisMatch.DrawHelper2 = drawHelper * 2;
                        drawHelper++;
                    }
                    else // Otherwise it's a bye
                    {
                        thisMatch.IsBye = true;
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
                Console.WriteLine("Insert scores for each match. 0 if you want it randomized, q to stop input temporarily. b FOR ROBOTS");
                int consoleEndPosition = currentRound.Count + 1; // Where the cursor will be after data entry
                int maxStringLength = 0;
                foreach (TournamentMatch match in currentRound) // Print all rounds first (so that they can be simulated if needed
                {
                    string matchString;
                    if (match.IsBye)
                    {
                        matchString = $"{match.Player1} gets a bye";
                    }
                    else
                    {
                        matchString = $"{match.Player1} v {match.Player2}";
                    }
                    maxStringLength = Math.Max(maxStringLength, matchString.Length);
                    Console.WriteLine(matchString);
                }
                for (int i = 0; i < currentRound.Count; i++)
                {
                    TournamentMatch match = currentRound[i];
                    Console.SetCursorPosition(maxStringLength + 1, i + 1); // Put the cursor on the right, and starting from 1 (to avoid message string)
                    if (!ResolveMatch(match, backEndData)) // Do the match, if not succesful (e.g. aborted), then we stop here
                    {
                        return;
                    }
                    if ((playerProcessed % 2) == 0) // Even players, first of the next match
                    {
                        TournamentMatch nextMatch = new TournamentMatch
                        {
                            Player1 = match.Winner,
                            DrawHelper1 = match.IsBye ? match.DrawHelper1 : CalculateMidPoint(match.DrawHelper1, match.DrawHelper2) // Bye continues in same place, bracket goes to midpoint
                        };
                        nextRound.Add(nextMatch);
                    }
                    else // It's the player 2
                    {
                        TournamentMatch nextMatch = nextRound.Last();
                        nextMatch.Player2 = match.Winner;
                        nextMatch.DrawHelper2 = match.IsBye ? match.DrawHelper1 : CalculateMidPoint(match.DrawHelper1, match.DrawHelper2); // Bye continues in same place, bracket goes to midpoint
                    }
                    playerProcessed++;
                }
                // Finally, now here's the next round, unless tournament was finished
                if (currentRound.Count == 1) // If all's finished (either manually or because this was the last round)
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
            Console.CursorVisible = false;
        }
        public override void FinaliseTournament()
        {
            // Find person with the longest name
            int nameLength = 0;
            foreach (string participant in Participants)
            {
                nameLength = Math.Max(nameLength, participant.Length + 1);
            }
            // Need to resize console so this fits
            int minXSize = ((RoundHistory.Count + 1) * nameLength) + RoundHistory.Count; // Need to fit names and brackets
            int minYSize = (NPlayers * 2) + 1; // Need to fit names
            while ((Console.WindowHeight < minYSize) || (Console.WindowWidth < minXSize))
            {
                Console.WriteLine($"Console has to have atleast dimensions X:{minXSize} Y: {minYSize}");
                Console.WriteLine($"Current X:{Console.WindowWidth} Y: {Console.WindowHeight}");
                Console.ReadLine();
            }
            // Then, perform a tournament animation
            Console.Clear();
            int round, cursorX;
            for (round = 0; round < RoundHistory.Count; round++) // Check each round
            {
                cursorX = round * (nameLength + 2); // Each one will have a horizontal offset of round * (name + 1 + 1) (name+bracket+margin)
                List<TournamentMatch> matchesThisRound = RoundHistory[round];
                if (round == 0) // In first round, need to place players beforehand
                {
                    foreach (TournamentMatch match in matchesThisRound) // First, draw all names in the right position
                    {
                        Console.SetCursorPosition(cursorX, match.DrawHelper1); // First player always there
                        Console.Write(match.Player1);
                        if (!match.IsBye)
                        {
                            Console.SetCursorPosition(cursorX, match.DrawHelper2); // Also draw p2 if there's any
                            Console.Write(match.Player2);
                        }
                    }
                }
                // OK now the brackets...
                foreach (TournamentMatch match in matchesThisRound)
                {
                    if (match.IsBye)
                    {
                        Thread.Sleep((int)(DRAW_RYTHM_PERIOD * 1000)); // Wait and then draw the single line right
                        Console.SetCursorPosition(cursorX + nameLength, match.DrawHelper1); // Put it after name
                        Console.Write("─");
                        Console.SetCursorPosition(((round + 1) * (nameLength + 2)), match.DrawHelper1); // Need to place the winner in next round
                    }
                    else
                    {
                        // Need to draw and re-draw the bracket...
                        if (BLINK_TOGGLE_PERIOD < DRAW_RYTHM_PERIOD)
                        {
                            Thread.Sleep((int)((DRAW_RYTHM_PERIOD - BLINK_TOGGLE_PERIOD) * 1000)); // For visual rythm consistnecy
                        }
                        for (int blink = 0; blink < NUMBER_OF_BLINKS; blink++)
                        {
                            Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000)); // Wait and then draw the bracket, on and off
                            DrawBracket(match.DrawHelper1, match.DrawHelper2, cursorX + nameLength, true);
                            Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000)); // Wait and then draw the bracket, on and off
                            DrawBracket(match.DrawHelper1, match.DrawHelper2, cursorX + nameLength, false);
                        }
                        Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000)); // Wait and then draw the bracket, on and off
                        DrawBracket(match.DrawHelper1, match.DrawHelper2, cursorX + nameLength, true);
                        // And then after blink just put the score between
                        Console.SetCursorPosition(cursorX, CalculateMidPoint(match.DrawHelper1, match.DrawHelper2)); // Put it after name
                        if (BLINK_TOGGLE_PERIOD < DRAW_RYTHM_PERIOD)
                        {
                            Thread.Sleep((int)((DRAW_RYTHM_PERIOD - BLINK_TOGGLE_PERIOD) * 1000)); // For visual rythm consistnecy
                        }
                        Console.Write($"({match.Score1}-{match.Score2})");
                        Console.SetCursorPosition(((round + 1) * (nameLength + 2)), CalculateMidPoint(match.DrawHelper1, match.DrawHelper2)); // Need to place the winner in next round
                    }
                    Console.Write(match.Winner);
                }
            }
            Console.ReadKey();
            Console.Clear();
            // And that should be it?!
        }
        #region BRACKET_DRAWING
        /// <summary>
        /// Draws a bracket in console, always vertical
        /// </summary>
        /// <param name="beginningY">Where bracket begins Y</param>
        /// <param name="endY">Where bracket ends Y</param>
        /// <param name="x">X position of bracker</param>
        /// <param name="show">Whether to show or to hide</param>
        static void DrawBracket(int beginningY, int endY, int x, bool show)
        {
            int midpoint = CalculateMidPoint(beginningY, endY);
            // Walk char by char drawing accordingly
            for (int y = beginningY; y <= endY; y++)
            {
                Console.SetCursorPosition(x, y); // Next position
                if (!show)
                {
                    Console.CursorLeft++;
                    Console.Write("\b "); // Overwrite with space??
                }
                else if (y == beginningY)
                {
                    Console.Write("┐");
                }
                else if (y == endY)
                {
                    Console.Write("┘");
                }
                else if (y == midpoint)
                {
                    Console.Write("├");
                }
                else
                {
                    Console.Write("│");
                }
            }
        }
        /// <summary>
        /// Calculates midpoint for bracket calculation. in a function to keep consistent
        /// </summary>
        /// <param name="line1">Where bracket begins</param>
        /// <param name="line2">Where bracket ends</param>
        /// <returns>The location of midpoint</returns>
        static int CalculateMidPoint(int line1, int line2)
        {
            int average = (line1 + line2);
            bool integer = (average % 2) == 0;
            average /= 2;
            if (!integer) average++; // Move to the next odd number if middle is even (a good bracket should fall in odd numbers)
            return average;
        }
        #endregion
        public override void UpdateLeaderboard(TournamentHistory leaderboard, DataContainers backend)
        {
            RegisterTournamentParticipation(leaderboard, backend); // First, make sure all players who participated have their tournament # increased
            for (int round = 0; round < RoundHistory.Count; round++) // Check round by round
            {
                List<TournamentMatch> matchesThisRound = RoundHistory[round]; // Get matches for this round
                foreach (TournamentMatch match in matchesThisRound) // Need to gather the matches to calculate each match stat
                {
                    ProcessMatchStandings(match, leaderboard, backend);
                }
            }
            // Ok! finally need the winner
            string winnerName = RoundHistory.Last().Last().Winner; // Last winner of last match of last round is tournament winner
            SetTournamentWinner(winnerName, leaderboard, backend);
        }
    }
    public class KingOfTheHillTournament : Tournament
    {
        // Internal draw helpers
        const float DRAW_RYTHM_PERIOD = 1.0f;
        const float BLINK_TOGGLE_PERIOD = 0.5f;
        const int NUMBER_OF_BLINKS = 3;
        public List<TournamentMatch> MatchHistory { get; set; } = null;
        public override void RequestAdditionalInfo()
        {
            AskIfOfficial();
            AskIf2ndPart();
            AskSpecialRulesets();
        }
        public override void ResetTournament()
        {
            MatchHistory = null;
        }
        public override void ShuffleWithSeeds(List<string> Seeds)
        {
            // Shuffle everything
            Utilities.ShuffleList(Participants, 0, Participants.Count);
            // Seeding in KOH means putting the best in the bottom
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = Participants.IndexOf(Seeds[seed]); // Find where seed is currently
                int targetIndex = Participants.Count - 1 - seed; // Bottom up
                // Perform switch
                if (currentIndex != targetIndex)
                {
                    (Participants[currentIndex], Participants[targetIndex]) = (Participants[targetIndex], Participants[currentIndex]); // Swap
                }
            }
        }
        public override void PlayTournament(DataContainers _backEndData)
        {
            Console.CursorVisible = true;
            if (MatchHistory == null) // Brand new tournament
            {
                MatchHistory = new List<TournamentMatch>();
                TournamentMatch firstMatch = new TournamentMatch
                {
                    Player1 = Participants[0] // First person is first player
                };
                if (Participants.Count > 1) // One would assume...
                {
                    firstMatch.Player2 = Participants[1]; // Add the 2nd
                }
                else
                {
                    firstMatch.IsBye = true;
                }
                MatchHistory.Add(firstMatch);
            }
            // This is the part that loads the tournament, visually it prints all matches and prompts user one by one
            Console.Clear();
            Console.WriteLine("Insert scores for each match. 0 if you want it randomized, q to stop input temporarily. b FOR ROBOTS");
            int consoleEndPosition = Participants.Count + 1; // Where the cursor will be after data entry (there's n matches total)
            int maxStringLength = 0;
            foreach (string participant in Participants) // Matchups are not known, so just assume max v max is the text
            {
                maxStringLength = Math.Max(maxStringLength, (2 * participant.Length) + 3); // Need to fit "p1 v p1"
            }
            bool finished = false;
            while (!finished) // In this case, it'll be battle by battle, each decided by the winner of previous
            {
                int matchNumber = MatchHistory.Count;
                TournamentMatch match = MatchHistory.Last(); // Gets last (current) match
                Console.SetCursorPosition(0, MatchHistory.Count);
                Console.Write($"{match.Player1} v {match.Player2}");
                Console.SetCursorPosition(maxStringLength, MatchHistory.Count); // Put the cursor on the right, able to fit "p1 v p1" with p1 the longest possible player name
                if (!ResolveMatch(match, _backEndData))
                {
                    return;
                }
                // Check end condition
                if (matchNumber >= (Participants.Count - 1)) // This signifies end of tournament
                {
                    finished = true;
                    Console.SetCursorPosition(0, consoleEndPosition); // Sets the console in the right place
                    Console.WriteLine($"Tournament data entry done");
                }
                else // Next match then
                {
                    TournamentMatch nextMatch = new TournamentMatch();
                    MatchHistory.Add(nextMatch); // Add to pile
                    // Winner will advance in the same pos as they were
                    bool winnerWas1 = (match.Winner == match.Player1);
                    nextMatch.Player1 = winnerWas1 ? match.Winner : Participants[MatchHistory.Count];
                    nextMatch.Player2 = winnerWas1 ? Participants[MatchHistory.Count] : match.Winner;
                }
            }
            Console.CursorVisible = false;
        }
        public override void FinaliseTournament()
        {
            // Find person with the longest name
            int nameLength = 0;
            foreach (string participant in Participants)
            {
                nameLength = Math.Max(nameLength, participant.Length); // Need to fit "p1 v p1" so keep in mind
            }
            // Need to resize console so this fits
            int minXSize = (2 * nameLength) + 3; // Need to fit number of matches
            int minYSize = MatchHistory.Count; // Need to fit names
            while ((Console.WindowHeight < minYSize) || (Console.WindowWidth < minXSize))
            {
                Console.WriteLine($"Console has to have atleast dimensions X:{minXSize} Y: {minYSize}");
                Console.WriteLine($"Current X:{Console.WindowWidth} Y: {Console.WindowHeight}");
                Console.ReadLine();
            }
            // Then, perform a tournament animation
            Console.Clear();
            string emptyName = new string(' ', nameLength);
            string emptyLine = new string(' ', (2 * nameLength) + 3);
            for (int line = 0; line < MatchHistory.Count; line++) // Each match is a line
            {
                TournamentMatch match = MatchHistory[line];
                string player1String = match.Player1.PadRight(nameLength);
                string player2String = match.Player2.PadRight(nameLength);
                string matchString = $"{player1String} v {player2String}";
                string resultString = (match.Winner == match.Player1) ? $"{emptyName} v {player2String}" : $"{player1String} v {emptyName}";
                // Print Line by Line
                for (int blink = 0; blink < NUMBER_OF_BLINKS; blink++) // Blink the next match
                {
                    Console.SetCursorPosition(0, line);
                    Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000));
                    Console.Write(matchString);
                    Console.SetCursorPosition(0, line);
                    Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000));
                    Console.Write(emptyLine);
                }
                Thread.Sleep((int)(BLINK_TOGGLE_PERIOD * 1000)); // Show it one last time, also show score
                Console.SetCursorPosition(0, line);
                Console.Write(matchString);
                Console.SetCursorPosition(matchString.Length + 1, line);
                Console.Write($"({match.Score1}-{match.Score2})");
                Thread.Sleep((int)(2 * BLINK_TOGGLE_PERIOD * 1000)); // Remove the winner, will move down
                Console.SetCursorPosition(0, line);
                Console.Write(emptyLine);
                Console.SetCursorPosition(0, line);
                Console.Write(resultString);
                Thread.Sleep((int)(2 * BLINK_TOGGLE_PERIOD * 1000)); // Remove the winner, will move down
                if (line >= MatchHistory.Count - 1) // The final
                {
                    Console.SetCursorPosition(0, line + 1);
                    string finalString = (match.Winner == match.Player1) ? $"{player1String}   {emptyName}" : $"{emptyName}   {player2String}";
                    Console.Write(finalString);
                    Console.SetCursorPosition(matchString.Length + 1, line + 1);
                    Console.Write($"WINNER");
                }
            }
            Console.ReadKey();
            Console.Clear();
            // And that should be it?!
        }
        public override void UpdateLeaderboard(TournamentHistory leaderboard, DataContainers backend)
        {
            RegisterTournamentParticipation(leaderboard, backend); // First, make sure all players who participated have their tournament # increased
            // Now, need to gather the matches to calculate each match stat
            foreach (TournamentMatch match in MatchHistory)
            {
                ProcessMatchStandings(match, leaderboard, backend);
            }
            // Ok! finally need the winner
            string winnerName = MatchHistory.Last().Winner; // Winner of last match is tournament winner
            SetTournamentWinner(winnerName, leaderboard, backend);
        }
    }
    public class GroupStageTournament : Tournament
    {
        public int NGroups { get; set; }
        public int PlayersPerGroup { get; set; }
        public int NWeeks { get; set; }
        public int MatchesPerWeek { get; set; }
        public int NMathesTotalPerGroup { get; set; }
        public string[,] Groups { get; set; } = null;
        public List<List<List<TournamentMatch>>> MatchHistory { get; set; } = null; // Will go by weeks (list), each week will contain all groups with all matches due that week for that group
        public override void RequestAdditionalInfo()
        {
            // Ask the typical
            AskIfOfficial();
            AskIf2ndPart();
            AskSpecialRulesets();
            // Then group specific
            Console.WriteLine("How many players each group?");
            PlayersPerGroup = int.Parse(Console.ReadLine());
            if ((NPlayers % PlayersPerGroup) != 0)
            {
                throw new Exception("Can't evenly distribute in groups");
            }
            else
            {
                NGroups = NPlayers / PlayersPerGroup;
            }
            Groups = new string[NGroups, PlayersPerGroup]; // Group contains all participants
            // Calculate the rest of things
            MatchesPerWeek = PlayersPerGroup / 2; // Most that can be played is /2 as all players are playing at the same time
            NMathesTotalPerGroup = ((PlayersPerGroup * PlayersPerGroup) - PlayersPerGroup) / 2; // How many matches total will be played in all groups
            NWeeks = NMathesTotalPerGroup / MatchesPerWeek; // Therefore this is the number of weeks needed
        }
        public override void ResetTournament()
        {
            MatchHistory = null;
        }
        public override void ShuffleWithSeeds(List<string> Seeds)
        {
            // Shuffle everything
            Random _rng = new Random();
            Utilities.ShuffleList(Participants, 0, Participants.Count, _rng);
            // Seeding will involve putting the best first
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = Participants.IndexOf(Seeds[seed]); // Find where seed is currently
                // Perform switch
                if (currentIndex != seed)
                {
                    (Participants[currentIndex], Participants[seed]) = (Participants[seed], Participants[currentIndex]); // Swap
                }
            }
            // Another shuffle to shuffle "pots" of players, players will be segregated by skill tiers and distributed
            for (int tier = 0; tier < PlayersPerGroup; tier++)
            {
                int pot = tier * NGroups; // Next pot is the next n players
                Utilities.ShuffleList(Participants, pot, NGroups, _rng);
            }
            // Finally, place in each position of group
            int participant = 0;
            for (int level = 0; level < PlayersPerGroup; level++)
            {
                for (int group = 0; group < NGroups; group++)
                {
                    Groups[group, level] = Participants[participant];
                    participant++;
                }
            }
        }
        public override void PlayTournament(DataContainers backEndData)
        {
            Console.CursorVisible = true;
            if (MatchHistory == null) // Brand new tournament
            {
                MatchHistory = new List<List<List<TournamentMatch>>>();
                List<List<TournamentMatch>> matchesInGroupsThisWeek = new List<List<TournamentMatch>>();
                for (int group = 0; group < NGroups; group++)
                {
                    matchesInGroupsThisWeek.Add(NextRoundRobinMatch(group, 0)); // Add next matches in group here
                }
                MatchHistory.Add(matchesInGroupsThisWeek); // Added week 0
            }
            // This is the part that loads a tournament, visually it prints all matches and prompts user one by one
            bool finished = false;
            while (!finished)
            {
                int week = MatchHistory.Count;
                List<List<TournamentMatch>> currentWeek = MatchHistory.Last(); // Got the current week
                Console.Clear();
                Console.WriteLine("Insert scores for each match. 0 if you want it randomized, q to stop input temporarily. b FOR ROBOTS");
                int consoleEndPosition = (NGroups * MatchesPerWeek) + 1; // Where the cursor will be after data entry
                int maxStringLength = 0;
                foreach (string participant in Participants) // Matchups are not known, so just assume max v max is the text
                {
                    maxStringLength = Math.Max(maxStringLength, (2 * participant.Length) + 3); // Need to fit "p1 v p1"
                }
                int line = 1;
                foreach (List<TournamentMatch> matchesThisGroup in currentWeek)
                {
                    foreach (TournamentMatch match in matchesThisGroup) // Print all rounds first (so that they can be simulated if needed
                    {
                        Console.SetCursorPosition(0, line);
                        if (match.IsBye)
                        {
                            Console.Write($"{match.Player1} gets a bye");
                        }
                        else
                        {
                            Console.Write($"{match.Player1} v {match.Player2}");
                        }
                        Console.SetCursorPosition(maxStringLength + 1, line); // Put the cursor on the right, and starting from 1 (to avoid message string)
                        if (!ResolveMatch(match, backEndData))
                        {
                            return;
                        }
                        line++;
                    }
                }
                // Finally, now here's the next round, unless tournament was finished
                if (week >= NWeeks) // If this was the final week...
                {
                    finished = true;
                    Console.SetCursorPosition(0, consoleEndPosition); // Sets the console in the right place
                    Console.WriteLine($"Tournament data entry done");
                }
                else // Need to add the next week
                {
                    List<List<TournamentMatch>> nextWeek = new List<List<TournamentMatch>>();
                    for (int group = 0; group < NGroups; group++)
                    {
                        nextWeek.Add(NextRoundRobinMatch(group, week)); // Add next matches in group here
                    }
                    MatchHistory.Add(nextWeek); // Added next week
                }
            }
            Console.CursorVisible = false;
        }
        /// <summary>
        /// Will return the next set of matches for a specific week in a specific group
        /// </summary>
        /// <param name="group">Which group</param>
        /// <param name="week">Which week</param>
        /// <returns>List of matches</returns>
        List<TournamentMatch> NextRoundRobinMatch(int group, int week)
        {
            List<TournamentMatch> schedule = new List<TournamentMatch>();
            int rrPlayersPerGroup = ((PlayersPerGroup % 2) == 0) ? PlayersPerGroup : PlayersPerGroup + 1; // Round up to closest even
            int nPairs = rrPlayersPerGroup / 2; // Number of player pairs
            for (int pair = 0; pair < nPairs; pair++)
            {
                // Related indices of players (initial value before rotating), they're mirrored
                int p1Index = pair;
                int p2Index = rrPlayersPerGroup - 1 - pair;
                // Rotate around round robin, except player 0 which will always be 0 (anchor)
                p1Index = (p1Index == 0) ? 0 : ((p1Index - 1 + week) % (rrPlayersPerGroup - 1)) + 1;
                p2Index = ((p2Index - 1 + week) % (rrPlayersPerGroup - 1)) + 1;
                // Finally, create the match if both ends of pair are valid players (i.e. indices valid)
                if (p1Index < PlayersPerGroup && p2Index < PlayersPerGroup)
                {
                    TournamentMatch match = new TournamentMatch()
                    {
                        IsBye = false, // Never a bye, always pairs
                        Player1 = Groups[group, p1Index], // And get players
                        Player2 = Groups[group, p2Index]
                    };
                    schedule.Add(match);
                }
            }
            return schedule;
        }
        public class GroupStanding
        {
            public string Name;
            public int Wins = 0;
            public int Diff = 0;
            public override string ToString()
            {
                return $"{Name}, {Wins}W ({Diff})";
            }
        }
        public override void FinaliseTournament()
        {
            // Find person with the longest name
            int groupTextLength = "group 000".Length; // Minimum size needs to fit header and 999 (!) groups
            int matchUpLength = 0;
            foreach (string participant in Participants)
            {
                matchUpLength = Math.Max(matchUpLength, (2 * (participant.Length)) + " v ".Length);
                groupTextLength = Math.Max(groupTextLength, matchUpLength + " (0-0)".Length); // Need to fit "p1 v p1 (X-X)"
            }
            // Need to resize console so this fits
            int minXSize = NGroups * (groupTextLength + 1); // Fit all groups horizontally with a little space
            int minYSize = (2 * NMathesTotalPerGroup) + 1 + 2 + PlayersPerGroup; // Need to fir all weeks separated by ----- and the header of groups, also list of players at the end
            while ((Console.WindowHeight < minYSize) || (Console.WindowWidth < minXSize))
            {
                Console.WriteLine($"Console has to have atleast dimensions X:{minXSize} Y: {minYSize}");
                Console.WriteLine($"Current X:{Console.WindowWidth} Y: {Console.WindowHeight}");
                Console.ReadLine();
            }
            // Then, perform a tournament animation
            Console.Clear();
            for (int group = 0; group < NGroups; group++) // First print the headers
            {
                Console.SetCursorPosition((groupTextLength * group) + 1, 0);
                Console.Write($"Group {group + 1}");
                Console.SetCursorPosition((groupTextLength * group) + 1, 1);
                Console.Write(new string('-', groupTextLength));
            }
            int cursorY = 2; // Start putting teams here
            // Just print, theres no animation here, won't be showcased as is
            // Also load group data
            List<Dictionary<string, GroupStanding>> groupResults = new List<Dictionary<string, GroupStanding>>(); // All players for all groups
            for (int i = 0; i < NGroups; i++) { groupResults.Add(new Dictionary<string, GroupStanding>()); } // Prepare all groups
            foreach (List<List<TournamentMatch>> matchesThisWeek in MatchHistory) // Plot every week
            {
                for (int group = 0; group < NGroups; group++) // get every group
                {
                    int localY = cursorY;
                    int cursorX = (groupTextLength * group) + 1; // this is where the match will begin
                    List<TournamentMatch> matchesThisGroup = matchesThisWeek[group];
                    foreach (TournamentMatch match in matchesThisGroup)
                    {
                        Console.SetCursorPosition(cursorX, localY);
                        Console.Write($"{match.Player1} v {match.Player2} ({match.Score1}-{match.Score2})");
                        localY++;
                        // Add match stats to relevant group member, create player if not exists
                        if (!groupResults[group].ContainsKey(match.Player1)) groupResults[group].Add(match.Player1, new GroupStanding() { Name = match.Player1 });
                        if (!groupResults[group].ContainsKey(match.Player2)) groupResults[group].Add(match.Player2, new GroupStanding() { Name = match.Player2 });
                        groupResults[group][match.Player1].Diff += match.Score1 - match.Score2;
                        groupResults[group][match.Player2].Diff += match.Score2 - match.Score1;
                        groupResults[group][match.Winner].Wins++;
                    }
                    Console.SetCursorPosition(cursorX, localY);
                    Console.Write(new string('-', groupTextLength));
                }
                cursorY += MatchesPerWeek + 1; // Continue moving
            }
            // Finally print who won each group in order
            for (int group = 0; group < NGroups; group++)
            {
                int cursorX = (groupTextLength * group) + 1; // this is where the match will begin
                List<GroupStanding> sortedStandings = [.. groupResults[group].Values.ToList().OrderByDescending(p => p.Wins).ThenByDescending(p => p.Diff).ThenBy(p => p.Name)];
                int localY = cursorY;
                foreach (GroupStanding standing in sortedStandings)
                {
                    Console.SetCursorPosition(cursorX, localY);
                    Console.Write(standing.ToString());
                    localY++;
                }
            }
            Console.ReadKey();
            Console.Clear();
            // And that should be it?!
        }
        public override void UpdateLeaderboard(TournamentHistory leaderboard, DataContainers backend)
        {
            RegisterTournamentParticipation(leaderboard, backend); // First, make sure all players who participated have their tournament # increased
            // Now, need to gather the matches to calculate each match stat
            foreach (List<List<TournamentMatch>> weekResults in MatchHistory)
            {
                foreach (List<TournamentMatch> groupResults in weekResults)
                {
                    foreach (TournamentMatch match in groupResults)
                    {
                        ProcessMatchStandings(match, leaderboard, backend);
                    }
                }
            }
            // There's no real winner for this, so it ends here
        }
    }
}
