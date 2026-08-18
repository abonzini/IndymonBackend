namespace Gameplay.GameplayElements
{
    public class HeldItem : SimulationElement
    {
        // Properties
        public PokemonType CramType = PokemonType.NONE;

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<HeldItem> GetAllHeldItems()
        {
            yield return new HeldItem()
            {
                Name = "Leftovers",
                _description = "Held Item Description"
            };
        }
    }
}
