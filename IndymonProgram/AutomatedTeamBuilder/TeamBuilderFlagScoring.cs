using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Obtains the score of a specific flag present in a move or ability
        /// </summary>
        /// <param name="flag">Which flag to check</param>
        /// <param name="monCtx">The context where the flag is scored</param>
        /// <returns>The score of this flag</returns>
        static double GetEffectFlagMultWeight(EffectFlag flag, PokemonBuildInfo monCtx)
        {
            if (flag == EffectFlag.BANNED) return 0; // This should've been checked before but just in case
            if (flag == EffectFlag.DOUBLES_ONLY) return 0; // Doubles flags make the move/ability quite pointless
            (ElementType, string) flagTag = (ElementType.EFFECT_FLAGS, flag.ToString());
            double result = 1;
            // Go in order, first check if disabled/enabled, then initial, then weight mods
            if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(flagTag)) // If tag is disabled by default,
            {
                if (!monCtx.EnabledOptions.TryGetValue(flagTag, out result)) // If not enabled, then it has no weight
                {
                    return 0;
                }
            }
            if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(flagTag, out double mult)) // Initial
            {
                result *= mult;
            }
            if (monCtx.WeightMods.TryGetValue(flagTag, out mult)) // Other weight mods...
            {
                result *= mult;
            }
            return result;
        }
        /// <summary>
        /// Gets the flat additive increase of a flag
        /// </summary>
        /// <param name="flag">Which flag</param>
        /// <returns>The additive flat increase</returns>
        static double GetEffectFlagFlatIncrease(EffectFlag flag)
        {
            if (flag == EffectFlag.BANNED) return 0; // This should've been checked before but just in case
            if (flag == EffectFlag.DOUBLES_ONLY) return 0; // Doubles flags make the move/ability quite pointless
            (ElementType, string) flagTag = (ElementType.EFFECT_FLAGS, flag.ToString());
            return MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(flagTag); // 0 if nothing there
        }
    }
}
