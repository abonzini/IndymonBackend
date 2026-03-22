using GameData;
using MechanicsData;
using MechanicsDataContainer;
using Utilities;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        enum MonBuildState
        {
            CHOOSING_ABILITY,
            CHOOSING_MOVES,
            CHOOSING_MOD_ITEM,
            CHOOSING_BATTLE_ITEM,
            DONE
        }
        /// <summary>
        /// Defines all sets for a trainer
        /// </summary>
        /// <param name="trainer">Which trainer I'll do</param>
        /// <param name="smart">Smart build or not (i.e. "Thinks" of a set instead of random stuff)</param>
        /// <param name="archetypes">Valid archetypes for this mon set</param>
        /// <param name="initialWeather">Initial weathe rfor this mon set</param>
        /// <param name="initialTerrain">Initial terrain for this mon set</param>
        /// <param name="buildConstraints">Specific constraints needed for mon</param>
        /// <param name="pokemonFaced">All the pokemon that may be faced, to calculate types and stats</param>
        /// <param name="seed">Seed if the trainer set, to ensure consistency if saved</param>
        public static void DefineTrainerSets(Trainer trainer, bool smart, HashSet<TeamArchetype> archetypes, Weather initialWeather, Terrain initialTerrain, Constraint buildConstraints, List<Pokemon> pokemonFaced, int seed = 0)
        {
            // Create a build ctx to start team build
            TeamBuildContext buildCtx = new TeamBuildContext
            {
                smartTeamBuild = smart,
                CurrentWeather = initialWeather,
                CurrentTerrain = initialTerrain,
            };
            buildCtx.CurrentTeamArchetypes.UnionWith(archetypes); // Add archetypes
            buildCtx.TeamBuildConstraints.Add(buildConstraints); // Add design constraints
            if (smart) // In smart build, theres a constraint where every mon needs to have an attackign move no matter what (to avoid locks)
            {
                Constraint damagingMoveConstraint = new Constraint();
                damagingMoveConstraint.AllConstraints.Add((ElementType.ANY_DAMAGING_MOVE, "-"));
                buildCtx.TeamBuildConstraints.Add(damagingMoveConstraint);
            }
            // Finally, if building against other pokemon, need to fetch the stats and other things
            if (pokemonFaced.Count > 0)
            {
                // Initial pass, load all data
                foreach (Pokemon facedMon in pokemonFaced)
                {
                    buildCtx.OpponentsTypes.Add(facedMon.Types); // Add type combo (base) to list
                    for (int i = 0; i < 6; i++)
                    {
                        buildCtx.OpponentsStats[i] += facedMon.Stats[i] / pokemonFaced.Count;
                    }
                    buildCtx.AverageOpponentWeight += facedMon.Weight / pokemonFaced.Count;
                }
                // Another pass for variance
                foreach (Pokemon facedMon in pokemonFaced)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double meanDifference = (facedMon.Stats[i] - buildCtx.OpponentsStats[i]);
                        meanDifference *= meanDifference; // Square it
                        buildCtx.OppStatVariance[i] += meanDifference / pokemonFaced.Count; // Variance
                    }
                }
            }
            else if (smart)
            {
                throw new Exception("Smart teambuild needs data about opp mons!");
            }
            else
            {
                // Stupid build, reserved for wild mons
            }
            BuildTeam(trainer, buildCtx, seed);
        }
        /// <summary>
        /// Sets the movesets of all mons of a trainer's battle team
        /// </summary>
        /// <param name="trainer">Which trainer to build</param>
        /// <param name="buildCtx">Context containing other team build things that may be important (May be modified by a set)</param>
        /// <param name="seed">Team building seed, 0 to generate a random one</param>
        static void BuildTeam(Trainer trainer, TeamBuildContext buildCtx, int seed)
        {
            int teamSeed = (seed == 0) ? GeneralUtilities.GetRandomNumber(int.MaxValue) : seed;
            Random teamRng = new Random(teamSeed); // Not ideal but lets us retry with same value
            int monSeed = teamRng.Next(); // Get the next seed of mon
            // Will build a set for each mon
            for (int monIndex = 0; monIndex < trainer.BattleTeam.Count; monIndex++)
            {
                // Init stuff
                Random monRng = new Random(monSeed); // Will use this for the mon, in order to be able to reuse seed
                TrainerPokemon mon = trainer.BattleTeam[monIndex];
                mon.ChosenAbility = null;
                mon.ChosenMoveset.Clear();
                // Also get the mons ability and moveset here
                Pokemon monData;
                monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
                // First thing is to check if mon has set item equipped, if so, add the move/ability already
                if (mon.SetItem != null)
                {
                    if (!mon.SetItem.CanEquip(mon)) throw new Exception($"Mon {mon.Species} can't equip {mon.SetItem.Name}"); // Verif to ensure no one equipped an invalid item
                    mon.ChosenAbility = mon.SetItem.AddedAbility;
                    mon.ChosenMoveset = [.. mon.SetItem.AddedMoves];
                }
                // Now that the initial set is assembled, evaluate with a state machine
                MonBuildState state = MonBuildState.CHOOSING_ABILITY; // Begin with ability
                while (state != MonBuildState.DONE)
                {
                    PokemonBuildContext monCtx = new PokemonBuildContext();
                    if (buildCtx.smartTeamBuild) // Build info needs to do scoring stuff if in a smart build
                    {
                        monCtx = ObtainPokemonSetContext(mon, buildCtx); // Obtain current Pokemon mods and score and such
                    }
                    // Monctx contains all the ongoing constraints, need only the ones which haven't been fulfilled yet
                    List<Constraint> ongoingConstraints = new List<Constraint>();
                    foreach (Constraint constraint in monCtx.AdditionalConstraints) // filter constraint set out
                    {
                        // If constraint not yet satisfied, add to list to requirements
                        if (!constraint.SatisfiedByMon(mon, false)) ongoingConstraints.Add(constraint);
                    }
                    // Got constraint list finally! Now just do state machine
                    const double WEIGHT_PER_ITEM = 0.1; // Each item already in inventory adds 0.1 to the item weight, maximizing at 1 (10 items) where it tries to use item as long as useful!
                    switch (state)
                    {
                        case MonBuildState.CHOOSING_ABILITY:
                            // Then, define the mon's ability (unless already defined)
                            if (mon.ChosenAbility == null) // Mon needs an ability
                            {
                                List<Ability> possibleAbilities = [.. monData.Abilities.Where(a => !a.Flags.Contains(EffectFlag.BANNED))]; // All (non banned) possible abilities
                                if (mon.Species.ToLower().Contains("unown")) // And then again, weird mechanic. Unown will actually be able to use all abilities starting with the letter
                                {
                                    char letter = (mon.Species == "Unown") ? 'a' : mon.Species.ToLower().Last(); // Basic unown is A
                                    List<Ability> unownAbilities = [.. MechanicsDataContainers.GlobalMechanicsData.Abilities.Values.Where(a => a.Name.ToLower().StartsWith(letter) && !a.Flags.Contains(EffectFlag.BANNED))]; // Get all abilities
                                    if (unownAbilities.Count > 0) possibleAbilities = unownAbilities; // If no abilities (X/Y), then use standard (levitate)
                                }
                                List<double> abilityScores = [.. Enumerable.Repeat<double>(1, possibleAbilities.Count)]; // All of their values is init to 1
                                if (trainer.AutoSetItem && mon.SetItem != null) // If I can equip other set items AND set item provides useful abilities, I'll add them too
                                {
                                    int setItemCount = trainer.SetItems.Values.Sum(); // How many items does the trainer have total?
                                    double initialItemScore = WEIGHT_PER_ITEM * setItemCount;
                                    initialItemScore = Math.Clamp(initialItemScore, WEIGHT_PER_ITEM, 1);
                                    List<SetItem> possibleSetItems = [.. trainer.SetItems.Keys.OrderBy(s => s.Name)];
                                    foreach (SetItem setItem in possibleSetItems) // Need to check which set items are available
                                    {
                                        if (setItem.CanEquip(mon))
                                        {
                                            if (setItem.AddedAbility != null && !possibleAbilities.Contains(setItem.AddedAbility)) // if available and not included yet, I add to possibilities
                                            {
                                                possibleAbilities.Add(setItem.AddedAbility);
                                                abilityScores.Add(initialItemScore);
                                            }
                                        }
                                    }
                                }
                                // If there's a constraint that requires specific abilities, need to filter list further
                                List<Ability> acceptableAbilities = new List<Ability>();
                                List<double> acceptableAbilitiesScores = new List<double>();
                                foreach (Constraint constraint in ongoingConstraints) // Quick check of which constraints an ability could solve
                                {
                                    // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                    for (int i = 0; i < possibleAbilities.Count; i++)
                                    {
                                        Ability potentialAbility = possibleAbilities[i];
                                        if (constraint.SatisfiedByAbility(potentialAbility))
                                        {
                                            acceptableAbilities.Add(potentialAbility); // This is an ability I can consider then!
                                            acceptableAbilitiesScores.Add(abilityScores[i]); // Add its score too
                                        }
                                    }
                                }
                                if (acceptableAbilities.Count == 0)
                                {
                                    acceptableAbilities = possibleAbilities; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    acceptableAbilitiesScores = abilityScores;
                                }
                                // Finally, score the abilities, create an array with same count with scores, choose an index, choose ability
                                for (int i = 0; i < acceptableAbilities.Count; i++)
                                {
                                    if (buildCtx.smartTeamBuild) // If smart, abilities are weighted according to how useful they are
                                    {
                                        Ability nextAbility = acceptableAbilities[i];
                                        double abilityScore = GetAbilityWeight(nextAbility, mon, monCtx, buildCtx, monIndex == 0, monIndex == (trainer.BattleTeam.Count - 1));
                                        acceptableAbilitiesScores[i] *= abilityScore;
                                    } // Otherwise score is kept as is (possibly 1) for "dumb" build
                                } // Gottem scores
                                int chosenAbilityIndex = RandomIndexOfWeights(acceptableAbilitiesScores, monRng, 1.2); // Use power of 1.2 nudge toward good
                                Ability chosenAbility = acceptableAbilities[chosenAbilityIndex]; // Got the ability
                                mon.ChosenAbility = chosenAbility; // Apply to mon, all good here
                                if (!monData.Abilities.Contains(chosenAbility)) // If not in mon, this was seen as a set item then
                                {
                                    mon.SetItem = trainer.SetItems.Keys.First(i => i.AddedAbility == chosenAbility); // Get the first set item that satisfies
                                    GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, mon.SetItem, -1, true); // Remove 1 charge of set item from trainer
                                                                                                                       // If set item just equipped, also means the mon has been added moves
                                    mon.ChosenMoveset = [.. mon.ChosenMoveset.Union(mon.SetItem.AddedMoves)];
                                }
                            }
                            if (mon.ChosenAbility != null) // After this step, this should be true always and move on!
                            {
                                state = MonBuildState.CHOOSING_MOVES;
                            }
                            break;
                        case MonBuildState.CHOOSING_MOVES:
                            const int NUMBER_OF_MOVES_PER_MON = 4; // 4 moves, classic unless like, added via crazy move disk
                            if (mon.ChosenMoveset.Count < NUMBER_OF_MOVES_PER_MON) // Time to add a next move (or empty)
                            {
                                List<Move> possibleMoves = [.. monData.Moveset.Where(m => !m.Flags.Contains(EffectFlag.BANNED))]; // All possible (legal) moves
                                if (mon.Species.ToLower().Contains("unown")) // And then again, weird mechanic because I can only allow the moves that start with the unown letter
                                {
                                    char letter = (mon.Species == "Unown") ? 'a' : mon.Species.ToLower().Last(); // Basic unown is A
                                    possibleMoves = [.. possibleMoves.Where(m => m.Name.ToLower().StartsWith(letter))]; // Additional move filter
                                }
                                List<double> moveScores = [.. Enumerable.Repeat<double>(1, possibleMoves.Count)]; // All of their values is init to 1
                                // Continue, check if add set item?
                                if (trainer.AutoSetItem && mon.SetItem == null) // If I can equip other set items AND set item provides useful moves, I'll add them too
                                {
                                    int setItemCount = trainer.SetItems.Values.Sum(); // How many items does the trainer have total?
                                    double initialItemScore = WEIGHT_PER_ITEM * setItemCount;
                                    initialItemScore = Math.Clamp(initialItemScore, WEIGHT_PER_ITEM, 1);
                                    List<SetItem> possibleSetItems = [.. trainer.SetItems.Keys.OrderBy(s => s.Name)];
                                    foreach (SetItem setItem in possibleSetItems) // Need to check which set items are available
                                    {
                                        if (setItem.CanEquip(mon))
                                        {
                                            if (setItem.AddedMoves.Count > 0 && setItem.AddedMoves.Except(possibleMoves).Any()) // if available and some move would be new, add to option
                                            {
                                                foreach (Move addedMove in setItem.AddedMoves)
                                                {
                                                    if (!possibleMoves.Contains(addedMove)) // Add the moves that weren't there before
                                                    {
                                                        possibleMoves.Add(addedMove); // Adds the missing moves to list
                                                        moveScores.Add(initialItemScore);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                // Remove the ones I already have!
                                foreach (Move alreadyPresentMove in mon.ChosenMoveset)
                                {
                                    if (possibleMoves.Contains(alreadyPresentMove))
                                    {
                                        int moveIndex = possibleMoves.IndexOf(alreadyPresentMove);
                                        possibleMoves.RemoveAt(moveIndex); // Remove already present move
                                        moveScores.RemoveAt(moveIndex);
                                    }
                                }
                                // If there's a constraint that requires specific moves, need to filter list further
                                List<Move> acceptableMoves = new List<Move>();
                                List<double> acceptableMovesScores = new List<double>();
                                foreach (Constraint constraint in ongoingConstraints) // Quick check of which constraints a move could solve
                                {
                                    // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                    for (int i = 0; i < possibleMoves.Count; i++)
                                    {
                                        Move potentialMove = possibleMoves[i];
                                        if (constraint.SatisfiedByMove(potentialMove))
                                        {
                                            acceptableMoves.Add(potentialMove); // This is a move I can consider then!
                                            acceptableMovesScores.Add(moveScores[i]);
                                        }
                                    }
                                }
                                if (acceptableMoves.Count == 0)
                                {
                                    // Ok then, no move is mandatory, will choose if I want to pivot or what
                                    const double BASE_PIVOT_CHANCE = 0.1; // Pivot has a hard chance of 10%
                                    double pivotModdedChance = monCtx.WeightMods.GetValueOrDefault((ElementType.EFFECT_FLAGS, EffectFlag.PIVOT.ToString()), 1);
                                    pivotModdedChance *= BASE_PIVOT_CHANCE;
                                    // Obtain list of pivot moves
                                    List<Move> pivotMoves = [];
                                    List<double> pivotScores = [];
                                    for (int i = 0; i < possibleMoves.Count; i++)
                                    {
                                        Move potentialPivot = possibleMoves[i];
                                        if (!ExtractMoveFlags(potentialPivot, monCtx).Contains(EffectFlag.PIVOT)) // Check if pivot
                                        {
                                            continue; // Skip if not pivot
                                        }
                                        pivotMoves.Add(potentialPivot);
                                        pivotScores.Add(moveScores[i]);
                                    }
                                    // Will pivot only if the mon actually benefits from pivots, and/or there's pivot moves
                                    // In this case, the chance will be rolled
                                    if ((pivotModdedChance > BASE_PIVOT_CHANCE || pivotMoves.Count > 0) && pivotModdedChance > monRng.NextDouble()) // Roll this chance
                                    {
                                        acceptableMoves = pivotMoves;
                                        acceptableMovesScores = pivotScores;
                                    }
                                    else // No forced pivot, choose any move
                                    {
                                        acceptableMoves = possibleMoves; // If no move fills constraint (or no constraint) then just use all, yolo.
                                        acceptableMovesScores = moveScores;
                                    }
                                }
                                // Score the moves, create an array with same count with scores, choose an index, choose move
                                if (acceptableMoves.Count > 0) // Theres moves out of a subset to choose from
                                {
                                    for (int i = 0; i < acceptableMoves.Count; i++)
                                    {
                                        if (buildCtx.smartTeamBuild) // If smart, abilities are weighted according to how useful they are
                                        {
                                            Move nextMove = acceptableMoves[i];
                                            double nextMoveScore = GetMoveWeight(nextMove, mon, monCtx, buildCtx, monIndex == 0, monIndex == (trainer.BattleTeam.Count - 1));
                                            acceptableMovesScores[i] *= nextMoveScore;
                                        } // Otherwise, kept as original (usually 1)
                                    } // Gottem scores
                                    int chosenMoveIndex = RandomIndexOfWeights(acceptableMovesScores, monRng, 3); // Experimenting with a power to filter out the lesser scored moves, movesets are usually 40+
                                    Move chosenMove = acceptableMoves[chosenMoveIndex]; // Got the move
                                    // Check if it was part of a set item or not
                                    if (!monData.Moveset.Contains(chosenMove)) // In this case it was caused by a set item
                                    {
                                        mon.SetItem = trainer.SetItems.Keys.First(i => i.AddedMoves.Contains(chosenMove));
                                        GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, mon.SetItem, -1, true); // Remove 1 charge of set item from trainer
                                                                                                                           // Add all the item moves to the dict
                                        mon.ChosenMoveset = [.. mon.ChosenMoveset.Union(mon.SetItem.AddedMoves)];
                                    }
                                    else
                                    {
                                        mon.ChosenMoveset.Add(chosenMove); // Apply to mon, all good here
                                    }
                                }
                                else
                                {
                                    mon.ChosenMoveset.Add(null); // Add null move (hard switch)
                                }
                            }
                            else
                            {
                                state = MonBuildState.CHOOSING_MOD_ITEM;
                            }
                            if (mon.ChosenMoveset.Count >= NUMBER_OF_MOVES_PER_MON) // If finished, go next state
                            {
                                state = MonBuildState.CHOOSING_MOD_ITEM;
                            }
                            break;
                        // Item cases are slightly different because no item is valid option either if no item good or no other better ones
                        case MonBuildState.CHOOSING_MOD_ITEM:
                            if (mon.ModItem == null && trainer.AutoModItem) // If mon already has battle item or trainer doesnt allow the auto item, then finished
                            {
                                int modItemCount = trainer.ModItems.Values.Sum(); // How many items does the trainer have total?
                                double initialItemScore = WEIGHT_PER_ITEM * modItemCount;
                                initialItemScore = Math.Clamp(initialItemScore, WEIGHT_PER_ITEM, 1);
                                // If there's a constraint that requires specific mod items, need to filter list further
                                bool modItemMandatory = false;
                                List<Item> possibleModItems = [.. trainer.ModItems.Keys.OrderBy(m => m.Name)];
                                List<double> possibleModItemsScores = [.. Enumerable.Repeat(initialItemScore, possibleModItems.Count)];
                                List<Item> acceptableModItems = new List<Item>();
                                List<double> acceptableModItemsScores = new List<double>();
                                foreach (Constraint constraint in ongoingConstraints) // Quick check of which constraints an ability could solve
                                {
                                    // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                    for (int i = 0; i < possibleModItems.Count; i++)
                                    {
                                        Item potentialModItem = possibleModItems[i];
                                        if (constraint.SatisfiedByItem(potentialModItem))
                                        {
                                            acceptableModItems.Add(potentialModItem);
                                            acceptableModItemsScores.Add(possibleModItemsScores[i]);
                                            modItemMandatory = true; // Usage of a mod item becomes mandatory, and no item is no option
                                        }
                                    }
                                }
                                if (acceptableModItems.Count == 0)
                                {
                                    acceptableModItems = possibleModItems; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    acceptableModItemsScores = possibleModItemsScores;
                                }
                                // And also, a secret "no item" that is only equipped if makes sense, competes against all other items but only if allowed
                                Item noItem = new Item()
                                {
                                    Name = "No item",
                                    Flags = [ItemFlag.NO_ITEM]
                                };
                                if (!modItemMandatory)
                                {
                                    acceptableModItems.Add(noItem);
                                    acceptableModItemsScores.Add(1); // Score of 1 means feasible and even encouraged in some odd cases, also helps to compare against bad mod items
                                }
                                // Now the sorting itself
                                List<Item> validModItems = new List<Item>();
                                List<double> modItemScores = new List<double>();
                                for (int i = 0; i < acceptableModItems.Count; i++) // Check each of the trainers battle items
                                {
                                    // Score battle item according to context. First, check if disableds
                                    Item modItem = acceptableModItems[i];
                                    double score = GetItemWeight(modItem, ElementType.MOD_ITEM, mon, monCtx, buildCtx);
                                    if (modItemMandatory) score = Math.Clamp(score, 0.01, double.PositiveInfinity); // If bad but need it (?!?), just give it a low score
                                    score *= acceptableModItemsScores[i];
                                    if (score > 0)
                                    {
                                        modItemScores.Add(score);
                                        validModItems.Add(modItem);
                                    }
                                }
                                if (validModItems.Count > 0) // Choose between reasonable items
                                {
                                    int chosenItemIndex = RandomIndexOfWeights(modItemScores, monRng, 1.2); // Use power of 1.2 nudge toward good
                                    Item chosenModItem = validModItems[chosenItemIndex]; // Got the item
                                    if (chosenModItem != noItem) // Check if winner was actually an item
                                    {
                                        mon.ModItem = chosenModItem; // Apply to mon, all good here
                                        GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, chosenModItem, -1, true); // Remove 1 charge of battle item from trainer
                                    }
                                }
                            }
                            state = MonBuildState.CHOOSING_BATTLE_ITEM; // Regardless I'm done
                            break;
                        case MonBuildState.CHOOSING_BATTLE_ITEM:
                            if (mon.BattleItem == null && trainer.AutoBattleItem) // If mon already has battle item or trainer doesnt allow the auto item, then finished
                            {
                                int battleItemCount = trainer.BattleItems.Values.Sum(); // How many items does the trainer have total?
                                double initialItemScore = WEIGHT_PER_ITEM * battleItemCount;
                                initialItemScore = Math.Clamp(initialItemScore, WEIGHT_PER_ITEM, 1);
                                // If there's a constraint that requires specific mod items, need to filter list further
                                bool battleItemMandatory = false;
                                List<Item> possibleBattleItems = [.. trainer.BattleItems.Keys.OrderBy(b => b.Name)];
                                List<double> possibleBattleItemsScores = [.. Enumerable.Repeat(initialItemScore, possibleBattleItems.Count)];
                                List<Item> acceptableBattleItems = new List<Item>();
                                List<double> acceptableBattleItemsScores = new List<double>();
                                foreach (Constraint constraint in ongoingConstraints) // Quick check of which constraints an ability could solve
                                {
                                    // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                    for (int i = 0; i < possibleBattleItems.Count; i++)
                                    {
                                        Item potentialBattleItem = possibleBattleItems[i];
                                        if (constraint.SatisfiedByItem(potentialBattleItem))
                                        {
                                            acceptableBattleItems.Add(potentialBattleItem);
                                            acceptableBattleItemsScores.Add(possibleBattleItemsScores[i]);
                                            battleItemMandatory = true; // Usage of a mod item becomes mandatory, and no item is no option
                                        }
                                    }
                                }
                                if (acceptableBattleItems.Count == 0)
                                {
                                    acceptableBattleItems = possibleBattleItems; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    acceptableBattleItemsScores = possibleBattleItemsScores;
                                }
                                // And also, a secret "no item" that is only equipped if makes sense, competes against all other items but only if allowed
                                Item noItem = new Item()
                                {
                                    Name = "No item",
                                    Flags = [ItemFlag.NO_ITEM]
                                };
                                if (!battleItemMandatory)
                                {
                                    acceptableBattleItems.Add(noItem);
                                    acceptableBattleItemsScores.Add(1); // Score of 1 means feasible and even encouraged in some odd cases
                                }
                                // Now the sorting itself
                                List<Item> validBattleItems = new List<Item>();
                                List<double> battleItemScores = new List<double>();
                                for (int i = 0; i < acceptableBattleItems.Count; i++) // Check each of the trainers battle items
                                {
                                    // Score battle item according to context. First, check if disableds
                                    Item battleItem = acceptableBattleItems[i];
                                    double score = GetItemWeight(battleItem, ElementType.BATTLE_ITEM, mon, monCtx, buildCtx);
                                    if (battleItemMandatory) score = Math.Clamp(score, 0.01, double.PositiveInfinity); // If bad but need it (?!?), just give it a low score
                                    score *= acceptableBattleItemsScores[i];
                                    if (score > 0)
                                    {
                                        battleItemScores.Add(score);
                                        validBattleItems.Add(battleItem);
                                    }
                                }
                                if (validBattleItems.Count > 0) // Choose between reasonable battle items
                                {
                                    int chosenItemIndex = RandomIndexOfWeights(battleItemScores, monRng, 1.2); // Use power of 1.2 nudge toward good
                                    Item chosenBattleItem = validBattleItems[chosenItemIndex]; // Got the item
                                    if (chosenBattleItem != noItem) // Check if winner was actually an item
                                    {
                                        mon.BattleItem = chosenBattleItem; // Apply to mon, all good here
                                        GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, chosenBattleItem, -1, true); // Remove 1 charge of battle item from trainer
                                    }
                                }
                            }
                            state = MonBuildState.DONE; // Regardless I'm done
                            break;
                        default:
                            throw new NotImplementedException("State machine broke");
                    }
                }
                Console.WriteLine($"Chosen set for mon ({teamSeed}-{monSeed}): {mon.PrintSet()}");
                Console.WriteLine("Accept? Y, n (redo seed for debug)");
                string monAccepted = Console.ReadLine();
                // Depending on the choice, a new seed is chosen and the mon is redone
                if (monAccepted.ToLower() == "n")
                {
                    // Ok, need to restore mon then
                    if (mon.SetItem != null && !mon.SetItemChosen) // If mon needs to return set item
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, mon.SetItem, 1); // Re-adds item
                        mon.SetItem = null;
                    }
                    if (mon.ModItem != null && !mon.ModItemChosen) // If mon needs to return mod item
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, mon.ModItem, 1); // Re-adds item
                        mon.ModItem = null;
                    }
                    if (mon.BattleItem != null && !mon.BattleItemChosen) // If mon needs to return battle item
                    {
                        GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, mon.BattleItem, 1); // Re-adds item
                        mon.BattleItem = null;
                    }
                    // Also redo the mon ofc
                    monIndex--; // Horrible but makes the loop go again
                }
                else
                {
                    // All good, do next seed then
                    monSeed = teamRng.Next();
                    // Also, this mon may modify the team context
                    PokemonBuildContext monCtx = ObtainPokemonSetContext(mon, buildCtx); // Obtain current Pokemon mods and score and such
                    buildCtx.CurrentTeamArchetypes.UnionWith(monCtx.AdditionalArchetypes); // New archetypes found here are added into all team's archetypes
                    buildCtx.CurrentWeather = monCtx.CurrentWeather; // Something may have changed the current weather
                    buildCtx.CurrentTerrain = monCtx.CurrentTerrain; // Something may have changed the current terrain
                }
            }
            // If team accepted, then get all mons ctx one last time, and apply the necessary things to them
            foreach (TrainerPokemon mon in trainer.BattleTeam)
            {
                PokemonBuildContext monCtx = new PokemonBuildContext(); // Get all the mons context data
                // In here, a bit of logic. What if the trainer chose a mod item that improves logic (e.g. dawn stone) but has not chosen a set?
                // Then everything is randomized, and this means the mod item si just luck based, so I'll try to atleast use a first-slot move that makes sense
                // This will reorder moves a bunch and notify
                if (monCtx.MonLogic == PokemonLogic.FIRST_ONCE && (mon.SetItem == null || !mon.SetItemChosen)) // Ensure logic item is there without a conscious set item choice
                {
                    // Need to find best move candidate for swap
                    List<Move> goodFirstMoves = [.. mon.ChosenMoveset.Where(m => ExtractMoveFlags(m, monCtx).Contains(EffectFlag.GOOD_FIRST_MOVE))]; // Obtain moves that are good first move candidate
                    if (goodFirstMoves.Count > 0) // One of these will be chosen first, at random I guess
                    {
                        Console.WriteLine($"The following moves [{string.Join(',', goodFirstMoves.Select(m => m.Name))}] are all good candidates as a first move for the FIRST_ONCE logic");
                        Move firstMove = goodFirstMoves[GeneralUtilities.GetRandomNumber(goodFirstMoves.Count)]; // Doesn't use the seeded RNG but whatever, this just reorders and doesn't technically use the set
                        int firstMoveIndex = mon.ChosenMoveset.IndexOf(firstMove);
                        (mon.ChosenMoveset[0], mon.ChosenMoveset[firstMoveIndex]) = (mon.ChosenMoveset[firstMoveIndex], mon.ChosenMoveset[0]); // Swap
                        Console.WriteLine($"The first move for this set has been chosen as {firstMove} at random.");
                    }
                    else
                    {
                        Console.WriteLine($"No move from [{string.Join(',', mon.ChosenMoveset.Select(m => m.Name))}] is a good candidate for first move for the FIRST_ONCE logic");
                    }
                }
                // Copy all the relevant build (mod?) stats too
                mon.TeraType = monCtx.TeraType;
                mon.Nature = monCtx.Nature;
                mon.Logic = monCtx.MonLogic;
                for (int i = 0; i < 6; i++)
                {
                    mon.Evs[i] = monCtx.Evs[i];
                }
                mon.ShinyOverride = monCtx.ShinyOverride;
                mon.Level = (int)(monCtx.LevelMultiplier * 100); // Resulting lvl, check really closely if it rounds down but it shouldn't
                mon.DefaultStatus = monCtx.DefaultStatus; // Sets the status the pokemon will have in normal conditions
            }
        }
        /// <summary>
        /// Returns an index of a list. The list contains the weights so that chance is weighted towards bigger indices. No need to be normalized
        /// </summary>
        /// <param name="weights">List of weight</param>
        /// <param name="power">Optional power to elevate weights, to skew the decision towards higher/lower weights</param>
        /// <returns>A random index within the list. List is modified by the power</returns>
        static int RandomIndexOfWeights(List<double> weights, Random rng, double power = 1.0f)
        {
            double totalSum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                double weight = Math.Pow(weights[i], power);
                weights[i] = weight;
                totalSum += weight;
            }
            // Once processed, I'll get a random number, uniform within sum
            double hit = totalSum * rng.NextDouble();
            // Finally, search for which element is the winner, one by one
            totalSum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] + totalSum >= hit)
                {
                    return i;
                }
                totalSum += weights[i];
            }
            throw new Exception("Impossible chance reached");
        }
    }
}
