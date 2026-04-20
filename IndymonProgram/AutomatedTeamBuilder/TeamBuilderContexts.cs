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
        public HashSet<TeamArchetype> CurrentTeamArchetypes = new HashSet<TeamArchetype>(); // Contains an ongoing archetype that applies for all team
        public Weather CurrentWeather = Weather.NONE; // Ongoing weather
        public Terrain CurrentTerrain = Terrain.NONE; // Ongoing terrain
        public List<Constraint> TeamBuildConstraints = new List<Constraint>(); // Constraints applied to this team building
        public List<(PokemonType, PokemonType)> OpponentsTypes = new List<(PokemonType, PokemonType)>(); // Contains a list of all types found in opp teams
        public double[] OpponentsStats = new double[6]; // All opp stats in average
        public double[] OppStatVariance = new double[6]; // The variance of opp stat
        public double AverageOpponentWeight = 0; // Average weight of opponents
        public bool smartTeamBuild = true; // If NPC mons, the moves are just selected randomly withouth smart weights
    }
    /// <summary>
    /// The context of a currently building mon. Also used to store data about current mods and stuff
    /// </summary>
    public class PokemonBuildContext
    {
        // Things that are added on the transcourse of building a set, makes some other stuff more or less desirable
        public HashSet<TeamArchetype> AdditionalArchetypes = new HashSet<TeamArchetype>(); /// Contains archetypes created by this mon
        public Weather CurrentWeather = Weather.NONE; // Ongoing weather
        public Terrain CurrentTerrain = Terrain.NONE; // Ongoing terrain
        public List<Constraint> AdditionalConstraints = new List<Constraint>(); /// Teambuild constraint that are added due to required builds for example I NEED a specific move
        public Dictionary<(ElementType, string), double> EnabledOptions = new Dictionary<(ElementType, string), double>(); /// Things that normally are disabled but are now enabled, and the weight by where they were just enabled
        public HashSet<(StatModifier, string)> ModifiedTypeEffectiveness = new HashSet<(StatModifier, string)>(); /// Some modified type effectiveness for receiving damage
        public Dictionary<(ElementType, string), double> MoveBpMods = new Dictionary<(ElementType, string), double>(); /// All the stat mods that modified a moves BP
        public Dictionary<(ElementType, string), PokemonType> MoveTypeMods = new Dictionary<(ElementType, string), PokemonType>(); /// All the move types mods
        public Dictionary<(ElementType, string), double> MoveAccMods = new Dictionary<(ElementType, string), double>(); /// All the move accuracy mods
        public Dictionary<(ElementType, string), HashSet<EffectFlag>> AllAddedFlags = new Dictionary<(ElementType, string), HashSet<EffectFlag>>(); /// All the extra flags added to moves/abilities
        public Dictionary<(ElementType, string), HashSet<EffectFlag>> AllRemovedFlags = new Dictionary<(ElementType, string), HashSet<EffectFlag>>(); /// All the extra flags removed from moves/abilities
        public Dictionary<(ElementType, string), double> WeightMods = new Dictionary<(ElementType, string), double>();
        // Then stuff that alters current mon
        public string BaseSpecies = ""; // The species of the mon itself, can be modified by "some" stuff
        public string MonSuffix = ""; // The suffix used for this mon, normally an alternate form
        public string SpeciesOverride = ""; // Possible override of the species
        public string AbilityOverride = ""; // Possible override of a mons ability, use this for stuff calculation
        public Nature Nature = Nature.SERIOUS;
        public PokemonType TeraType = PokemonType.NONE;
        public (PokemonType, PokemonType) PokemonTypes = (PokemonType.NONE, PokemonType.NONE);
        public PokemonLogic MonLogic = PokemonLogic.DONT_REPEAT;
        public double[] MonStats = new double[6];
        public int[] Evs = new int[6];
        public double[] StatBoosts = new double[7]; // Where the 7th is not a stat per se, it's the "hightest" stat, applied last in stat calc
        public double StatBoostsMultiplier = 1;
        public double NegativeStatBoostsMultiplier = 1;
        public bool AddOppBoosts = false; // Where the user will instead use the opp boosts
        public double[] StatMultipliers = [1, 1, 1, 1, 1, 1];
        public double MonWeight = 1;
        public int CriticalStages = 0;
        public bool ShinyOverride = false;
        public double LevelMultiplier = 1;
        public string DefaultStatus = "";
        // Things that alter opp mon
        public double[] OppStatBoosts = new double[6];
        public double OppStatBoostsMultiplier = 1;
        public double[] OppStatMultipliers = [1, 1, 1, 1, 1, 1];
        // Battle sim (how much damage my attacks do, how much damage mon takes from stuff, speed creep)
        public double DamageScore = 1;
        public double Survivability = 1;
        public double SpeedScore = 1;
        public string GetPokemonSpecies()
        {
            string newSpecies = (SpeciesOverride != "") ? SpeciesOverride : BaseSpecies + MonSuffix;
            if (!MechanicsDataContainers.GlobalMechanicsData.Dex.ContainsKey(newSpecies)) newSpecies = BaseSpecies; // If mon doesn't exist (e.g. Pikachu-Pirouette) then just go normal form, avoids zen crown and others
            return newSpecies;
        }
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Given a Pokemon, scores and examines the current mon set, both in order to examine how valuable a specific set but also obtain many important characteristics of the final mon for simulation
        /// </summary>
        /// <param name="pokemon">The Pokemon with its current set</param>
        /// <param name="teamCtx">Extra context of the fight, null if skips the context checks</param>
        /// <returns>The Pokemon build details</returns>
        static PokemonBuildContext ObtainPokemonSetContext(TrainerPokemon pokemon, TeamBuildContext teamCtx)
        {
            PokemonBuildContext result = new PokemonBuildContext
            {
                BaseSpecies = pokemon.Species
            };
            // Dump all the team-based data into here
            result.AdditionalArchetypes.UnionWith(teamCtx.CurrentTeamArchetypes); // Add all archetypes present overall in the team
            List<Constraint> originalConstraints = [.. teamCtx.TeamBuildConstraints]; // Original constraints, will only care about these if non-smart
            result.AdditionalConstraints.AddRange(teamCtx.TeamBuildConstraints);
            result.CurrentTerrain = teamCtx.CurrentTerrain;
            result.CurrentWeather = teamCtx.CurrentWeather;
            // Check what level the mon is meant to be because all calcs are based on lvl 100, so I check the lvl modifier
            result.LevelMultiplier = pokemon.Level / 100.0;
            // Then obtain, step by step, all mods applied by all the (currently known) elements of the mon's build
            ExtractAlwaysMods(result); // Extract the mods that are present in absolutely everyhting
            ExtractPokeballMods(pokemon.PokeBall, result); // Pokeball can't change so extract its mods now
            if (pokemon.ModItem != null)
            {
                ExtractModItemMods(pokemon.ModItem, result);
            }
            if (pokemon.BattleItem != null)
            {
                ExtractBattleItemMods(pokemon.BattleItem, result);
            }
            else
            {
                ExtractMods((ElementType.ITEM_FLAGS, ItemFlag.NO_ITEM.ToString()), result); // Add "no item" to results flag
            }
            foreach (Move move in pokemon.ChosenMoveset)
            {
                if (move != null)
                {
                    ExtractMoveMods(move, result);
                }
            }
            // What ability should I consider for this?
            Ability currentAbility = (result.AbilityOverride != "") ? MechanicsDataContainers.GlobalMechanicsData.Abilities[result.AbilityOverride] : pokemon.ChosenAbility;
            if (currentAbility != null)
            {
                ExtractAbilityMods(currentAbility, result);
            }
            // Then weather/terrain/archetypes which have been modified by the existing things
            foreach (TeamArchetype archetype in result.AdditionalArchetypes)
            {
                ExtractArchetypeMods(archetype, result);
            }
            // All of these mods may have changed the pokemon itself (e.g. zen mode, pirouette, random shit, so only then we verify the mon and stuff)
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[result.GetPokemonSpecies()];
            result.MonStats = monData.Stats;
            result.MonWeight = monData.Weight;
            result.PokemonTypes = monData.Types; // Set base type
            ExtractMonMods(result);
            // Finally, gather all flags and apply flag mods but only once (e.g. 2 instances of same flag don't stack)
            // This only extracts the flags that are "present" and affect other's scores, but not caring too much about the ones that affect the move damage and effectiveness itself
            HashSet<EffectFlag> allFlags = [];
            if (currentAbility != null)
            {
                allFlags = [.. currentAbility.Flags]; // Ability flags are already 100% known as abilities aren't modded
            }
            foreach (Move move in pokemon.ChosenMoveset)
            {
                allFlags.UnionWith(ExtractMoveFlags(move, result));
            }
            foreach (EffectFlag flag in allFlags) // Finally, apply all flag mods once per flag
            {
                ExtractMods((ElementType.EFFECT_FLAGS, flag.ToString()), result);
            }
            // Finally, all of these may have affected weather or terrain, which are the last to be loaded
            ExtractWeatherMods(result.CurrentWeather, result);
            ExtractTerrainMods(result.CurrentTerrain, result);
            // At this point all stat mods should be loaded so I can see what's the current average status of the Pokemon's boosts
            List<double> statBoosts = [.. result.StatBoosts.Select(b => (b < 0) ? b * result.NegativeStatBoostsMultiplier : b)];
            double aggregateBoosts = statBoosts.Sum() * result.StatBoostsMultiplier;
            if (aggregateBoosts > 0) ExtractMods((ElementType.POKEMON_HAS_POSITIVE_BOOSTS, "-"), result);
            else if (aggregateBoosts < 0) ExtractMods((ElementType.POKEMON_HAS_NEGATIVE_BOOSTS, "-"), result);
            else { } // Neither positive nor negative boosts ongoing
            // Weather and specific weather/terrain-dependant effects
            if (pokemon.ChosenAbility?.Name == "Forecast") // Change type on weather
            {
                if (result.CurrentWeather == Weather.SUN)
                {
                    result.PokemonTypes = (PokemonType.FIRE, PokemonType.NONE);
                }
                if (result.CurrentWeather == Weather.RAIN)
                {
                    result.PokemonTypes = (PokemonType.WATER, PokemonType.NONE);
                }
                if (result.CurrentWeather == Weather.SNOW || result.CurrentWeather == Weather.HAIL)
                {
                    result.PokemonTypes = (PokemonType.ICE, PokemonType.NONE);
                }
            }
            else if (pokemon.ChosenAbility?.Name == "Mimicry" || pokemon.ChosenMoveset.Any(m => m?.Name == "Camouflage")) // Change type on terrain
            {
                if (result.CurrentTerrain == Terrain.GRASSY)
                {
                    result.PokemonTypes = (PokemonType.GRASS, PokemonType.NONE);
                }
                if (result.CurrentTerrain == Terrain.PSYCHIC)
                {
                    result.PokemonTypes = (PokemonType.PSYCHIC, PokemonType.NONE);
                }
                if (result.CurrentTerrain == Terrain.ELECTRIC)
                {
                    result.PokemonTypes = (PokemonType.ELECTRIC, PokemonType.NONE);
                }
                if (result.CurrentTerrain == Terrain.MISTY)
                {
                    result.PokemonTypes = (PokemonType.FAIRY, PokemonType.NONE);
                }
            }
            else
            {
                // No mon type changes
            }
            if (result.CurrentWeather == Weather.SNOW) // Snow makes the mon have 1.5xdef if ice
            {
                // Check either tera 
                bool isValidTera = (result.TeraType == PokemonType.ICE);
                bool hasValidMainType = (result.PokemonTypes.Item1 == PokemonType.ICE) || (result.PokemonTypes.Item2 == PokemonType.ICE);
                if (isValidTera || hasValidMainType)
                {
                    result.StatMultipliers[2] *= 1.5;
                }
            }
            if (result.CurrentWeather == Weather.SAND) // Sand makes the mon have 1.5 x spdef if rock
            {
                // Check either tera 
                bool isValidTera = (result.TeraType == PokemonType.ROCK);
                bool hasValidMainType = (result.PokemonTypes.Item1 == PokemonType.ROCK) || (result.PokemonTypes.Item2 == PokemonType.ROCK);
                if (isValidTera || hasValidMainType)
                {
                    result.StatMultipliers[4] *= 1.5;
                }
            }
            // Finally, need to obtain offensive/defensive/speed scores to do some comparisons
            (double[] monStats, _) = MonStatCalculation(result, teamCtx, false, 1, 1); // Get mon stats (variance is 0 anyway)
            (double[] oppStats, double[] oppVariance) = MonStatCalculation(result, teamCtx, true, 1, 1); // Get opp stats and variance
            // Offensive value calculation (this can be only done with 2 or more moves, otherwise comparing offensive utility gets weird when adding the first move
            if (teamCtx.smartTeamBuild) // Non-smart building won't care about this
            {
                List<double> movesDamage = [];
                List<List<double>> movesTypeCoverage = [];
                // Calculate damage of moves, if no moves yet, will do some basic stab placeholders at 80BP
                List<Move> movesToEvaluate = [.. pokemon.ChosenMoveset.Where(m => m != null)];
                bool hypotheticalMoves = false;
                if (!movesToEvaluate.Any(m => m.Category != MoveCategory.STATUS)) // If mon has no damaging moves (only status) will use a placeholder set of moves to get their damaging utility atleast
                {
                    hypotheticalMoves = true;
                    if (result.PokemonTypes.Item1 != PokemonType.NONE)
                    {
                        movesToEvaluate.Add(new Move()
                        {
                            Name = "Physical 1",
                            Type = result.PokemonTypes.Item1,
                            Bp = 70,
                            Acc = 100,
                            Category = MoveCategory.PHYSICAL,
                            Flags = []
                        });
                        movesToEvaluate.Add(new Move()
                        {
                            Name = "Special 1",
                            Type = result.PokemonTypes.Item1,
                            Bp = 70,
                            Acc = 100,
                            Category = MoveCategory.SPECIAL,
                            Flags = []
                        });
                    }
                    if (result.PokemonTypes.Item2 != PokemonType.NONE)
                    {
                        movesToEvaluate.Add(new Move()
                        {
                            Name = "Physical 2",
                            Type = result.PokemonTypes.Item2,
                            Bp = 70,
                            Acc = 100,
                            Category = MoveCategory.PHYSICAL,
                            Flags = []
                        });
                        movesToEvaluate.Add(new Move()
                        {
                            Name = "Special 2",
                            Type = result.PokemonTypes.Item2,
                            Bp = 70,
                            Acc = 100,
                            Category = MoveCategory.SPECIAL,
                            Flags = []
                        });
                    }
                }
                // Check all moves
                foreach (Move move in movesToEvaluate)
                {
                    if (move == null) continue; // Nothing to calculate if hard switch
                    if (move.Category == MoveCategory.STATUS) continue; // We don't check for status moves
                    movesDamage.Add(CalcMoveDamage(move, result, 100 * result.LevelMultiplier, teamCtx,
                        (pokemon.ChosenAbility?.Name == "Protean" || pokemon.ChosenAbility?.Name == "Libero"), // This will cause stab to be always active unless tera
                        pokemon.ChosenAbility?.Name == "Adaptability", // Adaptability and loaded dice affect move damage in nonlinear ways, sniper adds to crit dmg
                        pokemon.BattleItem?.Name == "Loaded Dice",
                        pokemon.ChosenAbility?.Name == "Skill Link",
                        pokemon.ChosenAbility?.Name == "Sniper"));
                    PokemonType moveType = GetModifiedMoveType(move, result); // Get the final move type for type effectiveness
                    // Get the move coverage, making sure some specific crazy effects that modify moves
                    double seMultiplier = 1;
                    if (pokemon.BattleItem?.Name == "Expert Belt") seMultiplier *= 1.2;
                    if (pokemon.ChosenAbility?.Name == "Neuroforce") seMultiplier *= 1.25;
                    movesTypeCoverage.Add(CalculateOffensiveTypeCoverage(moveType, teamCtx.OpponentsTypes,
                        ExtractMoveFlags(move, result).Contains(EffectFlag.BYPASSES_IMMUNITY), // Whether the move will bypass immunities
                        pokemon.ChosenAbility?.Name == "Tinted Lens", // Tinted lense x2 resisted moves
                        move.Name == "Freeze Dry", // Freeze dry is SE against water
                        seMultiplier
                        ));
                }
                // Do some magic to calculate average move damage with average type coverage
                List<double> bestCaseMoveCoverage;
                double avgMoveDamage;
                if (hypotheticalMoves) // Hyp moves would be unfair for calcs so choose the best possible with the coverage it has, work from there
                {
                    bestCaseMoveCoverage = [];
                    for (int i = 0; i < movesDamage.Count; i++)
                    {
                        double avgCoverage = movesTypeCoverage[i].Average();
                        avgCoverage *= movesDamage[i];
                        bestCaseMoveCoverage.Add(avgCoverage);
                    }
                    avgMoveDamage = bestCaseMoveCoverage.Max(); // Choose the better one
                    const double MIN_DAMAGE_PRECENT = 0.2; // Cap min damage to 20% of opp hp, assuming scald, seismic toss would be better in these cases
                    if ((MIN_DAMAGE_PRECENT * oppStats[0]) > avgMoveDamage)
                    {
                        avgMoveDamage = oppStats[0] * MIN_DAMAGE_PRECENT;
                    }
                }
                else
                {
                    bestCaseMoveCoverage = GeneralUtilities.ArrayMax(movesTypeCoverage);
                    avgMoveDamage = (movesDamage.Sum() / movesToEvaluate.Count) * bestCaseMoveCoverage.Average(); // Average takes into account ALL moves due to expected opportunity loss
                }
                // Now, check how the damage I can output works, assume I can deal between 0-X HP of damage (more is "overkill")
                const double DAMAGE_OVERKILL_THRESHOLD = 2;
                result.DamageScore = avgMoveDamage / (DAMAGE_OVERKILL_THRESHOLD * oppStats[0]);
                result.DamageScore = Math.Clamp(result.DamageScore, 0.1, 1); // The score is clamped to 1 to avoid giving lots of points to overkill setup/items. In the bottom, it's clamped to 0.1 to avoid the unnecessary boosting of low damage mons
                // Little trick to ensure the mon tries to get a move coverage that can hit every mon (e.g. if there's immunities)
                if (!hypotheticalMoves && bestCaseMoveCoverage.Min() > 0)
                {
                    result.DamageScore *= 2;
                }
            }
            // Defensive value calculation
            if (teamCtx.smartTeamBuild) // Non-smart building won't care about this
            {
                // Gets hit by a ton of moves, both physical and special, equivalent to every single move stab in the game, average both
                // This gives me the average punch in the face I get, my HP is evaluated inside this
                // This makes underkills make my hp be close to 1 and overkills make it tend to 0
                // Improvements will then involve moves move me in this curve but if I gain defense for nothing, or is not enough anyway, then it's ok
                (PokemonType, PokemonType) defendingPokemonType = (result.TeraType != PokemonType.NONE && result.TeraType != PokemonType.STELLAR) ? (result.TeraType, PokemonType.NONE) : result.PokemonTypes;
                List<double> moveDamagesReceived = EvaluateDefenderMoveTaken(defendingPokemonType, result, teamCtx); // All the damages by all types in weather calculating placeholder damage
                double averageDamage = moveDamagesReceived.Average();
                if (averageDamage == 0) averageDamage = 1; // Always 1HP damage to avoid div 0
                //  Checks how "bulky" a mon is, which increases surv rating, measured in #hits survived
                result.Survivability = monStats[0] / averageDamage; // This tells us how many hits the pokemon can take before dying
                if (result.Survivability <= 1 && (pokemon.BattleItem?.Name == "Focus Sash" || pokemon.ChosenAbility?.Name == "Sturdy")) // If mon would be one-shotted, it's not
                {
                    result.Survivability += 1; // Then the mon lasts 1 more turn anyway
                }
                if (pokemon.BattleItem?.Name == "Focus Band")
                {
                    result.Survivability += 0.1; // Idk what this would do honestly
                }
            }
            // Speed value calculation
            if (teamCtx.smartTeamBuild) // Non-smart building won't care about this
            {
                // Just compare my speed with opps speed, nothing crazy
                Normal oppSpeedDistro = new Normal(oppStats[5], Math.Sqrt(oppVariance[5])); // Get the std dev ofc
                result.SpeedScore = oppSpeedDistro.CumulativeDistribution(monStats[5]); // Compare my speed w opp speed
                if (result.AdditionalArchetypes.Contains(TeamArchetype.TRICK_ROOM)) // Trick room inverts this
                {
                    result.SpeedScore = 1 - result.SpeedScore; // Inverts percentile since it will be symmetric around opp speed
                }
                result.SpeedScore = Math.Clamp(result.SpeedScore, 0.1, 1); // Min value 0.1 to avoid some improvements suddenly dividing by 0
            }
            if (!teamCtx.smartTeamBuild) // Keep only original constraints, this way we can keep the team-defined constraints but random moves don't add constraints
            {
                result.AdditionalConstraints = originalConstraints;
            }
            return result;
        }
    }
}
