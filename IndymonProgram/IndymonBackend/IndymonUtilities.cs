using GameData;
using GameDataContainer;

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
                    if (trainer.SetItems.ContainsKey(replacementItem) && trainer.SetItems[replacementItem] >= trainerMon.SetItem.ItemReplacementQuantity) // Try to remove the replacement item first
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
    }
}
