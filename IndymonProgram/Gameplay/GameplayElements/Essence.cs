using Utilities;

namespace Gameplay.GameplayElements
{
    public class Essence : GameplayElement
    {
        public PokemonType Type = PokemonType.NONE;
        public static IEnumerable<Essence> GetAllEssences()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                yield return new Essence()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Essence",
                    Type = type,
                    _description = $"A {GeneralUtilities.ApaCapitalize(type.ToString())}-type essence."
                };
            }
        }
    }
}
