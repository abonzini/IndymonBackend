using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gameplay.GameplayElements
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PokemonType
    {
        NONE,
        NORMAL,
        FIGHTING,
        FLYING,
        POISON,
        GROUND,
        ROCK,
        BUG,
        GHOST,
        STEEL,
        FIRE,
        WATER,
        GRASS,
        ELECTRIC,
        PSYCHIC,
        ICE,
        DRAGON,
        DARK,
        FAIRY,
    }
}
