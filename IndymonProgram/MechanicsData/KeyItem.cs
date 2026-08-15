namespace MechanicsData
{
    public class KeyItem
    {
        public string Name = "";
        public override string ToString()
        {
            return Name;
        }
        public PokemonType CramType = PokemonType.NONE;
        public string DescriptionString = "";
        public string GetDescription()
        {
            return DescriptionString;
        }
    }
}
