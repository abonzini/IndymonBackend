using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        const double MIN_ABILITY_SCORE = 0.0001; // Abilities can't have a score of 0 because there's too few and in some cases a single one, so I make the score very small but not 0 so it can technically be chosen
        /// <summary>
        /// Sets the movesets of all mons of a trainer's battle team
        /// </summary>
        /// <param name="trainer">Which trainer to build</param>
        /// <param name="buildCtx">Context containing other team build things that may be important</param>
        public static void BuildTeam(Trainer trainer, TeamBuildContext buildCtx)
        {
            // Will build a set for each mon
            for (int monIndex = 0; monIndex < trainer.BattleTeam.Count; monIndex++)
            {
                TrainerPokemon mon = trainer.BattleTeam[monIndex];
                PokemonBuildInfo monCtx = ObtainPokemonSetContext(mon, buildCtx); // Obtain current Pokemon mods and score and such
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
                    // Now, got a list of all abilities to choose from to add to the mon, let's go
                }
            }
        }
        /// <summary>
        /// Returns the relative score of an ability given a specific context 
        /// </summary>
        /// <param name="ability">The ability in question</param>
        /// <param name="monCtx">The mon Ctx</param>
        /// <param name="buildCtx">The general team ctx</param>
        /// <returns>A score rating the ability</returns>
        static double GetBaseAbilityWeight(Ability ability, PokemonBuildInfo monCtx, TeamBuildContext buildCtx)
        {
            double score = 1;
            return score;
        }
    }
}
