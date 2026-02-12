using GameDataContainer;
using MechanicsDataContainer;
using Newtonsoft.Json;

namespace IndymonBackendProgram
{
    public static class Program
    {
        static void Main()
        {
            // Things
            TournamentManager tournamentManager = new TournamentManager();
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
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            if (Path.Exists(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE)))
            {
                tournamentManager = JsonConvert.DeserializeObject<TournamentManager>(File.ReadAllText(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE)));
            }
            // Beginning of indymon program
            string InputString;
            do
            {
                Console.ForegroundColor = ConsoleColor.White;
                MainMenuInstructions();
                InputString = Console.ReadLine();
                switch (InputString)
                {
                    case "0":
                        {
                            GameDataContainers.GlobalGameData.SaveBattleStats(directoryPath, "tourn_stats.csv");
                            Console.WriteLine("Serializing jsons");
                            File.WriteAllText(Path.Combine(directoryPath, TOURNAMENT_JSON_FILE), JsonConvert.SerializeObject(tournamentManager, jsonSettings));
                        }
                        break;
                    case "1":
                        tournamentManager = new TournamentManager();
                        tournamentManager.GenerateNewTournament();
                        break;
                    case "2":
                        tournamentManager.UpdateTournamentTeams();
                        break;
                    case "3":
                        tournamentManager.ExecuteTournament();
                        break;
                    case "4":
                        tournamentManager.FinaliseTournament();
                        break;
                    case "5":
                        //_allData.ExplorationManager = new ExplorationManager(_allData.DataContainer);
                        //_allData.ExplorationManager.InitializeExploration();
                        break;
                    case "6":
                        //_allData.ExplorationManager.ExecuteExploration();
                        break;
                    case "7":
                        //_allData.ExplorationManager.AnimateExploration();
                        break;
                    case "8":
                        //if (_allData.ExplorationManager.NextDungeon != "")
                        //{
                        //    _allData.ExplorationManager.InitializeNextDungeon();
                        //}
                        //else
                        //{
                        //    Console.ForegroundColor = ConsoleColor.Red;
                        //    Console.WriteLine("ERROR. Can't do next dungeon because there isn't any!");
                        //    Console.ForegroundColor = ConsoleColor.White;
                        //}
                        break;
                    case "9":
                        //{
                        //    TrainerData chosenTrainer = Utilities.ChooseOneTrainerDialog(TeambuildSettings.NONE, _allData.DataContainer);
                        //    string obtainedMon = chosenTrainer.Teamsheet[RandomNumberGenerator.GetInt32(chosenTrainer.Teamsheet.Count)].Species;
                        //    Console.WriteLine($"Obtained child version of {obtainedMon}");
                        //}
                        break;
                    case "10":
                        //{
                        //    List<string> options = [.. _allData.DataContainer.Dungeons.Keys];
                        //    Console.WriteLine("Which dungeon?");
                        //    for (int i = 0; i < options.Count; i++)
                        //    {
                        //        Console.Write($"{i + 1}: {options[i]}, ");
                        //    }
                        //    Console.WriteLine("");
                        //    string dungeon = options[int.Parse(Console.ReadLine()) - 1];
                        //    Dungeon theDungeon = _allData.DataContainer.Dungeons[dungeon];
                        //    Console.WriteLine("Which type of reward?\n" +
                        //        "\t1 - Trainer: 2 commons, 1 disk/plate\n" +
                        //        "\t2 - Gym Leader: 2 commons, 1 disk/plate, 1 rare\n" +
                        //        "\t3 - Elite 4: 4 commons, 1 disk/plate, 1 rare\n" +
                        //        "\t4 - Champion: 4 commons, 1 disk/plate, 2 rares");
                        //    string choice = Console.ReadLine();
                        //    int nCommons = 0, nRares = 0;
                        //    bool isDisk = (RandomNumberGenerator.GetInt32(100000) % 2 == 0); // Random even/odd
                        //    switch (choice)
                        //    {
                        //        case "1":
                        //            nCommons = 2;
                        //            nRares = 0;
                        //            break;
                        //        case "2":
                        //            nCommons = 2;
                        //            nRares = 1;
                        //            break;
                        //        case "3":
                        //            nCommons = 4;
                        //            nRares = 1;
                        //            break;
                        //        case "4":
                        //            nCommons = 4;
                        //            nRares = 2;
                        //            break;
                        //        default:
                        //            break;
                        //    }
                        //    List<string> commons = new List<string>();
                        //    for (int i = 0; i < nCommons; i++)
                        //    {
                        //        string item = theDungeon.CommonItems[RandomNumberGenerator.GetInt32(theDungeon.CommonItems.Count)];
                        //        commons.Add(item);
                        //    }
                        //    if (isDisk)
                        //    {
                        //        string obtainedDisk = _allData.DataContainer.MoveItemData.Keys.ToList()[RandomNumberGenerator.GetInt32(_allData.DataContainer.MoveItemData.Count)]; // Get random move disk
                        //        commons.Add(obtainedDisk);
                        //    }
                        //    else
                        //    {
                        //        List<string> platesList = [.. _allData.DataContainer.OffensiveItemData.Keys.Where(i => i.Contains("plate"))];
                        //        string chosenPlate = platesList[RandomNumberGenerator.GetInt32(platesList.Count)];
                        //        commons.Add(chosenPlate);
                        //    }
                        //    List<string> rares = new List<string>();
                        //    for (int i = 0; i < nRares; i++)
                        //    {
                        //        string item = theDungeon.RareItems[RandomNumberGenerator.GetInt32(theDungeon.RareItems.Count)];
                        //        rares.Add(item);
                        //    }
                        //    Console.WriteLine($"Commmon items: {string.Join(",", commons)}");
                        //    Console.WriteLine($"Rare items: {string.Join(",", rares)}");
                        //}
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
                "2 - Update tournament participant's team sheets\n" +
                "3 - Input tournament data\n" +
                "4 - Finalize tournament. Animation + export new tournament data\n" +
                "5 - Generate exploration, choose place, player, etc\n" +
                "6 - Simulate current exploration\n" +
                "7 - Animate resolved exploration\n" +
                "8 - Resolve finished exploration (if needed, e.g. move to next dungeon)\n" +
                "9 - Random Pokemon from trainer (Favor resolution)\n" +
                "10 - Random exploration rewards (tiered favor resolutions)\n"
            );
        }
    }
}
