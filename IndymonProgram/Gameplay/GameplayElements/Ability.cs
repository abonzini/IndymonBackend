namespace Gameplay.GameplayElements
{
    public class Ability : SimulationElement
    {
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Ability> GetAllAbilities()
        {
            yield return new Ability()
            {
                Name = "Beast Boost",
                _description = "Ability description"
            };
            yield return new Ability()
            {
                Name = "Intimidate",
                _description = "Ability description"
            };
            yield return new Ability()
            {
                Name = "Reckless",
                _description = "Ability description"
            };
        }
    }
}
