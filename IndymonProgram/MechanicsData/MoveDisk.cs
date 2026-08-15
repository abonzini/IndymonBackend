namespace MechanicsData
{
    public class MoveDisk
    {
        // Consts
        public const string BLANK_DISK = "Blank Disk";
        public const string MOVE_DISK_TEXT = "Disk";
        // Data
        public string Name = "";
        public bool IsRandomMove = false;
        public PokemonType Type = PokemonType.NONE;
        public bool IsSacrificial = false;
        public Move AddedMove = null;
        /// <summary>
        /// Parses a move disk given name, may be a Blank Disk or a disk associated with a move
        /// </summary>
        /// <param name="itemName">Name of item to parse (contains all the data)</param>
        /// <param name="moveDb">Place where to look up moves</param>
        /// <returns>The Parsed move</returns>
        public static MoveDisk Parse(string itemName, Dictionary<string, Move> moveDb)
        {
            MoveDisk resultingItem = new MoveDisk
            {
                Name = itemName
            };
            if (itemName == BLANK_DISK)
            {
                resultingItem.IsRandomMove = true;
                resultingItem.IsSacrificial = true;
            }
            else
            {
                // Checks if it's a move or a tpye one
                string diskPrefix = itemName.Split(MOVE_DISK_TEXT)[0].Trim(); // Keep the first part (before disk?)
                if (Enum.TryParse(diskPrefix.ToUpper(), out resultingItem.Type)) // Check if it's a type (e.g. Fire Disk)
                {
                    resultingItem.IsRandomMove = true;
                }
                else
                {
                    resultingItem.AddedMove = moveDb[diskPrefix];
                    resultingItem.Type = resultingItem.AddedMove.Type;
                }
            }
            return resultingItem;
        }
        /// <summary>
        /// Gets the human-readable description of an object, useful when assembling a glossary
        /// </summary>
        /// <returns>A description of the item</returns>
        public string GetDescription()
        {
            string effect = "When equipped into a Pokemon's slot, this slot will be filled with ";
            if (IsRandomMove)
            {
                effect += "a random ";
                if (Type != PokemonType.NONE)
                {
                    effect += $"{Utilities.GeneralUtilities.ApaCapitalize(Type.ToString())}-type ";
                }
                effect += "move in the Pokemon's learnset (for the week).";
            }
            else
            {
                effect += $"{AddedMove.Name}: {AddedMove.Description}";
            }
            // Also, blank disk will be used as a sacrifice to avoid the waste of more important items
            if (IsSacrificial)
            {
                effect += " Additionally, this item will be consumed in a player's inventory before any other move disks, so the player can keep their more valuable disks.";
            }
            return effect; // TODO: later on the json will contain a special field with this string, and will be printed here
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
