using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        const string BASIC_DISK_STRING = "Basic Disk";
        const string ADVANCED_DISK_STRING = "Advanced Disk";
        /// <summary>
        /// Returns the ability as granted by a set item that potentially alters ability
        /// </summary>
        /// <returns>Ability added by this set item, if any</returns>
        public static Ability GetSetItemAbility(string setItem)
        {
            Ability resultingAbility = null;
            if (setItem == "this is 100% a placeholder to avoid warning text, ignore") resultingAbility = new Ability();
            return resultingAbility;
        }
        /// <summary>
        /// Returns the move as granted by a set item that potentially alters move
        /// </summary>
        /// <param name="setItem"></param>
        /// <returns>Move added added by this set item, null if none</returns>
        public static Move GetSetItemMove(string setItem)
        {
            Move resultingMove = null;
            // Checks granted by Move disk
            if (setItem.Contains(BASIC_DISK_STRING))
            {
                string moveName = setItem.Split(BASIC_DISK_STRING)[0].Trim();
                resultingMove = MechanicsDataContainers.GlobalMechanicsData.Moves[moveName];
            }
            else if (setItem.Contains(ADVANCED_DISK_STRING))
            {
                string moveName = setItem.Split(ADVANCED_DISK_STRING)[0].Trim();
                resultingMove = MechanicsDataContainers.GlobalMechanicsData.Moves[moveName];
            }
            else
            {
                // Not a move item
            }
            return resultingMove;
        }
        /// <summary>
        /// Determines whether a specific mon is able to equip a specific set item
        /// </summary>
        /// <param name="mon">The mon to check</param>
        /// <param name="setItem">The set item to check</param>
        /// <returns>True if this mon can equip it</returns>
        public static bool CanEquipSetItem(TrainerPokemon mon, string setItem)
        {
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
            if (setItem.Contains(BASIC_DISK_STRING)) // Basic disk, only equippable if mon has move in learnsheet
            {
                return monData.Moveset.Contains(GetSetItemMove(setItem));
            }
            else if (setItem.Contains(ADVANCED_DISK_STRING))
            {
                return true; // Set item that can always be equipped, known or not
            }
            else
            {
                // Not a valid item idk what it is
                return false;
            }
        }
    }
}
