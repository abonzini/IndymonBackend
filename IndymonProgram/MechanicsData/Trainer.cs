namespace MechanicsData
{
    public class Trainer
    {
        public const int MAX_NUMBER_POKEMON = 10;
        public const int MAX_NUMBER_BAG = 19; // Any bag/box/ has 20 items but the first is the label
        public string Name = "";
        public string PictureUrl = "";
        public string DiscordId = "";
        public int Imp = 0;
        public bool AutoTeam = false;
        public bool AutoMoveDisk = false;
        public bool AutoHeldItem = false;
        public bool AutoFavour = false;
        public bool AutoGummy = false;
        public Jewelry EquippedJewelry = null;
        public int EquippedJewelryUses = 0;
        public List<PokemonEntity> TeamPokemon = new List<PokemonEntity>();
        public Dictionary<Gummy, int> Gummies = new Dictionary<Gummy, int>();
        public Dictionary<MoveDisk, int> MoveDisks = new Dictionary<MoveDisk, int>();
        public Dictionary<HeldItem, int> HeldItems = new Dictionary<HeldItem, int>();
        public Dictionary<Mint, int> Mints = new Dictionary<Mint, int>();
        public Dictionary<EvoPlate, int> EvoPlates = new Dictionary<EvoPlate, int>();
        public Dictionary<Essence, int> Essences = new Dictionary<Essence, int>();
        public Dictionary<KeyItem, int> KeyItems = new Dictionary<KeyItem, int>();
        public Dictionary<PokeBall, int> PokeBalls = new Dictionary<PokeBall, int>();
        public List<Sandwich> Sandwiches = new List<Sandwich>();
        public Dictionary<NpcTrainer, int> Favours = new Dictionary<NpcTrainer, int>();
        public HashSet<string> BoxedMons = new HashSet<string>(); // Boxed mons are just the id as the boxed mon data is global for all trainers
        public override string ToString()
        {
            return Name;
        }
    }
}
