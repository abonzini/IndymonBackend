namespace MechanicsData
{
    public class MechanicsDataContainer
    {
        public TypeChart TypeChart { get; set; }
        public Dictionary<string, Move> Moves { get; set; }
        public Dictionary<string, Pokemon> Dex { get; set; }
        public Dictionary<string, ModItem> ModItems { get; set; }
        public Dictionary<string, BattleItem> BattleItems { get; set; }
    }
}
