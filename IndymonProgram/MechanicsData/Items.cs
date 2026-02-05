using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemFlag
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
        BULKY, // Items that restore HP or WP, useful mostly in bulky mons
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Nature
    {
        SERIOUS, // No nature basically
        LONELY,
        ADAMANT,
        NAUGHTY,
        BRAVE,
        BOLD,
        IMPISH,
        LAX,
        RELAXED,
        MODEST,
        MILD,
        RASH,
        QUIET,
        CALM,
        GENTLE,
        CAREFUL,
        SASSY,
        TIMID,
        HASTY,
        JOLLY,
        NAIVE
    }
    public class Item
    {
        public string Name { get; set; } = "";
        public HashSet<ItemFlag> Flags { get; set; } = new HashSet<ItemFlag>(); /// Flags that an item may have
        public override string ToString()
        {
            return Name;
        }
    }
}
