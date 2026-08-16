namespace Gameplay.GameplayElements
{
    public class PokemonSpecies
    {
        public string Name = "";
        public (PokemonType, PokemonType) Types = (PokemonType.NONE, PokemonType.NONE);
        public PokemonSpecies Prevo = null;
        public List<PokemonSpecies> Evos = new List<PokemonSpecies>();
        public PokemonSpecies AlternativeOf = null;
        public List<PokemonSpecies> WildAlternatives = new List<PokemonSpecies>();
        public double[] Stats = new double[6]; // All stats, hopefully init to 0
        public double Weight = 0.0f;
        public double Height = 0.0f;
        public HashSet<Move> Moveset = new HashSet<Move>();
        public HashSet<Ability> Abilities = new HashSet<Ability>();
        public string ImageUrl = "";
        public override string ToString()
        {
            return Name;
        }
    }
}
