namespace MechanicsData
{
    public class PokemonSpecies
    {
        public string Name { get; set; } = "";
        public (PokemonType, PokemonType) Types { get; set; } = (PokemonType.NONE, PokemonType.NONE);
        public PokemonSpecies Prevo { get; set; } = null;
        public List<PokemonSpecies> Evos { get; set; } = new List<PokemonSpecies>();
        public PokemonSpecies AlternativeOf { get; set; } = null;
        public List<PokemonSpecies> WildAlternatives { get; set; } = new List<PokemonSpecies>();
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
