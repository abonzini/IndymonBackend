using Gameplay.GameplayElements;
using Utilities;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Generates all gummies given they're "Type" Gummy
        /// </summary>
        void GenerateGummies()
        {
            Console.WriteLine("Generating Gummies");
            Gummies.Clear();
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                Gummy newGummy = new Gummy()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Gummy",
                    Type = type
                };
                Gummies.Add(newGummy.Name, newGummy);
            }
        }
        /// <summary>
        /// Generates all nature Mints given each has a nature
        /// </summary>
        void GenerateMints()
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
        /// <summary>
        /// Generates all essences given they're "Type" Essence
        /// </summary>
        void GenerateEssences()
        {
            Console.WriteLine("Generating Essences");
            Essences.Clear();
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                Essence newEssence = new Essence()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Essence",
                    Type = type
                };
                Essences.Add(newEssence.Name, newEssence);
            }
        }
        /// <summary>
        /// Generates a random blank pokemon that can be later used as a template
        /// </summary>
        /// <param name="Species"></param>
        /// <returns></returns>
        PokemonEntity GenerateBlankPokemon(string Species)
        {
            PokemonEntity newPokemon = new PokemonEntity
            {
                Name = Species
            };
            if (Species.Contains('★'))
            {
                newPokemon.IsShiny = true;
                Species = Species.Split('★')[0].Trim(); // Get the actual species then
            }
            newPokemon.Species = Dex[Species];
            return newPokemon;
        }
    }
}
