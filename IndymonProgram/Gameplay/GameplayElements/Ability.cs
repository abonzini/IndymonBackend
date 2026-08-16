namespace Gameplay.GameplayElements
{
    public class Ability
    {
        // Properties
        public string Name { get; set; } = "";
        public string Description = "";
        public override string ToString()
        {
            return Name;
        }

        // The hooks, e.g. :
        //public Func<Move, float> OnGetDamageModifier { get; set; } = delegate (Move move) { return 1; };
        // Use as: OnGetDamageModifier = delegate (Move move) { return 2; } when defining new instances

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Ability> GetAllAbilities()
        {
            yield return new Ability()
            {
                Name = "Beast Boost",
                Description = "Ability description"
            };
            yield return new Ability()
            {
                Name = "Intimidate",
                Description = "Ability description"
            };
            yield return new Ability()
            {
                Name = "Reckless",
                Description = "Ability description"
            };
        }
    }
}
