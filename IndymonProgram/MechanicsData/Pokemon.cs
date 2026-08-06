namespace MechanicsData
{
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public (PokemonType, PokemonType) Types { get; set; } = (PokemonType.NONE, PokemonType.NONE);
        public Pokemon Prevo { get; set; } = null;
        public List<Pokemon> Evos { get; set; } = new List<Pokemon>();
        public Pokemon AlternativeOf { get; set; } = null;
        public List<Pokemon> WildAlternatives { get; set; } = new List<Pokemon>();
        public double[] Stats { get; set; } = new double[6]; // All stats, hopefully init to 0
        public double Weight { get; set; } = 0.0f;
        public double Height { get; set; } = 0.0f;
        public HashSet<Move> Moveset { get; set; } = new HashSet<Move>();
        public HashSet<Ability> Abilities { get; set; } = new HashSet<Ability>();
        public string ImageUrl { get; set; } = "";
        public override string ToString()
        {
            return Name;
        }
    }
}
