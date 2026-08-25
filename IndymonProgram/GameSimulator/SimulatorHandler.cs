using Gameplay.GameEngine;
using Gameplay.GameplayElements;
using Utilities;

namespace GameSimulator
{
    public static class SimulatorHandler
    {
        /// <summary>
        /// Simulates a full game, given a list of trainers. Edits the trainer and pokemon instances during the simulation to return current hp, etc
        /// </summary>
        /// <param name="trainers">Trainers who'll participate in the fight</param>
        /// <param name="rng">The rng for this game (determinism!)</param>
        /// TODO This should also receive a field setup instance to define how the field will look and work
        /// <returns>The outcome of this game + rendering queue</returns>
        public static GameOutcome SimulateGame(List<TrainerInstance> trainers, Random rng)
        {
            // Random winner idk
            HashSet<Side> participatingTeams = [.. trainers.Select(t => t.Team)];
            Side winningTeam = GeneralUtilities.GetRandomPick([.. participatingTeams], rng);
            List<TrainerInstance> winningTrainers = [.. trainers.Where(p => p.Team == winningTeam)];
            return new GameOutcome()
            {
                WinningTeam = winningTeam,
                WinningTrainers = winningTrainers
            };
        }
        /// <summary>
        /// Used to create a pokemon gameplay instance from a base Pokemon. Deals with defining the remaining sets (move disks) as well as set up Hp and etc. Will then be made permanent for the rest of the event
        /// </summary>
        /// <param name="pokemon">Pokemon to instance</param>
        /// <param name="rng">The Rng to define the rest of this mon's things</param>
        /// <returns>The generated Pokemon instance ready for game</returns>
        public static PokemonInstance InstancePokemon(PokemonEntity pokemon, Random rng)
        {
            return new PokemonInstance()
            {
                Name = pokemon.ToString(), // Print it with nickname w.e.
            };
        }
    }
}
