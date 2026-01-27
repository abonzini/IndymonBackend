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
        REQUIRES_OFF_INCREASE, // Item requires offensive increase to be selected (to avoid giving specs to physicals and stuff like that)
        REQUIRES_DEF_INCREASE, // Item requires defensive increase to be selected (tera shard e.g.)
        REQUIRES_SPEED_INCREASE, // Item requires meaningful speed increase to be selected (to avoid giving scarf to fast (or too slow) mons)
    }
    public class BattleItem
    {
        public string Name { get; set; } = "";
        public HashSet<BattleItemFlag> Flags { get; set; } = new HashSet<BattleItemFlag>(); /// Flags that an item may have
        public override string ToString()
        {
            return Name;
        }
    }
}
