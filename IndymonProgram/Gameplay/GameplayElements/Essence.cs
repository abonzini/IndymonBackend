using Utilities;

namespace Gameplay.GameplayElements
{
    public class Essence : GameplayElement
    {
        public PokemonType Type = PokemonType.NONE;
        public override string GetDescription()
        {
            return $"A {GeneralUtilities.ApaCapitalize(Type.ToString())}-type essence.";
        }
    }
}
