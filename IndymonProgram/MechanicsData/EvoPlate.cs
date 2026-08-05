namespace MechanicsData
{
    public class EvoPlate
    {
        public string Name = "";
        public PokemonType Type = PokemonType.NONE;

        public override string ToString()
        {
            return $"{Name} -> {Type.ToString()}";
        }
    }
}
