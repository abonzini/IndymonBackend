using MechanicsData;
using MechanicsDataContainer;

namespace GameData
{
    public class SetItem
    {
        public string Name = "";
        public Ability AddedAbility = null;
        public List<Move> AddedMoves = [];
        public bool AlwaysAllowedItem = true;
        public override string ToString()
        {
            return Name;
        }
        public bool CanEquip(TrainerPokemon mon)
        {
            if (mon.SetItem != null) return false; // Mon already has set item so it can't equip more!
            if (AlwaysAllowedItem) return true; // If its always allowed, then it's fine too
            // Otherwise need to make sure mon can learn every single thing
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
            bool canEquip = true;
            if (AddedAbility != null)
            {
                canEquip &= monData.Abilities.Contains(AddedAbility);
            }
            foreach (Move addedMove in AddedMoves)
            {
                canEquip &= monData.Moveset.Contains(addedMove);
            }
            return canEquip;
        }
        public static SetItem Parse(string itemName)
        {
            const string BASIC_DISK_STRING = "Basic Disk";
            const string ADVANCED_DISK_STRING = "Advanced Disk";
            const string WATER_STONE = "Water Stone";
            SetItem resultingItem = new SetItem
            {
                Name = itemName
            };
            // Checks moves granted
            string[] addedMoveNames;
            if (itemName.Contains(BASIC_DISK_STRING))
            {
                addedMoveNames = itemName.Split(BASIC_DISK_STRING)[0].Trim().Split(";"); // Remove the tag and then add the Move(s) separated by ;
                resultingItem.AlwaysAllowedItem = false; // Basic disks only work if mon already had the moves
            }
            else if (itemName.Contains(ADVANCED_DISK_STRING))
            {
                addedMoveNames = itemName.Split(ADVANCED_DISK_STRING)[0].Trim().Split(";"); // Remove the tag and then add the Move(s) separated by ;
            }
            else if (itemName.Contains(WATER_STONE)) // Adds a lot of crazy water moves
            {
                addedMoveNames = ["Splash", "Water Sport", "Surf", "Waterfall", "Water Spout", "Origin Pulse", "Octazooka", "Muddy Water", "Wave Crash", "Water Shuriken", "Triple Dive", "Scald", "Steam Eruption", "Soak", "Aqua Jet", "Aqua Ring", "Jet Punch", "Clamp", "Flip Turn", "Fishious Rend"];
            }
            else
            {
                throw new Exception("Invalid Set Item");
            }
            foreach (string addedMove in addedMoveNames)
            {
                Move nextMove = MechanicsDataContainers.GlobalMechanicsData.Moves[addedMove];
                resultingItem.AddedMoves.Add(nextMove);
            }
            // Set item finished parsing
            return resultingItem;
        }
        public static bool TryParse(string itemName, out SetItem item)
        {
            bool success;
            item = null;
            try
            {
                item = Parse(itemName);
                success = true;
            }
            catch
            {
                success = false;
            }
            return success;
        }
    }
}
