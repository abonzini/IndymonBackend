using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Checks how valuable an ability would be in a specific mon
        /// </summary>
        /// <param name="ability">Which ability</param>
        /// <param name="theMon">Mon</param>
        /// <param name="monCtx">Context to score ability</param>
        /// <param name="buildCtx">More context</param>
        /// <param name="isFirstMon">Whether the mon is the first one</param>
        /// <param name="isFirstMon">Whether the mon is the last one</param>
        /// <param name="checkSynergy">Whether to check synergy too or not (for second round)</param>
        /// <returns></returns>
        static double GetAbilityWeight(Ability ability, TrainerPokemon theMon, PokemonBuildContext monCtx, TeamBuildContext buildCtx, bool isFirstMon, bool isLastMon, bool checkSynergy)
        {
            const double MIN_ABILITY_SCORE = 0.0001; // Abilities can't have a score of 0 because there's too few and in some cases a single one, so I make the score very small but not 0 so it can technically be chosen
            Ability oldAbility = theMon.ChosenAbility; // Save this, because it needs to be put back
            (ElementType, string) abilityTag = (ElementType.ABILITY, ability.Name);
            double score = 1;
            // Some scores will make the ability value 0
            if (!isFirstMon && ability.Flags.Contains(EffectFlag.GOOD_FIRST_MON))
            {
                score = 0;
            }
            else if (!isLastMon && ability.Flags.Contains(EffectFlag.GOOD_LAST_MON))
            {
                score = 0;
            }
            else if (isLastMon && ability.Flags.Contains(EffectFlag.BAD_LAST_MON))
            {
                score = 0;
            }
            else if (ability.Flags.Contains(EffectFlag.DOUBLES_ONLY))
            {
                score = 0;
            }
            else if (ability.Flags.Contains(EffectFlag.NORMALLY_UNAVAILABLE) && !theMon.Species.ToLower().Contains("unown")) // Avoids mon using z moves and stuff but unown can
            {
                return 0;
            }
            else
            {
                // Start by obtaining ability score
                score *= GetAbilityMultWeight(ability, monCtx);
                // Then each of the effect flags
                foreach (EffectFlag flag in ability.Flags)
                {
                    score *= GetEffectFlagMultWeight(flag, monCtx);
                }
                // In this case, if ability has a value (not 0), can do additives then
                if (score > 0)
                {
                    foreach (EffectFlag flag in ability.Flags)
                    {
                        score += GetEffectFlagFlatIncrease(flag);
                    }
                    score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(abilityTag); // Adds if something there
                }
            }
            // Then, we need to do the hypotetical, does this ability add to defensive, offensive or speed utilities?
            theMon.ChosenAbility = ability; // First, equip this ability to mon
            PokemonBuildContext newCtx = ObtainPokemonSetContext(theMon, buildCtx); // Check the new context
            double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
            double defImprovement = Math.Ceiling(newCtx.Survivability) / Math.Ceiling(monCtx.Survivability);
            double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
            score *= dmgImprovement * defImprovement * speedImprovement; // Then multiply all utilities gain, give or remove utility from final set!
            if (ability.Flags.Contains(EffectFlag.HEAL)) // Healing abilities (or stuff that works on bulky mon) that are healer are weighted on whether the mon can actually make decent use of this
            {
                score *= newCtx.Survivability / 3; // If you can take 3 hits or more you're officially a bulky mon (because most recovery is 50% based)
            }
            theMon.ChosenAbility = oldAbility; // Revert this ofc
            if (checkSynergy && ability.Flags.Contains(EffectFlag.NEED_SYNERGY))
            {
                if (score <= 1) score = 0; // Synergic abilities need to ensure score >1 to ensure they're actually helping anything
            }
            // Finally, we got a score, an ability needs to eb chosen so it'll always have a value, even if 0
            if (score <= MIN_ABILITY_SCORE)
            {
                score = MIN_ABILITY_SCORE;
            }
            return score;
        }
        /// <summary>
        /// Gets the weight of the ability (name)
        /// </summary>
        /// <param name="ability">Which ability to evaluate</param>
        /// <param name="monCtx">The context to obtain score from</param>
        /// <returns>The ability weight</returns>
        static double GetAbilityMultWeight(Ability ability, PokemonBuildContext monCtx)
        {
            (ElementType, string) abilityTag = (ElementType.ABILITY, ability.Name);
            double result = 1;
            // Go in order, first check if disabled/enabled, then initial, then weight mods
            if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(abilityTag)) // If tag is disabled by default,
            {
                if (!monCtx.EnabledOptions.TryGetValue(abilityTag, out result)) // If not enabled, then it has no weight
                {
                    return 0;
                }
            }
            if (monCtx.WeightMods.TryGetValue(abilityTag, out double mult)) // Other weight mods...
            {
                result *= mult;
            }
            return result;
        }
    }
}
