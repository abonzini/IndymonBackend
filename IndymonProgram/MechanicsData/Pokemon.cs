namespace MechanicsData
{
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public (PokemonType, PokemonType) Types { get; set; } = (PokemonType.NONE, PokemonType.NONE);
        public List<Ability> Abilities { get; set; } = new List<Ability>();
        public Pokemon Prevo { get; set; } = null;
        public List<Pokemon> Evos { get; set; } = new List<Pokemon>();
        public double[] Stats { get; set; } = new double[6]; // All stats, hopefully init to 0
        public List<Move> Moveset { get; set; } = new List<Move>();
        public double Weight { get; set; } = 0.0f;
        public override string ToString()
        {
            return Name;
        }
    }
}
