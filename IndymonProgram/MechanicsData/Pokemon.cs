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
        public List<Ability> Abilities { get; set; } = new List<Ability>();
        public Pokemon Prevo { get; set; } = null;
        public List<Pokemon> Evos { get; set; } = new List<Pokemon>();
        public int[] Stats { get; set; } = new int[6]; // All stats, hopefully init to 0
        public List<Move> Moveset { get; set; } = new List<Move>();
        public double Weight { get; set; } = 0.0f;
        public override string ToString()
        {
            return Name;
        }
    }
}
