namespace MechanicsData
{
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public PokemonType[] Types { get; set; } = [PokemonType.NONE, PokemonType.NONE];
        public HashSet<string> Abilities { get; set; } = new HashSet<string>();
        public string Prevo { get; set; } = "";
        public HashSet<string> Evos { get; set; } = new HashSet<string>();
        public int[] Stats { get; set; } = [0, 0, 0, 0, 0, 0]; // Hp, Attack, Defense, Special Attack, Special Defense, Speed
        public HashSet<string> Moves { get; set; } = new HashSet<string>();
        public override string ToString()
        {
            return Name;
        }
    }
}
