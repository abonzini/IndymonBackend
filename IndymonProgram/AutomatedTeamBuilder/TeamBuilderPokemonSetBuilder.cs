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
        public List<TeamArchetype> CurrentTeamArchetypes = new List<TeamArchetype>(); // Contains an ongoing archetype that applies for all team
        TeamBuildConstraints teamBuildConstraints = new TeamBuildConstraints(); // Constraints applied to this team building
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
            public TeamBuildConstraints AdditionalConstraints = new TeamBuildConstraints(); /// Teambuild constraint that are added due to required builds (unless unable to complete obviously)
            public Dictionary<(ElementType, string), float> EnabledOptions = new Dictionary<(ElementType, string), float>(); /// Things that normally are disabled but are now enabled, and the weight by where they were just enabled
            public HashSet<(StatModifier, string)> ModifiedTypeEffectiveness = new HashSet<(StatModifier, string)>();
            public Dictionary<(ElementType, string), Dictionary<MoveModifier, string>> MoveMods = new Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>();
            public Dictionary<(ElementType, string), double> WeightMods = new Dictionary<(ElementType, string), double>();
            public List<List<PokemonType>> StillResistedTypes = new List<List<PokemonType>>(); // Types that this mon can't hit very well, to give a bit of a "coverage boost"
            // Then stuff that alters current mon
            public Nature Nature = Nature.SERIOUS;
            public PokemonType TeraType = PokemonType.NONE;
            public PokemonType[] PokemonTypes = [PokemonType.NONE, PokemonType.NONE];
            public int[] Evs = new int[6];
            public int[] StatBoosts = new int[6];
            public double[] StatMultipliers = [1, 1, 1, 1, 1, 1];
            public double PhysicalAccuracyMultiplier = 1;
            public double SpecialAccuracyMultiplier = 1;
            public double WeightMultiplier = 1;
            // Things that alter opp mon
            public int[] OppStatBoosts = new int[6];
            public double[] OppStatMultipliers = [1, 1, 1, 1, 1, 1];
            // Battle sim (how much damage my attacks do, how much damage mon takes from stuff, speed creep)
            public double DamageScore = 0;
            public double DefenseScore = 0;
            public double SpeedScore = 0;
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
            PokemonBuildInfo result = new PokemonBuildInfo();
            // Step 1, Obtain all mods from items, ability, moves. Some go into lists, others are applied to ctx directly
            // Step 2, If ctx, also adds avg power, def, speed gains
            // And thats it actually
            return result;
        }
    }
}
