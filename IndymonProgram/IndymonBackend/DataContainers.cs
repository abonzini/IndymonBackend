using ParsersAndData;

namespace IndymonBackend
{
    public class DataContainers
    {
        public string MasterDirectory = "";
        public Dictionary<string, Pokemon> LocalPokemonSettings { get; set; } = null;
        public Dictionary<string, Dictionary<string, float>> TypeChart { get; set; } = null;
        public Dictionary<string, Move> MoveData { get; set; } = null;
        public Dictionary<string, string> OffensiveItemData { get; set; } = null;
        public Dictionary<string, string> DefensiveItemData { get; set; } = null;
        public Dictionary<string, string> NatureItemData { get; set; } = null;
        public Dictionary<string, string> EvItemData { get; set; } = null;
        public Dictionary<string, string> TeraItemData { get; set; } = null;
        public List<TrainerData> TrainerData { get; set; } = new List<TrainerData>();
        public List<TrainerData> NpcData { get; set; } = new List<TrainerData>();
        public List<TrainerData> NamedTrainerData { get; set; } = new List<TrainerData>();
    }
    public class Item
    {
        public string Name { get; set; }
        public int Uses { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public class TrainersPokemon
    {
        public string Name { get; set; }
        public bool Shiny { get; set; }
        public string Ability { get; set; }
        public string[] Moves { get; set; } = new string[4];
        public Item Item { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
    public class TrainerData
    {
        public string Name { get; set; }
        public bool AutoItem { get; set; }
        public bool AutoTeam { get; set; }
        public List<Item> BattleItems { get; set; } = new List<Item>();
        public List<TrainersPokemon> TrainersPokemon { get; set; } = new List<TrainersPokemon>(6);
        public override string ToString()
        {
            return Name;
        }
    }
}
