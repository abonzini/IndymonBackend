using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
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
        FAIRY
    }
    public class TypeChart
    {
        public Dictionary<PokemonType, Dictionary<PokemonType, float>> DefensiveChart { get; set; } = new Dictionary<PokemonType, Dictionary<PokemonType, float>>();
        public float GetReceivedDamage(HashSet<PokemonType> receiverTypes, PokemonType moveType)
        {
            float result = 1.0f;
            foreach (PokemonType receiverType in receiverTypes)
            {
                result *= DefensiveChart[receiverType][moveType];
            }
            return result;
        }
        public void Clear()
        {
            DefensiveChart.Clear();
        }
    }
}
