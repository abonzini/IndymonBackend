using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    /// <summary>
    /// A context of things going on, to be able to build mons sets. Includes opp average profile, typechart, and ongoing archetypes if present
    /// </summary>
    public class TeamBuildContext
    {
        public int currentMon = 0;
        public HashSet<TeamArchetype> CurrentTeamArchetypes = new HashSet<TeamArchetype>(); // Contains an ongoing archetype that applies for all team
        public TeamBuildConstraints TeamBuildConstraints = new TeamBuildConstraints(); // Constraints applied to this team building. Different meaning to team build, as this is a list of all necessary stuff (A+B)*(C+D)
        public List<List<PokemonType>> OpponentsTypes = new List<List<PokemonType>>(); // Contains a list of all types found in opp teams
        public double[] OpponentsStats = new double[6]; // All opp stats in average
        public double OppSpeedVariance = 0; // Speed is special as I need the variance to calculate speed creep
        public double AverageOpponentWeight = 0; // Average weight of opponents
        public bool smartTeamBuild = true; // If NPC mons, the moves are just selected randomly withouth smart weights
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// The context of a currently building mon. Also used to store data about current mods and stuff
        /// </summary>
        class PokemonBuildInfo
        {
            // Things that are added on the transcourse of building a set, makes some other stuff more or less desirable
            public HashSet<TeamArchetype> AdditionalArchetypes = new HashSet<TeamArchetype>(); /// Contains archetypes created by this mon
            public TeamBuildConstraints AdditionalConstraints = new TeamBuildConstraints(); /// Teambuild constraint that are added due to required builds (unless unable to complete obviously)
            public Dictionary<(ElementType, string), double> EnabledOptions = new Dictionary<(ElementType, string), double>(); /// Things that normally are disabled but are now enabled, and the weight by where they were just enabled
            public HashSet<(StatModifier, string)> ModifiedTypeEffectiveness = new HashSet<(StatModifier, string)>(); /// Some modified type effectiveness for receiving damage
            public Dictionary<(ElementType, string), double> MoveBpMods = new Dictionary<(ElementType, string), double>(); /// All the stat mods that modified a moves BP
            public Dictionary<(ElementType, string), PokemonType> MoveTypeMods = new Dictionary<(ElementType, string), PokemonType>(); /// All the move types mods
            public Dictionary<(ElementType, string), double> MoveAccMods = new Dictionary<(ElementType, string), double>(); /// All the move accuracy mods
            public Dictionary<(ElementType, string), HashSet<EffectFlag>> AllAddedFlags = new Dictionary<(ElementType, string), HashSet<EffectFlag>>(); /// All the extra flags added to moves/abilities
            public Dictionary<(ElementType, string), HashSet<EffectFlag>> AllRemovedFlags = new Dictionary<(ElementType, string), HashSet<EffectFlag>>(); /// All the extra flags removed from moves/abilities
            public Dictionary<(ElementType, string), double> WeightMods = new Dictionary<(ElementType, string), double>();
            // Then stuff that alters current mon
            public Nature Nature = Nature.SERIOUS;
            public PokemonType TeraType = PokemonType.NONE;
            public PokemonType[] PokemonTypes = [PokemonType.NONE, PokemonType.NONE];
            public int[] Evs = new int[6];
            public int[] StatBoosts = new int[7]; // Where the 7th is not a stat per se, it's the "hightest" stat, applied last in stat calc
            public double[] StatMultipliers = [1, 1, 1, 1, 1, 1];
            public double PhysicalAccuracyMultiplier = 1;
            public double SpecialAccuracyMultiplier = 1;
            public double MonWeight = 1;
            public int CriticalStages = 0;
            // Things that alter opp mon
            public int[] OppStatBoosts = new int[6];
            public double[] OppStatMultipliers = [1, 1, 1, 1, 1, 1];
            // Battle sim (how much damage my attacks do, how much damage mon takes from stuff, speed creep)
            public double DamageScore = 1;
            public double DefenseScore = 1;
            public double SpeedScore = 1;
        }
        /// <summary>
        /// Given a Pokemon, scores and examines the current mon set, both in order to examine how valuable a specific set but also obtain many important characteristics of the final mon for simulation
        /// </summary>
        /// <param name="pokemon">The Pokemon with its current set</param>
        /// <param name="teamCtx">Extra context of the fight, null if skips the context checks</param>
        /// <returns>The Pokemon build details</returns>
        static PokemonBuildInfo ObtainPokemonSetContext(TrainerPokemon pokemon, TeamBuildContext teamCtx = null)
        {
            PokemonBuildInfo result = new PokemonBuildInfo();
            // First, need to load mon base types
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[pokemon.Species];
            result.PokemonTypes[0] = monData.Types[0];
            result.PokemonTypes[1] = monData.Types[1];
            result.MonWeight = monData.Weight;
            // Dump all the team-based data into here
            if (teamCtx != null)
            {
                result.AdditionalArchetypes.UnionWith(teamCtx.CurrentTeamArchetypes); // Add all archetypes present overall in the team
                result.AdditionalConstraints = teamCtx.TeamBuildConstraints.Clone();
            }
            // Then obtain, step by step, all mods applied by all the (currently known) elements of the mon's build
            foreach (TeamArchetype archetype in result.AdditionalArchetypes)
            {
                ExtractArchetypeMods(archetype, result);
            }
            if (pokemon.ModItem != null)
            {
                ExtractModItemMods(pokemon.ModItem, result);
            }
            if (pokemon.BattleItem != null)
            {
                ExtractBattleItemMods(pokemon.BattleItem, result);
            }
            if (pokemon.ChosenAbility != null)
            {
                ExtractAbilityMods(pokemon.ChosenAbility, result);
            }
            foreach (Move move in pokemon.ChosenMoveset)
            {
                if (move != null)
                {
                    ExtractMoveMods(move, result);
                }
            }
            ExtractMonMods(pokemon, result);
            // Finally, gather all flags and apply flag mods but onyl once (e.g. 2 instances of same flag don't stack)
            HashSet<EffectFlag> allFlags = [];
            if (pokemon.ChosenAbility != null)
            {
                allFlags = [.. pokemon.ChosenAbility.Flags]; // Ability flags are already 100% known as abilities aren't modded
            }
            foreach (Move move in pokemon.ChosenMoveset)
            {
                if (move != null)
                {
                    allFlags.UnionWith(ExtractMoveFlags(move, result));
                }
            }
            foreach (EffectFlag flag in allFlags) // Finally, apply all flag mods once per flag
            {
                ExtractMods((ElementType.EFFECT_FLAGS, flag.ToString()), result);
            }
            // Finally, need to obtain offensive/defensive/speed scores
            if (teamCtx != null) // This can only occur if I know the context of battle
            {
                // TODO: Actually calculate the 3 scores
            }
            return result;
        }
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
        static void ExtractModItemMods(ModItem item, PokemonBuildInfo monCtx)
        {
            // For active mod items, it's also very simple
            ExtractMods((ElementType.MOD_ITEM, item.Name), monCtx);
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx
        /// </summary>
        /// <param name="item">Mod item</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractBattleItemMods(BattleItem item, PokemonBuildInfo monCtx)
        {
            // This one is trickier, need to add both the items and the flags
            ExtractMods((ElementType.BATTLE_ITEM, item.Name), monCtx);
            foreach (BattleItemFlag flag in item.Flags)
            {
                ExtractMods((ElementType.BATTLE_ITEM_FLAGS, flag.ToString()), monCtx);
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
        /// Obtains all flags applied to this move. To be called last to ensure everything has had time to modify flags
        /// </summary>
        /// <param name="move">Move</param>
        /// <param name="monCtx">Context to see which flags are added/removed from move</param>
        /// <returns>All flags in this move</returns>
        static HashSet<EffectFlag> ExtractMoveFlags(Move move, PokemonBuildInfo monCtx)
        {
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
            bool moveTypeChanged;
            PokemonType moveType = move.Type;
            do
            {
                moveTypeChanged = false;
                addedFlags.UnionWith(monCtx.AllAddedFlags[(ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString())]);
                removedFlags.UnionWith(monCtx.AllRemovedFlags[(ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString())]);
                PokemonType moddedType = GetModifiedMoveType(move, monCtx);
                if (moddedType != moveType)
                {
                    moveType = moddedType;
                    moveTypeChanged = true;
                }
            } while (moveTypeChanged);
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
            // Checks the move type mod everywhere (including own flagsbut not the added flags)
            moveType = monCtx.MoveTypeMods.GetValueOrDefault((ElementType.MOVE, move.Name), moveType);
            moveType = monCtx.MoveTypeMods.GetValueOrDefault((ElementType.MOVE_CATEGORY, move.Category.ToString()), moveType);
            moveType = monCtx.MoveTypeMods.GetValueOrDefault((ElementType.ANY_DAMAGING_MOVE, "-"), moveType);
            moveType = monCtx.MoveTypeMods.GetValueOrDefault((ElementType.DAMAGING_MOVE_OF_TYPE, move.Type.ToString()), moveType);
            return moveType;
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
                ExtractMods((ElementType.POKEMON_TYPE, monCtx.PokemonTypes[0].ToString()), monCtx);
                ExtractMods((ElementType.POKEMON_TYPE, monCtx.PokemonTypes[1].ToString()), monCtx);
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
            foreach ((ElementType, string) nextForced in MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds[element]) // Finds what this element forces
            {
                monCtx.AdditionalConstraints.AllConstraints.Add([nextForced]); // Add to all the big list of constrtaints (no issue if repeated anyway)
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
                    case StatModifier.ATTACK_MULTIPLIER:
                    case StatModifier.DEFENSE_MULTIPLIER:
                    case StatModifier.SPECIAL_ATTACK_MULTIPLIER:
                    case StatModifier.SPECIAL_DEFENSE_MULTIPLIER:
                    case StatModifier.SPEED_MULTIPLIER:
                        auxIndex = statMod.Item1 switch
                        {
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
                    case StatModifier.ALL_OPP_BOOSTS:
                        auxStatBoostArray = statMod.Item1 switch
                        {
                            StatModifier.ALL_BOOSTS => monCtx.StatBoosts,
                            StatModifier.ALL_OPP_BOOSTS => monCtx.OppStatBoosts,
                            _ => throw new Exception("???"),
                        };
                        auxInt = int.Parse(statMod.Item2); // Will multiply all stat boosts
                        for (int i = 0; i < auxStatBoostArray.Length; i++)
                        {
                            int resultingBoost = auxStatBoostArray[i] * auxInt;
                            resultingBoost = Math.Clamp(resultingBoost, -6, 6); // Clamp to +-6
                            auxStatBoostArray[i] *= auxInt;
                        }
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
                        monCtx.PokemonTypes[0] = Enum.Parse<PokemonType>(statMod.Item2);
                        break;
                    case StatModifier.TYPE_2:
                        monCtx.PokemonTypes[1] = Enum.Parse<PokemonType>(statMod.Item2);
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
