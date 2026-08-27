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
        public static IEnumerable<EvoPlate> GetAllEvoPlates()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                string name = type switch
                {
                    PokemonType.NORMAL => "Blank",
                    PokemonType.FIGHTING => "Fist",
                    PokemonType.FLYING => "Sky",
                    PokemonType.POISON => "Toxic",
                    PokemonType.GROUND => "Earth",
                    PokemonType.ROCK => "Stone",
                    PokemonType.BUG => "Insect",
                    PokemonType.GHOST => "Spooky",
                    PokemonType.STEEL => "Iron",
                    PokemonType.FIRE => "Flame",
                    PokemonType.WATER => "Splash",
                    PokemonType.GRASS => "Meadow",
                    PokemonType.ELECTRIC => "Zap",
                    PokemonType.PSYCHIC => "Mind",
                    PokemonType.ICE => "Icicle",
                    PokemonType.DRAGON => "Draco",
                    PokemonType.DARK => "Dread",
                    PokemonType.FAIRY => "Pixie",
                    _ => "Unknown"
                };
                name += " Plate";
                yield return new EvoPlate()
                {
                    Name = name,
                    Type = type,
                    _description = $"A {GeneralUtilities.ApaCapitalize(type.ToString())}-type evolution plate."
                };
            }
        }
    }
}
