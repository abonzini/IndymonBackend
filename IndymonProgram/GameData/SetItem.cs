using MechanicsData;
using MechanicsDataContainer;

namespace GameData
{
    public class SetItem
    {
        // Consts
        const string BLANK_DISK = "Blank Disk";
        const string BASIC_DISK = "Basic Disk";
        const string ADVANCED_DISK = "Advanced Disk";
        const string ABILITY_CHARM = "Ability Charm";
        const string ABILITY_CAPSULE = "Ability Capsule";
        const string WATER_STONE = "Water Stone";
        const string ICE_STONE = "Ice Stone";
        // Data
        public string Name = "";
        public Ability AddedAbility = null;
        public List<Move> AddedMoves = [];
        public bool Expires = false;
        public string ItemReplacement = "";
        public bool AlwaysAllowedItem = true;
        public override string ToString()
        {
            return Name;
        }
        public bool CanEquip(TrainerPokemon mon)
        {
            if (Name == BLANK_DISK) return false; // Blank disk can't be equipped directly
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
            SetItem resultingItem = new SetItem
            {
                Name = itemName
            };
            // Checks moves granted
            string[] addedMoveNames = [];
            string addedAbilityName = "";
            if (itemName.Contains(BASIC_DISK))
            {
                addedMoveNames = itemName.Split(BASIC_DISK)[0].Trim().Split(";"); // Remove the tag and then add the Move(s) separated by ;
                resultingItem.AlwaysAllowedItem = false; // Basic disks only work if mon already had the moves
                resultingItem.ItemReplacement = BLANK_DISK;
                resultingItem.Expires = true;
            }
            else if (itemName.Contains(ADVANCED_DISK))
            {
                addedMoveNames = itemName.Split(ADVANCED_DISK)[0].Trim().Split(";"); // Remove the tag and then add the Move(s) separated by ;
                resultingItem.AlwaysAllowedItem = true;
                resultingItem.ItemReplacement = BLANK_DISK;
                resultingItem.Expires = true;
            }
            else if (itemName.Contains(ABILITY_CHARM))
            {
                addedAbilityName = itemName.Split(ABILITY_CHARM)[0].Trim(); // Remove the tag and then add the ability
                resultingItem.AlwaysAllowedItem = false;
                resultingItem.ItemReplacement = "";
                resultingItem.Expires = false;
            }
            else if (itemName.Contains(ABILITY_CAPSULE))
            {
                addedAbilityName = itemName.Split(ABILITY_CAPSULE)[0].Trim(); // Remove the tag and then add the ability
                resultingItem.AlwaysAllowedItem = true;
                resultingItem.ItemReplacement = "";
                resultingItem.Expires = false;
            }
            else if (itemName.Contains(WATER_STONE)) // Adds a lot of crazy water moves
            {
                addedMoveNames = ["Splash", "Water Sport", "Surf", "Waterfall", "Water Spout", "Origin Pulse", "Octazooka", "Muddy Water", "Wave Crash", "Water Shuriken", "Triple Dive", "Scald", "Steam Eruption", "Soak", "Aqua Jet", "Aqua Ring", "Jet Punch", "Clamp", "Flip Turn", "Fishious Rend"];
                resultingItem.AlwaysAllowedItem = true;
                resultingItem.ItemReplacement = "";
                resultingItem.Expires = true;
            }
            else if (itemName.Contains(ICE_STONE)) // Adds sheer cold
            {
                addedMoveNames = ["Sheer Cold"];
                resultingItem.AlwaysAllowedItem = true;
                resultingItem.ItemReplacement = "";
                resultingItem.Expires = true;
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
            if (MechanicsDataContainers.GlobalMechanicsData.Abilities.TryGetValue(addedAbilityName, out Ability ability))
            {
                resultingItem.AddedAbility = ability;
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
