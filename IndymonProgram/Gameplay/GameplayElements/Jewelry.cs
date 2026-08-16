namespace Gameplay.GameplayElements
{
    public class Jewelry
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
        public static IEnumerable<Jewelry> GetAllJewelry()
        {
            yield return new Jewelry()
            {
                Name = "Amulet Coin",
                Description = "Jewelry Description"
            };
        }
    }
}
