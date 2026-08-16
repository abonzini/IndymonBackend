using Utilities;

namespace Gameplay.GameplayElements
{
    public class Essence
    {
        public string Name = "";
        public PokemonType Type = PokemonType.NONE;
        public override string ToString()
        {
            return Name;
        }
        public string GetDescription()
        {
            return $"A {GeneralUtilities.ApaCapitalize(Type.ToString())}-type essence.";
        }
    }
}
