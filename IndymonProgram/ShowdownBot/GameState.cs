namespace ShowdownBot
{
    public class AvailableMove
    {
        public string move { get; set; }
        public int pp { get; set; }
        public bool disabled { get; set; }
        public override string ToString()
        {
            return move;
        }
    }
    public class ActiveOptions
    {
        public List<AvailableMove> moves { get; set; }
    }
    public class SideOptions
    {
        public string name { get; set; }
        public List<SidePokemon> pokemon { get; set; }
        public int GetAliveMons()
        {
            int result = 0;
            foreach (SidePokemon option in pokemon)
            {
                if (!option.condition.Contains("fnt"))
                {
                    result++;
                }
            }
            return result;
        }
        public List<string> GetValidSwitchIns() // What pokemon can I switch to
        {
            List<string> switchIns = new List<string>();
            foreach (SidePokemon option in pokemon)
            {
                if (option.IsValidSwitchIn()) switchIns.Add(option.details);
            }
            return switchIns;
        }
    }
    public class SidePokemon
    {
        public string ident { get; set; }
        public bool active { get; set; }
        public string details { get; set; }
        public string condition { get; set; }
        public bool IsValidSwitchIn() // Mon cant be switch if active or dead
        {
            if (active) return false;
            if (condition.Contains("fnt")) return false;
            else return true;
        }
    }
    public class GameState
    {
        public List<bool> forceSwitch { get; set; }
        public bool teamPreview { get; set; }
        public List<ActiveOptions> active { get; set; }
        public SideOptions side { get; set; }
        public bool wait { get; set; }
    }
}
