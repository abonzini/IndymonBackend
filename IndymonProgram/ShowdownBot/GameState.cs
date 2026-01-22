namespace ShowdownBot
{
    public class AvailableMove
    {
        [JsonProperty("move")]
        public string Move { get; set; }
        [JsonProperty("pp")]
        public int Pp { get; set; } = 1; // Moves without pp (struggle, recharge) are always usable
        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
        public override string ToString()
        {
            return Move;
        }
    }
    public class ActiveOptions
    {
        [JsonProperty("moves")]
        public List<AvailableMove> Moves { get; set; }
        [JsonProperty("trapped")]
        public bool Trapped { get; set; }
        [JsonProperty("canTerastallize")]
        public string CanTerastallize { get; set; } = "";
    }
    public class SideOptions
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("pokemon")]
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
        [JsonProperty("ident")]
        public string Ident { get; set; }
        [JsonProperty("active")]
        public bool Active { get; set; }
        [JsonProperty("details")]
        public string Details { get; set; }
        [JsonProperty("condition")]
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
        [JsonProperty("forceSwitch")]
        public List<bool> ForceSwitch { get; set; }
        [JsonProperty("teamPreview")]
        public bool TeamPreview { get; set; }
        [JsonProperty("active")]
        public List<ActiveOptions> Active { get; set; }
        [JsonProperty("side")]
        public SideOptions Side { get; set; }
        [JsonProperty("wait")]
        public bool Wait { get; set; }
    }
}
