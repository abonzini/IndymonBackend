using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Given a Pokemon, scores and examines the current mon set, both in order to examine how valuable a specific set but also obtain many important characteristics of the final mon for simulation
        /// </summary>
        /// <param name="pokemon">The Pokemon with its current set</param>
        /// <param name="nMonInTeam">The order of mon in team, important to prioritize specific moves on different teamslots</param>
        /// <param name="nMons">Total number of mons in team</param>
        /// <param name="teamBuildConstraints">Constraints of team build present from the beginning of team build</param>
        /// <param name="teamCtx">Extra context of the fight, null if none</param>
        /// <returns>The Pokemon build details</returns>
        static PokemonBuildInfo ObtainPokemonSetContext(TrainerPokemon pokemon, int nMonInTeam, int nMons, TeamBuildConstraints teamBuildConstraints, TeamBuildContext teamCtx = null)
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
