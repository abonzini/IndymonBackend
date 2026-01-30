namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Calculates the total stats of a mon
        /// </summary>
        /// <param name="monCtx">The extra context (e.g. mods) to apply to stat</param>
        /// <param name="battleContext">Context of battle, basically to get opp data</param>
        /// <param name="isOpponent">If the stat to calculate is the opponent's</param>
        /// <returns>The total stat, after multipliers and all and its variance</returns>
        static (double[], double[]) MonStatCalculation(PokemonBuildInfo monCtx, TeamBuildContext battleContext = null, bool isOpponent = false)
        {
            double[] resultingStats = [0, 0, 0, 0, 0, 0];
            double[] resultingStatVariance = [0, 0, 0, 0, 0, 0];
            // Get stat/ev/mult array based on whether I'm calculating opponent or mine
            double[] baseStats = (isOpponent) ? battleContext.OpponentsStats : monCtx.MonStats;
            int[] evs = (isOpponent) ? [0, 0, 0, 0, 0, 0] : monCtx.Evs;
            double[] multipliers = (isOpponent) ? monCtx.OppStatMultipliers : monCtx.StatMultipliers;
            int[] boosts = (isOpponent) ? monCtx.OppStatBoosts : monCtx.StatBoosts;
            int boostsMultiplier = (isOpponent) ? monCtx.OppStatBoostsMultiplier : monCtx.StatBoostsMultiplier;
            double[] variances = (isOpponent) ? battleContext.OppStatVariance : [0, 0, 0, 0, 0, 0]; // Only opp will have variance
            // Calculat stats (except boosts)
            for (int i = 0; i < 6; i++)
            {
                // Stat formula
                double theStat = baseStats[0] * 2;
                double theVariance = variances[0] * 2 * 2;
                theStat += 31 + (evs[i] / 4); // Use 31 IV always don't go too deep here. No variance as it is a sum
                // There would be a level/100 here but only add if really will implement lvl mods
                theStat += (i == 0) ? 105 : 5; // Also level based. Hp gains Lvl+5. No variance gain
                theStat *= multipliers[i];
                theVariance *= multipliers[i] * multipliers[i];
                resultingStats[i] = theStat;
                resultingStatVariance[i] = theVariance;
            }
            // Intermission, check which stat is highest so that "highest stat boost would apply to this
            int highestStatIndex = 1;
            double highestStat = double.NegativeInfinity;
            for (int i = 1; i < 6; i++) // Check all stats except HP
            {
                if (resultingStats[i] > highestStat)
                {
                    highestStat = resultingStats[i];
                    highestStatIndex = i;
                }
            }
            // Finally, apply stat boosts
            for (int i = 0; i < 6; i++)
            {
                // Check boost amount (+highest stat if any)
                int theBoost = boosts[i];
                if (i == highestStatIndex)
                {
                    theBoost += boosts[7];
                    theBoost *= boostsMultiplier; // Multiplier applied last to all possible boosts
                    theBoost = Math.Clamp(theBoost, -6, 6); // Clamp in case it overflows
                }
                // Calculate the boost itself
                int num = 2;
                int den = 2;
                if (theBoost > 0) num += theBoost;
                if (theBoost < 0) den += theBoost;
                double boostMultiplier = ((double)num) / ((double)den); // Not sure how much is necessary but may be a rounding problem otherwise
                resultingStats[i] *= boostMultiplier;
                resultingStatVariance[i] *= boostMultiplier * boostMultiplier;
            }
            return (resultingStats, resultingStatVariance);
        }
    }
}
