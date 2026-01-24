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
            string initialWeightsTab = lines[8].Split(",")[0];
            ParseInitialWeights(sheetId, initialWeightsTab);
            string enablementTab = lines[9].Split(",")[0];
            ParseEnabledOptions(sheetId, enablementTab);
            string forcedBuildsTab = lines[10].Split(",")[0];
            ParseForcedBuilds(sheetId, forcedBuildsTab);
            string statModsTab = lines[11].Split(",")[0];
            ParseStatModifiers(sheetId, statModsTab);
            string moveModsTab = lines[12].Split(",")[0];
            ParseMoveModifiers(sheetId, moveModsTab);
            string weightModsTab = lines[13].Split(",")[0];
            ParseWeightModifiers(sheetId, weightModsTab);
            string fixedModsTab = lines[14].Split(",")[0];
            ParseFixedModifiers(sheetId, fixedModsTab);
        }
        public TypeChart TypeChart { get; set; } = new TypeChart();
        public Dictionary<string, Move> Moves { get; set; } = new Dictionary<string, Move>();
        public Dictionary<string, Ability> Abilities { get; set; } = new Dictionary<string, Ability>();
        public Dictionary<string, Pokemon> Dex { get; set; } = new Dictionary<string, Pokemon>();
        public Dictionary<string, ModItem> ModItems { get; set; } = new Dictionary<string, ModItem>();
        public Dictionary<string, BattleItem> BattleItems { get; set; } = new Dictionary<string, BattleItem>();
        public Dictionary<(ElementType, string), double> InitialWeights { get; set; } = new Dictionary<(ElementType, string), double>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>> Enablers { get; set; } = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>>();
        public HashSet<(ElementType, string)> DisabledOptions { get; set; } = new HashSet<(ElementType, string)>();
        public Dictionary<(ElementType, string), HashSet<(ElementType, string)>> ForcedBuilds { get; set; } = new Dictionary<(ElementType, string), HashSet<(ElementType, string)>>();
        public Dictionary<(ElementType, string), HashSet<(StatModifier, string)>> StatModifiers { get; set; } = new Dictionary<(ElementType, string), HashSet<(StatModifier, string)>>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>> MoveModifiers { get; set; } = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>>();
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>> WeightModifiers { get; set; } = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>>();
        public Dictionary<(ElementType, string), double> FixedModifiers { get; set; } = new Dictionary<(ElementType, string), double>();
    }
}
