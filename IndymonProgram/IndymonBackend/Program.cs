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
                    default:
                        break;
                }
                Console.WriteLine("");
            } while (InputString.ToLower() != "q");
            /*
            using HttpClient client = new HttpClient();
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "1015902951";
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={tab}";
            string csv = client.GetStringAsync(url).GetAwaiter().GetResult();*/
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
    }
}
