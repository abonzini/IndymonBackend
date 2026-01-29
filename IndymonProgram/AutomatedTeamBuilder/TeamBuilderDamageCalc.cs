using MechanicsData;
using MechanicsDataContainer;

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
        /// <summary>
        /// Calculates the damage of a (damaging!) move
        /// </summary>
        /// <param name="move">The move itself</param>
        /// <param name="monCtx">Context of the mon using the move</param>
        /// <param name="attackerStats">Stats of mon using move</param>
        /// <param name="defenderStats">Stats of mon receiving the attack</param>
        /// <param name="battleContext">Additional context</param>
        /// <param name="alwaysStab">Libero/protean which cause move to always be stab</param>
        /// <param name="extraStab">Adaptability complex stab check</param>
        /// <param name="loadedDice">Loaded dice modifies moves like crazy</param>
        /// <returns>The damage (in HP) the opp receives</returns>
        static (double, double) CalcMoveDamage(Move move, PokemonBuildInfo monCtx, double[] attackerStats, double[] defenderStats, double[] attackingStatVariances, TeamBuildContext battleContext, bool alwaysStab = false, bool extraStab = false, bool loadedDice = false)
        {
            // First, get the ACTUAL flags of the move (because some may have been added/removed
            HashSet<EffectFlag> moveFlags = ExtractMoveFlags(move, monCtx);
            if (move.Category == MoveCategory.STATUS) // Status moves don't deal damage this should never happen tho
            {
                return (0, 0);
            }
            else if (moveFlags.Contains(EffectFlag.FIXED_DAMAGE)) // If the move is fixed damage, the modified moveDex has already some estimation of damage in it
            {
                return (move.Bp, 0);
            }
            else if (move.Name == "Sky Drop" && battleContext.AverageOpponentWeight >= 200) // Sky drop fails if opp too heavy
            {
                return (0, 0);
            }
            else // Damage calc incoming...
            {
                // First, obtain the Bp, some special cases calculate Bp differently, and if no Bp, then it's a default value of 60
                double moveBp;
                double moveAccuracy = move.Acc / 100; // 0-1 acc
                if (moveFlags.Contains(EffectFlag.DAMAGE_PROP_WEIGTH_DIFFERENCE)) // Depending on relative diff
                {
                    double opponentWeightPercentage = battleContext.AverageOpponentWeight / monCtx.MonWeight;
                    if (opponentWeightPercentage > 0.5) moveBp = 40;
                    else if (opponentWeightPercentage > 0.335) moveBp = 60;
                    else if (opponentWeightPercentage > 0.2501) moveBp = 80;
                    else if (opponentWeightPercentage > 0.201) moveBp = 100;
                    else moveBp = 120;
                }
                else if (moveFlags.Contains(EffectFlag.DAMAGE_PROP_OPP_WEIGTH)) // Depending on opp weight (Kg)
                {
                    if (battleContext.AverageOpponentWeight > 200) moveBp = 120;
                    else if (battleContext.AverageOpponentWeight > 200) moveBp = 100;
                    else if (battleContext.AverageOpponentWeight > 200) moveBp = 80;
                    else if (battleContext.AverageOpponentWeight > 200) moveBp = 60;
                    else if (battleContext.AverageOpponentWeight > 200) moveBp = 40;
                    else moveBp = 20;
                }
                else if (moveFlags.Contains(EffectFlag.DAMAGE_PROP_SPEED_DIFFERENCE)) // Depending on rel speed diff
                {
                    double oppSpeedPercentage = defenderStats[5] / attackerStats[5];
                    if (oppSpeedPercentage > 1) moveBp = 40;
                    else if (oppSpeedPercentage > 0.5001) moveBp = 60;
                    else if (oppSpeedPercentage > 0.3334) moveBp = 80;
                    else if (oppSpeedPercentage > 0.2501) moveBp = 120;
                    else moveBp = 150;
                }
                else if (moveFlags.Contains(EffectFlag.DAMAGE_INV_SPEED_DIFFERENCE))
                {
                    moveBp = (25 * defenderStats[5] / attackerStats[5]) + 1;
                    moveBp = Math.Min(150, moveBp);
                }
                else if (move.Bp == 0)
                {
                    moveBp = 60; // Average of weird remaining moves of variable powers
                }
                else
                {
                    moveBp = move.Bp;
                }
                // Then get the Bp mods
                moveBp *= GetMoveBpMods(move, monCtx);
                // Cleanup of Acc
                moveAccuracy *= GetMoveAccMods(move, monCtx);
                if (moveAccuracy == 0) moveAccuracy = 1; // 0 Acc means sure hit
                moveAccuracy = Math.Clamp(moveAccuracy, 0, 1); // Clamp to clean out moves with acc > 100% to not do extra damage
                // Apply type mods
                PokemonType moveType = GetModifiedMoveType(move, monCtx);
                // At this point got BP and Acc, missing stats only
                double attackingStat;
                double attackingStatVariance; // Will hold the variance of the attacking stat, to return the cariance of the damage
                if (moveFlags.Contains(EffectFlag.DEFENSE_DAMAGE))
                {
                    attackingStat = attackerStats[2]; // Defense
                    attackingStatVariance = attackingStatVariances[2];
                }
                else if (moveFlags.Contains(EffectFlag.OPP_ATTACK_DAMAGE))
                {
                    attackingStat = defenderStats[1]; // Uses the opp attack stat
                    attackingStatVariance = attackingStatVariances[1];
                }
                else
                {
                    // Otherwise phy/spe depending
                    attackingStat = (move.Category == MoveCategory.PHYSICAL) ? attackerStats[1] : attackerStats[3];
                    attackingStatVariance = (move.Category == MoveCategory.PHYSICAL) ? attackingStatVariances[1] : attackingStatVariances[3];
                }
                double defendingStat;
                if (move.Category == MoveCategory.PHYSICAL) // Get the correct defense unless move switches them
                {
                    defendingStat = (moveFlags.Contains(EffectFlag.OTHER_DEFENSE_STAT)) ? defenderStats[4] : defenderStats[2];
                }
                else
                {
                    defendingStat = (moveFlags.Contains(EffectFlag.OTHER_DEFENSE_STAT)) ? defenderStats[2] : defenderStats[4];
                }
                // Final calculation
                double hitDamage = 42; // This depends on mon level so keep in mind
                hitDamage *= attackingStat;
                double hitDamageVariance = attackingStatVariance * 42 * 42; // start doing variance at this point
                double remainingFactor = moveBp / (defendingStat * 50); // rest of the damage formula
                hitDamage *= remainingFactor;
                hitDamageVariance *= remainingFactor * remainingFactor;
                hitDamage += 2; // This is the hit damage, no variance needed here
                // Crit chance
                int critStages = 0; // Th
                if (moveFlags.Contains(EffectFlag.HIGH_CRIT)) critStages++;
                if (moveFlags.Contains(EffectFlag.CRITICAL)) critStages = 3;
                critStages += monCtx.CriticalStages;
                critStages = Math.Clamp(critStages, 0, 3); // Max is 3
                double critChance = critStages switch // Chance of crit depending on crit stages
                {
                    1 => 12.5,
                    2 => 0.5,
                    3 => 1,
                    _ => 4.17
                };
                double critMultiplier = (critChance * 1.5) + ((1 - critChance) * 1); // Crit may increase a hit damage in average
                hitDamage *= critMultiplier;
                hitDamageVariance *= critMultiplier * critMultiplier;
                // Stab, check if tera is involved
                double stabBonus = 1;
                if (monCtx.TeraType == moveType) // Tera-induced stab
                {
                    if (monCtx.PokemonTypes.Item1 == monCtx.TeraType || monCtx.PokemonTypes.Item2 == monCtx.TeraType) // Depending on whether new type or not
                    {
                        stabBonus = (extraStab) ? 2.25 : 2;
                    }
                    else
                    {
                        stabBonus = (extraStab) ? 2 : 1.5;
                    }
                }
                else // Otherwise check for normal stab
                {
                    if (monCtx.PokemonTypes.Item1 == monCtx.TeraType || monCtx.PokemonTypes.Item2 == monCtx.TeraType || alwaysStab)
                    {
                        stabBonus = (extraStab) ? 2 : 1.5;
                    }
                }
                hitDamage *= stabBonus;
                hitDamageVariance *= stabBonus * stabBonus;
                // Random
                double randomHitRoll = (100 + 85) / 100; // Hit roll
                hitDamage *= randomHitRoll;
                hitDamageVariance *= randomHitRoll * randomHitRoll;
                // Finally, Acc application, left for last because of multihit
                if (moveFlags.Contains(EffectFlag.MULTIHIT_2_MOVE))
                {
                    // No data so I assume it's always 2 hits subject to the first one hitting
                    double multihitModifier = 2 * moveAccuracy;
                    hitDamage *= multihitModifier;
                    hitDamageVariance *= multihitModifier * multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_3_MOVE))
                {
                    // No data so I assume it's always 3 hits subject to the first one hitting
                    double multihitModifier = 3 * moveAccuracy;
                    hitDamage *= multihitModifier;
                    hitDamageVariance *= multihitModifier * multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_2_TO_5_MOVE))
                {
                    // If connects, then accuracy
                    double hits = (loadedDice) ? 4.5 : 3.1;
                    double multihitModifier = hits * moveAccuracy;
                    hitDamage *= multihitModifier;
                    hitDamageVariance *= multihitModifier * multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_3_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (loadedDice)
                    {
                        double multihitModifier = 6 * hitDamage * moveAccuracy; // 6 because it's 1,2,3 each hit with single accuracy check
                        hitDamage *= multihitModifier;
                        hitDamageVariance *= multihitModifier * multihitModifier;
                    }
                    else
                    {
                        // Manually because I'm lazy to think
                        double multihitModifier = 6 * moveAccuracy * moveAccuracy * moveAccuracy +
                            3 * moveAccuracy * moveAccuracy * (1 - moveAccuracy) +
                            moveAccuracy * (1 - moveAccuracy) * (1 - moveAccuracy);
                        hitDamage *= multihitModifier;
                        hitDamageVariance *= multihitModifier * multihitModifier;
                    }
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_10_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (loadedDice)
                    {
                        double multihitModifier = 7 * hitDamage * moveAccuracy; // 7 because 4-10 with equal chance equals to 7 average and then a single acc check
                        hitDamage *= multihitModifier;
                        hitDamageVariance *= multihitModifier * multihitModifier;
                    }
                    else
                    {
                        // Estimate for each hit chance, fortunately moves dont increase in damage
                        double multihitModifier = 0;
                        for (int hitNumber = 1; hitNumber < 10; hitNumber++) // Hits 1-9 meaning X straight rolls and a miss
                        {
                            multihitModifier += hitNumber * Math.Pow(moveAccuracy, hitNumber) * (1 - moveAccuracy);
                        }
                        multihitModifier += 10 * Math.Pow(moveAccuracy, 10); // Add the damage of 10 hits too
                        hitDamage *= multihitModifier;
                        hitDamageVariance *= multihitModifier * multihitModifier;
                    }
                }
                else
                {
                    hitDamage *= moveAccuracy; // Just good ol hit * chance of hit
                    hitDamageVariance *= moveAccuracy * moveAccuracy;
                }
                // And this should be all I think
                return (hitDamage, hitDamageVariance);
            }
        }
        /// <summary>
        /// Gets the move Bp mods
        /// </summary>
        /// <param name="move">Which move</param>
        /// <param name="monCtx">Ctx that contains the mods</param>
        /// <returns>The move Bp multiplier</returns>
        static double GetMoveBpMods(Move move, PokemonBuildInfo monCtx)
        {
            double result = 1;
            PokemonType moveBaseType = move.Type;
            // Get mods that affect this move specifically (1 if none)
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
            // Get mods that affect moves of a specific category
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
            // Get mods that affect all damaging moves
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
            // Apply type ones (and type mod) if move type changed, there may be more mods
            bool moveTypeChanged;
            do
            {
                moveTypeChanged = false;
                result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveBaseType.ToString()), 1);
                PokemonType moddedType = GetModifiedMoveType(move, monCtx);
                if (moddedType != moveBaseType)
                {
                    moveBaseType = moddedType;
                    moveTypeChanged = true;
                }
            } while (moveTypeChanged);
            return result;
        }
        /// <summary>
        /// Gets the move Accuracy mods
        /// </summary>
        /// <param name="move">Which move</param>
        /// <param name="monCtx">Ctx that contains the mods</param>
        /// <returns>The move Accuracy multiplier</returns>
        static double GetMoveAccMods(Move move, PokemonBuildInfo monCtx)
        {
            double result = 1;
            PokemonType moveBaseType = move.Type;
            // Get mods that affect this move specifically (1 if none)
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
            // Get mods that affect moves of a specific category
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
            // Get mods that affect all damaging moves
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
            // Apply type ones (and type mod) if move type changed, there may be more mods
            bool moveTypeChanged;
            do
            {
                moveTypeChanged = false;
                result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveBaseType.ToString()), 1);
                PokemonType moddedType = GetModifiedMoveType(move, monCtx);
                if (moddedType != moveBaseType)
                {
                    moveBaseType = moddedType;
                    moveTypeChanged = true;
                }
            } while (moveTypeChanged);
            return result;
        }
        /// <summary>
        /// Calculates the offensive type coverage given an attacking type into many defensive multitypes
        /// </summary>
        /// <param name="attackingType">Type of attacking move</param>
        /// <param name="defenderTypes">All the types that defend</param>
        /// <param name="ignoresImmunity">If a move hits immunity, is this ignored?</param>
        /// <param name="doubleNotEffectiveDamage">If hits a not very effective, is the result doubled?</param>
        /// <param name="seAgainstWater">Will the move hit water for double damage instead of typechart value?</param>
        /// <returns>The maximum dmaage multiplier for this type/defender combo</returns>
        static List<double> CalculateOffensiveTypeCoverage(PokemonType attackingType, List<(PokemonType, PokemonType)> defenderTypes, bool ignoresImmunity, bool doubleNotEffectiveDamage, bool seAgainstWater)
        {
            List<double> result = new List<double>();
            foreach ((PokemonType, PokemonType) defenderType in defenderTypes)
            {
                static double damageFromType(PokemonType attackingType, PokemonType defendingType, bool ignoresImmunity, bool seAgainstWater)
                {
                    if (seAgainstWater && defendingType == PokemonType.WATER) return 2; // Skip the whole damage calc idc
                    double result = MechanicsDataContainers.GlobalMechanicsData.DefensiveTypeChart[defendingType][attackingType];
                    if (ignoresImmunity && result == 0) result = 1;
                    return result;
                }
                double damageT1 = 1, damageT2 = 1;
                if (defenderType.Item1 != PokemonType.NONE)
                {
                    damageT1 = damageFromType(attackingType, defenderType.Item1, ignoresImmunity, seAgainstWater);
                }
                if (defenderType.Item2 != PokemonType.NONE)
                {
                    damageT2 = damageFromType(attackingType, defenderType.Item2, ignoresImmunity, seAgainstWater);
                }
                double resultingMultiplier = damageT1 * damageT2;
                if (doubleNotEffectiveDamage && resultingMultiplier < 1) resultingMultiplier *= 2; // Tinted lens doubles not very effective dmg
                result.Add(resultingMultiplier);
            }
            return result;
        }
        /// <summary>
        /// For a lot of attackers, assume you're being attacked stab for all types, assuming no abilities. Gets you a list of all damage modifiers received, defender may have abilities too
        /// </summary>
        /// <param name="defendingType">Type of defending mon</param>
        /// <param name="attackingTypes">Attacking types of attacking mons, will try all stabs</param>
        /// <param name="ModifiedTypeEffectiveness">All the mods that would affect defending type effectiveness</param>
        /// <returns>A list with all stab damage multipliers</returns>
        static List<double> CalculateDefensiveTypeStabCoverage((PokemonType, PokemonType) defendingType, List<(PokemonType, PokemonType)> attackingTypes, HashSet<(StatModifier, string)> ModifiedTypeEffectiveness)
        {
            List<double> result = new List<double>();
            foreach ((PokemonType, PokemonType) attackingType in attackingTypes)
            {
                // Consider this as 2 separate attacks now, first T1 and then T2. Get basic damage, multiply, then decide if nullify
                static double DefensiveTypeCheck(PokemonType attackingType, (PokemonType, PokemonType) defendingType, HashSet<(StatModifier, string)> ModifiedTypeEffectiveness)
                {
                    double damage = CalculateOffensiveTypeCoverage(attackingType, [defendingType], false, false, false)[0]; // Reuse the attackign formula, check how much this messes me up, don''t know enemy abilities so all false
                    // SE checks here before extra modifiers
                    if (damage > 1) // SE!
                    {
                        if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_SE_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                        foreach ((StatModifier, string) alterSeCase in ModifiedTypeEffectiveness.Where(m => m.Item1 == StatModifier.ALTER_RECV_SE_DAMAGE)) // Can be any number so I need to search by key
                        {
                            damage *= double.Parse(alterSeCase.Item2);
                        }
                    }
                    else // non SE!
                    {
                        foreach ((StatModifier, string) alterSeCase in ModifiedTypeEffectiveness.Where(m => m.Item1 == StatModifier.ALTER_RECV_NON_SE_DAMAGE)) // Can be any number so I need to search by key
                        {
                            damage *= double.Parse(alterSeCase.Item2);
                        }
                    }
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.NULLIFIES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.DOUBLES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 2;
                    return damage;
                }
                // Calculate and add for valid types, incorporate stab mult too
                if (attackingType.Item1 != PokemonType.NONE) result.Add(1.5 * DefensiveTypeCheck(attackingType.Item1, defendingType, ModifiedTypeEffectiveness));
                if (attackingType.Item2 != PokemonType.NONE) result.Add(1.5 * DefensiveTypeCheck(attackingType.Item2, defendingType, ModifiedTypeEffectiveness));
            }
            return result;
        }
    }
}
