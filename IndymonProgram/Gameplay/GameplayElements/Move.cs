namespace Gameplay.GameplayElements
{
    public class Move : SimulationElement
    {
        // Properties
        public PokemonType Type = PokemonType.NONE;
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Move> GetAllMoves()
        {
            yield return new Move()
            {
                Name = "Idling",
                Type = PokemonType.NORMAL,
                _description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Brave Bird",
                Type = PokemonType.FLYING,
                _description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Double-Edge",
                Type = PokemonType.NORMAL,
                _description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Heat Wave",
                Type = PokemonType.FIRE,
                _description = "Description of attack"
            };
            yield return new Move()
            {
                Name = "Wing Attack",
                Type = PokemonType.FLYING,
                _description = "Description of attack"
            };
        }
    }
}
