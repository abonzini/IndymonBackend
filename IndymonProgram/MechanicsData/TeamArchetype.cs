using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TeamArchetype
    {
        NONE,
        EXPLORATION,
        BACKGROUND_TERRAIN,
        BACKGROUND_WEATHER,
        TRICK_ROOM,
        GRAVITY,
        ROCKS,
        SPIKES,
        TSPIKES,
        WEBS,
        TERA, // For teams with a dedicated tera mon
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Weather
    {
        NONE,
        SUN,
        RAIN,
        SAND,
        SNOW,
        DESOLATE_LAND,
        PRIMORDIAL_SEA
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Terrain
    {
        NONE,
        ELECTRIC,
        GRASSY,
        PSYCHIC,
        MISTY
    }
}
