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
            public HashSet<(StatModifier, string)> ModifiedTypeEffectiveness = new HashSet<(StatModifier, string)>();
            public Dictionary<(ElementType, string), List<(MoveModifier, string)>> MoveMods = new Dictionary<(ElementType, string), List<(MoveModifier, string)>>();
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
            public double WeightMultiplier = 1;
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
        /// <param name="nMonInTeam">The order of mon in team, important to prioritize specific moves on different teamslots</param>
        /// <param name="nMons">Total number of mons in team</param>
        /// <param name="teamBuildConstraints">Constraints of team build present from the beginning of team build</param>
        /// <param name="teamCtx">Extra context of the fight, null if skips the context checks</param>
        /// <returns>The Pokemon build details</returns>
        static PokemonBuildInfo ObtainPokemonSetContext(TrainerPokemon pokemon, int nMonInTeam, int nMons, TeamBuildContext teamCtx = null)
        {
            Pokemon pokemonData = MechanicsDataContainers.GlobalMechanicsData.Dex[pokemon.Species]; // Get mon data from species
            // Create the result, dump all the team-based data into here
            PokemonBuildInfo result = new PokemonBuildInfo();
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
            foreach (EffectFlag flag in ability.Flags)
            {
                ExtractMods((ElementType.EFFECT_FLAGS, flag.ToString()), monCtx);
            }
        }
        /// <summary>
        /// Obtains all mods associated to this, updates Ctx
        /// </summary>
        /// <param name="move">Move</param>
        /// <param name="monCtx">Context where to add the mods</param>
        static void ExtractMoveMods(Move move, PokemonBuildInfo monCtx)
        {
            // Moves are the most complex ones
            ExtractMods((ElementType.MOVE, move.Name), monCtx);
            foreach (EffectFlag flag in move.Flags)
            {
                ExtractMods((ElementType.EFFECT_FLAGS, flag.ToString()), monCtx);
            }
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
                Stat affectedStat; // This may be useful as many of these mod affect stats
                int auxInt;
                switch (statMod.Item1)
                {
                    case StatModifier.WEIGHT_MULTIPLIER:
                        monCtx.WeightMultiplier *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.ATTACK_MULTIPLIER:
                    case StatModifier.DEFENSE_MULTIPLIER:
                    case StatModifier.SPECIAL_ATTACK_MULTIPLIER:
                    case StatModifier.SPECIAL_DEFENSE_MULTIPLIER:
                    case StatModifier.SPEED_MULTIPLIER:
                        affectedStat = statMod.Item1 switch
                        {
                            StatModifier.ATTACK_MULTIPLIER => Stat.ATTACK,
                            StatModifier.DEFENSE_MULTIPLIER => Stat.DEFENSE,
                            StatModifier.SPECIAL_ATTACK_MULTIPLIER => Stat.SPECIAL_ATTACK,
                            StatModifier.SPECIAL_DEFENSE_MULTIPLIER => Stat.SPECIAL_DEFENSE,
                            StatModifier.SPEED_MULTIPLIER => Stat.SPEED,
                            _ => throw new Exception("???")
                        };
                        monCtx.StatMultipliers[(int)affectedStat] *= double.Parse(statMod.Item2);
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
                        affectedStat = statMod.Item1 switch
                        {
                            StatModifier.OPP_HP_MULTIPLIER => Stat.HP,
                            StatModifier.OPP_ATTACK_MULTIPLIER => Stat.ATTACK,
                            StatModifier.OPP_DEFENSE_MULTIPLIER => Stat.DEFENSE,
                            StatModifier.OPP_SPECIAL_ATTACK_MULTIPLIER => Stat.SPECIAL_ATTACK,
                            StatModifier.OPP_SPECIAL_DEFENSE_MULTIPLIER => Stat.SPECIAL_DEFENSE,
                            StatModifier.OPP_SPEED_MULTIPLIER => Stat.SPEED,
                            _ => throw new Exception("???")
                        };
                        monCtx.OppStatMultipliers[(int)affectedStat] *= double.Parse(statMod.Item2);
                        break;
                    case StatModifier.ATTACK_BOOST:
                    case StatModifier.DEFENSE_BOOST:
                    case StatModifier.SPECIAL_ATTACK_BOOST:
                    case StatModifier.SPECIAL_DEFENSE_BOOST:
                    case StatModifier.SPEED_BOOST:
                        affectedStat = statMod.Item1 switch
                        {
                            StatModifier.ATTACK_BOOST => Stat.ATTACK,
                            StatModifier.DEFENSE_BOOST => Stat.DEFENSE,
                            StatModifier.SPECIAL_ATTACK_BOOST => Stat.SPECIAL_ATTACK,
                            StatModifier.SPECIAL_DEFENSE_BOOST => Stat.SPECIAL_DEFENSE,
                            StatModifier.SPEED_BOOST => Stat.SPEED,
                            _ => throw new Exception("???")
                        };
                        monCtx.StatBoosts[(int)affectedStat] += int.Parse(statMod.Item2);
                        break;
                    case StatModifier.OPP_ATTACK_BOOST:
                    case StatModifier.OPP_DEFENSE_BOOST:
                    case StatModifier.OPP_SPECIAL_ATTACK_BOOST:
                    case StatModifier.OPP_SPECIAL_DEFENSE_BOOST:
                    case StatModifier.OPP_SPEED_BOOST:
                        affectedStat = statMod.Item1 switch
                        {
                            StatModifier.OPP_ATTACK_BOOST => Stat.ATTACK,
                            StatModifier.OPP_DEFENSE_BOOST => Stat.DEFENSE,
                            StatModifier.OPP_SPECIAL_ATTACK_BOOST => Stat.SPECIAL_ATTACK,
                            StatModifier.OPP_SPECIAL_DEFENSE_BOOST => Stat.SPECIAL_DEFENSE,
                            StatModifier.OPP_SPEED_BOOST => Stat.SPEED,
                            _ => throw new Exception("???")
                        };
                        monCtx.OppStatBoosts[(int)affectedStat] += int.Parse(statMod.Item2);
                        break;
                    case StatModifier.HIGHEST_STAT_BOOST:
                        monCtx.OppStatBoosts[6] += int.Parse(statMod.Item2); // Will be stored here and calculated later
                        break;
                    case StatModifier.ALL_BOOSTS:
                        auxInt = int.Parse(statMod.Item2); // Will multiply all stat boosts
                        for (int i = 0; i < monCtx.StatBoosts.Length; i++)
                        {
                            monCtx.StatBoosts[i] *= auxInt;
                        }
                        break;
                    case StatModifier.ALL_OPP_BOOSTS:
                        auxInt = int.Parse(statMod.Item2); // Will multiply all stat boosts
                        for (int i = 0; i < monCtx.OppStatBoosts.Length; i++)
                        {
                            monCtx.OppStatBoosts[i] *= auxInt;
                        }
                        break;
                    case StatModifier.HP_EV:
                    case StatModifier.ATK_EV:
                    case StatModifier.DEF_EV:
                    case StatModifier.SPATK_EV:
                    case StatModifier.SPDEF_EV:
                    case StatModifier.SPEED_EV:
                        affectedStat = statMod.Item1 switch
                        {
                            StatModifier.HP_EV => Stat.HP,
                            StatModifier.ATK_EV => Stat.ATTACK,
                            StatModifier.DEF_EV => Stat.DEFENSE,
                            StatModifier.SPATK_EV => Stat.SPECIAL_ATTACK,
                            StatModifier.SPDEF_EV => Stat.SPECIAL_DEFENSE,
                            StatModifier.SPEED_EV => Stat.SPEED,
                            _ => throw new Exception("???")
                        };
                        monCtx.Evs[(int)affectedStat] += int.Parse(statMod.Item2);
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
            foreach (KeyValuePair<(ElementType, string), List<(MoveModifier, string)>> nextMoveMod in MechanicsDataContainers.GlobalMechanicsData.MoveModifiers[element]) // Modifies moves accordingly
            {
                // Some move mods will start stacking with each other, make sure we got them in the right way
                if (!monCtx.MoveMods.TryGetValue(nextMoveMod.Key, out List<(MoveModifier, string)> modList)) // If not there, need to create brand new
                {
                    modList = new List<(MoveModifier, string)>();
                    monCtx.MoveMods.Add(nextMoveMod.Key, modList);
                }
                // Then just add to the list
                modList.AddRange(modList);
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
