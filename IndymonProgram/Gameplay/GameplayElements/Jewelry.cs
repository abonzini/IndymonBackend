namespace Gameplay.GameplayElements
{
    public class Jewelry : SimulationElement
    {
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Jewelry> GetAllJewelry()
        {
            yield return new Jewelry()
            {
                Name = "Amulet Coin",
                _description = "Jewelry Description"
            };
        }
    }
}
