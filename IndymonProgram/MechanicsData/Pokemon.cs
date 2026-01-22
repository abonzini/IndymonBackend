using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Stat
    {
        HP = 0,
        ATTACK = 1,
        DEFENSE = 2,
        SPECIAL_ATTACK = 3,
        SPECIAL_DEFENSE = 4,
        SPEED = 5,
    }
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public PokemonType[] Types { get; set; } = [PokemonType.NONE, PokemonType.NONE];
        public HashSet<string> Abilities { get; set; } = new HashSet<string>();
        public string Prevo { get; set; } = "";
        public HashSet<string> Evos { get; set; } = new HashSet<string>();
        public int[] Stats { get; set; } = new int[6]; // All stats, hopefully init to 0
        public HashSet<string> Moves { get; set; } = new HashSet<string>();
        public override string ToString()
        {
            return Name;
        }
    }
}
