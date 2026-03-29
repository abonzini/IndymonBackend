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
        /// <param name="positiveBoostEffectiveness">How effective positive boosts are (e.g. when calcing opp's on a crit)</param>
        /// <param name="negativeBoostEffectiveness">How effective negative boosts are (e.g. when calcing opp's on a crit)</param>
        /// <returns>The total stat, after multipliers and all and its variance</returns>
        static (double[], double[]) MonStatCalculation(PokemonBuildContext monCtx, TeamBuildContext battleContext, bool isOpponent, double positiveBoostEffectiveness, double negativeBoostEffectiveness)
        {
            double[] resultingStats = [0, 0, 0, 0, 0, 0];
            double[] resultingStatVariance = [0, 0, 0, 0, 0, 0];
            // Get stat/ev/mult array based on whether I'm calculating opponent or mine
            double[] baseStats = (isOpponent) ? battleContext.OpponentsStats : monCtx.MonStats;
            int[] evs = (isOpponent) ? [0, 0, 0, 0, 0, 0] : monCtx.Evs;
            double[] multipliers = (isOpponent) ? monCtx.OppStatMultipliers : monCtx.StatMultipliers;
            double[] boosts = (isOpponent) ? monCtx.OppStatBoosts : monCtx.StatBoosts;
            double boostsMultiplier = (isOpponent) ? monCtx.OppStatBoostsMultiplier : monCtx.StatBoostsMultiplier;
            double[] variances = (isOpponent) ? battleContext.OppStatVariance : [0, 0, 0, 0, 0, 0]; // Only opp will have variance
            double level = (isOpponent) ? 100 : monCtx.LevelMultiplier * 100; // User may have its level modified
            // Calculat stats (except boosts)
            for (int i = 0; i < 6; i++)
            {
                // Stat formula
                double theStat = baseStats[i] * 2;
                double theVariance = variances[i] * 2 * 2;
                theStat += 31 + (evs[i] / 4); // Use 31 IV always don't go too deep here. No variance as it is a sum
                theStat *= level / 100;
                theVariance *= (level / 100) * (level / 100);
                theStat += (i == 0) ? (level + 10) : 5; // No variance gain
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
                double theBoost = boosts[i];
                if (!isOpponent && monCtx.AddOppBoosts && monCtx.OppStatBoosts[i] > 0) theBoost += monCtx.OppStatBoosts[i]; // I may also be able to add the opp positive stat boosts to myself, e.g. mirror herb
                if (!isOpponent && i == highestStatIndex) // Opp doesn't have "highest stat boost" options
                {
                    theBoost += boosts[6];
                    theBoost = Math.Clamp(theBoost, -6, 6); // Clamp in case it overflows
                }
                // Calculate the boost itself
                theBoost *= boostsMultiplier; // Multiplier applied last to all possible boosts
                if (theBoost > 0) // Will apply effectiveness of how much of the positive/negative boosts to ignore
                {
                    theBoost *= positiveBoostEffectiveness;
                }
                else
                {
                    theBoost *= negativeBoostEffectiveness;
                    if (!isOpponent) theBoost *= monCtx.NegativeStatBoostsMultiplier; // Non-opp mon also has the option of multiplying stat boost further
                }
                // Ok now calculate the stat changes
                double num = 2;
                double den = 2;
                if (theBoost > 0) num += theBoost;
                if (theBoost < 0) den -= theBoost;
                double boostMultiplier = num / den; // Not sure how much is necessary but may be a rounding problem otherwise
                resultingStats[i] *= boostMultiplier;
                resultingStatVariance[i] *= boostMultiplier * boostMultiplier;
            }
            return (resultingStats, resultingStatVariance);
        }
    }
}
