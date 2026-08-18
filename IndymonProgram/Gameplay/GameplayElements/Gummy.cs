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
        public override string GetDescription()
        {
            return $"A {GeneralUtilities.ApaCapitalize(Type.ToString())}-type gummy.";
        }
    }
}