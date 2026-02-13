using MechanicsData;

namespace GameData
{
    public enum TrainerRank
    {
        UNRANKED,
        GYM,
        ELITE4,
        CHAMPION
    }
    public class Trainer
    {
        public const int MAX_MONS_IN_TEAM = 12; /// How many mons top can the team have (rest goes to box)
        public string Name = "";
        public string DungeonIdentifier = "?";
        public int IMP = 0;
        public string Avatar = "";
        public string AvatarUrl = "";
        public string DiscordNumber = "";
        public TrainerRank TrainerRank = TrainerRank.UNRANKED;
        public bool AutoTeam = true;
        public bool AutoFavour = true;
        public bool AutoSetItem = true;
        public bool AutoModItem = true;
        public bool AutoBattleItem = true;
        public List<TrainerPokemon> PartyPokemon = new List<TrainerPokemon>();
        public List<TrainerPokemon> BoxedPokemon = new List<TrainerPokemon>();
        public Dictionary<SetItem, int> SetItems = new Dictionary<SetItem, int>();
        public Dictionary<Item, int> ModItems = new Dictionary<Item, int>();
        public Dictionary<Item, int> BattleItems = new Dictionary<Item, int>();
        public Dictionary<string, int> KeyItems = new Dictionary<string, int>();
        public Dictionary<Trainer, int> TrainerFavours = new Dictionary<Trainer, int>();
        public Dictionary<string, int> PokeBalls = new Dictionary<string, int>();
        public override string ToString()
        {
            return Name;
        }
        // Things related to an assembled team ready for battle
        public List<TrainerPokemon> BattleTeam = new List<TrainerPokemon>(); // A subset of team but can also have borrowed mons ready to battle
        /// <summary>
        /// Reset the state of all mons pre-battle
        /// </summary>
        public void RestoreAll()
        {
            foreach (TrainerPokemon mon in BattleTeam)
            {
                mon.HealFull();
            }
        }
        /// <summary>
        /// Gets trainer data as part of a packed string as is received by (my modified version of) showdown
        /// </summary>
        /// <returns>Packed string</returns>
        public string GetShowdownPackedString()
        {
            List<string> eachMonPacked = new List<string>();
            foreach (TrainerPokemon mon in BattleTeam)
            {
                eachMonPacked.Add(mon.GetShowdownPackedString());
            }
            return string.Join("]", eachMonPacked); // Returns the packed data joined with ]
        }
    }
}
