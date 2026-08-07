namespace MechanicsData
{
    public class Gummy
    {
        public string Name = "";
        public PokemonType Type = PokemonType.NONE;
        public override string ToString()
        {
            return $"{Name}";
        }
    }
}