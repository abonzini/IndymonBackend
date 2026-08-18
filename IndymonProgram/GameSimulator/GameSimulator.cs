using Gameplay.GameEngine;
using Gameplay.GameplayElements;

namespace GameSimulator
{
    public static class GameSimulator
    {
        /// <summary>
        /// Simulates a full game, given a list of trainers. Edits the trainer and pokemon instances during the simulation to return hp, etc
        /// </summary>
        /// <param name="trainers">Trainers who'll participate in the fight</param>
        /// TODO This should also receive a field setup instance to define how the field will look and work
        /// TODO This should also return a rendering queue object or similar
        public static void SimulateGame(List<TrainerInstance> trainers)
        {

        }
        /// <summary>
        /// Used to create a pokemon gameplay instance from a base Pokemon. Deals with defining the remaining sets (move disks) as well as set up Hp and etc. Will then be made permanent for the rest of the event
        /// </summary>
        /// <param name="pokemon">Pokemon to instance</param>
        /// <param name="seed">Rng seed for operations that are random</param>
        /// <returns></returns>
        public static PokemonInstance InstancePokemon(PokemonEntity pokemon, int seed)
        {
            return new PokemonInstance();
        }
    }
}
