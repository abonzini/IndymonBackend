namespace MechanicsData
{
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public HashSet<PokemonType> Types { get; set; } = new HashSet<PokemonType>();
        public HashSet<string> Abilities { get; set; } = new HashSet<string>();
        public string Prevo { get; set; } = "";
        public HashSet<string> Evos { get; set; } = new HashSet<string>();
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
        public HashSet<string> Moves { get; set; } = new HashSet<string>();
        public override string ToString()
        {
            return Name;
        }
    }
}
