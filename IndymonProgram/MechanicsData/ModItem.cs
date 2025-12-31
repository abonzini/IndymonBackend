using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
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
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModItemExtraFlag // Some more complext modifiers of what a mod item does
    {
        NONE, // Nothing else
        ADD_NATURE, // Will add nature to the mon 
        ADD_HP_EV, // Adds ev to a stat
        ADD_ATK_EV,
        ADD_DEF_EV,
        ADD_SPATK_EV,
        ADD_SPDEF_EV,
        ADD_SPEED_EV,
        ADD_TERA_TYPE, // Adds tera type to mon
        ADD_MOVE, // Adds move to mon
        ADD_ABILITY, // Adds ability to mon
        LOGIC_MOD // Complex logic
    }
    public class ModItem
    {
        public string Name { get; set; } = "";
        public List<(ModItemExtraFlag, string)> Mods { get; set; } = new List<(ModItemExtraFlag, string)>(); /// Effects of the mod item
        public override string ToString()
        {
            return Name;
        }
    }
}