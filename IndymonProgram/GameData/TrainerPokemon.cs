using MechanicsData;

namespace GameData
{
    public class TrainerPokemon
    {
        public string Species = "";
        public string Nickname = "";
        public bool IsShiny = false;
        public SetItem SetItem = null;
        public Item ModItem = null;
        public Item BattleItem = null;
        public override string ToString()
        {
            return (Nickname != "") ? $"{Nickname} ({Species})" : Species;
        }
        public string GetInformalName()
        {
            return (Nickname != "") ? Nickname : Species;
        }
        // Etc (set-related for exporting, filled by team builder unless you really know what you're doing)
        public Ability ChosenAbility = null;
        public List<Move> ChosenMoveset = [];
        public int[] Evs = new int[6];
        public Nature Nature = Nature.SERIOUS;
        public PokemonType TeraType = PokemonType.NONE;
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
        // Showdown related, importable/exportable data for the battle sim
        public int HealthPercentage = 100; // 100 percent default
        public string NonVolatileStatus = "";
        public List<int> MovePp = [];
        public int Level = 100; // Default is 100
        /// <summary>
        /// Imports status as seen in showdown
        /// </summary>
        /// <param name="status">Status string given by showdown</param>
        public void ImportShowdownStatus(string status)
        {
            if (status.ToLower() == "0 fnt")
            {
                HealthPercentage = 1; // Would be cool to res mons at 1% no matter what, also 0% or fnt is bugged as hell
                NonVolatileStatus = "";
            }
            else
            {
                string[] splitStatus = status.Split(' ');
                string[] splitHealth = splitStatus[0].Split("/");
                HealthPercentage = (100 * int.Parse(splitHealth[0])) / int.Parse(splitHealth[1]);
                if (HealthPercentage == 0) HealthPercentage = 1; // Can never be 0 because otherwise it'd be fainted
                NonVolatileStatus = (splitStatus.Length == 2) ? splitStatus[1] : "";
            }
        }
        /// <summary>
        /// Restores sim stats to default
        /// </summary>
        public void HealFull()
        {
            HealthPercentage = 100;
            NonVolatileStatus = "";
            MovePp.Clear();
            foreach (Move move in ChosenMoveset)
            {
                MovePp.Add(99); // Add default highest
            }
        }
        /// <summary>
        /// Gets mon set data as part of a packed string as is received by (my modified version of) showdown
        /// </summary>
        /// <returns>Packed string</returns>
        public string GetShowdownPackedString()
        {
            //NICKNAME|SPECIES|ITEM|ABILITY|MOVES|NATURE|EVS|GENDER|IVS|SHINY|LEVEL|HAPPINESS,POKEBALL,HIDDENPOWERTYPE,GIGANTAMAX,DYNAMAXLEVEL,TERATYPE(,HP%,NONVOLATILESTATUS)<- My stuff
            List<string> packedStrings = new List<string>();
            packedStrings.Add(Nickname);
            packedStrings.Add(Species);
            packedStrings.Add((BattleItem != null) ? BattleItem.Name : "");
            packedStrings.Add(ChosenAbility.Name);
            List<string> movesWithUses = new List<string>();
            for (int i = 0; i < ChosenMoveset.Count; i++)
            {
                string moveString = (ChosenMoveset[i] != null) ? ChosenMoveset[i].Name : "";
                movesWithUses.Add($"{moveString}#{MovePp[i]}"); // Add move with the number of recorded uses (no idea how this works with hard switch)
            }
            packedStrings.Add(string.Join(",", movesWithUses));
            packedStrings.Add(Nature.ToString());
            packedStrings.Add(string.Join(",", Evs));
            packedStrings.Add("");
            packedStrings.Add(""); // No IVs I don't care
            packedStrings.Add(IsShiny ? "S" : ""); // Depending if shiny
            packedStrings.Add(Level.ToString()); // Mon level is "usually" 100
            string lastPackedString = $",,,,,{TeraType.ToString()},{HealthPercentage.ToString()},{NonVolatileStatus}"; // Add the "remaining" useless stuff needed for tera, etc
            packedStrings.Add(lastPackedString);
            return string.Join("|", packedStrings); // Join them together with |
        }
    }
}
