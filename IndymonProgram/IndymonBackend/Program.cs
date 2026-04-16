using GameData;
using GameDataContainer;
using MechanicsData;
using MechanicsDataContainer;
using Newtonsoft.Json;
using Utilities;

namespace IndymonBackendProgram
{
    public static class Program
    {
        static void Main()
        {
            // Things
            TournamentManager tournamentManager = new TournamentManager();
            ExplorationManager explorationManager = new ExplorationManager();
            // Parsing
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Indymon manager program ☺");
            Console.CursorVisible = false;
            Console.WriteLine($"Folder where data is located?");
            string directoryPath = Console.ReadLine();
            // Begin with the mechanics back end
            string MECHANICS_DATA_FILE = "mechanics_data.txt";
            MechanicsDataContainers.GlobalMechanicsData.InitializeData(Path.Combine(directoryPath, MECHANICS_DATA_FILE));
            // Then the trainer data back end
            string DUNGEON_DATA_DIR = "dungeons";
            GameDataContainers.GlobalGameData.InitializeDungeonData(Path.Combine(directoryPath, DUNGEON_DATA_DIR));
            // Then the trainer data back end
            string GAME_DATA_FILE = "game_data.txt";
            GameDataContainers.GlobalGameData.InitializeTrainerData(Path.Combine(directoryPath, GAME_DATA_FILE));
            // Finally, proper data of possible ongoing sims
            string TOURNAMENT_JSON_FILE = "current_tournament.json";
            string EXPLORATION_JSON_FILE = "current_exploration.json";
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            if (Path.Exists(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE)))
            {
                tournamentManager = JsonConvert.DeserializeObject<TournamentManager>(File.ReadAllText(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE)), jsonSettings);
            }
            if (Path.Exists(Path.Combine(directoryPath, EXPLORATION_JSON_FILE)))
            {
                explorationManager = JsonConvert.DeserializeObject<ExplorationManager>(File.ReadAllText(Path.Combine(directoryPath, EXPLORATION_JSON_FILE)), jsonSettings);
            }
            // Beginning of indymon program
            string InputString;
            do
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                MainMenuInstructions();
                InputString = Console.ReadLine();
                switch (InputString)
                {
                    case "0":
                        Console.WriteLine("Serializing jsons");
                        File.WriteAllText(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE), JsonConvert.SerializeObject(tournamentManager, jsonSettings));
                        File.WriteAllText(Path.Combine(directoryPath, EXPLORATION_JSON_FILE), JsonConvert.SerializeObject(explorationManager, jsonSettings));
                        Console.WriteLine("Writing tournament stats");
                        GameDataContainers.GlobalGameData.SaveBattleStats(directoryPath, "tourn_stats.csv");
                        break;
                    case "1":
                        tournamentManager = new TournamentManager
                        {
                            DirectoryPath = directoryPath
                        };
                        tournamentManager.GenerateNewTournament();
                        break;
                    case "2":
                        tournamentManager.UpdateTournamentTeams();
                        tournamentManager.ExecuteTournament();
                        break;
                    case "3":
                        tournamentManager.UpdateTournamentTeams(true); // May need to redo team seeding if file was loaded (auto tho)
                        tournamentManager.FinaliseTournament();
                        Console.WriteLine("Writing tournament stats");
                        GameDataContainers.GlobalGameData.SaveBattleStats(directoryPath, "tourn_stats.csv");
                        break;
                    case "4":
                        foreach (Trainer trainer in GameDataContainers.GlobalGameData.TrainerData.Values)
                        {
                            // Will quickly export all trainers csvs, useful for cleanup functions
                            string trainerFilePath = Path.Combine(directoryPath, $"{trainer.Name.ToUpper().Replace(" ", "").Replace("?", "")}.trainer");
                            trainer.SaveTrainerCsv(trainerFilePath);
                        }
                        break;
                    case "5":
                        explorationManager = new ExplorationManager
                        {
                            DirectoryPath = directoryPath
                        };
                        explorationManager.InitializeExploration();
                        break;
                    case "6":
                        explorationManager.ExecuteExploration();
                        break;
                    case "7":
                        explorationManager.AnimateExploration();
                        break;
                    case "8":
                        {
                            Console.WriteLine($"Drawing random favour, please select the category (ortherwise random): [{string.Join(',', [.. Enum.GetValues(typeof(TrainerRank))])}]");
                            List<string> trainerBag = [];
                            if (Enum.TryParse(Console.ReadLine().ToUpper(), out TrainerRank rank))
                            {
                                trainerBag = [.. MechanicsDataContainers.GlobalMechanicsData.TrainerLookup.Where(i => i.Value == rank).Select(i => i.Key)]; // Get keys of trainers filter'd by rank
                            }
                            else
                            {
                                trainerBag = [.. MechanicsDataContainers.GlobalMechanicsData.TrainerLookup.Select(i => i.Key)]; // Use the whole pool
                            }
                            Console.WriteLine("How many pulls?");
                            int pulls = int.Parse(Console.ReadLine());
                            GeneralUtilities.ShuffleList(trainerBag);
                            List<string> obtainedTrainers = trainerBag[0..pulls];
                            Console.WriteLine(string.Join(", ", obtainedTrainers));
                        }
                        break;
                    case "9":
                        {
                            Console.WriteLine("Which trainer to pull from?");
                            string trainerName = Console.ReadLine();
                            if (GameDataContainers.GlobalGameData.FamousNpcData.TryGetValue(trainerName, out Trainer trainer)) { }
                            else if (GameDataContainers.GlobalGameData.NpcData.TryGetValue(trainerName, out trainer)) { }
                            else throw new Exception("Trainer didn't exist");
                            TrainerPokemon chosenMon = GeneralUtilities.GetRandomPick(trainer.PartyPokemon);
                            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[chosenMon.Species];
                            while (monData.Prevo != null) // Go deep into beginning of line
                            {
                                monData = monData.Prevo;
                            }
                            Console.WriteLine($"Obtained {monData.Name}");
                        }
                        break;
                    case "10":
                        {
                            ExplorationPrizes newPrizes = new ExplorationPrizes();
                            Console.WriteLine($"Which trainer? [{string.Join(", ", [.. GameDataContainers.GlobalGameData.TrainerData.Values.Where(t => t.Favours.Count > 0).Select(t => t.Name)])}");
                            string option = Console.ReadLine();
                            Trainer trainer = GameDataContainers.GlobalGameData.TrainerData[option];
                            Console.WriteLine($"And which favour? [{string.Join(", ", [.. trainer.Favours.Keys.Select(t => t.Name)])}]");
                            option = Console.ReadLine();
                            Trainer favourTrainer = trainer.Favours.Where(t => t.Key.Name == option).First().Key;
                            Console.WriteLine($"Which dungeon? {string.Join(", ", [.. GameDataContainers.GlobalGameData.Dungeons.Keys])}");
                            option = Console.ReadLine();
                            Dungeon theDungeon = GameDataContainers.GlobalGameData.Dungeons[option];
                            int nCommons = 0, nRares = 0;
                            List<int> rewardOptions = []; // 0,1,2 depending on what the dungeon can give you, disk plate or IMP
                            if (theDungeon.Events.Any(e => e.EventType == RoomEventType.PARADOX)) rewardOptions.Add(0);
                            if (theDungeon.Events.Any(e => e.EventType == RoomEventType.RESEARCHER)) rewardOptions.Add(1);
                            if (theDungeon.Events.Any(e => e.EventType == RoomEventType.IMP_GAIN)) rewardOptions.Add(2);
                            if (theDungeon.Events.Any(e => e.EventType == RoomEventType.APRICORN)) rewardOptions.Add(3);
                            int diskPlateOrImp = GeneralUtilities.GetRandomPick(rewardOptions);
                            switch (favourTrainer.TrainerRank)
                            {
                                case TrainerRank.GYM:
                                    nCommons = 2;
                                    nRares = 1;
                                    break;
                                case TrainerRank.ELITE4:
                                    nCommons = 4;
                                    nRares = 1;
                                    break;
                                case TrainerRank.CHAMPION:
                                    nCommons = 4;
                                    nRares = 2;
                                    break;
                                case TrainerRank.UNRANKED:
                                default:
                                    nCommons = 2;
                                    nRares = 0;
                                    break;
                            }
                            Console.WriteLine($"<@{trainer.DiscordNumber}> sent their friend {favourTrainer.Name} to explore the {theDungeon.Name} (Used a favour)");
                            for (int i = 0; i < nCommons; i++)
                            {
                                ItemReward item = GeneralUtilities.GetRandomPick(theDungeon.CommonItems);
                                int count = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1);
                                newPrizes.AddReward(item.Name, count);
                                Console.WriteLine($"Obtained {item.Name} x{count}");
                            }
                            switch (diskPlateOrImp)
                            {
                                case 0: // Disk
                                    string chosenMove = GeneralUtilities.GetRandomPick(MechanicsDataContainers.GlobalMechanicsData.Moves.Keys.ToList());
                                    string diskName = $"{chosenMove} {SetItem.ADVANCED_DISK}"; // Create the advanced disk
                                    newPrizes.AddReward(diskName, 1);
                                    Console.WriteLine($"Obtained {diskName}");
                                    break;
                                case 1: // Plate
                                    List<string> platesList = [.. MechanicsDataContainers.GlobalMechanicsData.BattleItems.Keys.Where(i => i.ToLower().Contains("plate"))];
                                    string chosenPlate = GeneralUtilities.GetRandomPick(platesList);
                                    newPrizes.AddReward(chosenPlate, 1);
                                    Console.WriteLine($"Obtained {chosenPlate}");
                                    break;
                                case 2: // IMP
                                    int impGain = GeneralUtilities.GetRandomNumber(2, 4); // 2-3 IMP
                                    newPrizes.AddImp(impGain);
                                    Console.WriteLine($"Obtained {impGain} IMP");
                                    break;
                                case 3: // Apricorn
                                    List<string> commonApricorns = ["Red", "Yellow", "Blue", "Green"];
                                    List<string> rareApricorns = ["Black", "White"];
                                    List<string> obtainedApricorns = [];
                                    // 5 commons and 2 rares
                                    for (int i = 0; i < 3; i++)
                                    {
                                        obtainedApricorns.Add(GeneralUtilities.GetRandomPick(commonApricorns));
                                    }
                                    for (int i = 0; i < 1; i++)
                                    {
                                        obtainedApricorns.Add(GeneralUtilities.GetRandomPick(rareApricorns));
                                    }
                                    foreach (string apricorn in obtainedApricorns)
                                    {
                                        newPrizes.AddReward(apricorn + " Apricorn", 1); // Add them
                                    }
                                    Console.WriteLine($"Obtained apricorns ({string.Join(", ", obtainedApricorns)})");
                                    break;
                                default:
                                    throw new Exception("Unreachabale code");
                            }
                            for (int i = 0; i < nRares; i++)
                            {
                                ItemReward item = GeneralUtilities.GetRandomPick(theDungeon.RareItems);
                                int count = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1);
                                newPrizes.AddReward(item.Name, count);
                                Console.WriteLine($"Obtained {item.Name} x{count}");
                            }
                            GeneralUtilities.AddtemToCountDictionary(trainer.Favours, favourTrainer, -1, true);
                            newPrizes.TransferToTrainer(trainer);
                            IndymonUtilities.WarnTrainer(trainer);//Warn trainer of the exceeded items
                            string trainerFilePath = Path.Combine(directoryPath, $"{trainer.Name.ToUpper().Replace(" ", "").Replace("?", "")}.trainer");
                            trainer.SaveTrainerCsv(trainerFilePath);
                        }
                        break;
                    case "11":
                        {
                            Console.WriteLine($"Which dungeons? {string.Join(", ", [.. GameDataContainers.GlobalGameData.Dungeons.Keys])}");
                            string input = Console.ReadLine();
                            List<string> dungeons = [.. input.Split(',')];
                            Console.WriteLine("How many mons?");
                            int count = int.Parse(Console.ReadLine());
                            List<string> foundMons = [];
                            foreach (string nextDungeon in dungeons)
                            {
                                Dungeon theDungeon = GameDataContainers.GlobalGameData.Dungeons[nextDungeon.Trim()];
                                List<string> possibleMons = theDungeon.PokemonEachFloor[0];
                                GeneralUtilities.ShuffleList(possibleMons);
                                foundMons.AddRange(possibleMons[0..count]);
                            }
                            Console.WriteLine(string.Join(", ", foundMons));
                        }
                        break;
                    case "12":
                        explorationManager = new ExplorationManager
                        {
                            DirectoryPath = directoryPath
                        };
                        explorationManager.TestDungeonImage();
                        break;
                    default:
                        break;
                }
                Console.WriteLine("");
            } while (InputString.ToLower() != "q");
            Console.WriteLine("Session finished. Have a good day and don't forget to update spreadsheet!");
        }
        /// <summary>
        /// Prints main menu instructions
        /// </summary>
        static void MainMenuInstructions()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("0 - Save to indy.mon\n" +
                "1 - Generate a new tournament\n" +
                "2 - Update tournament participant's team sheets and input tournament data\n" +
                "3 - Finalize tournament. Animation + export new tournament data\n" +
                "4 - Export all players csv data\n" +
                "5 - Generate exploration, choose place, player, etc\n" +
                "6 - Simulate current exploration\n" +
                "7 - Animate resolved exploration\n" +
                "8 - Draw from Favour Gacha\n" +
                "9 - Random 'Baby' Pokemon from trainer (Favor resolution)\n" +
                "10 - Random exploration rewards (tiered favor resolutions)\n" +
                "11 - Random exploration mons (for beginning of trainer's adventures)\n" +
                "12 - Test Dungeon Drawing"
            );
        }
    }
}
