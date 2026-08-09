using MechanicsData;
using Utilities;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
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
        /// Generates a random blank pokemon that can be later used as the actual template, or kept random to simplify generation in case of first-time or wild mons
        /// </summary>
        /// <param name="Species"></param>
        /// <param name="rng">The rng to be used to generate this mon</param>
        /// <returns></returns>
        PokemonEntity GenerateBlankPokemon(string Species, Random rng = null)
        {
            rng ??= _rng; // Use default rng if nothing present
            PokemonEntity newPokemon = new PokemonEntity();
            if (Species.Contains('★'))
            {
                newPokemon.IsShiny = true;
                Species = Species.Split('★')[0].Trim(); // Get the actual species then
            }
            newPokemon.Species = Dex[Species];
            newPokemon.PokeBall = PokeBalls["Poke Ball"]; // Poke Ball being the hardcoded default value of any Pokemon
            // Random nature selection
            List<Nature> natureList = [.. Natures.Values];
            newPokemon.Nature = GeneralUtilities.GetRandomPick(natureList, rng);
            // Move selection is random but do need to make sure the moves are not repeated
            List<Move> monLearnset = [.. newPokemon.Species.Moveset];
            newPokemon.Moves[0] = GeneralUtilities.GetRandomPick(monLearnset, rng);
            monLearnset.Remove(newPokemon.Moves[0]); // TODO: This needs to be remade into damaging moves once we get enough data
            if (monLearnset.Count > 0) newPokemon.Moves[1] = GeneralUtilities.GetRandomPick(monLearnset, rng); // Add only if there's still any, but no need to remove anythign afterwards
            // Ability selection
            List<Ability> monPossibleAbilities = [.. newPokemon.Species.Abilities];
            GeneralUtilities.ShuffleList(monPossibleAbilities, rng); // Shuffle the order the mon gets the abilities
            for (int i = 0; i < newPokemon.Abilities.Length && i < monPossibleAbilities.Count; i++) // Add as many abilities as possible as long as mon has space and abilities
            {
                newPokemon.Abilities[i] = monPossibleAbilities[i];
            }
            return newPokemon;
        }
    }
}
