using Gameplay.GameplayElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gameplay.GameEngine
{
    /// <summary>
    /// Whtether mon can participate in battle
    /// </summary>
    public enum BattleAvailability
    {
        READY,
        BUSY,
        FAINT,
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AdditionalPokemonParameter
    {
        BOSS, /// Mon is a boss and the whole team loses if boss dies
        GIANT, /// Boss is giant which means it's just like... very big and bigger hitbox and stuff
    }
    /// <summary>
    /// An instance of a pokemon in the simulator
    /// </summary>
    public class PokemonInstance
    {
        public string Name = "";
        public double HpPercent = 1;
        public bool IsShiny = false;
        public List<NonVolatileStatus> NonVolatileStatusConditions = [];
        public HashSet<AdditionalPokemonParameter> AdditionalParameters = [];
        public BattleAvailability Availability = BattleAvailability.READY;
        public double[] BaseStatMult = [1, 1, 1, 1, 1, 1];
        public double[] VolatileStatMult = [1, 1, 1, 1, 1, 1];
    }
}
