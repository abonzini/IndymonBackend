using Gameplay.GameplayElements;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Loads the move data found in json files
        /// </summary>
        void LoadMoves()
        {
            Console.WriteLine("Loading Moves");
            Moves.Clear();
            foreach (Move move in Move.GetAllMoves())
            {
                Moves.Add(move.Name, move);
            }
        }
        /// <summary>
        /// Loads the ability data found in json files
        /// </summary>
        void LoadAbilities()
        {
            Console.WriteLine("Loading Abilities");
            Abilities.Clear();
            foreach (Ability ability in Ability.GetAllAbilities())
            {
                Abilities.Add(ability.Name, ability);
            }
        }
        /// <summary>
        /// Loads the jewelry data found in json files
        /// </summary>
        void LoadJewelry()
        {
            Console.WriteLine("Loading Jewelry");
            Jewelries.Clear();
            foreach (Jewelry jewelry in Jewelry.GetAllJewelry())
            {
                Jewelries.Add(jewelry.Name, jewelry);
            }
        }
        /// <summary>
        /// Loads the held item data found in json files
        /// </summary>
        void LoadHeldItems()
        {
            Console.WriteLine("Loading Held Items");
            HeldItems.Clear();
            foreach (HeldItem heldItem in HeldItem.GetAllHeldItems())
            {
                HeldItems.Add(heldItem.Name, heldItem);
            }
        }
        /// <summary>
        /// Loads the nature data found in json files
        /// </summary>
        void LoadNatures()
        {
            Console.WriteLine("Loading Natures");
            Natures.Clear();
            foreach (Nature nature in Nature.GetAllNatures())
            {
                Natures.Add(nature.Name, nature);
            }
        }
        /// <summary>
        /// Loads the pokeball data found in json files
        /// </summary>
        void LoadPokeBalls()
        {
            Console.WriteLine("Loading Poke Balls");
            PokeBalls.Clear();
            foreach (PokeBall ball in PokeBall.GetAllPokeballs())
            {
                PokeBalls.Add(ball.Name, ball);
            }
        }
    }
}
