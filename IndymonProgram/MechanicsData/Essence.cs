namespace MechanicsData
{
    public class Essence
    {
        public string Name = "";
        public PokemonType Type = PokemonType.NONE;
        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
