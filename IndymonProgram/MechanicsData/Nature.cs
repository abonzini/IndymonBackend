namespace MechanicsData
{
    public class Mint
    {
        public string Name = "";
        public Nature AssociatedNature = null;
        public string GetDescription()
        {
            string description = $"Permanently changes a Pokemon's nature to {AssociatedNature.Name}: ";
            description += AssociatedNature.Description;
            return description;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    public class Nature
    {
        // Properties
        public string Name = "";
        public string Description = "";
        public override string ToString()
        {
            return Name;
        }

        // The hooks, e.g. :
        //public Func<Move, float> OnGetDamageModifier { get; set; } = delegate (Move move) { return 1; };
        // Use as: OnGetDamageModifier = delegate (Move move) { return 2; } when defining new instances

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Nature> GetAllNatures()
        {
            yield return new Nature()
            {
                Name = "Jolly",
                Description = "Nature description"
            };
            yield return new Nature()
            {
                Name = "Hardy",
                Description = "Nature description"
            };
        }
    }
}
