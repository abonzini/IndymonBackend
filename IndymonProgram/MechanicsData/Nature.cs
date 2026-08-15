namespace MechanicsData
{
    public class Nature
    {
        public string Name { get; set; } = "";
        public override string ToString()
        {
            return Name;
        }
        public string GetDescription()
        {
            return Name; // TODO: later on the json will contain a special field with this string, and will be printed here
        }
    }
    public class Mint
    {
        public string Name { get; set; } = "";
        public Nature AssociatedNature = null;
        public string GetDescription()
        {
            string description = $"Permanently changes a Pokemon's nature to {AssociatedNature.Name}: ";
            description += AssociatedNature.GetDescription();
            return description;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
