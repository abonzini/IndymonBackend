using GameData;
using Newtonsoft.Json;
using ParsersAndData;

namespace GameDataContainer
{
    public partial class GameDataContainers
    {
        public static GameDataContainers GlobalGameData { get; set; } = new GameDataContainers();
        public Dictionary<string, Dungeon> Dungeons = new Dictionary<string, Dungeon>();
        public Dictionary<string, Trainer> TrainerData = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> NpcData = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> FamousNpcData = new Dictionary<string, Trainer>();
        public Dictionary<string, SetItem> SetItems = new Dictionary<string, SetItem>();
        public BattleStats BattleStats = new BattleStats();
        /// <summary>
        /// <summary>
        /// Initializes dungeon data
        /// </summary>
        /// <param name="directoryPath">Directory where dungeons are, may later be migrated to fetch from sheet too</param>
        public void InitializeDungeonData(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) throw new Exception($"Directory {directoryPath} does not exist");
            Console.WriteLine("Loading dungeon data");
            foreach (string file in Directory.EnumerateFiles(directoryPath))
            {
                Dungeon nextDungeon = JsonConvert.DeserializeObject<Dungeon>(File.ReadAllText(file));
                Dungeons.Add(nextDungeon.Name, nextDungeon);
            }
        }
        /// <summary>
        /// Initializes trainer datadata
        /// </summary>
        /// <param name="filePath">Path with links to google sheets for where to find the stuff</param>
        public void InitializeTrainerData(string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception($"Path {filePath} does not exist");
            string[] lines = File.ReadAllLines(filePath);
            string sheetId = lines[0].Split(",")[0];
            // In inverse order so that trainers can find favors
            Console.WriteLine("Parsing Famous Trainer Cards");
            string famousNpcDataTab = lines[3].Split(",")[0];
            ParseTrainerCards(sheetId, famousNpcDataTab, FamousNpcData);
            Console.WriteLine("Parsing NPC Cards");
            string npcDataTab = lines[2].Split(",")[0];
            ParseTrainerCards(sheetId, npcDataTab, NpcData);
            Console.WriteLine("Parsing Trainer Cards");
            string trainerDataTab = lines[1].Split(",")[0];
            ParseTrainerCards(sheetId, trainerDataTab, TrainerData);
            Console.WriteLine("Parsing Battle Stats");
            string battleStatsTab = lines[3].Split(",")[0];
            ParseBattleStats(sheetId, battleStatsTab);
        }
        public Trainer GetTrainer(string Name)
        {
            if (FamousNpcData.TryGetValue(Name, out Trainer foundTrainer)) return foundTrainer;
            else if (NpcData.TryGetValue(Name, out foundTrainer)) return foundTrainer;
            else if (TrainerData.TryGetValue(Name, out foundTrainer)) return foundTrainer;
            else throw new Exception($"Trainer {Name} does not exist!");
        }
        /// Exports the battle statistics into a file
        /// </summary>
        /// <param name="directory">Directory where to store file</param>
        /// <param name="filename">Filename</param>
        public void SaveBattleStats(string directory, string filename)
        {
            Console.WriteLine("Ordering Tournament history and exporting csv");
            // Firstly, just sort the lists
            BattleStats.PlayerStats = [.. BattleStats.PlayerStats.OrderByDescending(c => c.TournamentWins).ThenByDescending(c => c.GameWinrate).ThenByDescending(c => c.Diff)];
            BattleStats.NpcStats = [.. BattleStats.NpcStats.OrderByDescending(c => c.TournamentWins).ThenByDescending(c => c.GameWinrate).ThenByDescending(c => c.Diff)];
            // Ok now I need to do multiple row and column csv:
            int nRows = 2 + BattleStats.PlayerStats.Count + BattleStats.NpcStats.Count; // this is how many rows It'll have (label + players)
            int nColumns = 10 + 3 * (BattleStats.PlayerStats.Count + BattleStats.NpcStats.Count); // Cols, will be the fixed + 3 per participant
            string[] lines = new string[nRows];
            // First row has names only
            string[] firstLine = new string[nColumns];
            firstLine[0] = "Individual Match History ->";
            int xOffset = 10; // First part starts from offset (players)
            for (int player = 0; player < BattleStats.PlayerStats.Count; player++)
            {
                firstLine[xOffset + (3 * player)] = $"vs {BattleStats.PlayerStats[player].Name}";
            }
            // Then NPCs
            xOffset = 10 + (3 * BattleStats.PlayerStats.Count);
            for (int player = 0; player < BattleStats.NpcStats.Count; player++)
            {
                firstLine[xOffset + 3 * player] = $"vs {BattleStats.NpcStats[player].Name}";
            }
            lines[0] = string.Join(",", firstLine);
            // Second row is no real content, just repeated
            string[] secondLine = new string[nColumns];
            secondLine[0] = "Trainer";
            secondLine[1] = "Tourn. Wins";
            secondLine[2] = "Tourn. Played";
            secondLine[3] = "(WR%)";
            secondLine[4] = "Games Won";
            secondLine[5] = "Games Played";
            secondLine[6] = "(WR%)";
            secondLine[7] = "K";
            secondLine[8] = "D";
            secondLine[9] = "DIFF";
            // Then all together
            xOffset = 10;
            for (int player = 0; player < (BattleStats.PlayerStats.Count + BattleStats.NpcStats.Count); player++)
            {
                secondLine[xOffset + (3 * player)] = "W";
                secondLine[xOffset + (3 * player) + 1] = "L";
                secondLine[xOffset + (3 * player) + 2] = "%";
            }
            lines[1] = string.Join(",", secondLine);
            // Ok finally need to do each player's
            int yOffset = 2;
            for (int player = 0; player < (BattleStats.PlayerStats.Count); player++)
            {
                string[] nextLine = new string[nColumns];
                PlayerAndStats nextPlayer = BattleStats.PlayerStats[player];
                nextLine[0] = nextPlayer.Name;
                nextLine[1] = nextPlayer.TournamentWins.ToString();
                nextLine[2] = nextPlayer.TournamentsPlayed.ToString();
                nextLine[3] = nextPlayer.Winrate.ToString();
                nextLine[4] = nextPlayer.GamesWon.ToString();
                nextLine[5] = nextPlayer.GamesPlayed.ToString();
                nextLine[6] = nextPlayer.GameWinrate.ToString();
                nextLine[7] = nextPlayer.Kills.ToString();
                nextLine[8] = nextPlayer.Deaths.ToString();
                nextLine[9] = nextPlayer.Diff.ToString();
                xOffset = 10;
                for (int opp = 0; opp < (BattleStats.PlayerStats.Count); opp++)
                {
                    if (opp == player) continue; // Inexistant MU (vs themselves?)
                    string oppName = BattleStats.PlayerStats[opp].Name;
                    if (!nextPlayer.EachMuWr.TryGetValue(oppName, out IndividualMu mu)) // Also inexistant (not played ever, empty one then)
                    {
                        mu = new IndividualMu()
                        {
                            Losses = 0,
                            Wins = 0
                        };
                    }
                    nextLine[xOffset + (3 * opp)] = mu.Wins.ToString();
                    nextLine[xOffset + (3 * opp) + 1] = mu.Losses.ToString();
                    nextLine[xOffset + (3 * opp) + 2] = mu.Winrate.ToString();
                }
                xOffset = 10 + (3 * BattleStats.PlayerStats.Count);
                for (int opp = 0; opp < (BattleStats.NpcStats.Count); opp++)
                {
                    string oppName = BattleStats.NpcStats[opp].Name;
                    if (!nextPlayer.EachMuWr.TryGetValue(oppName, out IndividualMu mu)) // Also inexistant (not played ever, empty one then)
                    {
                        mu = new IndividualMu()
                        {
                            Losses = 0,
                            Wins = 0
                        };
                    }
                    nextLine[xOffset + (3 * opp)] = mu.Wins.ToString();
                    nextLine[xOffset + (3 * opp) + 1] = mu.Losses.ToString();
                    nextLine[xOffset + (3 * opp) + 2] = mu.Winrate.ToString();
                }
                lines[yOffset + player] = string.Join(",", nextLine);
            }
            // And NPCs
            yOffset = 2 + BattleStats.PlayerStats.Count;
            for (int player = 0; player < (BattleStats.NpcStats.Count); player++)
            {
                string[] nextLine = new string[nColumns];
                PlayerAndStats nextPlayer = BattleStats.NpcStats[player];
                nextLine[0] = nextPlayer.Name;
                nextLine[1] = nextPlayer.TournamentWins.ToString();
                nextLine[2] = nextPlayer.TournamentsPlayed.ToString();
                nextLine[3] = nextPlayer.Winrate.ToString();
                nextLine[4] = nextPlayer.GamesWon.ToString();
                nextLine[5] = nextPlayer.GamesPlayed.ToString();
                nextLine[6] = nextPlayer.GameWinrate.ToString();
                nextLine[7] = nextPlayer.Kills.ToString();
                nextLine[8] = nextPlayer.Deaths.ToString();
                nextLine[9] = nextPlayer.Diff.ToString();
                lines[yOffset + player] = string.Join(",", nextLine);
            }
            // Ok finally save file
            string csvFile = Path.Combine(directory, filename);
            File.WriteAllText(csvFile, string.Join("\n", lines)); // Saves the new csv
        }
    }
}
