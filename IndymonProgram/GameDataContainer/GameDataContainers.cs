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
        }
        public Trainer GetTrainer(string Name)
        {
            if (FamousNpcData.TryGetValue(Name, out Trainer foundTrainer)) return foundTrainer;
            else if (NpcData.TryGetValue(Name, out foundTrainer)) return foundTrainer;
            else if (TrainerData.TryGetValue(Name, out foundTrainer)) return foundTrainer;
            else throw new Exception($"Trainer {Name} does not exist!");
        }
        public Dictionary<string, Dungeon> Dungeons { get; set; } = new Dictionary<string, Dungeon>();
        public Dictionary<string, Trainer> TrainerData { get; set; } = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> NpcData { get; set; } = new Dictionary<string, Trainer>();
        public Dictionary<string, Trainer> FamousNpcData { get; set; } = new Dictionary<string, Trainer>();
    }
}
