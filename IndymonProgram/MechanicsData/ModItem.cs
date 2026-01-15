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
    public class ModItem
    {
        public string Name { get; set; } = "";
        public override string ToString()
        {
            return Name;
        }
    }
}