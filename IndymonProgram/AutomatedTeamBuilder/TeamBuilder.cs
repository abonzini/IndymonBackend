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
            bool teamAccepted = false, seedAccepted = true;
            while (!teamAccepted)
            {
                if (!seedAccepted) // Change the seed I guess
                {
                    teamSeed = GeneralUtilities.GetRandomNumber(int.MaxValue);
                }
                Random teamRng = new Random(teamSeed); // Not ideal but lets us retry with same value
                int monSeed = teamRng.Next(seed); // Get the next seed of mon
                // Will build a set for each mon
                for (int monIndex = 0; monIndex < trainer.BattleTeam.Count; monIndex++)
                {
                    // Init stuff
                    Random monRng = new Random(seed); // Will use this for the mon, in order to be able to reuse seed
                    TrainerPokemon mon = trainer.BattleTeam[monIndex];
                    bool monHadSetItem = mon.SetItem != null;
                    bool monHadModItem = mon.ModItem != null;
                    bool monHadBattleItem = mon.ModItem != null;
                    // Also get the mons ability and moveset here
                    Pokemon monData;
                    if (mon.Species.ToLower().Contains("unown")) // Weird case because the species is always unown even if many aesthetic formes that are not in the dex
                    {
                        monData = MechanicsDataContainers.GlobalMechanicsData.Dex["Unown"];
                    }
                    else
                    {
                        monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
                    }
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
                        PokemonBuildInfo monCtx = new PokemonBuildInfo();
                        if (buildCtx.smartTeamBuild) // Build info needs to do scoring stuff if in a smart build
                        {
                            monCtx = ObtainPokemonSetContext(mon, buildCtx); // Obtain current Pokemon mods and score and such
                        }
                        buildCtx.CurrentTeamArchetypes.UnionWith(monCtx.AdditionalArchetypes); // New archetypes found here are added into all team's archetypes
                        buildCtx.CurrentWeather = monCtx.CurrentWeather; // Something may have changed the current weather
                        buildCtx.CurrentTerrain = monCtx.CurrentTerrain; // Something may have changed the current terrain
                        // Monctx contains all the ongoing constraints, need only the ones which haven't been fulfilled yet
                        List<Constraint> ongoingConstraints = new List<Constraint>();
                        foreach (Constraint constraint in monCtx.AdditionalConstraints) // filter constraint set out
                        {
                            // If constraint not yet satisfied, add to list to requirements
                            if (!constraint.SatisfiedByMon(mon, false)) ongoingConstraints.Add(constraint);
                        }
                        // Got constraint list finally! Now just do state machine
                        switch (state)
                        {
                            case MonBuildState.CHOOSING_ABILITY:
                                // Then, define the mon's ability (unless already defined)
                                if (mon.ChosenAbility == null) // Mon needs an ability
                                {
                                    List<Ability> possibleAbilities = [.. monData.Abilities]; // All possible abilities
                                    if (trainer.AutoSetItem && mon.SetItem != null) // If I can equip other set items AND set item provides useful abilities, I'll add them too
                                    {
                                        foreach (SetItem setItem in trainer.SetItems.Keys) // Need to check which set items are available
                                        {
                                            if (setItem.CanEquip(mon))
                                            {
                                                if (setItem.AddedAbility != null && !possibleAbilities.Contains(setItem.AddedAbility)) // if available and not included yet, I add to possibilities
                                                {
                                                    possibleAbilities.Add(setItem.AddedAbility);
                                                }
                                            }
                                        }
                                    }
                                    // If there's a constraint that requires specific abilities, need to filter list further
                                    List<Ability> acceptableAbilities = new List<Ability>();
                                    foreach (Constraint constraint in monCtx.AdditionalConstraints) // Quick check of which constraints an ability could solve
                                    {
                                        // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                        foreach (Ability constraintFillingAbility in possibleAbilities)
                                        {
                                            if (constraint.SatisfiedByAbility(constraintFillingAbility))
                                            {
                                                acceptableAbilities.Add(constraintFillingAbility); // This is an ability I can consider then!
                                            }
                                        }
                                    }
                                    if (acceptableAbilities.Count == 0) acceptableAbilities = possibleAbilities; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    // Finally, score the abilities, create an array with same count with scores, choose an index, choose ability
                                    List<double> abilityScores = new List<double>();
                                    foreach (Ability nextAbility in acceptableAbilities)
                                    {
                                        if (buildCtx.smartTeamBuild) // If smart, abilities are weighted according to how useful they are
                                        {
                                            abilityScores.Add(GetAbilityWeight(nextAbility, mon, monCtx, buildCtx, monIndex == 0));
                                        }
                                        else // Otherwise, 1 is added unless banned
                                        {
                                            if (nextAbility.Flags.Contains(EffectFlag.BANNED))
                                            {
                                                abilityScores.Add(0);
                                            }
                                            else
                                            {
                                                abilityScores.Add(1);
                                            }
                                        }
                                    } // Gottem scores
                                    int chosenAbilityIndex = RandomIndexOfWeights(abilityScores, monRng);
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
                                    List<Move> possibleMoves = [.. monData.Moveset]; // All possible moves
                                    if (mon.Species.ToLower().Contains("unown")) // And then again, weird mechanic because I can only allow the moves that start with the unown letter
                                    {
                                        char letter = mon.Species.ToLower().Last();
                                        possibleMoves = [.. possibleMoves.Where(m => m.Name.StartsWith(letter))]; // Additional move filter
                                    }
                                    if (trainer.AutoSetItem && mon.SetItem == null) // If I can equip other set items AND set item provides useful moves, I'll add them too
                                    {
                                        foreach (SetItem setItem in trainer.SetItems.Keys) // Need to check which set items are available
                                        {
                                            if (setItem.CanEquip(mon))
                                            {
                                                if (setItem.AddedMoves.Count > 0 && possibleMoves.Except(setItem.AddedMoves).Any()) // if available and some move would be new, add to option
                                                {
                                                    possibleMoves = [.. possibleMoves.Union(setItem.AddedMoves)]; // Adds the missing moves to list
                                                }
                                            }
                                        }
                                    }
                                    // Remove the ones I already have!
                                    possibleMoves = [.. possibleMoves.Except(mon.ChosenMoveset)];
                                    // If there's a constraint that requires specific moves, need to filter list further
                                    List<Move> acceptableMoves = new List<Move>();
                                    foreach (Constraint constraint in monCtx.AdditionalConstraints) // Quick check of which constraints a move could solve
                                    {
                                        // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                        foreach (Move constraintFillingMove in possibleMoves)
                                        {
                                            if (constraint.SatisfiedByMove(constraintFillingMove))
                                            {
                                                acceptableMoves.Add(constraintFillingMove); // This is a move I can consider then!
                                            }
                                        }
                                    }
                                    if (acceptableMoves.Count == 0)
                                    {
                                        // Ok then, no move is mandatory, will choose if I want to pivot or what
                                        const double BASE_PIVOT_CHANCE = 0.1; // Pivot has a hard chance of 10%
                                        if (!monCtx.WeightMods.TryGetValue((ElementType.EFFECT_FLAGS, EffectFlag.PIVOT.ToString()), out double pivotModdedChance)) // Check if something else modifies this
                                        {
                                            pivotModdedChance = 1;
                                        }
                                        pivotModdedChance *= BASE_PIVOT_CHANCE;
                                        if (pivotModdedChance > monRng.NextDouble()) // Roll this chance
                                        {
                                            // Forced pivot, choose only pivot moves
                                            acceptableMoves = [.. possibleMoves.Where(m => m.Flags.Contains(EffectFlag.PIVOT))];
                                        }
                                        else // No forced pivot, choose any move
                                        {
                                            acceptableMoves = possibleMoves; // If no move fills constraint (or no constraint) then just use all, yolo.
                                        }
                                    }
                                    // Score the moves, create an array with same count with scores, choose an index, choose move
                                    if (acceptableMoves.Count > 0) // Theres moves out of a subset to choose from
                                    {
                                        List<double> moveScores = new List<double>();
                                        foreach (Move nextMove in acceptableMoves)
                                        {
                                            if (buildCtx.smartTeamBuild) // If smart, abilities are weighted according to how useful they are
                                            {
                                                moveScores.Add(GetMoveWeight(nextMove, mon, monCtx, buildCtx, monIndex == 0));
                                            }
                                            else // Otherwise, 1 is added unless banned
                                            {
                                                if (nextMove.Flags.Contains(EffectFlag.BANNED)) // This doesnt deal with moves that are added "banned" as a special mod but this hasn't happened still 
                                                {
                                                    moveScores.Add(0);
                                                }
                                                else
                                                {
                                                    moveScores.Add(1);
                                                }
                                            }
                                        } // Gottem scores
                                        int chosenMoveIndex = RandomIndexOfWeights(moveScores, monRng);
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
                                break;
                            // Item cases are slightly different because no item is valid option either if no item good or no other better ones
                            case MonBuildState.CHOOSING_MOD_ITEM:
                                if (mon.ModItem == null && trainer.AutoModItem) // If mon already has battle item or trainer doesnt allow the auto item, then finished
                                {
                                    // If there's a constraint that requires specific mod items, need to filter list further
                                    bool modItemMandatory = false;
                                    List<Item> possibleModItems = [.. trainer.ModItems.Keys];
                                    List<Item> acceptableModItems = new List<Item>();
                                    foreach (Constraint constraint in monCtx.AdditionalConstraints) // Quick check of which constraints an ability could solve
                                    {
                                        // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                        foreach (Item constraintFillingModItem in possibleModItems)
                                        {
                                            if (constraint.SatisfiedByItem(constraintFillingModItem))
                                            {
                                                acceptableModItems.Add(constraintFillingModItem); // This is an ability I can consider then!
                                                modItemMandatory = true; // Usage of mod item becomes mandatory
                                            }
                                        }
                                    }
                                    if (acceptableModItems.Count == 0) acceptableModItems = possibleModItems; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    // Now the sorting itself
                                    List<Item> validModItems = new List<Item>();
                                    List<double> modItemScores = new List<double>();
                                    foreach (Item modItem in acceptableModItems) // Check each of the trainers mod items
                                    {
                                        // Fortunately mod items are not scored so just need to calc improvements
                                        double score = 1;
                                        mon.ModItem = modItem; // First, equip this item to mon
                                        PokemonBuildInfo newCtx = ObtainPokemonSetContext(mon, buildCtx); // Check the new context
                                        double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
                                        double defImprovement = newCtx.DefenseScore / monCtx.DefenseScore;
                                        double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
                                        // If needs an improvement, will be accepted as long as some of the improvements succeeds
                                        int nImprovChecks = 0;
                                        int nImproveFails = 0;
                                        if (modItem.Flags.Contains(ItemFlag.REQUIRES_OFF_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (modItem.Flags.Contains(ItemFlag.REQUIRES_DEF_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (modItem.Flags.Contains(ItemFlag.REQUIRES_SPEED_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (!modItemMandatory && nImproveFails == nImprovChecks)
                                        {
                                            continue; // If all checks failed, item not good, skip it
                                        }
                                        score *= dmgImprovement * defImprovement * speedImprovement; // Otherwise multiply all utilities gain, give or remove utility from final set!
                                        mon.ModItem = null; // Remove item ofc
                                        if (score > 0)
                                        {
                                            validModItems.Add(modItem);
                                            modItemScores.Add(score);
                                        }
                                    }
                                    if (validModItems.Count > 0) // Choose between reasonable mod items
                                    {
                                        int chosenItemIndex = RandomIndexOfWeights(modItemScores, monRng);
                                        Item chosenModitem = validModItems[chosenItemIndex]; // Got the item
                                        mon.ModItem = chosenModitem; // Apply to mon, all good here
                                        GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, chosenModitem, -1, true); // Remove 1 charge of mod item from trainer
                                    }
                                }
                                state = MonBuildState.CHOOSING_BATTLE_ITEM; // Regardless I'm done
                                break;
                            case MonBuildState.CHOOSING_BATTLE_ITEM:
                                if (mon.BattleItem == null && trainer.AutoBattleItem) // If mon already has battle item or trainer doesnt allow the auto item, then finished
                                {
                                    // If there's a constraint that requires specific mod items, need to filter list further
                                    bool battleItemMandatory = false;
                                    List<Item> possibleBattleItems = [.. trainer.BattleItems.Keys];
                                    List<Item> acceptableBattleItems = new List<Item>();
                                    foreach (Constraint constraint in monCtx.AdditionalConstraints) // Quick check of which constraints an ability could solve
                                    {
                                        // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                        foreach (Item constraintFillingBattleItem in possibleBattleItems)
                                        {
                                            if (constraint.SatisfiedByItem(constraintFillingBattleItem))
                                            {
                                                acceptableBattleItems.Add(constraintFillingBattleItem); // This is an ability I can consider then!
                                                battleItemMandatory = true; // Usage of mod item becomes mandatory
                                            }
                                        }
                                    }
                                    if (acceptableBattleItems.Count == 0) acceptableBattleItems = possibleBattleItems; // If no ability fills constraint (or no constraint) then just use all, yolo.
                                    // Now the sorting itself
                                    List<Item> validBattleItems = new List<Item>();
                                    List<double> battleItemScores = new List<double>();
                                    // And also, a secret "no item" that is only equipped if makes sense
                                    Item noItem = new Item()
                                    {
                                        Name = "No item",
                                        Flags = [ItemFlag.REQUIRES_OFF_INCREASE, ItemFlag.NO_ITEM]
                                    };
                                    if (!battleItemMandatory)
                                    {
                                        acceptableBattleItems.Add(noItem);
                                    }
                                    foreach (Item battleItem in acceptableBattleItems) // Check each of the trainers battle items
                                    {
                                        // Score battle item according to context. First, check if disableds
                                        double score = 1;
                                        double aux;
                                        (ElementType, string) battleItemNameTag = (ElementType.BATTLE_ITEM, battleItem.Name);
                                        if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(battleItemNameTag)) // If item is disabled but not re-enabled, skip it
                                        {
                                            if (monCtx.EnabledOptions.TryGetValue(battleItemNameTag, out aux))
                                            {
                                                score *= aux;
                                            }
                                            else
                                            {
                                                continue; // This item is no good
                                            }
                                        }
                                        foreach (ItemFlag flag in battleItem.Flags)
                                        {
                                            (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                                            if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(flagTag)) // If item is disabled but not re-enabled, skip it
                                            {
                                                if (monCtx.EnabledOptions.TryGetValue(flagTag, out aux))
                                                {
                                                    score *= aux;
                                                }
                                                else
                                                {
                                                    continue; // This item is no good
                                                }
                                            }
                                        }
                                        // Then initial weights
                                        if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(battleItemNameTag, out aux))
                                        {
                                            score *= aux;
                                        }
                                        foreach (ItemFlag flag in battleItem.Flags)
                                        {
                                            (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                                            if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(flagTag, out aux))
                                            {
                                                score *= aux;
                                            }
                                        }
                                        // Then weight mods
                                        if (monCtx.WeightMods.TryGetValue(battleItemNameTag, out aux))
                                        {
                                            score *= aux;
                                        }
                                        foreach (ItemFlag flag in battleItem.Flags)
                                        {
                                            (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                                            if (monCtx.WeightMods.TryGetValue(flagTag, out aux))
                                            {
                                                score *= aux;
                                            }
                                        }
                                        // And then additives
                                        if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(battleItemNameTag, out aux))
                                        {
                                            score += aux;
                                        }
                                        foreach (ItemFlag flag in battleItem.Flags)
                                        {
                                            (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                                            if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(flagTag, out aux))
                                            {
                                                score += aux;
                                            }
                                        }
                                        // Now the improvement based things
                                        mon.BattleItem = battleItem; // First, equip this item to mon
                                        PokemonBuildInfo newCtx = ObtainPokemonSetContext(mon, buildCtx); // Check the new context
                                        double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
                                        double defImprovement = newCtx.DefenseScore / monCtx.DefenseScore;
                                        double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
                                        // If needs an improvement, will be accepted as long as some of the improvements succeeds
                                        int nImprovChecks = 0;
                                        int nImproveFails = 0;
                                        if (battleItem.Flags.Contains(ItemFlag.REQUIRES_OFF_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (battleItem.Flags.Contains(ItemFlag.REQUIRES_DEF_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (battleItem.Flags.Contains(ItemFlag.REQUIRES_SPEED_INCREASE))
                                        {
                                            nImprovChecks++;
                                            if (dmgImprovement < 1.1) nImproveFails++;
                                        }
                                        if (!battleItemMandatory && nImproveFails == nImprovChecks)
                                        {
                                            continue;
                                        }
                                        score *= dmgImprovement * defImprovement * speedImprovement; // Then multiply all utilities gain, give or remove utility from final set!
                                        if (battleItem.Flags.Contains(ItemFlag.BULKY)) // Healing items are scored on whether they can actually make sense on the mon
                                        {
                                            score *= newCtx.Survivability;
                                        }
                                        mon.BattleItem = null; // Remove item ofc
                                        if (score > 0)
                                        {
                                            battleItemScores.Add(score);
                                            validBattleItems.Add(battleItem);
                                        }
                                    }
                                    if (validBattleItems.Count > 0) // Choose between reasonable battle items
                                    {
                                        int chosenItemIndex = RandomIndexOfWeights(battleItemScores, monRng);
                                        Item chosenBattleitem = validBattleItems[chosenItemIndex]; // Got the item
                                        if (chosenBattleitem != noItem) // Check if winner was actually an item
                                        {
                                            mon.ModItem = chosenBattleitem; // Apply to mon, all good here
                                            GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, chosenBattleitem, -1, true); // Remove 1 charge of battle item from trainer
                                        }
                                    }
                                }
                                state = MonBuildState.DONE; // Regardless I'm done
                                break;
                            default:
                                throw new NotImplementedException("State machine broke");
                        }
                    }
                    Console.WriteLine($"Chosen set for mon ({teamSeed}): {mon.PrintSet()}");
                    Console.WriteLine("Accept? Y, n (redo seed for debug)");
                    string monAccepted = Console.ReadLine();
                    // Depending on the choice, a new seed is chosen and the mon is redone
                    if (monAccepted.ToLower() == "n")
                    {
                        // Ok, need to restore mon then
                        if (mon.SetItem != null && !monHadSetItem) // If mon needs to return set item
                        {
                            GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, mon.SetItem, 1); // Re-adds item
                            mon.SetItem = null;
                        }
                        if (mon.ModItem != null && !monHadModItem) // If mon needs to return mod item
                        {
                            GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, mon.ModItem, 1); // Re-adds item
                            mon.ModItem = null;
                        }
                        if (mon.BattleItem != null && !monHadBattleItem) // If mon needs to return battle item
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
                        monSeed = teamRng.Next(seed);
                    }
                }
            }
            // If team accepted, then get all mons ctx one last time, and apply the necessary things to them
            foreach (TrainerPokemon mon in trainer.BattleTeam)
            {
                PokemonBuildInfo monCtx = new PokemonBuildInfo(); // Get all the mons context data
                // Copy all the relevant build (mod?) stats too
                mon.TeraType = monCtx.TeraType;
                mon.Nature = monCtx.Nature;
                for (int i = 0; i < 6; i++)
                {
                    mon.Evs[i] = monCtx.Evs[i];
                }
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
            }
            throw new Exception("Impossible chance reached");
        }
    }
}
