using Gameplay.GameplayElements;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Loads the move data
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
        /// Loads the ability data
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
        /// Loads the jewelry data
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
        /// Loads the held item data
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
        /// Loads the nature data
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
        /// Loads the pokeball data
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
        /// <summary>
        /// Loads the evo plate data
        /// </summary>
        void LoadEvoPlates()
        {
            Console.WriteLine("Loading Evo Plates");
            EvoPlates.Clear();
            foreach (EvoPlate plate in EvoPlate.GetAllEvoPlates())
            {
                EvoPlates.Add(plate.Name, plate);
            }
        }
        /// <summary>
        /// Loads the gummy data
        /// </summary>
        void LoadGummies()
        {
            Console.WriteLine("Loading Gummies");
            Gummies.Clear();
            foreach (Gummy gummy in Gummy.GetAllGummies())
            {
                Gummies.Add(gummy.Name, gummy);
            }
        }
        /// <summary>
        /// Loads the key item data
        /// </summary>
        void LoadKeyItems()
        {
            Console.WriteLine("Loading Key Items");
            KeyItems.Clear();
            foreach (KeyItem kItem in KeyItem.GetAllKeyItems())
            {
                KeyItems.Add(kItem.Name, kItem);
            }
        }
        /// <summary>
        /// Loads the key item data
        /// </summary>
        void LoadEssences()
        {
            Console.WriteLine("Loading Essences");
            Essences.Clear();
            foreach (Essence essence in Essence.GetAllEssences())
            {
                Essences.Add(essence.Name, essence);
            }
        }
        /// <summary>
        /// Loads all mints, this is a tricky one because it's actually generating them from the natures
        /// </summary>
        void LoadMints()
        {
            Console.WriteLine("Generating Mints");
            Mints.Clear();
            foreach (Nature nature in Natures.Values)
            {
                Mint newMint = new Mint()
                {
                    Name = $"{nature.Name} Mint",
                    AssociatedNature = nature
                };
                Mints.Add(newMint.Name, newMint);
            }
        }
    }
}
