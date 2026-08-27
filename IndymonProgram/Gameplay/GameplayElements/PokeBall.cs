namespace Gameplay.GameplayElements
{
    public class PokeBall : SimulationElement
    {
        // Properties
        public PokemonType CramType = PokemonType.NONE;
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<PokeBall> GetAllPokeballs()
        {
            yield return new PokeBall()
            {
                Name = "Poke Ball",
                CramType = PokemonType.NORMAL,
                _description = "Ball description"
            };
        }
    }
}
