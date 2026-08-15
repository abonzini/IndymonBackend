namespace MechanicsData
{
    public class PokeBall
    {
        public string Name { get; set; } = "";
        public PokemonType CramType { get; set; } = PokemonType.NONE;
        public override string ToString()
        {
            return Name;
        }
        public string GetDescription()
        {
            return Name; // TODO: later on the json will contain a special field with this string, and will be printed here
        }
    }
}
