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
        /// Sets the movesets of all mons of a trainer's battle team
        /// </summary>
        /// <param name="trainer">Which trainer to build</param>
        /// <param name="buildCtx">Context containing other team build things that may be important</param>
        public static void BuildTeam(Trainer trainer, TeamBuildContext buildCtx)
        {
            int teamSeed = IndymonUtilities.GetRandomNumber(int.MaxValue);
            bool teamAccepted = false, seedAccepted = true;
            while (!teamAccepted)
            {
                if (!seedAccepted) // Change the seed I guess
                {
                    teamSeed = IndymonUtilities.GetRandomNumber(int.MaxValue);
                }
                Random rng = new Random(teamSeed); // Not ideal but lets us retry with same value
                // Will build a set for each mon
                for (int monIndex = 0; monIndex < trainer.BattleTeam.Count; monIndex++)
                {
                    TrainerPokemon mon = trainer.BattleTeam[monIndex];
                    // Also get the mons ability and moveset here
                    Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
                    // First thing is to check if mon has set item equipped, if so, add the move/ability already
                    Ability setItemAbility = GetSetItemAbility(mon.SetItem);
                    if (setItemAbility != null)
                    {
                        mon.ChosenAbility = setItemAbility;
                    }
                    Move setItemMove = GetSetItemMove(mon.SetItem);
                    if (setItemMove != null)
                    {
                        mon.ChosenMoveset = [setItemMove];
                    }
                    // Now that the initial set is assembled, evaluate with a state machine
                    MonBuildState state = MonBuildState.CHOOSING_ABILITY; // Begin with ability
                    while (state != MonBuildState.DONE)
                    {
                        PokemonBuildInfo monCtx = ObtainPokemonSetContext(mon, buildCtx); // Obtain current Pokemon mods and score and such
                        buildCtx.CurrentTeamArchetypes.UnionWith(monCtx.AdditionalArchetypes); // Archetypes found here are added into all team's archetypes
                                                                                               // Monctx contains all the ongoing constraints, need only the ones which haven't been fulfilled yet
                        TeamBuildConstraints ongoingConstraints = new TeamBuildConstraints();
                        foreach (List<(ElementType, string)> constraintSet in monCtx.AdditionalConstraints.AllConstraints) // filter constraint set out
                        {
                            bool constraintSetSatisfied = false;
                            foreach ((ElementType, string) constraintCheck in constraintSet) // Check if this OR combination is satisfied by anything, returns at first valid one
                            {
                                constraintSetSatisfied |= ValidateComplexMonProperty(mon, monCtx, constraintCheck.Item1, constraintCheck.Item2);
                                if (constraintSetSatisfied) break;
                            }
                            if (!constraintSetSatisfied) ongoingConstraints.AllConstraints.Add(constraintSet); // Add for later
                        }
                        // Got constraint list finally! Now just do state machine
                        switch (state)
                        {
                            case MonBuildState.CHOOSING_ABILITY:
                                // Then, define the mon's ability (unless already defined)
                                if (mon.ChosenAbility == null) // Mon needs an ability
                                {
                                    List<Ability> possibleAbilities = [.. monData.Abilities]; // All possible abilities
                                    Dictionary<Ability, string> setItemAbilityLookup = new Dictionary<Ability, string>();
                                    if (trainer.AutoSetItem) // If I can equip other set items AND set item provides useful abilities, I'll add them too
                                    {
                                        foreach (string setItem in trainer.SetItems.Keys) // Need to check which set items are available
                                        {
                                            if (CanEquipSetItem(mon, setItem))
                                            {
                                                setItemAbility = GetSetItemAbility(setItem);
                                                if (setItemAbility != null && !possibleAbilities.Contains(setItemAbility)) // if available and not included yet, I add to possibilities
                                                {
                                                    possibleAbilities.Add(setItemAbility);
                                                    setItemAbilityLookup.Add(setItemAbility, setItem);
                                                }
                                            }
                                        }
                                    }
                                    // If there's a constraint that requires specific abilities, need to filter list further
                                    List<Ability> acceptableAbilities = new List<Ability>();
                                    foreach (List<(ElementType, string)> constraintSet in monCtx.AdditionalConstraints.AllConstraints) // Quick check of which constraints an ability could solve
                                    {
                                        // This has the effect where if an ability fills many constraints, it is added many times but that may be good? (Multiply chances I guess....)
                                        foreach (Ability constraintFillingAbility in possibleAbilities)
                                        {
                                            bool abilityAccepted = false;
                                            foreach ((ElementType, string) constraintCheck in constraintSet) // Check if this OR combination is satisfied by anything, returns at first valid one
                                            {
                                                abilityAccepted |= ValidateBasicAbilityProperty(constraintFillingAbility, constraintCheck.Item1, constraintCheck.Item2); // Check if ability ok
                                                if (abilityAccepted)
                                                {
                                                    acceptableAbilities.Add(constraintFillingAbility); // This is an ability I can consider then!
                                                    break;
                                                }
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
                                        else // Otherwise, 1 is added
                                        {
                                            abilityScores.Add(1);
                                        }
                                    } // Gottem scores
                                    int chosenAbilityIndex = RandomIndexOfWeights(abilityScores, rng);
                                    Ability chosenAbility = acceptableAbilities[chosenAbilityIndex]; // Got the ability
                                    mon.ChosenAbility = chosenAbility; // Apply to mon, all good here
                                    if (setItemAbilityLookup.ContainsKey(chosenAbility)) // If this was found through set item, need to equip set item
                                    {
                                        mon.SetItem = setItemAbilityLookup[chosenAbility];
                                        IndymonUtilities.AddtemToCountDictionary(trainer.SetItems, mon.SetItem, -1, true); // Remove 1 charge of set item from trainer
                                    }
                                }
                                if (mon.ChosenAbility != null) // After this step, this should be true always and move on!
                                {
                                    state = MonBuildState.CHOOSING_MOVES;
                                }
                                break;
                            default:
                                throw new NotImplementedException("State machine broke");
                        }
                    }
                }
                // Here I'll print team and ask, but later not now TODO TODO TODO TODO
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
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > hit)
                {
                    return i - 1;
                }
            }
            throw new Exception("Impossible chance reached");
        }
    }
}
