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
            string pokedexTab = lines[1].Split(",")[0];
            string learnsetsTab = lines[4].Split(",")[0];
            ParsePokemonData(sheetId, pokedexTab, learnsetsTab);
            string modItemsTab = lines[5].Split(",")[0];
            ParseModItems(sheetId, modItemsTab);
            string battleItemsTab = lines[6].Split(",")[0];
            ParseBattleItems(sheetId, battleItemsTab);
            string abilityTab = lines[7].Split(",")[0];
            ParseAbilities(sheetId, abilityTab);
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
        public TypeChart TypeChart { get; set; }
        public Dictionary<string, Move> Moves { get; set; }
        public Dictionary<string, Ability> Abilities { get; set; }
        public Dictionary<string, Pokemon> Dex { get; set; }
        public Dictionary<string, ModItem> ModItems { get; set; }
        public Dictionary<string, BattleItem> BattleItems { get; set; }
        public Dictionary<(ElementType, string), float> InitialWeights { get; set; }
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), float>> Enablers { get; set; }
        public HashSet<(ElementType, string)> DisabledOptions { get; set; }
        public Dictionary<(ElementType, string), HashSet<(ElementType, string)>> ForcedBuilds { get; set; }
        public Dictionary<(ElementType, string), HashSet<(StatModifier, string)>> StatModifiers { get; set; }
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>> MoveModifiers { get; set; }
        public Dictionary<(ElementType, string), Dictionary<(ElementType, string), float>> WeightModifiers { get; set; }
        public Dictionary<(ElementType, string), float> FixedModifiers { get; set; }
        float[] AverageStats { get; set; } = new float[6];
        public float GetAverageStat(Stat stat)
        {
            return AverageStats[(int)stat];
        }
    }
}
