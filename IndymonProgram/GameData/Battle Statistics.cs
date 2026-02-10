namespace GameData
{
    public class IndividualMu
    {
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public float Winrate { get { return ((Losses + Wins) > 0) ? (float)Wins / (float)(Losses + Wins) : 0.0f; } }
        public override string ToString()
        {
            return $"{Wins} ({Winrate})";
        }
    }
    public class PlayerAndStats
    {
        public string Name { get; set; }
        public Dictionary<string, IndividualMu> EachMuWr { get; set; } = new Dictionary<string, IndividualMu>(); // Contains each matchup
        public int TournamentWins { get; set; } = 0;
        public int TournamentsPlayed { get; set; } = 1;
        public float Winrate { get { return (float)TournamentWins / (float)TournamentsPlayed; } }
        public int GamesWon { get; set; } = 0;
        public int GamesPlayed { get; set; } = 1;
        public float GameWinrate { get { return (float)GamesWon / (float)GamesPlayed; } }
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Diff { get { return Kills - Deaths; } }
        public override string ToString()
        {
            return $"{Name}: {TournamentWins}/{TournamentsPlayed})";
        }
    }
    public class BattleStats
    {
        public List<PlayerAndStats> PlayerStats { get; set; } = new List<PlayerAndStats>();
        public List<PlayerAndStats> NpcStats { get; set; } = new List<PlayerAndStats>();
    }
}
