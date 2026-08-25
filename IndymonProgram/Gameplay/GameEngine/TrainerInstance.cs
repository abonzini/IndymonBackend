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
        public Side Team = 0; /// Which team they belong and which Side the spawn is
        public int MaxMonsInField = GameplayElementsContainer.GameplayElementsContainer.DEFAULT_BATTLE_NMONS; /// How many mons at the same team they can have (Normally 3 in 3v3 explorations but tag trainers may do 1, in some cases even more than 3, staring base needs to be adaptable)
        public int MaxMonsUsed = int.MaxValue; /// How many mons they'll use (e.g. 3 max in a 3v3 fight but could be all of them in an expl
        public List<PokemonInstance> PokemonInTeam = new List<PokemonInstance>(); /// The mons they have, may be KOd
        public Jewelry ActiveJewelry = null; /// Jewelry will give random passive effect too
        public override string ToString()
        {
            return Name;
        }
    }
}
