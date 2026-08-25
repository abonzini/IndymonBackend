using Gameplay.GameplayElements;
using Utilities;

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
        POKEMON,
        FAVOUR,
        FAVOUR_RANK,
        FAVOUR_GACHA
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
            ItemType resultingType;
            // Check one by one (this also adds move disks and sandwiches to lookup!
            if (Essences.ContainsKey(itemName)) resultingType = ItemType.ESSENCE;
            else if (EvoPlates.ContainsKey(itemName)) resultingType = ItemType.EVO_PLATE;
            else if (Gummies.ContainsKey(itemName)) resultingType = ItemType.GUMMY;
            else if (HeldItems.ContainsKey(itemName)) resultingType = ItemType.HELD_ITEM;
            else if (GetMoveDisk(itemName) != null) resultingType = ItemType.MOVE_DISK;
            else if (Mints.ContainsKey(itemName)) resultingType = ItemType.MINT;
            else if (PokeBalls.ContainsKey(itemName)) resultingType = ItemType.POKE_BALL;
            else if (Dex.ContainsKey(itemName)) resultingType = ItemType.POKEMON;
            else if (GetSandwich(itemName) != null) resultingType = ItemType.SANDWICH;
            else if (itemName.ToLower().Contains("imp")) resultingType = ItemType.IMP;
            else if (AllNpcTrainers.ContainsKey(itemName)) resultingType = ItemType.FAVOUR;
            else if (Enum.TryParse<TrainerRank>(itemName, out _)) resultingType = ItemType.FAVOUR_RANK;
            else if (itemName.ToLower() == "favour gacha") resultingType = ItemType.FAVOUR_GACHA;
            else resultingType = ItemType.UNKNOWN;
            // Return findings
            return resultingType;
        }
        /// <summary>
        /// Gives an item to a trainer, sorts the item type too
        /// </summary>
        /// <param name="prizeName">String name of prize</param>
        /// <param name="trainer">The trainer to give it to</param>
        /// <param name="count">How many of them (default 1)</param>
        /// <returns>True if addition was succesful (or trainer bag full)</returns>
        public bool GivePrizeToTrainer(string prizeName, TrainerEntity trainer, int count = 1)
        {
            int auxCount = 0;
            switch (GetItemType(prizeName))
            {
                case ItemType.IMP:
                    auxCount = trainer.Imp += count * int.Parse(prizeName.ToLower().Split("imp")[0]);
                    break;
                case ItemType.ESSENCE:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.Essences, Essences[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.EVO_PLATE:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.EvoPlates, EvoPlates[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.GUMMY:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.Gummies, Gummies[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.HELD_ITEM:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.HeldItems, HeldItems[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.JEWELRY:
                    trainer.EquippedJewelry = Jewelries[prizeName];
                    auxCount = trainer.EquippedJewelryUses = DEFAULT_JEWELRY_DURATION;
                    break;
                case ItemType.KEY_ITEM:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.KeyItems, KeyItems[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.MOVE_DISK:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.MoveDisks, GetMoveDisk(prizeName), count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.MINT:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.Mints, Mints[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.POKE_BALL:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.PokeBalls, PokeBalls[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.SANDWICH:
                    {
                        for (auxCount = 0; auxCount < count && trainer.Sandwiches.Count < TrainerEntity.MAX_NUMBER_BAG; auxCount++)
                        {
                            trainer.Sandwiches.Add(GetSandwich(prizeName));
                        }
                    }
                    break;
                case ItemType.POKEMON:
                    {
                        PokemonEntity newMon = new PokemonEntity()
                        {
                            Name = prizeName,
                            Species = Dex[prizeName],
                            PokeBall = PokeBalls["Poke Ball"], // Pokeball unless specified otherwise
                            IsShiny = CommonRng.Next(0, SHINY_CHANCE) == 0
                        };
                        if (trainer.Pokemon.Count < TrainerEntity.MAX_NUMBER_POKEMON) // Add to party if there's space
                        {
                            trainer.Pokemon.Add(newMon);
                            auxCount++;
                        }
                        else if (trainer.BoxedMons.Count < TrainerEntity.MAX_NUMBER_BAG) // Add to box otherwise if space
                        {
                            string idBase = prizeName + trainer.Name;
                            string fullId = idBase;
                            int numberOfTries = 1;
                            while (BoxedMons.ContainsKey(fullId)) // Create box mon id until there's a new one
                            {
                                numberOfTries++;
                                fullId = idBase + numberOfTries.ToString();
                            }
                            BoxedMons.Add(fullId, newMon);
                            trainer.BoxedMons.Add(fullId);
                            auxCount++;
                        }
                        else
                        {
                            // Can't add more mons unfort
                        }
                    }
                    break;
                case ItemType.FAVOUR:
                    auxCount = GeneralUtilities.AddtemToCountDictionary(trainer.Favours, AllNpcTrainers[prizeName], count, TrainerEntity.MAX_NUMBER_BAG);
                    break;
                case ItemType.FAVOUR_RANK:
                    {
                        TrainerRank rank = Enum.Parse<TrainerRank>(prizeName);
                        List<NpcTrainer> favourPool = [.. AllNpcTrainers.Values.Where(t => t.TrainerRank == rank)];
                        GeneralUtilities.ShuffleList(favourPool, CommonRng);
                        for (auxCount = 0; auxCount < count && auxCount < favourPool.Count; auxCount++)
                        {
                            GeneralUtilities.AddtemToCountDictionary(trainer.Favours, favourPool[auxCount], count, TrainerEntity.MAX_NUMBER_BAG);
                        }
                    }
                    break;
                case ItemType.FAVOUR_GACHA:
                    {
                        List<NpcTrainer> favourPool = [.. AllNpcTrainers.Values];
                        GeneralUtilities.ShuffleList(favourPool, CommonRng);
                        for (auxCount = 0; auxCount < count && auxCount < favourPool.Count; auxCount++)
                        {
                            GeneralUtilities.AddtemToCountDictionary(trainer.Favours, favourPool[auxCount], count, TrainerEntity.MAX_NUMBER_BAG);
                        }
                    }
                    break;
                case ItemType.UNKNOWN:
                default:
                    throw new Exception($"Item {prizeName} not recognized as valid item");
            }
            return count > 0;
        }
    }
}
