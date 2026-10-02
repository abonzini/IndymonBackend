namespace Gameplay.GameplayElements
{
    public class Weather : SimulationElement
    {
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Weather> GetAllWeathers()
        {
            yield return new Weather()
            {
                Name = "None",
                _description = "Handle for no weather (will be gone soon ig as soon as I start adding stuff)"
            };
        }
    }
}
