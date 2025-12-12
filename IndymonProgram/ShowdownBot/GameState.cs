namespace ShowdownBot
{
    public class AvailableMove
    {
        public string Move { get; set; }
        public int Pp { get; set; } = 1; // Moves without pp (struggle, recharge) are always usable
        public bool Disabled { get; set; }
        public override string ToString()
        {
            return Move;
        }
    }
    public class ActiveOptions
    {
        public List<AvailableMove> Moves { get; set; }
        public bool Trapped { get; set; }
        public string CanTerastallize { get; set; } = "";
    }
    public class SideOptions
    {
        public string Name { get; set; }
        public List<SidePokemon> Pokemon { get; set; }
        public int GetAliveMons()
        {
            int result = 0;
            foreach (SidePokemon option in Pokemon)
            {
                if (!option.Condition.Contains("fnt"))
                {
                    result++;
                }
            }
            return result;
        }
        public List<int> GetValidSwitchIns() // What pokemon can I switch to
        {
            List<int> switchIns = new List<int>();
            for (int i = 0; i < Pokemon.Count; i++)
            {
                SidePokemon option = Pokemon[i];
                if (option.IsValidSwitchIn()) switchIns.Add(i + 1);
            }
            return switchIns;
        }
    }
    public class SidePokemon
    {
        public string Ident { get; set; }
        public bool Active { get; set; }
        public string Details { get; set; }
        public string Condition { get; set; }
        public bool IsValidSwitchIn() // Mon cant be switch if active or dead
        {
            if (Active) return false;
            if (Condition.Contains("fnt")) return false;
            else return true;
        }
        public override string ToString()
        {
            return $"{Ident} ({Condition})";
        }
    }
    public class GameState
    {
        public List<bool> ForceSwitch { get; set; }
        public bool TeamPreview { get; set; }
        public List<ActiveOptions> Active { get; set; }
        public SideOptions Side { get; set; }
        public bool Wait { get; set; }
    }
}
