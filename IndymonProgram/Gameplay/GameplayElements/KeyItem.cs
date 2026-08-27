using Utilities;

namespace Gameplay.GameplayElements
{
    public class KeyItem : GameplayElement
    {
        public PokemonType CramType = PokemonType.NONE;
        public static IEnumerable<KeyItem> GetAllKeyItems()
        {
            // Shards are one per type so just automate it
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                yield return new KeyItem()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Shard",
                    CramType = type,
                    _description = $"Can be used to craft Jewelry."
                };
            }
            // For herba mystica, there's 5 types each with a cram type
            List<string> herbaNames = ["Bitter", "Salty", "Sour", "Spicy", "Sweet"];
            List<PokemonType> herbaTypes = [PokemonType.GHOST, PokemonType.ROCK, PokemonType.GRASS, PokemonType.FIRE, PokemonType.BUG];
            for (int i = 0; i < herbaNames.Count; i++)
            {
                yield return new KeyItem()
                {
                    Name = $"{herbaNames[i]} Herba Mystica",
                    CramType = herbaTypes[i],
                    _description = $"Can be used as a Sandwich ingredient."
                };
            }
            // Apricorns and their types
            List<string> apricornNames = ["Black", "Blue", "Green", "Red", "White", "Yellow"];
            List<PokemonType> apricornTypes = [PokemonType.DARK, PokemonType.WATER, PokemonType.GRASS, PokemonType.FIRE, PokemonType.NORMAL, PokemonType.ELECTRIC];
            for (int i = 0; i < apricornNames.Count; i++)
            {
                yield return new KeyItem()
                {
                    Name = $"{apricornNames[i]} Apricorn",
                    CramType = apricornTypes[i],
                    _description = $"Can be used to craft Poke Balls."
                };
            }
            // The 5 titan plates
            List<PokemonType> titanPlateTypes = [PokemonType.ELECTRIC, PokemonType.ROCK, PokemonType.STEEL, PokemonType.DRAGON, PokemonType.ICE];
            for (int i = 0; i < titanPlateTypes.Count; i++)
            {
                yield return new KeyItem()
                {
                    Name = $"Titan Plate {i + 1}",
                    CramType = titanPlateTypes[i],
                    _description = $"Collect all Titan plates 1-5 to unlock a special event."
                };
            }
            // Standalone key items
            yield return new KeyItem()
            {
                Name = $"Big Nugget",
                CramType = PokemonType.STEEL,
                _description = $"Can be used to craft Jewelry."
            };

        }
    }
}
