using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Calculates the total stats of a mon
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="monCtx">The extra context (e.g. mods) to apply to stat</param>
        /// <param name="battleContext">Context of battle, basically to get opp data</param>
        /// <param name="isOpponent">If the stat to calculate is the opponent's</param>
        /// <returns>The total stat, after multipliers and all</returns>
        static double[] MonStatCalculation(TrainerPokemon mon, PokemonBuildInfo monCtx, TeamBuildContext battleContext = null, bool isOpponent = false)
        {
            double[] result = [0, 0, 0, 0, 0, 0];
            // Get stat/ev/mult array based on whether I'm calculating opponent or mine
            double[] baseStats = (isOpponent) ? battleContext.OpponentsStats :
                MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species].Stats;
            int[] evs = (isOpponent) ? [0, 0, 0, 0, 0, 0] : monCtx.Evs;
            double[] multipliers = (isOpponent) ? monCtx.OppStatMultipliers : monCtx.StatMultipliers;
            int[] boosts = (isOpponent) ? monCtx.OppStatBoosts : monCtx.StatBoosts;
            // Calculat stats (except boosts)
            for (int i = 0; i < 6; i++)
            {
                // Stat formula
                double theStat = baseStats[0] * 2;
                theStat += 31 + (evs[i] / 4); // Use 31 IV always don't go too deep here
                // There would be a level/100 here but only add if really will implement lvl mods
                theStat += (i == 0) ? 105 : 5; // Also level based. Hp gains Lvl+5
                theStat *= multipliers[i];
                result[i] = theStat;
            }
            // Intermission, check which stat is highest so that "highest stat boost would apply to this
            int highestStatIndex = 1;
            double highestStat = double.NegativeInfinity;
            for (int i = 1; i < 6; i++) // Check all stats except HP
            {
                if (result[i] > highestStat)
                {
                    highestStat = result[i];
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
                    theBoost = Math.Clamp(theBoost, -6, 6); // Clamp in case it overflows
                }
                // Calculate the boost itself
                int num = 2;
                int den = 2;
                if (theBoost > 0) num += theBoost;
                if (theBoost < 0) den += theBoost;
                result[i] *= ((double)num) / ((double)den); // Not sure how much is necessary but may be a rounding problem otherwise
            }
            return result;
        }
        /// <summary>
        /// Calculates the damage of a (damaging!) move
        /// </summary>
        /// <param name="mon">The mon using the move</param>
        /// <param name="move">The move itself</param>
        /// <param name="monCtx">Context of the mon using the move</param>
        /// <param name="attackerStats">Stats of mon using move</param>
        /// <param name="defenderStats">Stats of mon receiving the attack</param>
        /// <param name="battleContext">Additional context</param>
        /// <returns>The damage (in HP) the opp receives</returns>
        static double CalcMoveDamage(TrainerPokemon mon, Move move, PokemonBuildInfo monCtx, double[] attackerStats, double[] defenderStats, TeamBuildContext battleContext)
        {
            // First, get the ACTUAL flags of the move (because some may have been added/removed
            HashSet<EffectFlag> moveFlags = ExtractMoveFlags(move, monCtx);
            PokemonType moveType = (move.Name == "Revelation Dance") ? monCtx.PokemonTypes[0] : move.Type; // Revelation dance overrides move type
            if (move.Category == MoveCategory.STATUS) // Status moves don't deal damage this should never happen tho
            {
                return 0;
            }
            else if (moveFlags.Contains(EffectFlag.FIXED_DAMAGE)) // If the move is fixed damage, the modified moveDex has already some estimation of damage in it
            {
                return move.Bp;
            }
            else if (move.Name == "Sky Drop" && battleContext.AverageOpponentWeight >= 200) // Sky drop fails if opp too heavy
            {
                return 0;
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
                // Then get the Bp/acc mods, most are easy but if move changes type I need to look for the moves of the new type too
                // Get mods that affect this move specifically (1 if none)
                moveBp *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
                moveAccuracy *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
                // Get mods that affect moves of a specific category
                moveBp *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
                moveAccuracy *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
                // Get mods that affect all damaging moves
                moveBp *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
                moveAccuracy *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
                // Apply type ones (and type mod) if move type changed, there may be more mods
                bool moveTypeChanged;
                do
                {
                    moveTypeChanged = false;
                    moveBp *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1);
                    moveAccuracy *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1);
                    PokemonType moddedType = GetModifiedMoveType(move, monCtx);
                    if (moddedType != moveType)
                    {
                        moveType = moddedType;
                        moveTypeChanged = true;
                    }
                } while (moveTypeChanged);
                // Cleanup of Acc
                if (moveAccuracy == 0) moveAccuracy = 1; // 0 Acc means sure hit
                moveAccuracy = Math.Clamp(moveAccuracy, 0, 1); // Clamp to clean out moves with acc > 100% to not do extra damage
                // At this point got BP and Acc, missing stats only
                double attackingStat;
                if (moveFlags.Contains(EffectFlag.DEFENSE_DAMAGE))
                {
                    attackingStat = attackerStats[2]; // Defense
                }
                else if (moveFlags.Contains(EffectFlag.OPP_ATTACK_DAMAGE))
                {
                    attackingStat = defenderStats[1]; // Uses the opp attack stat
                }
                else
                {
                    // Otherwise phy/spe depending
                    attackingStat = (move.Category == MoveCategory.PHYSICAL) ? attackerStats[1] : attackerStats[3];
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
                hitDamage *= moveBp * attackingStat / defendingStat;
                hitDamage /= 50;
                hitDamage += 2; // This is the hit damage
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
                hitDamage *= (critChance * 1.5) + ((1 - critChance) * 1); // Crit may increase a hit damage in average
                // Stab, check if tera is involved
                bool adaptabilityCheck = (mon.ChosenAbility?.Name == "Adaptability");
                if (monCtx.TeraType == moveType) // Tera-induced stab
                {
                    if (monCtx.PokemonTypes.Contains(monCtx.TeraType)) // Depending on whether new type or not
                    {
                        hitDamage *= (adaptabilityCheck) ? 2.25 : 2;
                    }
                    else
                    {
                        hitDamage *= (adaptabilityCheck) ? 2 : 1.5;
                    }
                }
                else // Otherwise check for normal stab
                {
                    if (monCtx.PokemonTypes.Contains(moveType))
                    {
                        hitDamage *= (adaptabilityCheck) ? 2 : 1.5;
                    }
                }
                // Random
                hitDamage *= (100 + 85) / 100; // Hit roll
                // Parental bond
                if (mon.ChosenAbility != null && mon.ChosenAbility.Name == "Parental Bond")
                {
                    hitDamage *= 1.25; // Parental bond hits twice
                }
                // Finally, Acc application, left for last because of multihit
                if (moveFlags.Contains(EffectFlag.MULTIHIT_2_MOVE))
                {
                    // No data so I assume it's always 2 hits subject to the first one hitting
                    hitDamage *= 2 * moveAccuracy;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_3_MOVE))
                {
                    // No data so I assume it's always 2 hits subject to the first one hitting
                    hitDamage *= 3 * moveAccuracy;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_2_TO_5_MOVE))
                {
                    // If connects, then accuracy
                    double hits = (mon.BattleItem?.Name == "Loaded Dice") ? 4.5 : 3.1;
                    hitDamage *= hits * moveAccuracy;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_3_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (mon.BattleItem?.Name == "Loaded Dice")
                    {
                        hitDamage = 6 * hitDamage * moveAccuracy; // 6 because it's 1,2,3 each hit with single accuracy check
                    }
                    else
                    {
                        // Manually because I'm lazy to think
                        hitDamage = 6 * hitDamage * moveAccuracy * moveAccuracy * moveAccuracy +
                            3 * hitDamage * moveAccuracy * moveAccuracy * (1 - moveAccuracy) +
                            hitDamage * moveAccuracy * (1 - moveAccuracy) * (1 - moveAccuracy);
                    }
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_10_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (mon.BattleItem?.Name == "Loaded Dice")
                    {
                        hitDamage = 7 * hitDamage * moveAccuracy; // 7 because 4-10 with equal chance equals to 7 average and then a single acc check
                    }
                    else
                    {
                        // Estimate for each hit chance, fortunately moves dont increase in damage
                        double averageDamage = 0;
                        for (int hitNumber = 1; hitNumber < 10; hitNumber++) // Hits 1-9 meaning X straight rolls and a miss
                        {
                            averageDamage += hitNumber * hitDamage * Math.Pow(moveAccuracy, hitNumber) * (1 - moveAccuracy);
                        }
                        averageDamage += 10 * hitDamage * Math.Pow(moveAccuracy, 10); // Add the damage of 10 hits too
                        hitDamage = averageDamage;
                    }
                }
                else
                {
                    hitDamage *= moveAccuracy; // Just good ol hit * chance of hit
                }
                // And this should be all I think
                return hitDamage;
            }
        }
    }
}
