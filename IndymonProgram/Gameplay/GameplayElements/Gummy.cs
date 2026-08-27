using Utilities;

namespace Gameplay.GameplayElements
{
    public class Gummy : GameplayElement
    {
        public PokemonType Type = PokemonType.NONE;
        public override string ToString()
        {
            return Name;
        }
        public static IEnumerable<Gummy> GetAllGummies()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                yield return new Gummy()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Gummy",
                    Type = type,
                    _description = $"A {GeneralUtilities.ApaCapitalize(type.ToString())}-type gummy."
                };
            }
        }
    }
}