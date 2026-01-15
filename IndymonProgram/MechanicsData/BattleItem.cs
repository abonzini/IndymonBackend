using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BattleItemFlag
    {
        NO_ITEM, // Item that is actualyl no item, helps acrobatics
        FIXED, // Fixed items cant be removed/thrown
        ALL_ITEMS, // Any item has this tag (except NO_ITEM of course)
        BERRY, // Berry
        CONSUMABLE, // Unburden, acro
        BAD_ITEM, // Items that are normally useless or bad/negative
        SET_AFFECTING_ITEM, // These items are meant to boost utility of a set, e.g. leftovers is good no matter what but choice item needs to boost to be useful, otherwise marked as useless
    }
    public class BattleItem
    {
        public string Name { get; set; } = "";
        public PokemonType DefensiveBoostType { get; set; } = PokemonType.NONE;
        public HashSet<BattleItemFlag> Flags { get; set; } = new HashSet<BattleItemFlag>(); /// Flags that an item may have
        public override string ToString()
        {
            return Name;
        }
    }
}
