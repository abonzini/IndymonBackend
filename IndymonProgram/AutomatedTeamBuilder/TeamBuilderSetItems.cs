using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Returns the ability as granted by a set item that potentially alters ability
        /// </summary>
        /// <returns>Ability added by this set item, if any</returns>
        public static Ability GetSetItemAbility(string setItem)
        {
            Ability resultingAbility = null;
            if (setItem.Contains(" Ability Capsule")) // Abilities granted by capsule
            {
                string abilityName = setItem.Split(" Ability Capsule")[0].Trim();
                resultingAbility = MechanicsDataContainers.GlobalMechanicsData.Abilities[abilityName];
            }
            return resultingAbility;
        }
        /// <summary>
        /// Returns the move as granted by a set item that potentially alters move
        /// </summary>
        /// <returns>Move added added by this set item, if any</returns>
        public static Move GetSetItemMove(string setItem)
        {
            Move resultingMove = null;
            if (setItem.Contains(" Move Disk")) // Moves granted by Move disk
            {
                string moveName = setItem.Split(" Move Disk")[0].Trim();
                resultingMove = MechanicsDataContainers.GlobalMechanicsData.Moves[moveName];
            }
            return resultingMove;
        }
    }
}
