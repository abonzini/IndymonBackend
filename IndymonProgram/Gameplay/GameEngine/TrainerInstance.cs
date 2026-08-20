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
    /// Contains all the data of a trainers in battle
    /// </summary>
    public class TrainerInstance
    {
        public string Name; /// Trainer name, for display
        public Side Side = Side.FIELD; /// Which side their mons spawn
        public int Team = 0; /// Which team they belong. In many cases, Team = (int)Side but in some cases (tag team) may differ. Mons in team 0 are hostile to everything
        public int MaxMonsInField = int.MaxValue; /// How many mons at the same team they can have (Normally 3 in 3v3 explorations but tag trainers may do 1)
        public int MaxMonsUsed = int.MaxValue; /// How many mons they'll use (e.g. 3 max in a 3v3 fight but could be all of them in an expl
        public List<PokemonInstance> PokemonInTeam = new List<PokemonInstance>(); /// The mons they have, may be KOd
        public Jewelry ActiveJewelry = null; /// Jewelry will give random passive effect too
        public override string ToString()
        {
            return Name;
        }
    }
}
