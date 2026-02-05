using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Given a mod item, obtains all mods associated to this item, updates Ctx
        /// </summary>
        /// <param name="archetype">Archetype</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractArchetypeMods(TeamArchetype archetype, PokemonBuildInfo monCtx)
        {
            // Once archetype is active, it's simple to find all the effects caused by it
            ExtractMods((ElementType.ARCHETYPE, archetype.ToString()), monCtx);
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx
        /// </summary>
        /// <param name="item">Mod item</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractItemMods(Item item, PokemonBuildInfo monCtx)
        {
            // This one is trickier, need to add both the items and the flags
            ExtractMods((ElementType.BATTLE_ITEM, item.Name), monCtx);
            foreach (ItemFlag flag in item.Flags)
            {
                ExtractMods((ElementType.ITEM_FLAGS, flag.ToString()), monCtx);
            }
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx
        /// </summary>
        /// <param name="ability">Ability</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractAbilityMods(Ability ability, PokemonBuildInfo monCtx)
        {
            // This one is trickier, need to add both the ability and the flag
            ExtractMods((ElementType.ABILITY, ability.Name), monCtx);
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx. Flags will be done afterwards after all elements have added ALL_FLAGS or REMOVE_FLAGS
        /// </summary>
        /// <param name="move">Move</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractMoveMods(Move move, PokemonBuildInfo monCtx)
        {
            if (move == null) return; // Null (pivot) doesn't have any of these
            // Moves are the most complex ones
            ExtractMods((ElementType.MOVE, move.Name), monCtx);
            ExtractMods((ElementType.MOVE_CATEGORY, move.Category.ToString()), monCtx);
            if (move.Category != MoveCategory.STATUS) // Damaging moves
            {
                ExtractMods((ElementType.ANY_DAMAGING_MOVE, "-"), monCtx); // Damaging move detected
                ExtractMods((ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString()), monCtx);
            }
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx
        /// </summary>
        /// <param name="mon">The pokemon in question</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractMonMods(TrainerPokemon mon, PokemonBuildInfo monCtx)
        {
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species]; // Obtain species of mon
            ExtractMods((ElementType.POKEMON, mon.Species), monCtx); // Mon activates stuff
            // Types are weird because they're modified before, so the mon needs to extract the ones of the current type at the last moment
            if (monCtx.TeraType != PokemonType.NONE)
            {
                ExtractMods((ElementType.POKEMON_TYPE, monCtx.TeraType.ToString()), monCtx);
            }
            else
            {
                ExtractMods((ElementType.POKEMON_TYPE, monCtx.PokemonTypes.Item1.ToString()), monCtx);
                ExtractMods((ElementType.POKEMON_TYPE, monCtx.PokemonTypes.Item2.ToString()), monCtx);
            }
            // Finally the eviolite thing, advertise whether has evo or not
            ExtractMods((ElementType.POKEMON_HAS_EVO, (monData.Evos.Count > 0).ToString().ToUpper()), monCtx);
        }
        /// <summary>
        /// Extract all the corresponding mods from a specific element regardless where it came from
        /// </summary>
        /// <param name="element">Element that causes the mod (type+key)</param>
        /// <param name="monCtx">Mon ctx where to add mods</param>
        static void ExtractMods((ElementType, string) element, PokemonBuildInfo monCtx)
        {
            // First, what this element enables
            foreach (KeyValuePair<(ElementType, string), double> nextEnabled in MechanicsDataContainers.GlobalMechanicsData.Enablers[element]) // One by one what this thing enables
            {
                if (nextEnabled.Key.Item1 == ElementType.ARCHETYPE) // Archetypes enabled are added elsewhere
                {
                    monCtx.AdditionalArchetypes.Add(Enum.Parse<TeamArchetype>(nextEnabled.Key.Item2)); // Add the archetype as obtained from string
                }
                else // Just update the enablement weight
                {
                    if (!monCtx.EnabledOptions.ContainsKey(nextEnabled.Key))
                    {
                        monCtx.EnabledOptions.Add(nextEnabled.Key, 1);
                    }
                    monCtx.EnabledOptions[nextEnabled.Key] *= nextEnabled.Value;
                }
            }
            // Then, forceds. If an item/ability/move asks for something to exist no matter what
            List<(ElementType, string)> forcedList = new List<(ElementType, string)>();
            foreach ((ElementType, string) nextForced in MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds[element]) // Finds what this element forces, if forces multiple things, only one needs to fulfill
            {
                forcedList.Add(nextForced);
            }
            if (forcedList.Count > 0)
            {
                monCtx.AdditionalConstraints.AllConstraints.Add(forcedList);
            }
            // Then, stat mods, these are funny because some mods are applied directly
            foreach ((StatModifier, string) statMod in MechanicsDataContainers.GlobalMechanicsData.StatModifiers[element])
            {
                int auxIndex; // This may be useful as many of these mod affect stats
                int auxInt;
                int[] auxStatBoostArray;
                switch (statMod.Item1)
                {
                    case StatModifier.WEIGHT_MULTIPLIER:
                        monCtx.MonWeight *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.HP_MULTIPLIER:
                    case StatModifier.ATTACK_MULTIPLIER:
                    case StatModifier.DEFENSE_MULTIPLIER:
                    case StatModifier.SPECIAL_ATTACK_MULTIPLIER:
                    case StatModifier.SPECIAL_DEFENSE_MULTIPLIER:
                    case StatModifier.SPEED_MULTIPLIER:
                        auxIndex = statMod.Item1 switch
                        {
                            StatModifier.HP_MULTIPLIER => 0,
                            StatModifier.ATTACK_MULTIPLIER => 1,
                            StatModifier.DEFENSE_MULTIPLIER => 2,
                            StatModifier.SPECIAL_ATTACK_MULTIPLIER => 3,
                            StatModifier.SPECIAL_DEFENSE_MULTIPLIER => 4,
                            StatModifier.SPEED_MULTIPLIER => 5,
                            _ => throw new Exception("???")
                        };
                        monCtx.StatMultipliers[auxIndex] *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.PHYSICAL_ACCURACY_MULTIPLIER:
                        monCtx.PhysicalAccuracyMultiplier *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.SPECIAL_ACCURACY_MULTIPLIER:
                        monCtx.SpecialAccuracyMultiplier *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.OPP_HP_MULTIPLIER:
                    case StatModifier.OPP_ATTACK_MULTIPLIER:
                    case StatModifier.OPP_DEFENSE_MULTIPLIER:
                    case StatModifier.OPP_SPECIAL_ATTACK_MULTIPLIER:
                    case StatModifier.OPP_SPECIAL_DEFENSE_MULTIPLIER:
                    case StatModifier.OPP_SPEED_MULTIPLIER:
                        auxIndex = statMod.Item1 switch
                        {
                            StatModifier.OPP_HP_MULTIPLIER => 0,
                            StatModifier.OPP_ATTACK_MULTIPLIER => 1,
                            StatModifier.OPP_DEFENSE_MULTIPLIER => 2,
                            StatModifier.OPP_SPECIAL_ATTACK_MULTIPLIER => 3,
                            StatModifier.OPP_SPECIAL_DEFENSE_MULTIPLIER => 4,
                            StatModifier.OPP_SPEED_MULTIPLIER => 5,
                            _ => throw new Exception("???")
                        };
                        monCtx.OppStatMultipliers[auxIndex] *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.ATTACK_BOOST:
                    case StatModifier.DEFENSE_BOOST:
                    case StatModifier.SPECIAL_ATTACK_BOOST:
                    case StatModifier.SPECIAL_DEFENSE_BOOST:
                    case StatModifier.SPEED_BOOST:
                    case StatModifier.OPP_ATTACK_BOOST:
                    case StatModifier.OPP_DEFENSE_BOOST:
                    case StatModifier.OPP_SPECIAL_ATTACK_BOOST:
                    case StatModifier.OPP_SPECIAL_DEFENSE_BOOST:
                    case StatModifier.OPP_SPEED_BOOST:
                        switch (statMod.Item1)
                        {
                            case StatModifier.ATTACK_BOOST:
                                auxIndex = 1;
                                auxStatBoostArray = monCtx.StatBoosts;
                                break;
                            case StatModifier.DEFENSE_BOOST:
                                auxIndex = 2;
                                auxStatBoostArray = monCtx.StatBoosts;
                                break;
                            case StatModifier.SPECIAL_ATTACK_BOOST:
                                auxIndex = 3;
                                auxStatBoostArray = monCtx.StatBoosts;
                                break;
                            case StatModifier.SPECIAL_DEFENSE_BOOST:
                                auxIndex = 4;
                                auxStatBoostArray = monCtx.StatBoosts;
                                break;
                            case StatModifier.SPEED_BOOST:
                                auxIndex = 5;
                                auxStatBoostArray = monCtx.StatBoosts;
                                break;
                            case StatModifier.OPP_ATTACK_BOOST:
                                auxIndex = 1;
                                auxStatBoostArray = monCtx.OppStatBoosts;
                                break;
                            case StatModifier.OPP_DEFENSE_BOOST:
                                auxIndex = 2;
                                auxStatBoostArray = monCtx.OppStatBoosts;
                                break;
                            case StatModifier.OPP_SPECIAL_ATTACK_BOOST:
                                auxIndex = 3;
                                auxStatBoostArray = monCtx.OppStatBoosts;
                                break;
                            case StatModifier.OPP_SPECIAL_DEFENSE_BOOST:
                                auxIndex = 4;
                                auxStatBoostArray = monCtx.OppStatBoosts;
                                break;
                            case StatModifier.OPP_SPEED_BOOST:
                                auxIndex = 5;
                                auxStatBoostArray = monCtx.OppStatBoosts;
                                break;
                            default:
                                throw new Exception("???");
                        }
                        auxInt = auxStatBoostArray[auxIndex] + int.Parse(statMod.Item2); // Add next boost to current boosts
                        auxInt = Math.Clamp(auxInt, -6, 6); // Stat boost can't surpass 6
                        auxStatBoostArray[auxIndex] = auxInt;
                        break;
                    case StatModifier.CRIT_BOOST:
                        monCtx.CriticalStages += int.Parse(statMod.Item2);
                        break;
                    case StatModifier.HIGHEST_STAT_BOOST:
                        monCtx.OppStatBoosts[6] += int.Parse(statMod.Item2); // Will be stored here and calculated later
                        break;
                    case StatModifier.ALL_BOOSTS:
                        monCtx.StatBoostsMultiplier *= int.Parse(statMod.Item2);
                        break;
                    case StatModifier.ALL_OPP_BOOSTS:
                        monCtx.OppStatBoostsMultiplier *= int.Parse(statMod.Item2);
                        break;
                    case StatModifier.HP_EV:
                    case StatModifier.ATK_EV:
                    case StatModifier.DEF_EV:
                    case StatModifier.SPATK_EV:
                    case StatModifier.SPDEF_EV:
                    case StatModifier.SPEED_EV:
                        auxIndex = statMod.Item1 switch
                        {
                            StatModifier.HP_EV => 0,
                            StatModifier.ATK_EV => 1,
                            StatModifier.DEF_EV => 2,
                            StatModifier.SPATK_EV => 3,
                            StatModifier.SPDEF_EV => 4,
                            StatModifier.SPEED_EV => 5,
                            _ => throw new Exception("???")
                        };
                        monCtx.Evs[auxIndex] += int.Parse(statMod.Item2);
                        break;
                    case StatModifier.NATURE:
                        monCtx.Nature = Enum.Parse<Nature>(statMod.Item2);
                        break;
                    case StatModifier.TYPE_1:
                        monCtx.PokemonTypes = (Enum.Parse<PokemonType>(statMod.Item2), monCtx.PokemonTypes.Item2);
                        break;
                    case StatModifier.TYPE_2:
                        monCtx.PokemonTypes = (monCtx.PokemonTypes.Item1, Enum.Parse<PokemonType>(statMod.Item2));
                        break;
                    case StatModifier.TERA:
                        monCtx.TeraType = Enum.Parse<PokemonType>(statMod.Item2);
                        break;
                    case StatModifier.NULLIFIES_RECV_DAMAGE_OF_TYPE: // If this happens, deal with later during the calc
                    case StatModifier.DOUBLES_RECV_DAMAGE_OF_TYPE:
                    case StatModifier.HALVES_RECV_DAMAGE_OF_TYPE:
                    case StatModifier.HALVES_RECV_SE_DAMAGE_OF_TYPE:
                    case StatModifier.ALTER_RECV_SE_DAMAGE:
                    case StatModifier.ALTER_RECV_NON_SE_DAMAGE:
                        monCtx.ModifiedTypeEffectiveness.Add(statMod);
                        break;
                    default:
                        throw new Exception("Unhandled stat boost");
                }
            }
            // Then, move mods, add all move mods to queue
            foreach (KeyValuePair<(ElementType, string), Dictionary<MoveModifier, string>> nextMoveMod in MechanicsDataContainers.GlobalMechanicsData.MoveModifiers[element]) // Modifies moves accordingly
            {
                // Move mods are very complex so I'd rather split them into 5 parts (all the mods currently)
                if (nextMoveMod.Value.TryGetValue(MoveModifier.MOVE_BP_MOD, out string auxValue)) // There's a BP mod
                {
                    double modValue = double.Parse(auxValue);
                    if (!monCtx.MoveBpMods.ContainsKey(nextMoveMod.Key))
                    {
                        monCtx.MoveBpMods.Add(nextMoveMod.Key, 1);
                    }
                    monCtx.MoveBpMods[nextMoveMod.Key] *= modValue;
                }
                if (nextMoveMod.Value.TryGetValue(MoveModifier.MOVE_ACC_MOD, out auxValue)) // There's an acc mod
                {
                    double modValue = double.Parse(auxValue);
                    if (!monCtx.MoveAccMods.ContainsKey(nextMoveMod.Key))
                    {
                        monCtx.MoveAccMods.Add(nextMoveMod.Key, 1);
                    }
                    monCtx.MoveAccMods[nextMoveMod.Key] *= modValue;
                }
                if (nextMoveMod.Value.TryGetValue(MoveModifier.MOVE_TYPE_MOD, out auxValue))
                {
                    PokemonType modType = Enum.Parse<PokemonType>(auxValue);
                    monCtx.MoveTypeMods[nextMoveMod.Key] = modType; // This one is weird because if exists, ill just need to replace, shouldn't happen tho
                }
                if (nextMoveMod.Value.TryGetValue(MoveModifier.ADD_FLAG, out auxValue))
                {
                    EffectFlag flag = Enum.Parse<EffectFlag>(auxValue);
                    if (!monCtx.AllAddedFlags.TryGetValue(nextMoveMod.Key, out HashSet<EffectFlag> value))
                    {
                        value = [];
                        monCtx.AllAddedFlags.Add(nextMoveMod.Key, value);
                    }

                    value.Add(flag);
                }
                if (nextMoveMod.Value.TryGetValue(MoveModifier.REMOVE_FLAG, out auxValue))
                {
                    EffectFlag flag = Enum.Parse<EffectFlag>(auxValue);
                    if (!monCtx.AllRemovedFlags.TryGetValue(nextMoveMod.Key, out HashSet<EffectFlag> value))
                    {
                        value = [];
                        monCtx.AllRemovedFlags.Add(nextMoveMod.Key, value);
                    }

                    value.Add(flag);
                }
            }
            // Then, weight mods, these ones are funny because they need to re-multiply if already existing
            foreach (KeyValuePair<(ElementType, string), double> weightMod in MechanicsDataContainers.GlobalMechanicsData.WeightModifiers[element])
            {
                if (!monCtx.WeightMods.ContainsKey(weightMod.Key))
                {
                    monCtx.EnabledOptions.Add(weightMod.Key, 1);
                }
                monCtx.EnabledOptions[weightMod.Key] *= weightMod.Value;
            }
        }
    }
}
