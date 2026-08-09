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
        public Move AddedMove = null;
        public override string ToString()
        {
            return Name;
        }
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
            }
            else
            {
                // Checks move then
                string moveName = itemName.Split(MOVE_DISK_TEXT)[0]; // Keep the first part (before disk?)
                resultingItem.AddedMove = moveDb[moveName];
            }
            return resultingItem;
        }
    }
}
