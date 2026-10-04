namespace Gameplay.GameplayElements
{
    public class NpcTrainer
    {
        public string Name = "";
        public TrainerRank TrainerRank = TrainerRank.UNRANKED;
        public string PictureUrl = "";
        public bool FullyLoadedData = true;
        public List<PokemonSpecies> AvailablePokemon = new List<PokemonSpecies>();
        public List<string> AvailableItems = new List<string>();
        public override string ToString()
        {
            return Name;
        }
    }
}
