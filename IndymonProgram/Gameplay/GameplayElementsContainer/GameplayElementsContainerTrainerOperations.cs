using Gameplay.GameplayElements;
using Utilities;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Given an npc name, create a trainer instance (temporary) to be used for NPC battles (e.g. wild Pokemon)
        /// </summary>
        /// <param name="npcId">Name of Npc to init</param>
        /// <param name="nMons">Number of mons to give this NPC</param>
        /// <param name="rngSeed">Seed to be used for making NPC team</param>
        /// <param name="itemEquipChance">Chance of using each of the npc's item on a mon</param>
        /// <param name="usedItems">Output of the items that have been used in this design (for exploration droprate), list WILL be modified</param>
        /// <returns>An instantiated trainer entity to use in battles and such. usedItems list will be also filled</returns>
        public TrainerEntity CreateNpcEntity(string npcId, int nMons, int rngSeed, double itemEquipChance, List<string> usedItems)
        {
            return CreateNpcEntity(AllNpcTrainers[npcId], nMons, rngSeed, itemEquipChance, usedItems);
        }
        /// <summary>
        /// Given an npc name, create a trainer instance (temporary) to be used for NPC battles (e.g. wild Pokemon)
        /// </summary>
        /// <param name="npcTrainer">Name of Npc to init</param>
        /// <param name="nMons">Number of mons to give this NPC</param>
        /// <param name="rngSeed">Seed to be used for making NPC team</param>
        /// <param name="itemEquipChance">Chance of using each of the npc's item on a mon</param>
        /// <param name="usedItems">Output of the items that have been used in this design (for exploration droprate), list WILL be modified</param>
        /// <returns>An instantiated trainer entity to use in battles and such. usedItems list will be also filled</returns>
        public TrainerEntity CreateNpcEntity(NpcTrainer npcTrainer, int nMons, int rngSeed, double itemEquipChance, List<string> usedItems)
        {
            if (!npcTrainer.FullyLoadedData) throw new Exception("Randomizing a trainer that has no data!");
            Random npcRng = new Random(rngSeed);
            usedItems.Clear();
            // Ok generate the trainer then
            TrainerEntity newTrainer = new TrainerEntity()
            {
                Name = npcTrainer.Name,
                AutoTeam = true
            };
            List<PokemonSpecies> allSpecies = [.. npcTrainer.AvailablePokemon]; // Copy as it'll be shuffled#
            GeneralUtilities.ShuffleList(allSpecies, npcRng);
            for (int i = 0; i < nMons && i < allSpecies.Count; i++) // Add species until no more to add
            {
                PokemonEntity newMon = new PokemonEntity()
                {
                    Name = allSpecies[i].Name,
                    Species = allSpecies[i],
                    PokeBall = PokeBalls["Poke Ball"], // All npc mons have a pokeball unless specified otherwise
                    IsShiny = npcRng.Next(0, SHINY_CHANCE) == 0
                };
                RandomizePokemon(newMon, npcRng); // Randomize the rest for this mon
                // Now, the fun part, if an item is to be assigned to this mon, do a roll and equip where corresponds
                if (npcTrainer.AvailableItems.Count > 0 && npcRng.NextDouble() < itemEquipChance)
                {
                    // An item will be chosen
                    string itemChosen = GeneralUtilities.GetRandomPick(npcTrainer.AvailableItems, npcRng);
                    usedItems.Add(itemChosen);
                    switch (GetItemType(itemChosen))
                    {
                        case ItemType.GUMMY:
                            EquipGummyToPokemon(newMon, Gummies[itemChosen], 1); // Equip gummy if possible, randomly into slot 1 if tiebreaks
                            break;
                        case ItemType.HELD_ITEM:
                            newMon.HeldItem = HeldItems[itemChosen];
                            break;
                        case ItemType.JEWELRY:
                            newTrainer.EquippedJewelry = Jewelries[itemChosen]; // This one is funny because it equips to the trainer
                            newTrainer.EquippedJewelryUses = 1;
                            break;
                        case ItemType.MOVE_DISK:
                            if (newMon.MoveDisks[0] == null)
                            {
                                newMon.MoveDisks[0] = GetMoveDisk(itemChosen);
                            }
                            else
                            {
                                newMon.MoveDisks[1] = GetMoveDisk(itemChosen); // Override slot 2 if already has some move disk
                            }
                            break;
                        case ItemType.MINT:
                            newMon.Nature = Mints[itemChosen].AssociatedNature;
                            break;
                        case ItemType.POKE_BALL:
                            newMon.PokeBall = PokeBalls[itemChosen];
                            break;
                        default:
                            break; // Item can't be equipped in this way to an npc
                    }
                }
                newTrainer.Pokemon.Add(newMon);
            }
            return newTrainer;
        }
        /// <summary>
        /// Consumes all items that the battle team has used. Auto replaced if there's items in inventory
        /// </summary>
        /// <param name="trainer">Trainer to consume</param>
        /// <param name="nMons">Mons whose items will be consumed (as not all would've participated in battle</param>
        public static void ConsumeTrainersItems(TrainerEntity trainer, int nMons = int.MaxValue)
        {
            // Verify if jewelry ran out
            trainer.EquippedJewelryUses--;
            if (trainer.EquippedJewelryUses <= 0)
            {
                trainer.EquippedJewelry = null;
            }
            // Then, verify mon items
            for (int i = 0; i < trainer.Pokemon.Count && i < nMons; i++)
            {
                PokemonEntity mon = trainer.Pokemon[i];
                // Gummies
                if (mon.AbilityActive[1])
                {
                    PokemonType gummyType = (mon.Species.Types.Item1 != PokemonType.NONE) ? mon.Species.Types.Item1 : mon.Species.Types.Item2;
                    Gummy replacementGummy = trainer.Gummies.Keys.Where(g => g.Type == gummyType).FirstOrDefault(); // Can apply extra logic filters
                    if (replacementGummy != null) // Can use a gummy to keep active
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.Gummies, replacementGummy, -1);
                    }
                    else // Otherwise, the ability can't be maintained anymore
                    {
                        mon.AbilityActive[1] = false;
                    }
                }
                if (mon.AbilityActive[2])
                {
                    PokemonType gummyType = (mon.Species.Types.Item2 != PokemonType.NONE) ? mon.Species.Types.Item2 : mon.Species.Types.Item1;
                    Gummy replacementGummy = trainer.Gummies.Keys.Where(g => g.Type == gummyType).FirstOrDefault(); // Can apply extra logic filters
                    if (replacementGummy != null) // Can use a gummy to keep active
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.Gummies, replacementGummy, -1);
                    }
                    else // Otherwise, the ability can't be maintained anymore
                    {
                        mon.AbilityActive[2] = false;
                    }
                }
                // Held Item
                if (mon.HeldItem != null)
                {
                    if (trainer.HeldItems.ContainsKey(mon.HeldItem))
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.HeldItems, mon.HeldItem, -1);
                    }
                    else
                    {
                        mon.HeldItem = null;
                    }
                }
                // Move disks
                for (int j = 0; j < mon.MoveDisks.Length; j++)
                {
                    if (mon.MoveDisks[j] != null)
                    {
                        // Prioritize consuming blank disk and otherwise try to see if there's copies of the same disk
                        MoveDisk replacementDisk = trainer.MoveDisks.Keys.Where(d => d.IsSacrificial).FirstOrDefault();
                        if (replacementDisk == null && trainer.MoveDisks.ContainsKey(mon.MoveDisks[j])) replacementDisk = mon.MoveDisks[0];
                        if (replacementDisk != null) // Can use a gummy to keep active
                        {
                            GeneralUtilities.AddtemToCountDictionary(trainer.MoveDisks, replacementDisk, -1);
                        }
                        else // Otherwise, the ability can't be maintained anymore
                        {
                            mon.MoveDisks[j] = null;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Warns trainer if the number of items is close to max
        /// </summary>
        /// <param name="trainer">Trainer to warn</param>
        /// <returns>string with the specific warning</returns>
        public static string GetTrainerInventoryWarning(TrainerEntity trainer)
        {
            string result = "";
            static string WarnBag(int count, int max, int threshold, string what)
            {
                if (count > threshold)
                {
                    return $"You currently have {count}/{max} {what}.";
                }
                return "";
            }
            int threshold = 8 * TrainerEntity.MAX_NUMBER_BAG / 10; // Warning at 80% bag full
            result += WarnBag(trainer.Gummies.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Gummies");
            result += WarnBag(trainer.MoveDisks.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Move Disks");
            result += WarnBag(trainer.HeldItems.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Held Items");
            result += WarnBag(trainer.Mints.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Mints");
            result += WarnBag(trainer.EvoPlates.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Evolution Plates");
            result += WarnBag(trainer.Essences.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Essences");
            result += WarnBag(trainer.KeyItems.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Key/Crafting Items");
            result += WarnBag(trainer.PokeBalls.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "PokeBalls");
            result += WarnBag(trainer.Sandwiches.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Sandwiches");
            result += WarnBag(trainer.Favours.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Favours");
            result += WarnBag(trainer.BoxedMons.Count, TrainerEntity.MAX_NUMBER_BAG, threshold, "Boxed Pokemon");
            if (result.Length > 0)
            {
                result += " Keep in mind that your bags can only hold up to {max} different items, and any additional items wont be picked up. You'll need to use, equip, discard or donate items if you want to make more space in your bag.";
            }
            return result;
        }
    }
}

