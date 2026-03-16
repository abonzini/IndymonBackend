using GameData;
using GameDataContainer;
using MechanicsDataContainer;

namespace IndymonBackendProgram
{
    public static class IndymonUtilities
    {
        /// <summary>
        /// Returns a trainer from a string containing the trainer's name
        /// </summary>
        /// <param name="name">Name of trainer</param>
        /// <returns>The trainer instance</returns>
        public static Trainer GetTrainerByName(string name)
        {
            if (GameDataContainers.GlobalGameData.TrainerData.TryGetValue(name, out Trainer trainer)) { }
            else if (GameDataContainers.GlobalGameData.NpcData.TryGetValue(name, out trainer)) { }
            else if (GameDataContainers.GlobalGameData.FamousNpcData.TryGetValue(name, out trainer)) { }
            else throw new Exception("Trainer not found!?");
            return trainer;
        }
        /// <summary>
        /// Attempts to get a trainer info by name
        /// </summary>
        /// <param name="name">Name of trainer</param>
        /// <param name="trainer">Out var where trainer is</param>
        /// <returns></returns>
        public static bool TryGetTrainerByName(string name, out Trainer trainer)
        {
            bool success = true;
            trainer = null;
            try
            {
                trainer = GetTrainerByName(name);
            }
            catch
            {
                success = false;
            }
            return success;
        }
        public enum RewardType
        {
            KEY,
            SET,
            MOD,
            BATTLE,
            FAVOUR,
            IMP,
            POKEMON
        }
        /// <summary>
        /// Returns an item's type
        /// </summary>
        /// <param name="name">Name of item to check</param>
        /// <returns>The type of item</returns>
        public static RewardType GetRewardType(string name)
        {
            RewardType type;
            if (GameDataContainers.GlobalGameData.SetItems.ContainsKey(name) || SetItem.TryParse(name, out _)) type = RewardType.SET; // See if set item exists or would exist
            else if (MechanicsDataContainers.GlobalMechanicsData.ModItems.ContainsKey(name)) type = RewardType.MOD;
            else if (MechanicsDataContainers.GlobalMechanicsData.BattleItems.ContainsKey(name)) type = RewardType.BATTLE;
            else if (TryGetTrainerByName(name, out _)) type = RewardType.FAVOUR;
            else if (name.ToLower().Contains("imp")) type = RewardType.IMP;
            else if (MechanicsDataContainers.GlobalMechanicsData.Dex.ContainsKey(name)) type = RewardType.POKEMON;
            else type = RewardType.KEY; // Couldn't find what it was so it's a key item by default
            return type;
        }
        /// <summary>
        /// Gets a set item guaranteeing that the item will be created if not existing but otherwise retrieved
        /// </summary>
        /// <param name="name">Name of set item</param>
        /// <returns>Reference of set item</returns>
        public static SetItem GetOrCreateSetItem(string name)
        {
            if (!GameDataContainers.GlobalGameData.SetItems.TryGetValue(name, out SetItem theSetItem)) // See if the item was there already
            {
                theSetItem = SetItem.Parse(name);
                GameDataContainers.GlobalGameData.SetItems.Add(name, theSetItem);
            }
            return theSetItem;
        }
        /// <summary>
        /// Consumes all items that the battle team has used
        /// </summary>
        /// <param name="trainer">Trainer to consume</param>
        public static void ConsumeTrainersItems(Trainer trainer)
        {
            List<string> trainerActions = [];
            foreach (TrainerPokemon trainerMon in trainer.BattleTeam)
            {
                List<string> monActions = [];
                if (trainerMon.SetItem != null && trainerMon.SetItem.Expires)
                {
                    SetItem replacementItem = GameDataContainers.GlobalGameData.SetItems.GetValueOrDefault(trainerMon.SetItem.ItemReplacement); // Obtain the potential replacement item, which will be consumed instead
                    if (trainer.SetItems.TryGetValue(replacementItem, out int value) && value >= trainerMon.SetItem.ItemReplacementQuantity) // Try to remove the replacement item first
                    {
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, replacementItem, -trainerMon.SetItem.ItemReplacementQuantity, true); // Remove replacement item instead
                        if (trainerMon.SetItemChosen) monActions.Add($"{trainerMon.SetItem.ItemReplacementQuantity}x {replacementItem.Name} (due to {trainerMon.SetItem.Name})");
                    }
                    else if (trainer.SetItems.ContainsKey(trainerMon.SetItem)) // If not, try to remove the inventory one instead
                    {
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, trainerMon.SetItem, -1, true);
                        if (trainerMon.SetItemChosen) monActions.Add($"{trainerMon.SetItem}");
                    }
                    else // Worst case jsut delete the mon's item
                    {
                        trainerMon.SetItem = null; // Delete
                        if (trainerMon.SetItemChosen) monActions.Add($"{trainerMon.SetItem}");
                    }
                    // Finally, if mon borrowed this item (auto), return it
                    if (trainerMon.SetItem != null && !trainerMon.SetItemChosen) // If this set item wasn't chosen by the auto builder itself, return it
                    {
                        // Return item
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, trainerMon.SetItem, 1);
                        trainerMon.SetItem = null;
                    }
                }
                if (trainerMon.ModItem != null)
                {
                    if (trainer.ModItems.ContainsKey(trainerMon.ModItem)) // Try to remove the inventory one
                    {
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, trainerMon.ModItem, -1, true);
                        if (trainerMon.ModItemChosen) monActions.Add($"{trainerMon.ModItem}");
                    }
                    else // Worst case jsut delete the mon's item
                    {
                        trainerMon.ModItem = null; // Delete
                        if (trainerMon.ModItemChosen) monActions.Add($"{trainerMon.ModItem}");
                    }
                    // Finally, if mon borrowed this item (auto), return it
                    if (trainerMon.ModItem != null && !trainerMon.ModItemChosen) // If this set item wasn't chosen by the auto builder itself, return it
                    {
                        // Return item
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, trainerMon.ModItem, 1);
                        trainerMon.ModItem = null;
                    }
                }
                if (trainerMon.BattleItem != null)
                {
                    if (trainer.BattleItems.ContainsKey(trainerMon.BattleItem)) // Try to remove the inventory one
                    {
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, trainerMon.BattleItem, -1, true);
                        if (trainerMon.SetItemChosen) monActions.Add($"{trainerMon.BattleItem}");
                    }
                    else // Worst case jsut delete the mon's item
                    {
                        trainerMon.BattleItem = null; // Delete
                        if (trainerMon.SetItemChosen) monActions.Add($"{trainerMon.BattleItem}");
                    }
                    // Finally, if mon borrowed this item (auto), return it
                    if (trainerMon.BattleItem != null && !trainerMon.BattleItemChosen) // If this set item wasn't chosen by the auto builder itself, return it
                    {
                        // Return item
                        Utilities.GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, trainerMon.BattleItem, 1);
                        trainerMon.BattleItem = null;
                    }
                }
                if (monActions.Count > 0) // If mon did something worth notifying
                {
                    string monString = $"{trainerMon.GetInformalName()} consumed a " + string.Join(" and a ", monActions) + ".";
                    trainerActions.Add(monString);
                }
            }
            if (trainerActions.Count > 0)
            {
                string trainerText = $"- <@{trainer.DiscordNumber}>: {string.Join(" ", trainerActions)}.";
                GameDataContainers.GlobalGameData.CurrentEventMessage.PostEventText.AppendLine(trainerText);
            }
        }
        /// <summary>
        /// Warns trainer if the number of items is exceeding stuff
        /// </summary>
        /// <param name="trainer"></param>
        public static void WarnTrainer(Trainer trainer)
        {
            const int MAX_ITEMS = 20;
            const int MAX_BOX = 16;
            static void WarnIf(int count, int max, string what)
            {
                if (count > max)
                {
                    GameDataContainers.GlobalGameData.CurrentEventMessage.PostEventText.AppendLine($"||You currently have {count}/{max} {what}. Please discard or use before the deadline, otherwise the last few will be discarded until they can fit.");
                }
            }
            WarnIf(trainer.SetItems.Count, MAX_ITEMS, "Set Items");
            WarnIf(trainer.ModItems.Count, MAX_ITEMS, "Mod Items");
            WarnIf(trainer.BattleItems.Count, MAX_ITEMS, "Battle Items");
            WarnIf(trainer.TrainerFavours.Count, MAX_ITEMS, "Favours");
            WarnIf(trainer.KeyItems.Count, MAX_ITEMS, "Key Items");
            WarnIf(trainer.BoxedPokemon.Count, MAX_BOX, "Boxed Pokemon");
        }
    }
}
