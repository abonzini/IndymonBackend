namespace MechanicsData
{
    public class Nature
    {
        public string Name { get; set; } = "";
        public override string ToString()
        {
            return Name;
        }
    }
    public class Mint
    {
        public string Name = "";
        public Nature AssociatedNature = null;
        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
