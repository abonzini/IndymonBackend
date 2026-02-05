using GameData;
using MechanicsData;
using MechanicsDataContainer;
using Utilities;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        // Calc area
        /// <summary>
        /// Calculated the damage of a simple placeholder move without any crazy mods or even a type. Just to have an idea of the defensive profile of a mon
        /// </summary>
        /// <param name="bp">Bp of move</param>
        /// <param name="category">Category of move</param>
        /// <param name="attackerStats">Stats of attacker entity</param>
        /// <param name="defenderStats">Stats of defending entity</param>
        /// <param name="attackingStatVariances">Variance of stats of attacking entity</param>
        /// <returns></returns>
        static (double, double) CalcPlaceholderMoveDamage(double bp, MoveCategory category, double[] attackerStats, double[] defenderStats, double[] attackingStatVariances)
        {
            double attackingStat = (category == MoveCategory.PHYSICAL) ? attackerStats[1] : attackerStats[3];
            double attackingStatVariance = (category == MoveCategory.PHYSICAL) ? attackingStatVariances[1] : attackingStatVariances[3];
            double defendingStat = (category == MoveCategory.PHYSICAL) ? defenderStats[2] : defenderStats[4];
            // Actual calculation    
            double hitDamage = 42; // This depends on mon level so keep in mind
            hitDamage *= attackingStat;
            double hitDamageVariance = attackingStatVariance * 42 * 42; // start doing variance at this point
            double remainingFactor = bp / (defendingStat * 50); // rest of the damage formula
            hitDamage *= remainingFactor;
            hitDamageVariance *= remainingFactor * remainingFactor;
            hitDamage += 2; // This is the hit damage, no variance needed here
            return (hitDamage, hitDamageVariance);
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
        /// <param name="highCritDamage">Sniper multilies crit damage</param>
        /// <returns>The damage (in HP) the opp receives</returns>
        static (double, double) CalcMoveDamage(Move move, PokemonBuildInfo monCtx, double[] attackerStats, double[] defenderStats, double[] attackingStatVariances, TeamBuildContext battleContext, bool alwaysStab = false, bool extraStab = false, bool loadedDice = false, bool highCritDamage = false)
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
                else if (moveFlags.Contains(EffectFlag.POSITIVE_STAT_BOOST))
                {
                    int numberOfStatBoosts = 0;
                    for (int i = 0; i < monCtx.StatBoosts.Length; i++)
                    {
                        if (monCtx.StatBoosts[i] > 0) // Only positive boosts are counted
                        {
                            numberOfStatBoosts += monCtx.StatBoosts[i];
                        }
                    }
                    moveBp = 20 * numberOfStatBoosts; //20* boosts
                    if (moveBp < 20) moveBp = 20; // Min dmg of 20 unboosted
                }
                else if (move.Bp == 0)
                {
                    moveBp = 60; // Average of weird remaining moves of variable powers
                }
                else
                {
                    moveBp = move.Bp;
                }
                // Then clamp to min 60 if theres tera involved, and apply the Bp mods after (even tho it may not always be correct)
                if (monCtx.TeraType == GetModifiedMoveType(move, monCtx) && moveBp < 60)
                {
                    moveBp = 60;
                }
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
                int critStages = 0;
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
                double critDamage = (highCritDamage) ? 2.25 : 1.5;
                double critMultiplier = (critChance * critDamage) + ((1 - critChance) * 1); // Crit may increase a hit damage in average
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
        /// <summary>
        /// Obtains all flags applied to this move. To be called last to ensure everything has had time to modify flags
        /// </summary>
        /// <param name="move">Move</param>
        /// <param name="monCtx">Context to see which flags are added/removed from move</param>
        /// <returns>All flags in this move</returns>
        static HashSet<EffectFlag> ExtractMoveFlags(Move move, PokemonBuildInfo monCtx)
        {
            if (move == null) return [EffectFlag.PIVOT]; // Null move (switch) is basically a pivot
            HashSet<EffectFlag> moveFlags = [.. move.Flags]; // Copies moves base flags
            HashSet<EffectFlag> removedFlags = [];
            HashSet<EffectFlag> addedFlags = [];
            // Check what has been added
            addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.MOVE, move.Name)]);
            addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.MOVE_CATEGORY, move.Category.ToString())]);
            addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.ANY_DAMAGING_MOVE, "-")]);
            // Check what has been removed
            removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.MOVE, move.Name)]);
            removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.MOVE_CATEGORY, move.Category.ToString())]);
            removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.ANY_DAMAGING_MOVE, "-")]);
            // For type mods, need to apply many times if the move's type has changed
            addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString())]);
            removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString())]);
            PokemonType moveType = GetModifiedMoveType(move, monCtx);
            addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString())]);
            removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString())]);
            // Add then remove, better to forget some flag than have a wrong one
            moveFlags.UnionWith(addedFlags);
            moveFlags.ExceptWith(removedFlags);
            return moveFlags;
        }
        /// <summary>
        /// Gets a move's type by frantically checking every move type mod
        /// </summary>
        /// <param name="move">Move to check</param>
        /// <param name="monCtx">Mon ctx to get mods</param>
        /// <returns></returns>
        static PokemonType GetModifiedMoveType(Move move, PokemonBuildInfo monCtx)
        {
            PokemonType moveType = move.Type;
            if (move.Name == "Revelation Dance") // Revelation dance overrides everything so I don't get cool mods
            {
                moveType = (monCtx.TeraType != PokemonType.NONE) ? monCtx.TeraType : monCtx.PokemonTypes.Item1;
            }
            else
            {
                bool typeChanged = false;
                do
                {
                    PokemonType newType = moveType;
                    // Checks the move type mod everywhere (including own flagsbut not the added flags)
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.MOVE, move.Name), out PokemonType typeMod)) newType = typeMod;
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.MOVE_CATEGORY, move.Category.ToString()), out typeMod)) newType = typeMod;
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.ANY_DAMAGING_MOVE, "-"), out typeMod)) newType = typeMod;
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString()), out typeMod)) newType = typeMod;
                    if (newType != moveType)
                    {
                        typeChanged = true;
                    }
                } while (typeChanged);
            }
            return moveType;
        }
        // Scoring area
        /// <summary>
        /// The main dish, gets the move weight. Move is more complex as it needs to calculate damage, apply weights, and then other shit
        /// </summary>
        /// <param name="move">The move to evaluate</param>
        /// <param name="mon">Mon that will equip move</param>
        /// <param name="monCtx">Mon ctx whete move is evaluated</param>
        /// <param name="buildCtx">Build context where everythign is evaluated</param>
        /// <param name="isFirstMon">Whether it's the first mon evaluated or not</param>
        /// <returns>A move score</returns>
        static double GetMoveWeight(Move move, TrainerPokemon mon, PokemonBuildInfo monCtx, TeamBuildContext buildCtx, bool isFirstMon)
        {
            // Get the move mods
            HashSet<EffectFlag> allMoveFlags = ExtractMoveFlags(move, monCtx); // Get all the flags a move has
            PokemonType moveType = GetModifiedMoveType(move, monCtx); // Get the final move type for type effectiveness
            // Beginning of scoring
            double score = 1;
            if (allMoveFlags.Contains(EffectFlag.BANNED)) // Banned moves can never be chosen
            {
                return 0;
            }
            else if (allMoveFlags.Contains(EffectFlag.DOUBLES_ONLY)) // Doubles moves can never be chosen
            {
                return 0;
            }
            else if (isFirstMon && allMoveFlags.Contains(EffectFlag.GOOD_FIRST_MON)) // Moves only for first mon are not chosen!
            {
                return 0;
            }
            else
            {
                // Assembly of tags rq
                (ElementType, string) moveNameTag = (ElementType.MOVE, move.Name);
                (ElementType, string) moveCatTag = (ElementType.MOVE_CATEGORY, move.Category.ToString());
                (ElementType, string) moveOTypeTag = (ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString());
                (ElementType, string) moveTypeTag = (ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()); // Won't be used if move non-damaging
                (ElementType, string) damagingMoveFlag = (ElementType.ANY_DAMAGING_MOVE, "-"); // Won't be used if move non-damaging
                bool moveIsDamaging = move.Category != MoveCategory.STATUS;
                // Quick check of what is disabled. Logic: If something was disabled and not re-enabled, then move can't be selected
                double aux;
                if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(moveNameTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveNameTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(moveCatTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveCatTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(moveOTypeTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveOTypeTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (moveIsDamaging) // Few extra checks for damaging moves
                {
                    if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(moveTypeTag))
                    {
                        if (monCtx.EnabledOptions.TryGetValue(moveTypeTag, out aux))
                        {
                            score *= aux;
                        }
                        else return 0;
                    }
                    if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(damagingMoveFlag))
                    {
                        if (monCtx.EnabledOptions.TryGetValue(damagingMoveFlag, out aux))
                        {
                            score *= aux;
                        }
                        else return 0;
                    }
                }
                foreach (EffectFlag effect in allMoveFlags)
                {
                    (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                    if (MechanicsDataContainers.GlobalMechanicsData.DisabledOptions.Contains(effectTag))
                    {
                        if (monCtx.EnabledOptions.TryGetValue(effectTag, out aux))
                        {
                            score *= aux;
                        }
                        else return 0;
                    }
                }
                // Ok, nothing disabled, so move on to calc weight
                if (!moveIsDamaging) // If status move, then the weight is its weight, but less if has low acc
                {
                    double moveAccuracy = move.Acc * GetMoveAccMods(move, monCtx) / 100; // 0-1 acc with mods
                    if (moveAccuracy == 0) moveAccuracy = 1; // Acc of 0 is reserved for no-miss
                    score *= moveAccuracy; // This is so if a status move is useful, but lower acc makes it less useful
                }
                else // If damage, then the damage it does will be scored too, with a value of 1 corresponding to 50% of the opp average HP
                {
                    // Check the actual stats of mon and opp
                    (double[] monStats, double[] monStatVariance) = MonStatCalculation(monCtx); // Get mon stats (variance is 0 anyway)
                    (double[] oppStats, _) = MonStatCalculation(monCtx, buildCtx, true); // Get opp stats and variance
                    double moveDamage = CalcMoveDamage(move, monCtx, monStats, oppStats, monStatVariance, buildCtx,
                        (mon.ChosenAbility?.Name == "Protean" || mon.ChosenAbility?.Name == "Libero"), // This will cause stab to be always active unless tera
                        mon.ChosenAbility?.Name == "Adaptability", // Adaptability and loaded dice affect move damage in nonlinear ways, sniper increases crit damage
                        mon.BattleItem?.Name == "Loaded Dice",
                        mon.ChosenAbility?.Name == "Sniper").Item1;
                    // Get the move coverage, making sure some specific crazy effects that modify moves
                    List<double> moveCoverage = CalculateOffensiveTypeCoverage(moveType, buildCtx.OpponentsTypes,
                        allMoveFlags.Contains(EffectFlag.BYPASSES_IMMUNITY), // Whether the move will bypass immunities
                        mon.ChosenAbility?.Name == "Tinted Lens", // Tinted lense x2 resisted moves
                        move.Name == "Freeze Dry"); // Freeze dry is SE against water
                    moveDamage *= IndymonUtilities.ArrayAverage(moveCoverage); // Average damage caused by move cvg
                    // Finally, how damage affects score
                    score *= (2 * moveDamage) / oppStats[0]; // A damage of 50% opp HP would have a score of 1
                }
                // Finally, all the mults associated with a move, another headache...
                // Initial weights first
                if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(moveNameTag, out aux))
                {
                    score *= aux;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(moveCatTag, out aux))
                {
                    score *= aux;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(moveOTypeTag, out aux))
                {
                    score *= aux;
                }
                if (moveIsDamaging) // Few extra checks for damaging moves
                {
                    if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(moveTypeTag, out aux))
                    {
                        score *= aux;
                    }
                    if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(damagingMoveFlag, out aux))
                    {
                        score *= aux;
                    }
                }
                foreach (EffectFlag effect in allMoveFlags)
                {
                    (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                    if (MechanicsDataContainers.GlobalMechanicsData.InitialWeights.TryGetValue(effectTag, out aux))
                    {
                        score *= aux;
                    }
                }
                // Then the smart mods (weights?)
                if (monCtx.WeightMods.TryGetValue(moveNameTag, out aux))
                {
                    score *= aux;
                }
                if (monCtx.WeightMods.TryGetValue(moveCatTag, out aux))
                {
                    score *= aux;
                }
                if (monCtx.WeightMods.TryGetValue(moveOTypeTag, out aux))
                {
                    score *= aux;
                }
                if (moveIsDamaging) // Few extra checks for damaging moves
                {
                    if (monCtx.WeightMods.TryGetValue(moveTypeTag, out aux))
                    {
                        score *= aux;
                    }
                    if (monCtx.WeightMods.TryGetValue(damagingMoveFlag, out aux))
                    {
                        score *= aux;
                    }
                }
                foreach (EffectFlag effect in allMoveFlags)
                {
                    (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                    if (monCtx.WeightMods.TryGetValue(effectTag, out aux))
                    {
                        score *= aux;
                    }
                }
                // And finally, if score non-0 add also the additive ones
                if (score > 0)
                {
                    if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(moveNameTag, out aux))
                    {
                        score += aux;
                    }
                    if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(moveCatTag, out aux))
                    {
                        score += aux;
                    }
                    if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(moveOTypeTag, out aux))
                    {
                        score += aux;
                    }
                    if (moveIsDamaging) // Few extra checks for damaging moves
                    {
                        if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(moveTypeTag, out aux))
                        {
                            score += aux;
                        }
                        if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(damagingMoveFlag, out aux))
                        {
                            score += aux;
                        }
                    }
                    foreach (EffectFlag effect in allMoveFlags)
                    {
                        (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                        if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(effectTag, out aux))
                        {
                            score += aux;
                        }
                    }
                }
                // Finally, we need to do the hypotetical, does this move add to defensive, offensive or speed utilities?
                mon.ChosenMoveset.Add(move); // First, equip this move to mon
                PokemonBuildInfo newCtx = ObtainPokemonSetContext(mon, buildCtx); // Check the new context
                double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
                double defImprovement = newCtx.DefenseScore / monCtx.DefenseScore;
                double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
                // If needs an improvement, will be accepted as long as some of the improvements succeeds
                int nImprovChecks = 0;
                int nImproveFails = 0;
                if (allMoveFlags.Contains(EffectFlag.OFF_UTILITY))
                {
                    nImprovChecks++;
                    if (dmgImprovement < 1.1) nImproveFails++;
                }
                if (allMoveFlags.Contains(EffectFlag.DEF_UTILITY))
                {
                    nImprovChecks++;
                    if (defImprovement < 1.1) nImproveFails++;
                }
                if (allMoveFlags.Contains(EffectFlag.SPEED_UTILITY))
                {
                    nImprovChecks++;
                    if (speedImprovement < 1.1) nImproveFails++;
                }
                if (nImproveFails == nImprovChecks) return 0; // If all checks failed, move not good
                score *= dmgImprovement * defImprovement * speedImprovement; // Then multiply all utilities gain, give or remove utility from final set!
                if (move.Flags.Contains(EffectFlag.HEAL)) // Healing status moves are weighted on whether the mon can actually make decent use of this
                {
                    score *= newCtx.Survivability;
                }
                mon.ChosenMoveset.RemoveAt(mon.ChosenMoveset.Count - 1); // Remove move ofc
            }
            return score;
        }
    }
}
