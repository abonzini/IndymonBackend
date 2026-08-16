namespace Gameplay.GameplayElementsContainer
{
    public enum ItemType
    {
        UNKNOWN,
        IMP,
        ESSENCE,
        EVO_PLATE,
        GUMMY,
        HELD_ITEM,
        JEWELRY,
        KEY_ITEM,
        MOVE_DISK,
        MINT,
        POKE_BALL,
        SANDWICH,
        POKEMON
    }
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Given an item name, query everything until the item type is found, useful to pull from correct lookup and put it in the correct bag
        /// </summary>
        /// <param name="itemName">Name of item being queried</param>
        /// <returns></returns>
        public ItemType GetItemType(string itemName)
        {
            // Go step by step until a suitable place is found
            ItemType resultingType = ItemType.UNKNOWN;
            // Check one by one (this also adds move disks and sandwiches to lookup!
            if (Essences.ContainsKey(itemName)) resultingType = ItemType.ESSENCE;
            else if (EvoPlates.ContainsKey(itemName)) resultingType = ItemType.EVO_PLATE;
            else if (Gummies.ContainsKey(itemName)) resultingType = ItemType.GUMMY;
            else if (HeldItems.ContainsKey(itemName)) resultingType = ItemType.HELD_ITEM;
            else if (GetMoveDisk(itemName) != null) resultingType = ItemType.MOVE_DISK;
            else if (Mints.ContainsKey(itemName)) resultingType = ItemType.MINT;
            else if (PokeBalls.ContainsKey(itemName)) resultingType = ItemType.POKE_BALL;
            else if (Dex.ContainsKey(itemName)) resultingType = resultingType = ItemType.POKEMON;
            else if (GetSandwich(itemName) != null) resultingType = ItemType.SANDWICH;
            else if (itemName.ToLower().Contains("imp")) resultingType = ItemType.IMP;
            else resultingType = ItemType.UNKNOWN;
            // Return findings
            return resultingType;
        }
    }
}
