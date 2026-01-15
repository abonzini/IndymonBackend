using MechanicsData;

namespace GameData
{
    public class Trainer
    {
        public string Name { get; set; } = "";
        public string DungeonIdentifier { get; set; } = "?";
        public string Avatar { get; set; } = "";
        public bool AutoTeam { get; set; } = true;
        public bool AutoSetItem { get; set; } = true;
        public bool AutoModItem { get; set; } = true;
        public bool AutoBattleItem { get; set; } = true;
        public List<PokemonSet> Pokemons { get; set; } = new List<PokemonSet>();
        public List<string> SetItems { get; set; } = new List<string>();
        public List<ModItem> ModItems { get; set; } = new List<ModItem>();
        public List<BattleItem> BattleItems { get; set; } = new List<BattleItem>();
        public List<string> TrainerFavors { get; set; } = new List<string>();
        // Boxes, IMP, balls, etc not added until needed
    }
}
