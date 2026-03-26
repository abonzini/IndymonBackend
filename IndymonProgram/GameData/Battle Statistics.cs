namespace GameData
{
    public class IndividualMu
    {
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public double Winrate { get { return ((Losses + Wins) > 0) ? (double)Wins / (double)(Losses + Wins) : 0.0f; } }
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
        public double Winrate { get { return (double)TournamentWins / (double)TournamentsPlayed; } }
        public int GamesWon { get; set; } = 0;
        public int GamesPlayed { get; set; } = 1;
        public double GameWinrate { get { return (double)GamesWon / (double)GamesPlayed; } }
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public double Diff { get { return ((double)Kills - (double)Deaths) / ((double)GamesPlayed); } }
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
