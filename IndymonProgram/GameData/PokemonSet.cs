using MechanicsData;

namespace GameData
{
    public class PokemonSet
    {
        public string Species { get; set; } = "";
        public string Nickname { get; set; } = "";
        public bool IsShiny { get; set; } = false;
        public string SetItem { get; set; } = "";
        public ModItem ModItem { get; set; } = null;
        public BattleItem BattleItem { get; set; } = null;
        // Etc (set-related i.g.)
        public override string ToString()
        {
            return (Nickname != "") ? $"{Nickname} ({Species})" : Species;
        }
    }
}
