using GameData;
using MechanicsData;
using MechanicsDataContainer;

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
        /// <returns></returns>
        static double CalcPlaceholderMoveDamage(double bp, MoveCategory category, double[] attackerStats, double[] defenderStats)
        {
            double attackingStat = (category == MoveCategory.PHYSICAL) ? attackerStats[1] : attackerStats[3];
            double defendingStat = (category == MoveCategory.PHYSICAL) ? defenderStats[2] : defenderStats[4];
            // Actual calculation    
            double hitDamage = 42; // This depends on mon level so keep in mind
            hitDamage *= attackingStat;
            double remainingFactor = bp / (defendingStat * 50); // rest of the damage formula
            hitDamage *= remainingFactor;
            hitDamage += 2; // This is the hit damage, no variance needed here
            return hitDamage;
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
        static double CalcMoveDamage(Move move, PokemonBuildContext monCtx, double[] attackerStats, double[] defenderStats, TeamBuildContext battleContext, bool alwaysStab = false, bool extraStab = false, bool loadedDice = false, bool skillLink = false, bool highCritDamage = false)
        {
            // First, get the ACTUAL flags of the move (because some may have been added/removed
            HashSet<EffectFlag> moveFlags = ExtractMoveFlags(move, monCtx);
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
                else if (moveFlags.Contains(EffectFlag.POSITIVE_STAT_BOOST))
                {
                    double numberOfStatBoosts = 0;
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
                if (moveAccuracy == 0 || moveAccuracy > 1) moveAccuracy = 1; // 0 Acc means sure hit
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
                hitDamage *= attackingStat;
                double remainingFactor = moveBp / (defendingStat * 50); // rest of the damage formula
                hitDamage *= remainingFactor;
                hitDamage += 2; // This is the hit damage, no variance needed here
                // Crit chance
                int critStages = 0;
                if (moveFlags.Contains(EffectFlag.HIGH_CRIT)) critStages++;
                if (moveFlags.Contains(EffectFlag.CRITICAL)) critStages = 3;
                critStages += monCtx.CriticalStages;
                critStages = Math.Clamp(critStages, 0, 3); // Max is 3
                double critChance = critStages switch // Chance of crit depending on crit stages
                {
                    1 => 0.125,
                    2 => 0.5,
                    3 => 1,
                    _ => 0.0417
                };
                double critDamage = (highCritDamage) ? 2.25 : 1.5;
                double critMultiplier = (critChance * critDamage) + ((1 - critChance) * 1); // Crit may increase a hit damage in average
                hitDamage *= critMultiplier;
                // Stab, check if tera is involved
                double stabBonus = 1;
                PokemonType moveType = GetModifiedMoveType(move, monCtx);
                if (moveType != PokemonType.STELLAR && moveType != PokemonType.NONE) // If move type is not crazy, then may apply stab
                {
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
                        if (monCtx.PokemonTypes.Item1 == moveType || monCtx.PokemonTypes.Item2 == moveType || alwaysStab)
                        {
                            stabBonus = (extraStab) ? 2 : 1.5;
                        }
                    }
                }
                hitDamage *= stabBonus;
                // Random
                double randomHitRoll = (100.0 + 85.0) / (2.0 * 100.0); // Hit roll (in float...)
                hitDamage *= randomHitRoll;
                // Finally, Acc application, left for last because of multihit
                if (moveFlags.Contains(EffectFlag.MULTIHIT_2_MOVE))
                {
                    // No data so I assume it's always 2 hits subject to the first one hitting
                    double multihitModifier = 2 * moveAccuracy;
                    hitDamage *= multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_3_MOVE))
                {
                    // No data so I assume it's always 3 hits subject to the first one hitting
                    double multihitModifier = 3 * moveAccuracy;
                    hitDamage *= multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_2_TO_5_MOVE))
                {
                    // If connects, then accuracy
                    double hits;
                    if (skillLink)
                    {
                        hits = 5;
                    }
                    else if (loadedDice)
                    {
                        hits = 4.5;
                    }
                    else
                    {
                        hits = 3.1;
                    }
                    double multihitModifier = hits * moveAccuracy;
                    hitDamage *= multihitModifier;
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_3_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (skillLink || loadedDice)
                    {
                        double multihitModifier = 6 * moveAccuracy; // 6 because it's 1,2,3 each hit with single accuracy check
                        hitDamage *= multihitModifier;
                    }
                    else
                    {
                        // Manually because I'm lazy to think
                        double multihitModifier = 6 * moveAccuracy * moveAccuracy * moveAccuracy +
                            3 * moveAccuracy * moveAccuracy * (1 - moveAccuracy) +
                            moveAccuracy * (1 - moveAccuracy) * (1 - moveAccuracy);
                        hitDamage *= multihitModifier;
                    }
                }
                else if (moveFlags.Contains(EffectFlag.MULTIHIT_ACC_BASED_10_HIT))
                {
                    // This one is the hardest, each hit has a chance and stops if doesnt hit, if loaded dice however, there's 1 acc check at beginning
                    if (skillLink)
                    {
                        double multihitModifier = 10 * moveAccuracy; // 10 guaranteed times subject to one check
                        hitDamage *= multihitModifier;
                    }
                    else if (loadedDice)
                    {
                        double multihitModifier = 7 * moveAccuracy; // 7 because 4-10 with equal chance equals to 7 average and then a single acc check
                        hitDamage *= multihitModifier;
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
        /// <summary>
        /// Gets the move Bp mods
        /// </summary>
        /// <param name="move">Which move</param>
        /// <param name="monCtx">Ctx that contains the mods</param>
        /// <returns>The move Bp multiplier</returns>
        static double GetMoveBpMods(Move move, PokemonBuildContext monCtx)
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
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.ANY_MOVE, "-"), 1); // Without any type mod
            // Type-based
            result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1); // With type mod
            // Flag based
            HashSet<EffectFlag> moveFlags = ExtractMoveFlags(move, monCtx);
            foreach (EffectFlag flag in moveFlags)
            {
                result *= monCtx.MoveBpMods.GetValueOrDefault((ElementType.EFFECT_FLAGS, flag.ToString()), 1);
            }
            return result;
        }
        /// <summary>
        /// Gets the move Accuracy mods
        /// </summary>
        /// <param name="move">Which move</param>
        /// <param name="monCtx">Ctx that contains the mods</param>
        /// <returns>The move Accuracy multiplier</returns>
        static double GetMoveAccMods(Move move, PokemonBuildContext monCtx)
        {
            double result = 1;
            PokemonType moveType = GetModifiedMoveType(move, monCtx);
            // Get mods that affect this move specifically (1 if none)
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE, move.Name), 1);
            // Get mods that affect moves of a specific category
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), 1);
            // Apply type ones (and type mod) if move type changed, there may be more mods
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString()), 1); // Without any type mod
            // Get flags that affect acc
            HashSet<EffectFlag> moveFlags = ExtractMoveFlags(move, monCtx);
            foreach (EffectFlag flag in moveFlags)
            {
                result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.EFFECT_FLAGS, flag.ToString()), 1);
            }
            // Get mods that affect damaging moves
            if (move.Category != MoveCategory.STATUS)
            {
                result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), 1);
                result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), 1); // With type mod
            }
            result *= monCtx.MoveAccMods.GetValueOrDefault((ElementType.ANY_MOVE, "-"), 1);
            return result;
        }
        /// <summary>
        /// Obtains all flags applied to this move. To be called last to ensure everything has had time to modify flags
        /// </summary>
        /// <param name="move">Move</param>
        /// <param name="monCtx">Context to see which flags are added/removed from move</param>
        /// <returns>All flags in this move</returns>
        static HashSet<EffectFlag> ExtractMoveFlags(Move move, PokemonBuildContext monCtx)
        {
            if (move == null) return [EffectFlag.PIVOT]; // Null move (switch) is basically a pivot
            HashSet<EffectFlag> moveFlags = [.. move.Flags]; // Copies moves base flags
            HashSet<EffectFlag> removedFlags = [];
            HashSet<EffectFlag> addedFlags = [];
            HashSet<EffectFlag> auxFlags;
            // Check what has been added
            auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.MOVE, move.Name), []);
            addedFlags.UnionWith(auxFlags);
            auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), []);
            addedFlags.UnionWith(auxFlags);
            auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.ANY_MOVE, "-"), []);
            addedFlags.UnionWith(auxFlags);
            if (move.Category != MoveCategory.STATUS)
            {
                auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), []);
                addedFlags.UnionWith(auxFlags);
            }
            auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString()), []);
            addedFlags.UnionWith(auxFlags);
            // Check what has been removed
            auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.MOVE, move.Name), []);
            removedFlags.UnionWith(auxFlags);
            auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), []);
            removedFlags.UnionWith(auxFlags);
            auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.ANY_MOVE, "-"), []);
            removedFlags.UnionWith(auxFlags);
            if (move.Category != MoveCategory.STATUS)
            {
                auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), []);
                removedFlags.UnionWith(auxFlags);
            }
            auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString()), []);
            removedFlags.UnionWith(auxFlags);
            // For the type that has potentially been modified
            PokemonType moveType = GetModifiedMoveType(move, monCtx);
            if (move.Category != MoveCategory.STATUS)
            {
                auxFlags = monCtx.AllAddedFlags.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), []);
                addedFlags.UnionWith(auxFlags);
                auxFlags = monCtx.AllRemovedFlags.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()), []);
                removedFlags.UnionWith(auxFlags);
            }
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
        static PokemonType GetModifiedMoveType(Move move, PokemonBuildContext monCtx)
        {
            PokemonType moveType = move.Type;
            if (move.Name == "Revelation Dance") // Revelation dance overrides everything so I don't get cool mods
            {
                moveType = (monCtx.TeraType != PokemonType.NONE && monCtx.TeraType != PokemonType.STELLAR) ? monCtx.TeraType : monCtx.PokemonTypes.Item1;
            }
            else
            {
                bool typeChanged;
                do
                {
                    typeChanged = false;
                    PokemonType newType = moveType;
                    // Checks the move type mod everywhere (including own flagsbut not the added flags)
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.MOVE, move.Name), out PokemonType typeMod)) newType = typeMod;
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.MOVE_CATEGORY, move.Category.ToString()), out typeMod)) newType = typeMod;
                    if (move.Category != MoveCategory.STATUS)
                    {
                        if (monCtx.MoveTypeMods.TryGetValue((ElementType.ANY_DAMAGING_MOVE, "-"), out typeMod)) newType = typeMod;
                        if (monCtx.MoveTypeMods.TryGetValue((ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString()), out typeMod)) newType = typeMod;
                    }
                    if (monCtx.MoveTypeMods.TryGetValue((ElementType.ANY_MOVE, "-"), out typeMod)) newType = typeMod;
                    if (newType != moveType)
                    {
                        moveType = newType;
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
        /// <param name="isLastMon">Whether it's the last mon evaluated or not</param>
        /// <returns>A move score</returns>
        static double GetMoveWeight(Move move, TrainerPokemon mon, PokemonBuildContext monCtx, TeamBuildContext buildCtx, bool isFirstMon, bool isLastMon)
        {
            // Get the move mods
            HashSet<EffectFlag> allMoveFlags = ExtractMoveFlags(move, monCtx); // Get all the flags a move has
            PokemonType moveType = GetModifiedMoveType(move, monCtx); // Get the final move type for type effectiveness
            // Beginning of scoring
            double score = 1;
            if (allMoveFlags.Contains(EffectFlag.DOUBLES_ONLY)) // Doubles moves can never be chosen
            {
                return 0;
            }
            else if (!isFirstMon && allMoveFlags.Contains(EffectFlag.GOOD_FIRST_MON)) // Moves only for first mon are not chosen!
            {
                return 0;
            }
            else if (!isLastMon && allMoveFlags.Contains(EffectFlag.GOOD_LAST_MON)) // Moves only for last mon are not chosen!
            {
                return 0;
            }
            else
            {
                // Assembly of tags rq
                (ElementType, string) moveNameTag = (ElementType.MOVE, move.Name);
                (ElementType, string) moveCatTag = (ElementType.MOVE_CATEGORY, move.Category.ToString());
                (ElementType, string) moveOTypeTag = (ElementType.ORIGINAL_TYPE_OF_MOVE, move.Type.ToString());
                (ElementType, string) damagingMoveOfTypeTag = (ElementType.DAMAGING_MOVE_OF_TYPE, moveType.ToString()); // Won't be used if move non-damaging
                (ElementType, string) damagingMoveTag = (ElementType.ANY_DAMAGING_MOVE, "-"); // Won't be used if move non-damaging
                (ElementType, string) anyMoveTag = (ElementType.ANY_MOVE, "-");
                bool moveIsDamaging = move.Category != MoveCategory.STATUS;
                // Quick check of what is disabled. Logic: If something was disabled and not re-enabled, then move can't be selected
                double aux;
                if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(moveNameTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveNameTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(moveCatTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveCatTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(moveOTypeTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(moveOTypeTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(anyMoveTag))
                {
                    if (monCtx.EnabledOptions.TryGetValue(anyMoveTag, out aux))
                    {
                        score *= aux;
                    }
                    else return 0;
                }
                if (moveIsDamaging) // Few extra checks for damaging moves
                {
                    if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(damagingMoveOfTypeTag))
                    {
                        if (monCtx.EnabledOptions.TryGetValue(damagingMoveOfTypeTag, out aux))
                        {
                            score *= aux;
                        }
                        else return 0;
                    }
                    if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(damagingMoveTag))
                    {
                        if (monCtx.EnabledOptions.TryGetValue(damagingMoveTag, out aux))
                        {
                            score *= aux;
                        }
                        else return 0;
                    }
                }
                foreach (EffectFlag effect in allMoveFlags)
                {
                    (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                    if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(effectTag))
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
                    if (moveAccuracy == 0 || moveAccuracy > 1) moveAccuracy = 1; // Acc of 0 is reserved for no-miss, acc has a max of 1
                    score *= moveAccuracy; // This is so if a status move is useful, but lower acc makes it less useful
                }
                else // If damage, then the damage it does will be scored too, with a value of 1 corresponding to 50% of the opp average HP
                {
                    // Check the actual stats of mon and opp
                    (double[] monStats, _) = MonStatCalculation(monCtx); // Get mon stats (variance is 0 anyway)
                    (double[] oppStats, _) = MonStatCalculation(monCtx, buildCtx, true); // Get opp stats and variance
                    double moveDamage = CalcMoveDamage(move, monCtx, monStats, oppStats, buildCtx,
                        (mon.ChosenAbility?.Name == "Protean" || mon.ChosenAbility?.Name == "Libero"), // This will cause stab to be always active unless tera
                        mon.ChosenAbility?.Name == "Adaptability", // Adaptability and loaded dice affect move damage in nonlinear ways, sniper increases crit damage
                        mon.BattleItem?.Name == "Loaded Dice",
                        mon.ChosenAbility?.Name == "Skill Link",
                        mon.ChosenAbility?.Name == "Sniper");
                    // Get the move coverage, making sure some specific crazy effects that modify moves
                    List<double> moveCoverage = CalculateOffensiveTypeCoverage(moveType, buildCtx.OpponentsTypes,
                        allMoveFlags.Contains(EffectFlag.BYPASSES_IMMUNITY), // Whether the move will bypass immunities
                        mon.ChosenAbility?.Name == "Tinted Lens", // Tinted lense x2 resisted moves
                        move.Name == "Freeze Dry", // Freeze dry is SE against water
                        (mon.BattleItem?.Name == "Expert Belt") ? 1.2 : 1 // Expert belt multiplies SE damage by 1.2
                    );
                    moveDamage *= moveCoverage.Average(); // Average damage caused by move cvg
                    // Finally, how damage affects score
                    score *= (2 * moveDamage) / oppStats[0]; // A damage of 50% opp HP would have a score of 1
                }
                // Finally, all the mults associated with a move, another headache...
                // Smart mods (weights?)
                score *= monCtx.WeightMods.GetValueOrDefault(moveNameTag, 1);
                score *= monCtx.WeightMods.GetValueOrDefault(moveCatTag, 1);
                score *= monCtx.WeightMods.GetValueOrDefault(moveOTypeTag, 1);
                score *= monCtx.WeightMods.GetValueOrDefault(anyMoveTag, 1);
                if (moveIsDamaging) // Few extra checks for damaging moves
                {
                    score *= monCtx.WeightMods.GetValueOrDefault(damagingMoveOfTypeTag, 1);
                    score *= monCtx.WeightMods.GetValueOrDefault(damagingMoveTag, 1);
                }
                foreach (EffectFlag effect in allMoveFlags)
                {
                    (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                    score *= monCtx.WeightMods.GetValueOrDefault(effectTag, 1);
                }
                // And finally, if score non-0 add also the additive ones
                if (score > 0)
                {
                    score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(moveNameTag, 0);
                    score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(moveCatTag, 0);
                    score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(moveOTypeTag, 0);
                    score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(anyMoveTag, 0);
                    if (moveIsDamaging) // Few extra checks for damaging moves
                    {
                        score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(damagingMoveOfTypeTag, 0);
                        score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(damagingMoveTag, 0);
                    }
                    foreach (EffectFlag effect in allMoveFlags)
                    {
                        (ElementType, string) effectTag = (ElementType.EFFECT_FLAGS, effect.ToString());
                        score += MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.GetValueOrDefault(effectTag, 0);
                    }
                }
                // Finally, we need to do the hypotetical, does this move add to defensive, offensive or speed utilities?
                mon.ChosenMoveset.Add(move); // First, equip this move to mon
                PokemonBuildContext newCtx = ObtainPokemonSetContext(mon, buildCtx); // Check the new context
                double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
                double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
                // Defensive score for moves is a tad different because the def is dependent on the move either being used and in some cases, used afterwards
                // So the improvement on "how much hits do I last" will be calculated accordingly
                double oldDamageInHp = 1 / monCtx.Survivability;
                double newDamageInHp = 1 / newCtx.Survivability;
                double hpAfterHit = 1 - oldDamageInHp; // Hp to calculate new survivability now (implicitly subtracting 1)
                double newSurvivability = hpAfterHit / newDamageInHp;
                newSurvivability += 1; // Return the prev hit to it
                double defImprovement = Math.Ceiling(newSurvivability) / Math.Ceiling(monCtx.Survivability); // Calculated surv improvement
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
                if (nImprovChecks > 0 && nImproveFails == nImprovChecks)
                {
                    score *= 0; // If all checks failed, move not good
                }
                else
                {
                    score *= dmgImprovement * defImprovement * speedImprovement; // Then multiply all utilities gain, give or remove utility from final set!
                }
                if (move.Flags.Contains(EffectFlag.HEAL)) // Healing status moves are weighted on whether the mon can actually make decent use of this
                {
                    // This is calculated from the prev context, assuming the move not used yet
                    score *= newCtx.Survivability / 3; // If you can take 3 hits or more you're officially a bulky mon (because most recovery is 50% based)
                }
                if (move.Category == MoveCategory.STATUS && monCtx.DamageScore > 0.75)
                {
                    score *= 0; // Avoid status move if the mon would break most shit apart
                }
                mon.ChosenMoveset.RemoveAt(mon.ChosenMoveset.Count - 1); // Remove move ofc
            }
            return score;
        }
    }
}
