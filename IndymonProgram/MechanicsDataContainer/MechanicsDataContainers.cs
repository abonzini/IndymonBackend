using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        public static MechanicsDataContainers GlobalMechanicsData { get; set; } = new MechanicsDataContainers();
        /// <summary>
        /// Initializes data
        /// </summary>
        /// <param name="masterDirectory">Master directory, contains the mechanicsFilePath as well as hardcoded data as JSON</param>
        /// <param name="mechanicsFilePath">Name of file with google spreadsheet</param>
        public void InitializeData(string masterDirectory, string mechanicsFilePath)
        {
            string[] lines = File.ReadAllLines(Path.Combine(masterDirectory, mechanicsFilePath));
            string sheetId = lines[0].Split(",")[0]; // Obtained google sheets
            // Firstly, find Moves, Abilities, and all other core gameplay elements that are found in json, important as the game won't contain all, so only the ones in json actually exist
            const string MECHANICS_DATA_FOLDER = "mechanics data";
            string nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "moves");
            ParseMoves(nextFolder);
            nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "abilities");
            ParseAbilities(nextFolder);
            nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "jewelry");
            ParseJewelry(nextFolder);
            nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "held items");
            ParseHeldItems(nextFolder);
            nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "natures");
            ParseNatures(nextFolder);
            nextFolder = Path.Combine(masterDirectory, MECHANICS_DATA_FOLDER, "poke balls");
            ParsePokeBalls(nextFolder);
            // Then the ones that are generated fully
            GenerateGummies();
            GenerateMints();
            GenerateEssences();
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
            string evoPlateTab = lines[6].Split(",")[0];
            ParseEvoPlateList(sheetId, evoPlateTab);
            string keyItemTab = lines[7].Split(",")[0];
            ParseKeyItemList(sheetId, keyItemTab);
            string npcArea = lines[8].Split(",")[0];
            FillNpcData(sheetId, npcArea);
            npcArea = lines[9].Split(",")[0]; // More npc data, split in the sheet to differentiate between famous and non
            FillNpcData(sheetId, npcArea);
            string boxMonArea = lines[10].Split(",")[0];
            FillBoxedMonData(sheetId, boxMonArea);
            string playerArea = lines[11].Split(",")[0];
            FillPlayers(sheetId, playerArea);
        }
        public Dictionary<PokemonType, Dictionary<PokemonType, double>> DefensiveTypeChart = new Dictionary<PokemonType, Dictionary<PokemonType, double>>();
        public Dictionary<string, Move> Moves = new Dictionary<string, Move>();
        public Dictionary<string, Ability> Abilities = new Dictionary<string, Ability>();
        public Dictionary<string, PokemonSpecies> Dex = new Dictionary<string, PokemonSpecies>();
        public Dictionary<string, string> UnownLookup = new Dictionary<string, string>();
        public Dictionary<string, NpcTrainer> AllNpcTrainers = new Dictionary<string, NpcTrainer>();
        public Dictionary<string, EvoPlate> EvoPlates = new Dictionary<string, EvoPlate>();
        public Dictionary<string, KeyItem> KeyItems = new Dictionary<string, KeyItem>();
        public Dictionary<string, Jewelry> Jewelry = new Dictionary<string, Jewelry>();
        public Dictionary<string, Gummy> Gummies = new Dictionary<string, Gummy>();
        public Dictionary<string, HeldItem> HeldItems = new Dictionary<string, HeldItem>();
        public Dictionary<string, Nature> Natures = new Dictionary<string, Nature>();
        public Dictionary<string, Mint> Mints = new Dictionary<string, Mint>();
        public Dictionary<string, Essence> Essences = new Dictionary<string, Essence>();
        public Dictionary<string, PokeBall> PokeBalls = new Dictionary<string, PokeBall>();
        public Dictionary<string, Sandwich> SandwichLookup = new Dictionary<string, Sandwich>();
        public Dictionary<string, MoveDisk> MoveDiskLookup = new Dictionary<string, MoveDisk>();
        public Dictionary<string, PokemonEntity> BoxedMons = new Dictionary<string, PokemonEntity>();
        public Dictionary<string, Trainer> Trainers = new Dictionary<string, Trainer>();
    }
}