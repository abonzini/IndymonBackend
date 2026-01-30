using MechanicsData;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
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
                PokemonType moveType = GetModifiedMoveType(move, monCtx);
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
            PokemonType moveType = GetModifiedMoveType(move, monCtx);
            // Get mods that affect this move specifically (1 if none)
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
            // Get mods that affect moves of a specific category
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
            // Get mods that affect all damaging moves
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString()), 1); // Without any type mod
            // Type-based
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1); // With type mod
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
            PokemonType moveType = GetModifiedMoveType(move, monCtx);
            // Get mods that affect this move specifically (1 if none)
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
            // Get mods that affect moves of a specific category
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
            // Get mods that affect all damaging moves
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
            // Apply type ones (and type mod) if move type changed, there may be more mods
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString()), 1); // Without any type mod
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1); // With type mod
            return result;
        }
    }
}
