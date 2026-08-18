using Gameplay.GameplayElements;

namespace Gameplay.GameEngine
{
    /// <summary>
    /// Defines the side of a trainer, where their mons will spawn
    /// </summary>
    public enum Side
    {
        FIELD, /// Team units will spawn randomly in the field (e.g. wild mons)
        BOTTOM,
        TOP,
        LEFT,
        RIGHT
    }
    /// <summary>
    /// Contains all the data of all trainers battling
    /// </summary>
    public class TrainerInstance
    {
        public Side Side = Side.FIELD; // Which side their mons spawn
        public int Team = 0; // Which team they belong. In many cases, Team = (int)Side but in some cases (tag team) may differ. Mons in team 0 are hostile to everything
        public int MaxMonsInField = int.MaxValue; // How many mons at the same team they can have (Normally 3 in 3v3 explorations but tag trainers may do 1)
        public List<PokemonInstance> PokemonInTeam = new List<PokemonInstance>(); // The mons they (still?) have
        public Jewelry activeJewelry = null; // Jewelry will give random passive effect too
    }
}
