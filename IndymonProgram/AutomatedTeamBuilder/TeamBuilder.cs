using GameData;
using GameDataContainer;
using MechanicsData;
using MechanicsDataContainer;
using Utilities;

namespace AutomatedTeamBuilder
{
    public class TeamBuildConstraints
    {
        /// Options that could generate a valid team. Some may contian multiple (or) options. E.g. dancer will require one ability or flags. Monotype is multiple separate options each with a possible team comp
        public List<List<(ElementType, string)>> DifferentOptions = new List<List<(ElementType, string)>>();
        /// <summary>
        /// Adds all monotype constraint options (e.g. a team of one type, each with a possible solution
        /// </summary>
        public void AddMonotypeConstraints()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                DifferentOptions.Add([(ElementType.POKEMON_TYPE, type.ToString())]);
            }
        }
    }
    /// <summary>
    /// When attempting to check a valid team build, this structure contains all possible options that fulfill the desired constraints
    /// </summary>
    public class ValidTeamBuild
    {
        /// <summary>
        /// The Trainer's mons that can be used for this
        /// </summary>
        public List<TrainerPokemon> TrainerOwnPokemon = new List<TrainerPokemon>();
        /// <summary>
        /// The set items that can be used to satisfy strict move/ability requirements
        /// </summary>
        public List<string> SetItems = new List<string>();
        /// <summary>
        /// Pokemon that a trainer could lend you as a favor that satisfy req
        /// </summary>
        public Dictionary<string, List<TrainerPokemon>> FavourPokemon = new Dictionary<string, List<TrainerPokemon>>();
    }
    public static class TeamBuilder
    {
        /// <summary>
        /// For a given trainer, gives me all the possible teams they could build with the corresponding constraint sets
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="nMons">How many mons need to satisfy the constraints (min)</param>
        /// <param name="constraints">The constraints for teambuild</param>
        /// <returns></returns>
        public static List<ValidTeamBuild> GetTrainersBuildOptions(Trainer trainer, int nMons, TeamBuildConstraints constraints)
        {
            List<ValidTeamBuild> resultingBuilds = new List<ValidTeamBuild>();
            int mostOwnMonsUsed = 0; // Will try to only use the options that use the most own mons to not overuse set items or favors if don't need
            foreach (List<(ElementType, string)> options in constraints.DifferentOptions) // Check each satisfied constraint
            {
                ValidTeamBuild buildOption = new ValidTeamBuild(); // The corresponding team options for this case
                // First, check all own mons that satisfy
                foreach (TrainerPokemon mon in trainer.PartyPokemon)
                {
                    bool monValid = false;
                    foreach ((ElementType, string) check in options)
                    {
                        monValid = ValidateMonProperty(mon, check.Item1, check.Item2);
                        if (monValid) // If mon ok, then add to build and ditch
                        {
                            buildOption.TrainerOwnPokemon.Add(mon);
                            break;
                        }
                    }
                }
                // Then, need to check if the set items in inventory also satisfy, there may be lots (but only if trainer can use them
                if (trainer.AutoSetItem)
                {
                    foreach (KeyValuePair<string, int> setItem in trainer.SetItems)
                    {
                        foreach ((ElementType, string) check in options)
                        {
                            Ability setItemAbility = GetSetItemAbility(setItem.Key);
                            if (setItemAbility != null) // Set items that grant ability may grant a satisfying ability
                            {
                                if (ValidateAbilityProperty(setItemAbility, check.Item1, check.Item2))
                                {
                                    buildOption.SetItems.AddRange(Enumerable.Repeat(setItem.Key, setItem.Value));
                                }
                            }
                            Move setItemMove = GetSetItemMove(setItem.Key);
                            if (setItemMove != null) // Set items that grant move may grant a satisfying move 
                            {
                                if (ValidateMoveProperty(setItemMove, check.Item1, check.Item2))
                                {
                                    buildOption.SetItems.AddRange(Enumerable.Repeat(setItem.Key, setItem.Value));
                                }
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
                            bool monValid = false;
                            foreach ((ElementType, string) check in options)
                            {
                                monValid = ValidateMonProperty(mon, check.Item1, check.Item2);
                                if (monValid) // If mon ok, then add to build and ditch
                                {
                                    if (!buildOption.FavourPokemon.TryGetValue(friendlyTrainer, out List<TrainerPokemon> value))
                                    {
                                        value = new List<TrainerPokemon>();
                                        buildOption.FavourPokemon.Add(friendlyTrainer, value);
                                    }
                                    value.Add(mon);
                                    break;
                                }
                            }
                        }
                    }
                }
                // Now that all the options are set in place, need to verify if all of these allow to complete a team
                int usableMons = buildOption.TrainerOwnPokemon.Count; // Mons that can be used
                int trainerInvalidMons = trainer.PartyPokemon.Count - usableMons; // These mons want to play but can't!
                usableMons += Math.Min(trainerInvalidMons, buildOption.SetItems.Count); // Can use these to add more mons, but only if i have enough items/mons
                foreach (KeyValuePair<string, List<TrainerPokemon>> favorOption in buildOption.FavourPokemon) // Then, see how much I can borrow from each trainer
                {
                    int numberOfFavors = trainer.TrainerFavours[favorOption.Key];
                    usableMons += Math.Min(favorOption.Value.Count, numberOfFavors); // Can borrow only the valid mon but also limited by number of fav available
                }
                if (usableMons >= nMons) // Need to check now if I have enough options to build a team with these constraints
                {
                    resultingBuilds.Add(buildOption);
                    mostOwnMonsUsed = Math.Max(mostOwnMonsUsed, buildOption.TrainerOwnPokemon.Count); // Also keep this in mind, by the end I'll choose between the options that need the least amount of items
                }
            }
            // Finally, crop to only propose the ones where I use mostly my mons as much as possible
            resultingBuilds = [.. resultingBuilds.Where(b => b.TrainerOwnPokemon.Count == mostOwnMonsUsed)];
            return resultingBuilds;
        }
        /// <summary>
        /// Returns the ability as granted by a set item that potentially alters ability
        /// </summary>
        /// <returns>Ability added by this set item, if any</returns>
        public static Ability GetSetItemAbility(string setItem)
        {
            Ability resultingAbility = null;
            if (setItem.Contains(" Ability Capsule")) // Abilities granted by capsule
            {
                string abilityName = setItem.Split(" Ability Capsule")[0].Trim();
                resultingAbility = MechanicsDataContainers.GlobalMechanicsData.Abilities[abilityName];
            }
            return resultingAbility;
        }
        /// <summary>
        /// Returns the move as granted by a set item that potentially alters move
        /// </summary>
        /// <returns>Move added added by this set item, if any</returns>
        public static Move GetSetItemMove(string setItem)
        {
            Move resultingMove = null;
            if (setItem.Contains(" Move Disk")) // Moves granted by Move disk
            {
                string moveName = setItem.Split(" Move Disk")[0].Trim();
                resultingMove = MechanicsDataContainers.GlobalMechanicsData.Moves[moveName];
            }
            return resultingMove;
        }
        /// <summary>
        /// Checks whether a mon fills property or not
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="elementToCheck">What property to check for</param>
        /// <param name="elementToCheckName">Name of property to look for</param>
        /// <returns></returns>
        static bool ValidateMonProperty(TrainerPokemon mon, ElementType elementToCheck, string elementToCheckName)
        {
            Pokemon pokemonData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species]; // Obtain mon data
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out BattleItemFlag battleItemFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            return elementToCheck switch // Some won't apply
            {
                ElementType.NONE => true,
                ElementType.POKEMON => pokemonData.Name == elementToCheckName,
                ElementType.POKEMON_TYPE => pokemonData.Types.Contains(typeToCheck),
                ElementType.POKEMON_HAS_EVO => pokemonData.Evos.Count > 0,
                ElementType.BATTLE_ITEM => mon.BattleItem?.Name == elementToCheckName,
                ElementType.BATTLE_ITEM_FLAGS => mon.BattleItem?.Flags.Contains(battleItemFlagToCheck) == true,
                ElementType.MOD_ITEM => mon.ModItem?.Name == elementToCheckName,
                ElementType.ABILITY => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Name == elementToCheckName), // If has ability or set item adds it
                ElementType.MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Name == elementToCheckName), // If has move or set item adds it
                // Complex one because both moves and abilities may have it!
                ElementType.EFFECT_FLAGS => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Flags.Contains(effectFlagToCheck) == true) ||
                    pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Flags.Contains(effectFlagToCheck) == true),
                ElementType.DAMAGING_MOVE_OF_TYPE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category != MoveCategory.STATUS && m?.Type == typeToCheck),
                ElementType.MOVE_CATEGORY => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category == moveCategoryToCheck),
                ElementType.ANY_DAMAGING_MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category != MoveCategory.STATUS),
                _ => false,
            };
        }
        /// <summary>
        /// Checks whether ability fulfills property or not
        /// </summary>
        /// <param name="ability">Ability to check</param>
        /// <param name="elementToCheck">Element to check</param>
        /// <param name="elementToCheckName">Name of element to check</param>
        /// <returns>True if the ability satisfies property</returns>
        static bool ValidateAbilityProperty(Ability ability, ElementType elementToCheck, string elementToCheckName)
        {
            if (ability == null) return false;
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            return elementToCheck switch
            {
                ElementType.NONE => true,
                ElementType.ABILITY => ability.Name == elementToCheckName,
                ElementType.EFFECT_FLAGS => ability.Flags.Contains(effectFlagToCheck) == true,
                _ => false,
            };
        }
        /// <summary>
        /// Checks whether move fulfills property or not
        /// </summary>
        /// <param name="move">Move to check</param>
        /// <param name="elementToCheck">Element to check</param>
        /// <param name="elementToCheckName">Name of element to check</param>
        /// <returns>True if the ability satisfies property</returns>
        static bool ValidateMoveProperty(Move move, ElementType elementToCheck, string elementToCheckName)
        {
            if (move == null) return false;
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            return elementToCheck switch // Some won't apply
            {
                ElementType.NONE => true,
                ElementType.MOVE => move.Name == elementToCheckName,
                ElementType.EFFECT_FLAGS => move.Flags.Contains(effectFlagToCheck),
                ElementType.DAMAGING_MOVE_OF_TYPE => move.Category != MoveCategory.STATUS && move.Type == typeToCheck,
                ElementType.MOVE_CATEGORY => move.Category == moveCategoryToCheck,
                ElementType.ANY_DAMAGING_MOVE => move.Category != MoveCategory.STATUS,
                _ => false,
            };
        }
        /// <summary>
        /// Assembles a trainer's team given all possible builds, randomized if there's any preference
        /// </summary>
        /// <param name="trainer">The trainer whose battle team to define</param>
        /// <param name="teamBuild">All the possible builds for the trainers</param>
        public static void AssembleTrainersBattleTeam(Trainer trainer, int nMons, List<ValidTeamBuild> teamBuild)
        {
            ValidTeamBuild usedBuild = IndymonUtilities.GetRandomPick(teamBuild); // TODO: May need to be chosen in some crazy monotypes instead of random
            List<TrainerPokemon> finalBattleTeam = []; // This is the result
            List<TrainerPokemon> allTrainerMons = [.. trainer.PartyPokemon]; // May take from here in order if needed
            if (trainer.AutoTeam) // If shuffling is allowed, all is shuffled then lol
            {
                IndymonUtilities.ShuffleList(allTrainerMons);
                IndymonUtilities.ShuffleList(usedBuild.TrainerOwnPokemon);
            }
            // Now, begin the sequence of try to add trainer mons, in order: Party -> Set Items -> Favours
            for (int i = 0; i < usedBuild.TrainerOwnPokemon.Count && finalBattleTeam.Count < nMons; i++) // Fill as much as possible from here until done or ran out of mons
            {
                finalBattleTeam.Add(usedBuild.TrainerOwnPokemon[i]); // Add to final team
            }
            // Then, if still need to fill, let's go items
            if (finalBattleTeam.Count < nMons)
            {
                allTrainerMons.RemoveAll(p => finalBattleTeam.Contains(p)); // Removes the mons already in the team
                // Add mons + battle item as long as i still have mons (and items!)
                for (int i = 0; usedBuild.SetItems.Count > 0 && i < allTrainerMons.Count && finalBattleTeam.Count < nMons; i++)
                {
                    // Pick the next mon, equip a random (!) set item, cram into party
                    TrainerPokemon chosenMon = allTrainerMons[i];
                    string chosenSetItem = IndymonUtilities.GetRandomPick(usedBuild.SetItems);
                    usedBuild.SetItems.Remove(chosenSetItem); // Need to remove so it's not picked again, mon is ok as it is iterating
                    IndymonUtilities.AddtemToDictionary(trainer.SetItems, chosenSetItem, -1, true); // Also need to remove from the inventory!
                    chosenMon.SetItem = chosenSetItem; // This should be empty as auto-set-item should made sure to take this thing away from mon during parsing
                    finalBattleTeam.Add(chosenMon);
                }
            }
            // Finally finally, same with favor dudes
            while (finalBattleTeam.Count < nMons)
            {
                // First, find which random favor I will cash in
                KeyValuePair<string, List<TrainerPokemon>> nextFavour = IndymonUtilities.GetRandomKvp(usedBuild.FavourPokemon);
                int remainingFavours = trainer.TrainerFavours[nextFavour.Key] - 1;
                IndymonUtilities.AddtemToDictionary(trainer.TrainerFavours, nextFavour.Key, -1, true); // Remove 1 from the remaining favors of trainer
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
