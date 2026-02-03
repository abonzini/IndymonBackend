using MechanicsData;

namespace GameData
{
    public class TrainerPokemon
    {
        public string Species { get; set; } = "";
        public string Nickname { get; set; } = "";
        public bool IsShiny { get; set; } = false;
        public string SetItem { get; set; } = "";
        public Item ModItem { get; set; } = null;
        public Item BattleItem { get; set; } = null;
        public override string ToString()
        {
            return (Nickname != "") ? $"{Nickname} ({Species})" : Species;
        }
        // Etc (set-related for exporting, filled by team builder unless you really know what you're doing)
        public Ability ChosenAbility { get; set; } = null;
        public List<Move> ChosenMoveset { get; set; } = [];
        public int[] Evs { get; set; } = new int[6];
        public Nature Nature { get; set; } = Nature.SERIOUS;
        public PokemonType TeraType { get; set; } = PokemonType.NONE;
        public string PrintSet()
        {
            List<string> moveNames = new List<string>(4);
            foreach (Move move in ChosenMoveset)
            {
                if (move != null)
                {
                    moveNames.Add(move.Name);
                }
                else
                {
                    moveNames.Add(" ");
                }
            }
            return $"{ToString()}:{ChosenAbility.Name}:{string.Join(",", moveNames)}";
        }
    }
}
