using MechanicsData;

namespace GameData
{
    public class Trainer
    {
        public const int MAX_MONS_IN_TEAM = 12; /// How many mons top can the team have (rest goes to box)
        public string Name { get; set; } = "";
        public string DungeonIdentifier { get; set; } = "?";
        public string Avatar { get; set; } = "";
        public bool AutoTeam { get; set; } = true;
        public bool AutoFavour { get; set; } = true;
        public bool AutoSetItem { get; set; } = true;
        public bool AutoModItem { get; set; } = true;
        public bool AutoBattleItem { get; set; } = true;
        public List<PokemonSet> Pokemons { get; set; } = new List<PokemonSet>();
        public Dictionary<string, int> SetItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<ModItem, int> ModItems { get; set; } = new Dictionary<ModItem, int>();
        public Dictionary<BattleItem, int> BattleItems { get; set; } = new Dictionary<BattleItem, int>();
        public Dictionary<string, int> TrainerFavours { get; set; } = new Dictionary<string, int>();
        // Boxes, IMP, balls, etc not added until needed
        public override string ToString()
        {
            return Name;
        }
    }
}
