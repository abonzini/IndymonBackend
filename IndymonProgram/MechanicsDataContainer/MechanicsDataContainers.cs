using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        public static MechanicsDataContainers GlobalMechanicsData { get; set; } = new MechanicsDataContainers();
        /// <summary>
        /// Initializes data
        /// </summary>
        /// <param name="filePath">Path with links to google sheets for where to find the stuff</param>
        public void InitializeData(string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception($"Path {filePath} does not exist");
            string[] lines = File.ReadAllLines(filePath);
            string sheetId = lines[0].Split(",")[0];
            string typechartTab = lines[2].Split(",")[0];
            ParseTypeChart(sheetId, typechartTab);
            string moveTab = lines[3].Split(",")[0];
            ParseMoves(sheetId, moveTab);
            string abilityTab = lines[7].Split(",")[0];
            ParseAbilities(sheetId, abilityTab);
            string pokedexTab = lines[1].Split(",")[0];
            string learnsetsTab = lines[4].Split(",")[0];
            ParsePokemonData(sheetId, pokedexTab, learnsetsTab);
            string modItemsTab = lines[5].Split(",")[0];
            ParseModItems(sheetId, modItemsTab);
            string battleItemsTab = lines[6].Split(",")[0];
            ParseBattleItems(sheetId, battleItemsTab);
            string ballsTab = lines[15].Split(",")[0];
            ParsePokeballs(sheetId, ballsTab);
            string enablementTab = lines[8].Split(",")[0];
            ParseEnabledOptions(sheetId, enablementTab);
            string statModsTab = lines[9].Split(",")[0];
            ParseStatModifiers(sheetId, statModsTab);
            string moveModsTab = lines[10].Split(",")[0];
            ParseMoveModifiers(sheetId, moveModsTab);
            string weightModsTab = lines[11].Split(",")[0];
            ParseWeightModifiers(sheetId, weightModsTab);
            string fixedModsTab = lines[12].Split(",")[0];
            ParseFixedModifiers(sheetId, fixedModsTab);
            string unownTab = lines[13].Split(",")[0];
            ParseUnownLookup(sheetId, unownTab);
            string trainersTab = lines[14].Split(",")[0];
            ParseTrainerNamesLookup(sheetId, trainersTab);
        }
        public Dictionary<PokemonType, Dictionary<PokemonType, double>> DefensiveTypeChart = new Dictionary<PokemonType, Dictionary<PokemonType, double>>();
        public Dictionary<string, Move> Moves = new Dictionary<string, Move>();
        public Dictionary<string, Ability> Abilities = new Dictionary<string, Ability>();
        public Dictionary<string, Pokemon> Dex = new Dictionary<string, Pokemon>();
        public Dictionary<string, Item> ModItems = new Dictionary<string, Item>();
        public Dictionary<string, Item> BattleItems = new Dictionary<string, Item>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>> Enablers = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>>();
        public Dictionary<(ElementType, string), HashSet<(ElementType, string)>> ForcedBuilds = new Dictionary<(ElementType, string), HashSet<(ElementType, string)>>();
        public Dictionary<(ElementType, string), HashSet<(StatModifier, string)>> StatModifiers = new Dictionary<(ElementType, string), HashSet<(StatModifier, string)>>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>> MoveModifiers = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>> WeightModifiers = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>>();
        public Dictionary<(ElementType, string), double> FlatIncreaseModifiers = new Dictionary<(ElementType, string), double>();
        public Dictionary<string, string> UnownLookup = new Dictionary<string, string>();
        public Dictionary<string, TrainerRank> TrainerLookup = new Dictionary<string, TrainerRank>();
        public HashSet<string> PokeBalls = new HashSet<string>();
    }
}
