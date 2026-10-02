namespace Gameplay.GameplayElements
{
    public class NonVolatileStatus : SimulationElement
    {
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<NonVolatileStatus> GetAllNonVolatileStatus()
        {
            yield return new NonVolatileStatus()
            {
                Name = "None",
                _description = "Handle for no status (will be gone soon ig as soon as I start adding stuff)"
            };
        }
    }
}