using GameData;
using GameDataContainer;
using MechanicsData;
using Utilities;

namespace AutomatedTeamBuilder
{
    /// <summary>
    /// When attempting to check a valid team build, this structure contains all possible options that fulfill the desired constraints
    /// </summary>
    public class PossibleTeamBuild
    {
        /// <summary>
        /// The Trainer's mons that can be used for this
        /// </summary>
        public List<TrainerPokemon> TrainerOwnPokemon = new List<TrainerPokemon>();
        /// <summary>
        /// The set items that can be used to satisfy strict move/ability requirements and which mons to apply to
        /// </summary>
        public Dictionary<string, List<TrainerPokemon>> TrainerOwnPokemonUsingSetItem = new Dictionary<string, List<TrainerPokemon>>();
        /// <summary>
        /// Pokemon that a trainer could lend you as a favor that satisfy req
        /// </summary>
        public Dictionary<string, List<TrainerPokemon>> FavourPokemon = new Dictionary<string, List<TrainerPokemon>>();
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Gets a lis tof all the possible good teams given a list of one or many team build constraints.
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="nMons">Number of mons desired in the team</param>
        /// <param name="constraintSets">All the different valid constraints that apply separately, only one needs to succeed</param>
        /// <returns></returns>
        public static List<PossibleTeamBuild> GetAllTrainersPossibleBuilds(Trainer trainer, int nMons, List<TeamBuildConstraints> constraintSets)
        {
            List<PossibleTeamBuild> resultingBuilds = new List<PossibleTeamBuild>();
            int mostOwnMonsUsed = 0; // Will try to only use the options that use the most own mons to not overuse set items or favors if don't need
            foreach (TeamBuildConstraints constraintSet in constraintSets)
            {
                // Get the possible lineup for this constraint
                PossibleTeamBuild thisTeamBuild = GetTrainersBuildOptions(trainer, constraintSet);
                int usableMons = thisTeamBuild.TrainerOwnPokemon.Count; // Mons that can be used
                foreach (KeyValuePair<string, List<TrainerPokemon>> setItemOptions in thisTeamBuild.TrainerOwnPokemonUsingSetItem) // Then check how many equippable items options
                {
                    int setItemCount = trainer.SetItems[setItemOptions.Key]; // How many of these items I have?
                    int potentialMonCount = setItemOptions.Value.Count;
                    usableMons += Math.Min(setItemCount, potentialMonCount); // Can use these to add more mons, but only if i have enough items/mons
                }
                foreach (KeyValuePair<string, List<TrainerPokemon>> favorOption in thisTeamBuild.FavourPokemon) // Then, see how much I can borrow from each trainer
                {
                    int numberOfFavors = trainer.TrainerFavours[favorOption.Key];
                    usableMons += Math.Min(favorOption.Value.Count, numberOfFavors); // Can borrow only the valid mon but also limited by number of fav available
                }
                if (usableMons >= nMons) // Need to check now if I have enough options to build a team with these constraints
                {
                    resultingBuilds.Add(thisTeamBuild);
                    mostOwnMonsUsed = Math.Max(mostOwnMonsUsed, thisTeamBuild.TrainerOwnPokemon.Count); // Also keep this in mind, by the end I'll choose between the options that need the least amount of items
                }
            }
            // Finally, crop to only propose the ones where I use mostly my mons as much as possible
            resultingBuilds = [.. resultingBuilds.Where(b => b.TrainerOwnPokemon.Count == mostOwnMonsUsed)];
            return resultingBuilds;
        }
        /// <summary>
        /// For a given trainer, gives me all the possible teams they could build with the corresponding constraint sets
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="constraints">The teambuild constraints</param>
        /// <returns>The teambuild that could satisfy all these</returns>
        public static PossibleTeamBuild GetTrainersBuildOptions(Trainer trainer, TeamBuildConstraints constraints)
        {
            PossibleTeamBuild resultingBuild = new PossibleTeamBuild();
            // First, check all own mons that satisfy by themselves
            List<TrainerPokemon> invalidPokemon = new List<TrainerPokemon>(); // Will contain pokemon that can't fill the case naturally
            foreach (TrainerPokemon mon in trainer.PartyPokemon)
            {
                bool monIsValid = true; // Mon starts valid unless its found that it can fulfill conditions
                foreach (List<(ElementType, string)> constraint in constraints.AllConstraints) // Need to satisfy all of these
                {
                    bool constraintSatisfied = false;
                    foreach ((ElementType, string) constraintCheck in constraint)
                    {
                        if (ValidateBasicMonProperty(mon, constraintCheck.Item1, constraintCheck.Item2))
                        {
                            constraintSatisfied = true;
                            break; // No need to look at the rest
                        }
                    }
                    if (!constraintSatisfied) // If a single constraint is not satisfied, mon is invalid
                    {
                        monIsValid = false;
                        break; // No need to continue then
                    }
                }
                if (!monIsValid) // If a mon didn't make it, add to list
                {
                    invalidPokemon.Add(mon);
                }
                else
                {
                    resultingBuild.TrainerOwnPokemon.Add(mon);
                }
            }
            // Then, need to check if mon could satisfy by just having a set item equipped
            if (trainer.AutoSetItem)
            {
                foreach (string setItem in trainer.SetItems.Keys)
                {
                    bool setItemIsValid = true;
                    foreach (List<(ElementType, string)> constraint in constraints.AllConstraints) // Need to satisfy all of these
                    {
                        bool constraintSatisfied = false;
                        foreach ((ElementType, string) constraintCheck in constraint)
                        {
                            Ability setItemAbility = GetSetItemAbility(setItem);
                            if (setItemAbility != null) // Set items that grant ability may grant a satisfying ability
                            {
                                if (ValidateBasicAbilityProperty(setItemAbility, constraintCheck.Item1, constraintCheck.Item2))
                                {
                                    constraintSatisfied = true;
                                    break;
                                }
                            }
                            Move setItemMove = GetSetItemMove(setItem);
                            if (setItemMove != null) // Set items that grant move may grant a satisfying move 
                            {
                                if (ValidateBasicMoveProperty(setItemMove, constraintCheck.Item1, constraintCheck.Item2))
                                {
                                    constraintSatisfied = true;
                                    break;
                                }
                            }
                        }
                        if (!constraintSatisfied) // If a single constraint is not satisfied, mon is invalid
                        {
                            setItemIsValid = false;
                            break; // No need to continue then
                        }
                    }
                    // If set item satisfies, then check which mons can use it and add them to result
                    if (setItemIsValid)
                    {
                        List<TrainerPokemon> validMons = new List<TrainerPokemon>();
                        foreach (TrainerPokemon mon in invalidPokemon)
                        {
                            if (CanEquipSetItem(mon, setItem))
                            {
                                validMons.Add(mon);
                            }
                        }
                        if (validMons.Count > 0)
                        {
                            resultingBuild.TrainerOwnPokemonUsingSetItem.Add(setItem, validMons);
                        }
                    }
                }
            }
            // Then, check trainer's favours and add the mons that satisfy, similarly
            if (trainer.AutoFavour)
            {
                foreach (string friendlyTrainer in trainer.TrainerFavours.Keys)
                {
                    Trainer theTrainer = GameDataContainers.GlobalGameData.GetTrainer(friendlyTrainer); // Find the trainer whoever it is
                    foreach (TrainerPokemon mon in theTrainer.PartyPokemon)
                    {
                        bool monIsVaild = true;
                        foreach (List<(ElementType, string)> constraint in constraints.AllConstraints) // Need to satisfy all of these
                        {
                            bool constraintSatisfied = false;
                            foreach ((ElementType, string) constraintCheck in constraint)
                            {
                                if (ValidateBasicMonProperty(mon, constraintCheck.Item1, constraintCheck.Item2)) // If mon ok, then add to build and ditch
                                {
                                    constraintSatisfied = true;
                                    break;
                                }
                            }
                            if (!constraintSatisfied) // If a single constraint is not satisfied, mon is invalid
                            {
                                monIsVaild = false;
                                break; // No need to continue then
                            }
                        }
                        if (monIsVaild)
                        {
                            if (!resultingBuild.FavourPokemon.TryGetValue(friendlyTrainer, out List<TrainerPokemon> value))
                            {
                                value = new List<TrainerPokemon>();
                                resultingBuild.FavourPokemon.Add(friendlyTrainer, value);
                            }
                            value.Add(mon);
                        }
                    }
                }
            }
            return resultingBuild;
        }
        /// <summary>
        /// Assembles a trainer's team given all possible builds, randomized if there's any preference
        /// </summary>
        /// <param name="trainer">The trainer whose battle team to define</param>
        /// <param name="teamBuild">All the possible builds for the trainers</param>
        public static void AssembleTrainersBattleTeam(Trainer trainer, int nMons, List<PossibleTeamBuild> teamBuild)
        {
            PossibleTeamBuild usedBuild = IndymonUtilities.GetRandomPick(teamBuild); // TODO: May need to be chosen in some crazy monotypes instead of random
            List<TrainerPokemon> finalBattleTeam = []; // This is the result
            // Now, begin the sequence of try to add trainer mons
            // If trainer defined a strict order, will add them in the order of team as stated, otherwise do mon>set item>favor
            if (trainer.AutoTeam) // If shuffling is allowed, all is shuffled then and picks prioritising item efficiency
            {
                IndymonUtilities.ShuffleList(usedBuild.TrainerOwnPokemon);
                for (int i = 0; i < usedBuild.TrainerOwnPokemon.Count && finalBattleTeam.Count < nMons; i++) // Fill as much as possible from here until done or ran out of mons
                {
                    finalBattleTeam.Add(usedBuild.TrainerOwnPokemon[i]); // Add to final team
                }
                // Then, if still need to fill, let's go items
                if (finalBattleTeam.Count < nMons)
                {
                    // Add mons + battle item as long as i still have mons (and items!) and need to get more
                    while (usedBuild.TrainerOwnPokemonUsingSetItem.Count > 0 && finalBattleTeam.Count < nMons)
                    {
                        // Pick a random set item, a random mon from the list, decrement uses of set item
                        string chosenSetItem = IndymonUtilities.GetRandomPick(usedBuild.TrainerOwnPokemonUsingSetItem.Keys.ToList()); // Choose an item
                        List<TrainerPokemon> potentialMons = [.. usedBuild.TrainerOwnPokemonUsingSetItem[chosenSetItem].Where(p => !finalBattleTeam.Contains(p))];
                        if (potentialMons.Count > 0) // ok theres something to work with
                        {
                            TrainerPokemon chosenMon = IndymonUtilities.GetRandomPick(potentialMons);
                            chosenMon.SetItem = chosenSetItem; // Equip the thing
                            int finalSetItemAmount = IndymonUtilities.AddtemToCountDictionary(trainer.SetItems, chosenSetItem, -1, true);
                            if (finalSetItemAmount <= 0) // If this was the last use of this item, then remove it from options from now on
                            {
                                usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                            }
                            finalBattleTeam.Add(chosenMon);
                        }
                        else
                        {
                            usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                        }
                    }
                }
            }
            else // Respect the strict order, mons are retrieved from their corresponding places
            {
                // Choose mons until i ran out of need or ran out of mon, but do so in the desired order
                for (int i = 0; i < trainer.PartyPokemon.Count && finalBattleTeam.Count < nMons; i++)
                {
                    TrainerPokemon mon = trainer.PartyPokemon[i];
                    if (usedBuild.TrainerOwnPokemon.Contains(mon)) // This was a valid mon, just add as is!
                    {
                        finalBattleTeam.Add(mon);
                    }
                    else // Then, may still be able to add it with a set item
                    {
                        List<string> potentialSetItems = new List<string>();
                        foreach (KeyValuePair<string, List<TrainerPokemon>> monsWSetItems in usedBuild.TrainerOwnPokemonUsingSetItem)
                        {
                            if (monsWSetItems.Value.Contains(mon)) // Mon can use this
                            {
                                potentialSetItems.Add(monsWSetItems.Key);
                            }
                        }
                        if (potentialSetItems.Count > 0) // Ok this mon can have the set item...
                        {
                            string chosenSetItem = IndymonUtilities.GetRandomPick(potentialSetItems); // Choose one at random
                            mon.SetItem = chosenSetItem; // Equip the thing
                            int finalSetItemAmount = IndymonUtilities.AddtemToCountDictionary(trainer.SetItems, chosenSetItem, -1, true);
                            if (finalSetItemAmount <= 0) // If this was the last use of this item, then remove it from options from now on
                            {
                                usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                            }
                            finalBattleTeam.Add(mon);
                        }
                    }
                }
            }
            // Finally finally, same with favor dudes, this will be random either way
            while (finalBattleTeam.Count < nMons)
            {
                // First, find which random favor I will cash in
                KeyValuePair<string, List<TrainerPokemon>> nextFavour = IndymonUtilities.GetRandomKvp(usedBuild.FavourPokemon);
                int remainingFavours = trainer.TrainerFavours[nextFavour.Key] - 1;
                IndymonUtilities.AddtemToCountDictionary(trainer.TrainerFavours, nextFavour.Key, -1, true); // Remove 1 from the remaining favors of trainer
                TrainerPokemon borrowedMon = IndymonUtilities.GetRandomPick(nextFavour.Value);
                if (trainer.TrainerFavours[nextFavour.Key] <= 0)// If i used the last trainer's favour
                {
                    usedBuild.FavourPokemon.Remove(nextFavour.Key); // This trainer can't be used anymore
                }
                else // Trainer still useful, just remove the mon i just borrowed
                {
                    usedBuild.FavourPokemon[nextFavour.Key].Remove(borrowedMon);
                }
                // This will continue until infinite loop or crash or sth because if i reached here everything should work
            }
            // Should be al good here I guess
            trainer.BattleTeam = finalBattleTeam; // Set this team for battle
        }
    }
}
