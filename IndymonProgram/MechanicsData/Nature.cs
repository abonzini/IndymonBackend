using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Nature
    {
        HARDY,
        DOCILE,
        BASHFUL,
        QUIRKY,
        SERIOUS,
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
    public class PokemonNature
    {
    }
}
