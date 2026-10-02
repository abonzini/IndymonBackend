using Gameplay.GameplayElements;

namespace Gameplay.GameEngine
{
    /// <summary>
    /// Whtether mon can participate in battle
    /// </summary>
    public enum BattleAvailability
    {
        NORMAL,
        BUSY,
        FAINT,
    }
    /// <summary>
    /// An instance of a pokemon in the simulator
    /// </summary>
    public class PokemonInstance
    {
        public string Name = "";
        public double HpPercent = 1;
        public NonVolatileStatus NonVolatileStatus = null;
        public BattleAvailability Availability = BattleAvailability.NORMAL;
        public bool IsBoss = true; /// If boss, the whole team loses if mon is knocked out
    }
}
