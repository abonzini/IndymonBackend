using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
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
    }
}
