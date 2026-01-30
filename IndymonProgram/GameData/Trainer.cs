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
        public List<TrainerPokemon> PartyPokemon { get; set; } = new List<TrainerPokemon>();
        public List<TrainerPokemon> BoxedPokemon { get; set; } = new List<TrainerPokemon>();
        public Dictionary<string, int> SetItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<Item, int> ModItems { get; set; } = new Dictionary<Item, int>();
        public Dictionary<Item, int> BattleItems { get; set; } = new Dictionary<Item, int>();
        public Dictionary<string, int> TrainerFavours { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> KeyItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PokeBalls { get; set; } = new Dictionary<string, int>();
        public override string ToString()
        {
            return Name;
        }
        // Things related to an assembled team ready for battle
        public List<TrainerPokemon> BattleTeam = new List<TrainerPokemon>(); // A subset of team but can also have borrowed mons ready to battle
    }
}
