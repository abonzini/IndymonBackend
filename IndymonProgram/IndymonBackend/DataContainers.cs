using ParsersAndData;

namespace IndymonBackend
{
    public class DataContainers
    {
        public string MasterDirectory = "";
        public Dictionary<string, Pokemon> LocalPokemonSettings { get; set; } = null;
        public Dictionary<string, Dictionary<string, float>> TypeChart { get; set; } = null;
        public Dictionary<string, Move> MoveData { get; set; } = null;
        public Dictionary<string, HashSet<string>> OffensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> DefensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> NatureItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> EvItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> TeraItemData { get; set; } = null;
        public Dictionary<string, TrainerData> TrainerData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NpcData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NamedNpcData { get; set; } = new Dictionary<string, TrainerData>();
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
