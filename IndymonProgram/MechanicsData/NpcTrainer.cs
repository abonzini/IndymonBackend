namespace MechanicsData
{
    public class NpcTrainer
    {
        public string Name = "";
        public TrainerRank TrainerRank = TrainerRank.UNRANKED;
        public bool FullyLoadedData = true;
        public List<PokemonSpecies> AvailablePokemon = new List<PokemonSpecies>();
        public List<string> AvailableItems = new List<string>();
        public override string ToString()
        {
            return Name;
        }
    }
}
