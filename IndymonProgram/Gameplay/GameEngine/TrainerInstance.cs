using Gameplay.GameplayElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gameplay.GameEngine
{
    /// <summary>
    /// Defines the side of a trainer, where their mons will spawn
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Side
    {
        NONE, /// Unused unless as a param
        FIELD, /// Field units are free for all
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
        public Side Team = Side.FIELD; /// Which team they belong and which Side the spawn is. Team 0 is for chaotic agents, some attacks and neutral players (if team 0 wins, it's a tie)
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
