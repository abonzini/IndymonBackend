using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BattleItemFlag
    {
        NO_ITEM, // Item that is actualyl no item, helps acrobatics
        BERRY, // Berry
        CONSUMABLE // Unburden, acro
    }
    public class BattleItem
    {
        public string Name { get; set; } = "";
        public PokemonType OffensiveBoostType { get; set; } = PokemonType.NONE;
        public PokemonType DefensiveBoostType { get; set; } = PokemonType.NONE;
        public HashSet<BattleItemFlag> Flags { get; set; } = new HashSet<BattleItemFlag>(); /// Flags that an item may have
        public override string ToString()
        {
            return Name;
        }
    }
}
