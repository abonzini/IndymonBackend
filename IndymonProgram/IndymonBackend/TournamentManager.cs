using System.Text;

namespace IndymonBackend
{
    public class TournamentManager
    {
        TournamentData _ongoingTournament;
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
                        _ongoingTournament = new TournamentData();
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
    }
    public class TournamentData
    {
        public int NPlayers = 0;
        public int NMons = 3;
        public List<TrainerData> Participants = new List<TrainerData>();
    }
}
