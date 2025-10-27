using ParsersAndData;
using System.Text.Json;

namespace IndymonBackend
{
    internal class Program
    {
        static DataContainers dataContainers = new DataContainers();
        static void Main(string[] args)
        {
            string FILE_NAME = "indy.mon";
            Console.WriteLine("Indymon manager program");
            if (args.Length == 0) // File not included, need to ask for it
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: Starting indymon from scratch. If attempting to load an existing session, make sure to open this program with the file path as parameter.");
                Console.ResetColor();
            }
            else
            {
                string indymonFile = args[0];
                dataContainers = JsonSerializer.Deserialize<DataContainers>(File.ReadAllText(indymonFile));
                dataContainers.MasterDirectory = Path.GetDirectoryName(indymonFile);
            }
            string InputString;
            do
            {
                PrintWarnings();
                MainMenuInstructions();
                InputString = Console.ReadLine();
                switch (InputString)
                {
                    case "0":
                        {
                            Console.WriteLine("Serializing json");
                            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                            string indymonFile = Path.Combine(dataContainers.MasterDirectory, FILE_NAME);
                            File.WriteAllText(indymonFile, JsonSerializer.Serialize(dataContainers, options));
                        }
                        break;
                    case "1":
                        LoadEssentialData();
                        break;
                    case "2":
                        LoadTrainerData();
                        LoadNpcData();
                        LoadNamedNpcData();
                        // TODO LoadTournamentHistory();
                        break;
                    default:
                        break;
                }
                Console.WriteLine("");
            } while (InputString.ToLower() != "q");
            Console.WriteLine("Session finished. Have a good day and don't forget to update spreadsheet!");
        }
        /// <summary>
        /// Prints warnings if missing essential data needed
        /// </summary>
        static void PrintWarnings()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (dataContainers.LocalPokemonSettings == null) Console.WriteLine("WARNING: Pokemon data not initialised yet");
            if (dataContainers.TypeChart == null) Console.WriteLine("WARNING: Type chart not initialised yet");
            if (dataContainers.MoveData == null) Console.WriteLine("WARNING: Move data not initialised yet");
            if (dataContainers.OffensiveItemData == null) Console.WriteLine("WARNING: Offensive item data not initialised yet");
            if (dataContainers.DefensiveItemData == null) Console.WriteLine("WARNING: Defensive item data not initialised yet");
            if (dataContainers.TeraItemData == null) Console.WriteLine("WARNING: Tera item data not initialised yet");
            if (dataContainers.EvItemData == null) Console.WriteLine("WARNING: Ev item data not initialised yet");
            if (dataContainers.NatureItemData == null) Console.WriteLine("WARNING: Nature item data not initialised yet");
            Console.ResetColor();
        }
        /// <summary>
        /// Prints main menu instructions
        /// </summary>
        static void MainMenuInstructions()
        {
            Console.WriteLine("0 - Save to indy.mon\n" +
                "1 - Load mechanics data from folder\n" +
                "2 - Fetch trainer data and tournament history from online sheet\n" +
                "3 - Generate a new tournament\n" +
                "4 - Update torunament participant's team sheets\n" +
                "5 - Input tournament data\n" +
                "6 - Finalize tournament. Animation + export new tournament data\n" +
                "7 - Generate exploration results\n"
                );
        }
        /// <summary>
        /// Loads the essential data (dex, etc) for running indymon. Asks user for the location
        /// </summary>
        static void LoadEssentialData()
        {
            Console.WriteLine("Input the folder where indy.mon is located, or atleast the other files");
            string directory = Console.ReadLine();
            string masterPath = Path.Combine(directory, "indy.mon");
            if (File.Exists(masterPath))
            {
                Console.WriteLine("Indymon file located, retrieving");
                dataContainers = JsonSerializer.Deserialize<DataContainers>(File.ReadAllText(masterPath));
                dataContainers.MasterDirectory = masterPath;
            }
            else
            {
                Console.WriteLine("No indymon file. Attempting to create one with the basic data");
                string learnsetPath = Path.Combine(directory, "learnsets.ts");
                string dexPath = Path.Combine(directory, "pokedex.ts");
                string movesPath = Path.Combine(directory, "moves.csv");
                string typeChartFile = Path.Combine(directory, "typechart.ts");
                string defItemFile = Path.Combine(directory, "defensiveitems.csv");
                string offItemFile = Path.Combine(directory, "offensiveitems.csv");
                string teraItemFile = Path.Combine(directory, "teraitems.csv");
                string evItemFile = Path.Combine(directory, "evitems.csv");
                string natureItemFile = Path.Combine(directory, "natureitems.csv");
                if (File.Exists(dexPath))
                {
                    // First, retrieve all mons
                    Dictionary<string, Pokemon> monData = DexParser.ParseDexFile(dexPath);
                    // Then, get their movesets
                    if (File.Exists(learnsetPath))
                    {
                        MovesetParser.ParseMovests(learnsetPath, monData);
                        // Then, use the proper name lookup and make evos/forms inherit movesets
                        monData = Cleanups.NameAndMovesetCleanup(monData);
                        if (File.Exists(movesPath))
                        {
                            // Finally, parse move data
                            Dictionary<string, Move> moveData = MoveParser.ParseMoves(movesPath);
                            // And clean up names in mons, obtain STAB
                            Cleanups.MoveDataCleanup(monData, moveData);
                            Console.WriteLine("Loaded dex and moves correctly");
                            dataContainers.LocalPokemonSettings = monData;
                            dataContainers.MoveData = moveData;
                        }
                    }
                }
                if (File.Exists(typeChartFile))
                {
                    dataContainers.TypeChart = TypeChartParser.ParseTypechartFile(typeChartFile);
                }
                if (File.Exists(defItemFile))
                {
                    dataContainers.DefensiveItemData = ItemParser.ParseItemAndEffect(defItemFile);
                }
                if (File.Exists(offItemFile))
                {
                    dataContainers.OffensiveItemData = ItemParser.ParseItemAndEffect(offItemFile);
                }
                if (File.Exists(teraItemFile))
                {
                    dataContainers.TeraItemData = ItemParser.ParseItemAndEffect(teraItemFile);
                }
                if (File.Exists(evItemFile))
                {
                    dataContainers.EvItemData = ItemParser.ParseItemAndEffect(evItemFile);
                }
                if (File.Exists(natureItemFile))
                {
                    dataContainers.NatureItemData = ItemParser.ParseItemAndEffect(natureItemFile);
                }
            }
            dataContainers.MasterDirectory = directory;
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadTrainerData()
        {
            Console.WriteLine("Loading data from trainers");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "1015902951";
            List<TrainerData> data = dataContainers.TrainerData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadNpcData()
        {
            Console.WriteLine("Loading data from NPCs");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "1499993838";
            List<TrainerData> data = dataContainers.NpcData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadNamedNpcData()
        {
            Console.WriteLine("Loading data from Named NPCs");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "224914063";
            List<TrainerData> data = dataContainers.NamedTrainerData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads the team data from entities
        /// </summary>
        /// <param name="trainerData">Container where this'll be stored</param>
        /// <param name="sheetId">Id of google doc</param>
        /// <param name="tab">Id of google doc tab (page)</param>
        private static void LoadTeamData(List<TrainerData> trainerData, string sheetId, string tab)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={tab}";
            using HttpClient client = new HttpClient();
            string csv = client.GetStringAsync(url).GetAwaiter().GetResult();
            // Trainer card format
            const int xSize = 10, ySize = 12; // Dimensions of the trainer cards in indices
            int verticalAmount, horizontalAmount;
            string[] rows = csv.Split("\n");
            // Verify vertical cards
            if ((rows.Length % ySize) != 0) throw new Exception("Y dimension of trainer cards doesnt fit");
            else verticalAmount = rows.Length / ySize;
            // Verify horizontal cards
            string[] csvFields = rows[0].Split(",");
            if ((csvFields.Length % xSize) != 0) throw new Exception("C dimension of trainer cards doesnt fit");
            else horizontalAmount = csvFields.Length / xSize;
            // Ok now ready to load card data, one by one
            trainerData.Clear(); // Empty list, will load from scratch
            for (int cardY = 0; cardY < verticalAmount; cardY++)
            {
                int offsetY = cardY * ySize;
                for (int cardX = 0; cardX < horizontalAmount; cardX++)
                {
                    int offsetX = cardX * xSize;
                    // Now parse all
                    // Row 0, contains Name, auto-flags
                    csvFields = rows[offsetY + 0].Split(",");
                    string trainerName = csvFields[offsetX + 2].Trim().ToLower();
                    if (trainerName == "") continue;
                    TrainerData newtrainer = new TrainerData();
                    newtrainer.Name = csvFields[offsetX + 2].Trim().ToLower();
                    newtrainer.AutoItem = (csvFields[offsetX + 7].Trim().ToLower() == "true");
                    newtrainer.AutoTeam = (csvFields[offsetX + 9].Trim().ToLower() == "true");
                    // Then, rows 2-7 contain the mons
                    for (int mon = 0; mon < 6; mon++)
                    {
                        csvFields = rows[offsetY + 2 + mon].Split(",");
                        string monName = csvFields[offsetX + 2].Trim().ToLower();
                        if (monName == "") break; // If no mon, then I'm done doing mons then
                        TrainersPokemon newMon = new TrainersPokemon();
                        newMon.Name = monName;
                        newMon.Shiny = (csvFields[offsetX + 0].Trim().ToLower() == "true");
                        if (!newtrainer.AutoTeam) // Check if moves and ability are actually relevant
                        {
                            newMon.Ability = csvFields[offsetX + 3].Trim().ToLower();
                            for (int move = 0; move < 4; move++)
                            {
                                newMon.Moves[move] = csvFields[offsetX + 4 + move].Trim().ToLower();
                            }
                        }
                        // Finally, item, check if need to place on mon or back into bag
                        string itemName = csvFields[offsetX + 8].Trim().ToLower();
                        if (itemName != "") // There's an item, then
                        {
                            int usesNumber = int.Parse(csvFields[offsetX + 9]);
                            Item newItem = new Item() { Name = itemName, Uses = usesNumber };
                            if (newtrainer.AutoTeam || newtrainer.AutoItem) // In this case, goes to bag
                            {
                                newtrainer.BattleItems.Add(newItem);
                            }
                            else
                            {
                                newMon.Item = newItem;
                            }
                        }
                        newtrainer.TrainersPokemon.Add(newMon);
                    }
                    // Then, row 8 contains the 1-use consumable items
                    csvFields = rows[offsetY + 8].Split(",");
                    for (int item = 0; item < 8; item++)
                    {
                        // First field is the trainer data label, not an actual item
                        string nextItem = csvFields[offsetX + 1 + item].Trim().ToLower();
                        if (nextItem == "") break; // End if no more items
                        // Otherwise add to bag
                        newtrainer.BattleItems.Add(new Item() { Name = nextItem, Uses = 1 }); // Always 1 use these ones
                    }
                    // Then, row 9 contains the n-use consumable items
                    csvFields = rows[offsetY + 9].Split(",");
                    for (int item = 0; item < 4; item++)
                    {
                        // First field is the trainer data label, not an actual item
                        string nextItem = csvFields[offsetX + 1 + (2 * item)].Trim().ToLower();
                        if (nextItem == "") break; // End if no more items
                        // Otherwise add to bag
                        int usesNumber = int.Parse(csvFields[offsetX + 2 + (2 * item)]);
                        newtrainer.BattleItems.Add(new Item() { Name = nextItem, Uses = usesNumber }); // Always 1 use these ones
                    }
                    trainerData.Add(newtrainer);
                }
            }
            // That should be it...
        }
    }
}
