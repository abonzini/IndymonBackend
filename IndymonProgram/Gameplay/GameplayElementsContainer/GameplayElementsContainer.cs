using Gameplay.GameplayElements;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        public static GameplayElementsContainer GlobalData { get; set; } = new GameplayElementsContainer();
        /// <summary>
        /// Initializes data
        /// </summary>
        /// <param name="masterDirectory">Master directory, contains the mechanicsFilePath as well as hardcoded data as JSON</param>
        /// <param name="mechanicsFilePath">Name of file with google spreadsheet</param>
        public void InitializeData(string masterDirectory, string mechanicsFilePath)
        {
            string[] lines = File.ReadAllLines(Path.Combine(masterDirectory, mechanicsFilePath));
            string sheetId = lines[0].Split(",")[0]; // Obtained google sheets
            // Find all code-defined objects
            LoadMoves();
            LoadAbilities();
            LoadJewelry();
            LoadHeldItems();
            LoadNatures();
            LoadPokeBalls();
            LoadEvoPlates();
            LoadGummies();
            LoadKeyItems();
            LoadEssences();
            LoadMints();
            // Then, load the data from the google sheets
            string typechartTab = lines[1].Split(",")[0];
            ParseTypeChart(sheetId, typechartTab);
            string pokedexTab = lines[2].Split(",")[0];
            string learnsetsTab = lines[3].Split(",")[0];
            ParsePokemonData(sheetId, pokedexTab, learnsetsTab);
            string unownTab = lines[4].Split(",")[0];
            ParseUnownLookup(sheetId, unownTab);
            string favourGachaTab = lines[5].Split(",")[0];
            ParseNpcTrainerList(sheetId, favourGachaTab);
            // After items, can do trainer and Pokemon
            string npcArea = lines[6].Split(",")[0];
            FillNpcData(sheetId, npcArea);
            npcArea = lines[7].Split(",")[0]; // More npc data, split in the sheet to differentiate between famous and non
            FillNpcData(sheetId, npcArea);
            string boxMonArea = lines[8].Split(",")[0];
            FillBoxedMonData(sheetId, boxMonArea);
            string playerArea = lines[9].Split(",")[0];
            FillPlayers(sheetId, playerArea);
        }
        public Random CommonRng = new Random(Guid.NewGuid().GetHashCode());
        public Dictionary<PokemonType, Dictionary<PokemonType, double>> DefensiveTypeChart = new Dictionary<PokemonType, Dictionary<PokemonType, double>>();
        public Dictionary<string, Move> Moves = new Dictionary<string, Move>();
        public Dictionary<string, Ability> Abilities = new Dictionary<string, Ability>();
        public Dictionary<string, PokemonSpecies> Dex = new Dictionary<string, PokemonSpecies>();
        public Dictionary<string, string> UnownLookup = new Dictionary<string, string>();
        public Dictionary<string, NpcTrainer> AllNpcTrainers = new Dictionary<string, NpcTrainer>();
        public Dictionary<string, EvoPlate> EvoPlates = new Dictionary<string, EvoPlate>();
        public Dictionary<string, KeyItem> KeyItems = new Dictionary<string, KeyItem>();
        public Dictionary<string, Jewelry> Jewelries = new Dictionary<string, Jewelry>();
        public Dictionary<string, Gummy> Gummies = new Dictionary<string, Gummy>();
        public Dictionary<string, HeldItem> HeldItems = new Dictionary<string, HeldItem>();
        public Dictionary<string, Nature> Natures = new Dictionary<string, Nature>();
        public Dictionary<string, Mint> Mints = new Dictionary<string, Mint>();
        public Dictionary<string, Essence> Essences = new Dictionary<string, Essence>();
        public Dictionary<string, PokeBall> PokeBalls = new Dictionary<string, PokeBall>();
        public Dictionary<string, Sandwich> SandwichLookup = new Dictionary<string, Sandwich>();
        public Dictionary<string, MoveDisk> MoveDiskLookup = new Dictionary<string, MoveDisk>();
        public Dictionary<string, PokemonEntity> BoxedMons = new Dictionary<string, PokemonEntity>();
        public Dictionary<string, TrainerEntity> Trainers = new Dictionary<string, TrainerEntity>();
    }
}