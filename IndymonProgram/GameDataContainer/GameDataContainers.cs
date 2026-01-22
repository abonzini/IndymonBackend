using GameData;
using Newtonsoft.Json;
using ParsersAndData;

namespace GameDataContainer
{
    public partial class GameDataContainers
    {
        public static GameDataContainers GlobalGameData { get; set; } = new GameDataContainers();
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
            string trainerDataTab = lines[1].Split(",")[0];
            ParseTrainerCards(sheetId, trainerDataTab, TrainerData);
            string npcDataTab = lines[2].Split(",")[0];
            ParseTrainerCards(sheetId, npcDataTab, NpcData);
            string famousNpcDataTab = lines[3].Split(",")[0];
            ParseTrainerCards(sheetId, famousNpcDataTab, FamousNpcData);
        }
        public Dictionary<string, Dungeon> Dungeons { get; set; } = new Dictionary<string, Dungeon>();
        public Dictionary<string, Trainer> TrainerData { get; set; } = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> NpcData { get; set; } = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> FamousNpcData { get; set; } = new Dictionary<string, Trainer>();
    }
}
