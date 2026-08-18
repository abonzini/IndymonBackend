namespace Gameplay.GameplayElements
{
    /// <summary>
    /// All gameplay elements with a name a description
    /// </summary>
    public class GameplayElement
    {
        public string Name = "";
        protected string _description = "";
        public virtual string GetDescription()
        {
            return _description;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    /// <summary>
    /// A gameplay element but with battle effects, e.g. they can declare event hooks for special properties
    /// </summary>
    public class SimulationElement : GameplayElement
    {
        // The hooks, e.g. :
        //public Func<Move, float> OnGetDamageModifier { get; set; } = delegate (Move move) { return 1; };
        // Use as: OnGetDamageModifier = delegate (Move move) { return 2; } when defining new instances
    }
}
