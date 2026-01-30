using GameData;
using MathNet.Numerics.Distributions;
using MechanicsData;
using MechanicsDataContainer;
using Utilities;

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
        public List<(PokemonType, PokemonType)> OpponentsTypes = new List<(PokemonType, PokemonType)>(); // Contains a list of all types found in opp teams
        public double[] OpponentsStats = new double[6]; // All opp stats in average
        public double[] OppStatVariance = new double[6]; // The variance of opp stat
        public double AverageOpponentWeight = 0; // Average weight of opponents
        public bool smartTeamBuild = true; // If NPC mons, the moves are just selected randomly withouth smart weights
    }
    /// <summary>
    /// The context of a currently building mon. Also used to store data about current mods and stuff
    /// </summary>
    internal class PokemonBuildInfo
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
        public (PokemonType, PokemonType) PokemonTypes = (PokemonType.NONE, PokemonType.NONE);
        public string MonSuffix = "";
        public double[] MonStats = new double[6];
        public int[] Evs = new int[6];
        public int[] StatBoosts = new int[7]; // Where the 7th is not a stat per se, it's the "hightest" stat, applied last in stat calc
        public int StatBoostsMultiplier = 1;
        public double[] StatMultipliers = [1, 1, 1, 1, 1, 1];
        public double PhysicalAccuracyMultiplier = 1;
        public double SpecialAccuracyMultiplier = 1;
        public double MonWeight = 1;
        public int CriticalStages = 0;
        // Things that alter opp mon
        public int[] OppStatBoosts = new int[6];
        public int OppStatBoostsMultiplier = 1;
        public double[] OppStatMultipliers = [1, 1, 1, 1, 1, 1];
        // Battle sim (how much damage my attacks do, how much damage mon takes from stuff, speed creep)
        public double DamageScore = 1;
        public double DefenseScore = 1;
        public double SpeedScore = 1;
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Given a Pokemon, scores and examines the current mon set, both in order to examine how valuable a specific set but also obtain many important characteristics of the final mon for simulation
        /// </summary>
        /// <param name="pokemon">The Pokemon with its current set</param>
        /// <param name="teamCtx">Extra context of the fight, null if skips the context checks</param>
        /// <param name="ignoreMostRecientMove">Ignores most recient move for off score, just to verify off score retroactive improvements without move dmaage affecting</param>
        /// <returns>The Pokemon build details</returns>
        static PokemonBuildInfo ObtainPokemonSetContext(TrainerPokemon pokemon, TeamBuildContext teamCtx = null, bool ignoreMostRecientMove = false)
        {
            PokemonBuildInfo result = new PokemonBuildInfo();
            // First, need to load mon base stuff
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[pokemon.Species];
            // And other stuff
            result.MonStats = monData.Stats;
            result.MonWeight = monData.Weight;
            // Dump all the team-based data into here
            if (teamCtx != null)
            {
                result.AdditionalArchetypes.UnionWith(teamCtx.CurrentTeamArchetypes); // Add all archetypes present overall in the team
                result.AdditionalConstraints = teamCtx.TeamBuildConstraints.Clone();
            }
            // Then, a calculation of mon's current type that may involve some hardcoded shenanigans
            result.PokemonTypes = monData.Types;// Set base type
            if (pokemon.ChosenAbility?.Name == "Forecast") // Change type on weather
            {
                if (result.AdditionalArchetypes.Contains(TeamArchetype.SUN))
                {
                    result.PokemonTypes = (PokemonType.FIRE, PokemonType.NONE);
                }
                if (result.AdditionalArchetypes.Contains(TeamArchetype.RAIN))
                {
                    result.PokemonTypes = (PokemonType.WATER, PokemonType.NONE);
                }
                if (result.AdditionalArchetypes.Contains(TeamArchetype.SNOW))
                {
                    result.PokemonTypes = (PokemonType.ICE, PokemonType.NONE);
                }
            }
            else if (pokemon.ChosenAbility?.Name == "Mimicry" || pokemon.ChosenMoveset.Any(m => m.Name == "Camouflage")) // Change type on terrain
            {
                if (result.AdditionalArchetypes.Contains(TeamArchetype.GRASSY_TERRAIN))
                {
                    result.PokemonTypes = (PokemonType.GRASS, PokemonType.NONE);
                }
                if (result.AdditionalArchetypes.Contains(TeamArchetype.PSY_TERRAIN))
                {
                    result.PokemonTypes = (PokemonType.PSYCHIC, PokemonType.NONE);
                }
                if (result.AdditionalArchetypes.Contains(TeamArchetype.ELE_TERRAIN))
                {
                    result.PokemonTypes = (PokemonType.ELECTRIC, PokemonType.NONE);
                }
                if (result.AdditionalArchetypes.Contains(TeamArchetype.MISTY_TERRAIN))
                {
                    result.PokemonTypes = (PokemonType.FAIRY, PokemonType.NONE);
                }
            }
            else
            {
                // No mon type changes
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
                (double[] monStats, double[] monStatVariance) = MonStatCalculation(result); // Get mon stats (variance is 0 anyway)
                (double[] oppStats, double[] oppVariance) = MonStatCalculation(result, teamCtx, true); // Get opp stats and variance
                // Offensive value calculation (this can be only done with 1 or more moves naturally otherwise set to 1
                {
                    List<double> movesDamage = [];
                    List<List<double>> movesTypeCoverage = [];
                    // Check all moves but ignore the last one if calculating improvement
                    for (int i = 0; i < (ignoreMostRecientMove ? pokemon.ChosenMoveset.Count - 1 : pokemon.ChosenMoveset.Count); i++)
                    {
                        Move move = pokemon.ChosenMoveset[i];
                        if (move.Category == MoveCategory.STATUS) continue; // We don't check for status moves
                        // Get move damage without variance
                        movesDamage.Add(CalcMoveDamage(move, result, monStats, oppStats, monStatVariance, teamCtx,
                            (pokemon.ChosenAbility?.Name == "Protean" || pokemon.ChosenAbility?.Name == "Libero"), // This will cause stab to be always active unless tera
                            pokemon.ChosenAbility?.Name == "Adaptability", // Adaptability and loaded dice affect move damage in nonlinear ways
                            pokemon.BattleItem?.Name == "Loaded Dice").Item1);
                        PokemonType moveType = GetModifiedMoveType(move, result); // Get the final move type for type effectiveness
                        // Get the move coverage, making sure some specific crazy effects that modify moves
                        movesTypeCoverage.Add(CalculateOffensiveTypeCoverage(moveType, teamCtx.OpponentsTypes,
                            ExtractMoveFlags(move, result).Contains(EffectFlag.BYPASSES_IMMUNITY), // Whether the move will bypass immunities
                            pokemon.ChosenAbility?.Name == "Tinted Lens", // Tinted lense x2 resisted moves
                            move.Name == "Freeze Dry")); // Freeze dry is SE against water
                    }
                    if (movesDamage.Count > 0) // There will be a offensive score then
                    {
                        // Do some magic to calculate average move damage with average type coverage
                        List<double> bestCaseMoveCoverage = IndymonUtilities.ArrayMax(movesTypeCoverage);
                        double averageMoveDamage = IndymonUtilities.ArrayAverage(movesDamage) * IndymonUtilities.ArrayAverage(bestCaseMoveCoverage);
                        // Finally the offensive score will be a function of the damage I do as a fucntion of the normal distro of opp HP
                        // This makes underkills have values of <0.5, and overkills values that ->1
                        // Improvements will then involve moves that make the move approach overkill, but increasing overkill will have diminishing returns
                        Normal enemyHpDistro = new Normal(oppStats[0], Math.Sqrt(oppVariance[0])); // Get the std dev ofc
                        result.DamageScore = enemyHpDistro.CumulativeDistribution(averageMoveDamage);
                    }
                }
                // Defensive value calculation
                {
                    // Gets hit by a ton of moves, both physical and special, equivalent to every single move stab in the game, average both
                    // This gives me the average punch in the face I get, my HP is evaluated inside this
                    // This makes underkills make my hp be close to 1 and overkills make it tend to 0
                    // Improvements will then involve moves move me in this curve but if I gain defense for nothing, or is not enough anyway, then it's ok
                    Move sampleMove = new Move()
                    {
                        Name = "Sample Move",
                        Type = PokemonType.NONE, // Irrelevant for base damage calc
                        Category = MoveCategory.PHYSICAL,
                        Bp = 80, // The Bp doesn't matter too much I do 80/100 as an average OK move
                        Acc = 100,
                    };
                    (double physicalDamage, double physicalVariance) = CalcMoveDamage(sampleMove, result, oppStats, monStats, oppVariance, teamCtx);
                    sampleMove.Category = MoveCategory.SPECIAL; // Also calc special move
                    (double specialDamage, double specialVariance) = CalcMoveDamage(sampleMove, result, oppStats, monStats, oppVariance, teamCtx);
                    double averageDamage = (physicalDamage + specialDamage) / 2;
                    double averageDamageVariance = (physicalVariance + specialVariance) / 4;
                    // Also get the average damage of all stabs punching me in the face, affect damage and variance accordingly
                    List<double> moveStabsReceived = CalculateDefensiveTypeStabCoverage(result.PokemonTypes, teamCtx.OpponentsTypes, result.ModifiedTypeEffectiveness);
                    double averageStabReceived = IndymonUtilities.ArrayAverage(moveStabsReceived);
                    averageDamage *= averageStabReceived;
                    averageDamageVariance *= averageStabReceived * averageStabReceived;
                    // Do the normal thing now
                    Normal damageReceivedDistro = new Normal(averageDamage, Math.Sqrt(averageDamageVariance)); // Get the std dev ofc
                    result.DefenseScore = damageReceivedDistro.CumulativeDistribution(monStats[0]); // Compare my HP with this damage
                }
                // Speed value calculation
                {
                    // Just compare my speed with opps speed, nothing crazy
                    Normal oppSpeedDistro = new Normal(oppStats[5], Math.Sqrt(oppVariance[5])); // Get the std dev ofc
                    result.SpeedScore = oppSpeedDistro.CumulativeDistribution(monStats[5]); // Compare my speed w opp speed
                    if (result.AdditionalArchetypes.Contains(TeamArchetype.TRICK_ROOM)) // Trick room inverts this
                    {
                        result.SpeedScore = 1 - result.SpeedScore; // Inverts percentile since it will be symmetric around opp speed
                    }
                }
            }
            return result;
        }
    }
}
