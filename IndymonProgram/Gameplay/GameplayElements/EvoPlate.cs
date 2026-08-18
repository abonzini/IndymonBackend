using Utilities;

namespace Gameplay.GameplayElements
{
    public class EvoPlate : GameplayElement
    {
        public PokemonType Type = PokemonType.NONE;
        public override string ToString()
        {
            return $"{Name} -> {Type}";
        }
        public override string GetDescription()
        {
            return $"A {GeneralUtilities.ApaCapitalize(Type.ToString())}-type evolution plate.";
        }
    }
}
