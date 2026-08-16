namespace Gameplay.GameplayElements
{
    public class Move
    {
        // Properties
        public string Name = "";
        public PokemonType Type = PokemonType.NONE;
        public string Description = "";
        public override string ToString()
        {
            return Name;
        }

        // The hooks, e.g. :
        //public Func<Move, float> OnGetDamageModifier { get; set; } = delegate (Move move) { return 1; };
        // Use as: OnGetDamageModifier = delegate (Move move) { return 2; } when defining new instances

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Move> GetAllMoves()
        {
            yield return new Move()
            {
                Name = "Idling",
                Type = PokemonType.NORMAL,
                Description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Brave Bird",
                Type = PokemonType.FLYING,
                Description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Double-Edge",
                Type = PokemonType.NORMAL,
                Description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Heat Wave",
                Type = PokemonType.FIRE,
                Description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Wing Attack",
                Type = PokemonType.FLYING,
                Description = "Description of attack"
            };
        }
    }
}
