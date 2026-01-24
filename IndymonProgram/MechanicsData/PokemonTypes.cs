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
        public Dictionary<PokemonType, Dictionary<PokemonType, double>> DefensiveChart { get; set; } = new Dictionary<PokemonType, Dictionary<PokemonType, double>>();
        /// <summary>
        /// Returns how much damage a mon with multiple types would get from an attack of specific type
        /// </summary>
        /// <param name="receiverTypes">Types of receiver mon</param>
        /// <param name="moveType">Type of move</param>
        /// <returns>Damage multiplier (usually between 0-4)</returns>
        public double GetReceivedDamage(IEnumerable<PokemonType> receiverTypes, PokemonType moveType)
        {
            double result = 1.0f;
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
