using ParsersAndData;
using System.Text.Json;

namespace IndymonBackend
{
    public class IndymonData
    {
        public DataContainers DataContainer { get; set; }
        public TournamentManager TournamentManager { get; set; }
        public TournamentHistory TournamentHistory { get; set; }
    }
    public static class Program
    {
        static IndymonData _allData = new IndymonData
        {
            DataContainer = new DataContainers(),
            TournamentManager = null
        };
        static void Main(string[] args)
        {
            string FILE_NAME = "indy.mon";
            string TOURN_CSV = "tournament_stats.csv";
            Console.WriteLine("Indymon manager program");
            Console.CursorVisible = false;
            if (args.Length == 0) // File not included, need to ask for it
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: Starting indymon from scratch. If attempting to load an existing session, make sure to open this program with the file path as parameter.");
                Console.ResetColor();
            }
            else
            {
                string indymonFile = args[0];
                _allData = JsonSerializer.Deserialize<IndymonData>(File.ReadAllText(indymonFile));
                _allData.DataContainer.MasterDirectory = Path.GetDirectoryName(indymonFile);
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
                            Console.WriteLine("Ordering Tournament history and exporting csv");
                            string csvFile = Path.Combine(_allData.DataContainer.MasterDirectory, TOURN_CSV);
                            File.WriteAllText(csvFile, FormatTournamentHistory());
                            Console.WriteLine("Serializing json");
                            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                            string indymonFile = Path.Combine(_allData.DataContainer.MasterDirectory, FILE_NAME);
                            File.WriteAllText(indymonFile, JsonSerializer.Serialize(_allData, options));
                        }
                        break;
                    case "1":
                        LoadEssentialData();
                        break;
                    case "2":
                        LoadTrainerData();
                        LoadNpcData();
                        LoadNamedNpcData();
                        LoadTournamentHistory();
                        break;
                    case "3":
                        _allData.TournamentManager = new TournamentManager(_allData.DataContainer);
                        _allData.TournamentManager.GenerateNewTournament();
                        break;
                    case "4":
                        _allData.TournamentManager.UpdateTournamentTeams();
                        break;
                    case "5":
                        _allData.TournamentManager.ExecuteTournament();
                        break;
                    case "6":
                        _allData.TournamentManager.FinaliseTournament();
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
            if (_allData.DataContainer.Dex == null) Console.WriteLine("WARNING: Pokemon data not initialised yet");
            if (_allData.DataContainer.TypeChart == null) Console.WriteLine("WARNING: Type chart not initialised yet");
            if (_allData.DataContainer.MoveData == null) Console.WriteLine("WARNING: Move data not initialised yet");
            if (_allData.DataContainer.OffensiveItemData == null) Console.WriteLine("WARNING: Offensive item data not initialised yet");
            if (_allData.DataContainer.DefensiveItemData == null) Console.WriteLine("WARNING: Defensive item data not initialised yet");
            if (_allData.DataContainer.TeraItemData == null) Console.WriteLine("WARNING: Tera item data not initialised yet");
            if (_allData.DataContainer.EvItemData == null) Console.WriteLine("WARNING: Ev item data not initialised yet");
            if (_allData.DataContainer.NatureItemData == null) Console.WriteLine("WARNING: Nature item data not initialised yet");
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
                "4 - Update tournament participant's team sheets\n" +
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
                _allData = JsonSerializer.Deserialize<IndymonData>(File.ReadAllText(masterPath));
                _allData.DataContainer.MasterDirectory = masterPath;
                if (_allData.TournamentManager != null)
                {
                    _allData.TournamentManager.SetBackEndData(_allData.DataContainer);
                }
            }
            else
            {
                Console.WriteLine("No indymon file. Will jsut try to import backend data");
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
                            // Finally clean moves themselves
                            moveData = Cleanups.MoveListCleanup(moveData);
                            Console.WriteLine("Loaded dex and moves correctly");
                            _allData.DataContainer.Dex = monData;
                            _allData.DataContainer.MoveData = moveData;
                        }
                    }
                }
                if (File.Exists(typeChartFile))
                {
                    _allData.DataContainer.TypeChart = TypeChartParser.ParseTypechartFile(typeChartFile);
                }
                if (File.Exists(defItemFile))
                {
                    _allData.DataContainer.DefensiveItemData = ItemParser.ParseItemAndEffects(defItemFile);
                }
                if (File.Exists(offItemFile))
                {
                    _allData.DataContainer.OffensiveItemData = ItemParser.ParseItemAndEffects(offItemFile);
                }
                if (File.Exists(teraItemFile))
                {
                    _allData.DataContainer.TeraItemData = ItemParser.ParseItemAndEffects(teraItemFile);
                }
                if (File.Exists(evItemFile))
                {
                    _allData.DataContainer.EvItemData = ItemParser.ParseItemAndEffects(evItemFile);
                }
                if (File.Exists(natureItemFile))
                {
                    _allData.DataContainer.NatureItemData = ItemParser.ParseItemAndEffects(natureItemFile);
                }
            }
            _allData.DataContainer.MasterDirectory = directory;
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadTrainerData()
        {
            Console.WriteLine("Loading data from trainers");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "1015902951";
            Dictionary<string, TrainerData> data = _allData.DataContainer.TrainerData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadNpcData()
        {
            Console.WriteLine("Loading data from NPCs");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "364323808";
            Dictionary<string, TrainerData> data = _allData.DataContainer.NpcData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads playable trainer data from google doc
        /// </summary>
        private static void LoadNamedNpcData()
        {
            Console.WriteLine("Loading data from Named NPCs");
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "43578104";
            Dictionary<string, TrainerData> data = _allData.DataContainer.NamedNpcData;
            LoadTeamData(data, sheetId, tab);
        }
        /// <summary>
        /// Loads the team data from entities
        /// </summary>
        /// <param name="trainerData">Container where this'll be stored</param>
        /// <param name="sheetId">Id of google doc</param>
        /// <param name="tab">Id of google doc tab (page)</param>
        private static void LoadTeamData(Dictionary<string, TrainerData> trainerData, string sheetId, string tab)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={tab}";
            using HttpClient client = new HttpClient();
            string csv = client.GetStringAsync(url).GetAwaiter().GetResult();
            // Trainer card format
            const int xSize = 10, ySize = 18; // Dimensions of the trainer cards in row-col
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
                    // Row 0, contains picture, Name, auto-flags
                    csvFields = rows[offsetY + 0].Split(",");
                    string trainerName = csvFields[offsetX + 2].Trim().ToLower();
                    if (trainerName == "") continue;
                    TrainerData newTrainer = new TrainerData();
                    newTrainer.Name = trainerName;
                    newTrainer.Avatar = csvFields[offsetX + 0].Trim().ToLower();
                    newTrainer.AutoItem = (csvFields[offsetX + 7].Trim().ToLower() == "true");
                    newTrainer.AutoTeam = (csvFields[offsetX + 9].Trim().ToLower() == "true");
                    // Then, rows 2-13 contain the mons
                    for (int mon = 0; mon < 6; mon++)
                    {
                        // First, get mon name and if shiny, data will come later
                        csvFields = rows[offsetY + 3 + (2 * mon)].Split(",");
                        string monName = csvFields[offsetX + 2].Trim().ToLower();
                        if (monName == "") break; // If no mon, then I'm done doing mons then
                        PokemonSet newMon = new PokemonSet();
                        newMon.Species = monName;
                        newMon.Shiny = (csvFields[offsetX + 0].Trim().ToLower() == "true");
                        // Then, check the other row for the rest
                        csvFields = rows[offsetY + 2 + (2 * mon)].Split(",");
                        newMon.Gender = csvFields[offsetX + 0].Trim().ToLower();
                        newMon.NickName = csvFields[offsetX + 2].Trim().ToLower();
                        if (!newTrainer.AutoTeam) // Check if moves and ability are actually relevant
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
                            if (newTrainer.AutoTeam || newTrainer.AutoItem) // In this case, goes to bag
                            {
                                newTrainer.BattleItems.Add(newItem);
                            }
                            else
                            {
                                newMon.Item = newItem;
                            }
                        }
                        newTrainer.Teamsheet.Add(newMon);
                    }
                    // Then, row 14 contains the 1-use consumable items
                    csvFields = rows[offsetY + 14].Split(",");
                    for (int item = 0; item < 8; item++)
                    {
                        // First field is the trainer data label, not an actual item
                        string nextItem = csvFields[offsetX + 1 + item].Trim().ToLower();
                        if (nextItem == "") break; // End if no more items
                        // Otherwise add to bag
                        newTrainer.BattleItems.Add(new Item() { Name = nextItem, Uses = 1 }); // Always 1 use these ones
                    }
                    // Then, row 15 contains the n-use consumable items
                    csvFields = rows[offsetY + 15].Split(",");
                    for (int item = 0; item < 4; item++)
                    {
                        // First field is the trainer data label, not an actual item
                        string nextItem = csvFields[offsetX + 1 + (2 * item)].Trim().ToLower();
                        if (nextItem == "") break; // End if no more items
                        // Otherwise add to bag
                        int usesNumber = int.Parse(csvFields[offsetX + 2 + (2 * item)]);
                        newTrainer.BattleItems.Add(new Item() { Name = nextItem, Uses = usesNumber }); // Always 1 use these ones
                    }
                    trainerData.Add(newTrainer.Name, newTrainer);
                }
            }
            // That should be it...
        }
        /// <summary>
        /// Fetches current tournament history from google sheets and stores into struct
        /// </summary>
        private static void LoadTournamentHistory()
        {
            string sheetId = "1-9T2xh10RirzTbSarbESU3rAJ2uweKoRoIyNlg0l31A";
            string tab = "282272919";
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={tab}";
            using HttpClient client = new HttpClient();
            string csv = client.GetStringAsync(url).GetAwaiter().GetResult();
            // Obtained tournament data csv
            // Assumes the row order is same as column order!
            string[] rows = csv.Split("\n");
            Console.WriteLine("Loading tournament history");
            _allData.TournamentHistory = new TournamentHistory();
            // First pass is to obtain list of players in the order given no matter what
            for (int row = 2; row < rows.Length; row++)
            {
                string[] cols = rows[row].Split(',');
                string playerName = cols[4].Trim().ToLower(); // Contains player name
                PlayerAndStats nextPlayer = new PlayerAndStats();
                nextPlayer.Name = playerName;
                // Statistics...
                nextPlayer.TournamentWins = int.Parse(cols[5]);
                nextPlayer.TournamentsPlayed = int.Parse(cols[6]);
                nextPlayer.Kills = int.Parse(cols[8]);
                nextPlayer.Deaths = int.Parse(cols[9]);
                // Finally add to the right place
                if (_allData.DataContainer.TrainerData.ContainsKey(playerName)) // This was an actual player, add to correct array
                {
                    _allData.TournamentHistory.PlayerStats.Add(nextPlayer);
                }
                else if (_allData.DataContainer.NpcData.ContainsKey(playerName)) // Otherwise it's NPC data
                {
                    _allData.TournamentHistory.NpcStats.Add(nextPlayer);
                }
                else
                {
                    throw new Exception("Found a non-npc and non-player in tournament data!");
                }
            }
            // Once the players are in the correct order, we begin the parsing
            for (int row = 0; row < _allData.TournamentHistory.PlayerStats.Count; row++) // Next part is to examine each PLAYER CHARACTER ONLY FOR STATS
            {
                int yOffset = 2; // 2nd row begins
                string[] cols = rows[yOffset + row].Split(',');
                int xOffset = 11; // Beginning of "vs trainer" data
                PlayerAndStats thisPlayer = _allData.TournamentHistory.PlayerStats[row]; // Get player owner of this data
                thisPlayer.EachMuWr = new Dictionary<string, IndividualMu>();
                for (int col = 0; col < _allData.TournamentHistory.PlayerStats.Count; col++) // Check all players score first
                {
                    if (row == col) continue; // No MU agains oneself
                    // Get data for this opp
                    string oppName = _allData.TournamentHistory.PlayerStats[col].Name;
                    int wins = int.Parse(cols[xOffset + (3 * col)]); // Data has 3 columns per player
                    int losses = int.Parse(cols[xOffset + (3 * col) + 1]);
                    thisPlayer.EachMuWr.Add(oppName, new IndividualMu { Wins = wins, Losses = losses }); // Add this data to the stats
                }
                xOffset = 11 + (3 * _allData.TournamentHistory.PlayerStats.Count); // Offset to NPC data
                for (int col = 0; col < _allData.TournamentHistory.NpcStats.Count; col++) // Check NPC score now
                {
                    // Get data for this opp
                    string oppName = _allData.TournamentHistory.NpcStats[col].Name;
                    int wins = int.Parse(cols[xOffset + (3 * col)]);
                    int losses = int.Parse(cols[xOffset + (3 * col) + 1]);
                    thisPlayer.EachMuWr.Add(oppName, new IndividualMu { Wins = wins, Losses = losses });
                }
            }
            // And thats it, tourn data has been found
        }
        /// <summary>
        /// Reorders tournament history to sort it by TW and then WR and then DIFF
        /// </summary>
        private static string FormatTournamentHistory()
        {
            // Firstly, just sort the lists
            _allData.TournamentHistory.PlayerStats = _allData.TournamentHistory.PlayerStats.OrderByDescending(c => c.TournamentWins).ThenBy(c => c.Winrate).ThenBy(c => c.Diff).ToList();
            _allData.TournamentHistory.NpcStats = _allData.TournamentHistory.NpcStats.OrderByDescending(c => c.TournamentWins).ThenBy(c => c.Winrate).ThenBy(c => c.Diff).ToList();
            // Ok now I need to do multiple row and column csv:
            int nRows = 2 + _allData.TournamentHistory.PlayerStats.Count + _allData.TournamentHistory.NpcStats.Count; // this is how many rows It'll have (label + players)
            int nColumns = 7 + 3 * (_allData.TournamentHistory.PlayerStats.Count + _allData.TournamentHistory.NpcStats.Count); // Cols, will be the fixed + 3 per participant
            string[] lines = new string[nRows];
            // First row has names only
            string[] firstLine = new string[nColumns];
            firstLine[0] = "Individual Match History ->";
            int xOffset = 7; // First part starts from offset (players)
            for (int player = 0; player < _allData.TournamentHistory.PlayerStats.Count; player++)
            {
                firstLine[xOffset + (3 * player)] = $"vs {_allData.TournamentHistory.PlayerStats[player].Name}";
            }
            // Then NPCs
            xOffset = 7 + (3 * _allData.TournamentHistory.PlayerStats.Count);
            for (int player = 0; player < _allData.TournamentHistory.NpcStats.Count; player++)
            {
                firstLine[xOffset + 3 * player] = $"vs {_allData.TournamentHistory.NpcStats[player].Name}";
            }
            lines[0] = string.Join(",", firstLine);
            // Second row is no real content, just repeated
            string[] secondLine = new string[nColumns];
            secondLine[0] = "Trainer";
            secondLine[1] = "Tourn. Wins";
            secondLine[2] = "Tourn. Played";
            secondLine[3] = "WR%";
            secondLine[4] = "K";
            secondLine[5] = "D";
            secondLine[6] = "DIFF";
            // Then all together
            xOffset = 7;
            for (int player = 0; player < (_allData.TournamentHistory.PlayerStats.Count + _allData.TournamentHistory.NpcStats.Count); player++)
            {
                secondLine[xOffset + (3 * player)] = "W";
                secondLine[xOffset + (3 * player) + 1] = "L";
                secondLine[xOffset + (3 * player) + 2] = "%";
            }
            lines[1] = string.Join(",", secondLine);
            // Ok finally need to do each player's
            int yOffset = 2;
            for (int player = 0; player < (_allData.TournamentHistory.PlayerStats.Count); player++)
            {
                string[] nextLine = new string[nColumns];
                PlayerAndStats nextPlayer = _allData.TournamentHistory.PlayerStats[player];
                nextLine[0] = nextPlayer.Name;
                nextLine[1] = nextPlayer.TournamentWins.ToString();
                nextLine[2] = nextPlayer.TournamentsPlayed.ToString();
                nextLine[3] = nextPlayer.Winrate.ToString();
                nextLine[4] = nextPlayer.Kills.ToString();
                nextLine[5] = nextPlayer.Deaths.ToString();
                nextLine[6] = nextPlayer.Diff.ToString();
                xOffset = 7;
                for (int opp = 0; opp < (_allData.TournamentHistory.PlayerStats.Count); opp++)
                {
                    if (opp == player) continue; // Inexistant MU
                    IndividualMu mu = nextPlayer.EachMuWr[_allData.TournamentHistory.PlayerStats[opp].Name];
                    nextLine[xOffset + (3 * opp)] = mu.Wins.ToString();
                    nextLine[xOffset + (3 * opp) + 1] = mu.Losses.ToString();
                    nextLine[xOffset + (3 * opp) + 2] = mu.Winrate.ToString();
                }
                xOffset = 7 + (3 * _allData.TournamentHistory.PlayerStats.Count);
                for (int opp = 0; opp < (_allData.TournamentHistory.NpcStats.Count); opp++)
                {
                    IndividualMu mu = nextPlayer.EachMuWr[_allData.TournamentHistory.NpcStats[opp].Name];
                    nextLine[xOffset + (3 * opp)] = mu.Wins.ToString();
                    nextLine[xOffset + (3 * opp) + 1] = mu.Losses.ToString();
                    nextLine[xOffset + (3 * opp) + 2] = mu.Winrate.ToString();
                }
                lines[yOffset + player] = string.Join(",", nextLine);
            }
            // And NPCs
            yOffset = 2 + _allData.TournamentHistory.PlayerStats.Count;
            for (int player = 0; player < (_allData.TournamentHistory.NpcStats.Count); player++)
            {
                string[] nextLine = new string[nColumns];
                PlayerAndStats nextPlayer = _allData.TournamentHistory.NpcStats[player];
                nextLine[0] = nextPlayer.Name;
                nextLine[1] = nextPlayer.TournamentWins.ToString();
                nextLine[2] = nextPlayer.TournamentsPlayed.ToString();
                nextLine[3] = nextPlayer.Winrate.ToString();
                nextLine[4] = nextPlayer.Kills.ToString();
                nextLine[5] = nextPlayer.Deaths.ToString();
                nextLine[6] = nextPlayer.Diff.ToString();
                lines[yOffset + player] = string.Join(",", nextLine);
            }
            // ok finally make the master string
            return string.Join("\n", lines);
        }
    }
}
