namespace Gameplay.GameplayElements
{
    public class PokeBall
    {
        // Properties
        public string Name = "";
        public PokemonType CramType = PokemonType.NONE;
        public string Description = "";

        public override string ToString()
        {
            return Name;
        }

        // The hooks, e.g. :
        //public Func<Move, float> OnGetDamageModifier { get; set; } = delegate (Move move) { return 1; };
        // Use as: OnGetDamageModifier = delegate (Move move) { return 2; } when defining new instances

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<PokeBall> GetAllPokeballs()
        {
            yield return new PokeBall()
            {
                Name = "Poke Ball",
                Description = "Ball description"
            };
        }
    }
}
