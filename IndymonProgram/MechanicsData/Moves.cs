using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MoveFlag
    {
        BANNED, // Forbidden moves
        DOUBLES_ONLY, // Moves that are good in doubles only, do not use in singles
        DRAIN, // Draining moves, benefit from root
        HEAL, // MOves that heal the user (triage)
        CHANCE, // Moves with a chance of something nice, for serene grace
        PIVOT, // Pivot moves (but not stuff like memento)
        SOUND, // Sound moves
        DEFENSE_DAMAGE, // Body press lol
        PUNCH, // Punching moves
        SHARP, // Sharpness
        DANCE, // Dance moves
        CONTACT, // Contact moves
        BITING, // Biting moves
        RECOIL, // Recoil moves
        RECKLESS, // Recoil + Crash damage
        LAUNCHER, // Launcher moves
        EXPLOSIVE, // Explosive moves
        PRIORITY, // Prio
        SELF_SLEEP, // Moves that need user to be sleeping
        SELF_STATUS, // Moves that benefit from user to be statused
        OPP_SLEEP, // Moves that need opponent to be asleep
        OPP_PARALYSIS, // Moves that require opponent to be paralyzed
        GIVE_ITEM, // Moves that give item to enemy and/or discard item from owner,
        CRITICAL, // Sniper, self debuff
        NEED_OPP_ITEM, // Moves that need opp to have an item to be useful
        POSITIVE_STAT_BOOST, // Moves that appreciate many stat boosts
        SETUP_STATUS, // Setup status moves
        NO_MISS, // Moves that don't miss
        SELF_DEBUFF, // Debuff user (contrary, sure crit)
        DAMAGE_PROP_WEIGTH, // Damage increases with weight
        DAMAGE_PROP_SPEED, // Damage proportional with speed (e.g. electro ball)
        DAMAGE_INV_SPEED, // Damage inverse with speed (e.g. gyro ball)
        FIXED_DAMAGE, // Seismic toss, dragon rage
        TRAPPING, // Trapping moves
        HIGH_CRIT, // Like sure crit but not sure
        SUB_60, // Moves under 60 damage will be increased by technician
        CHARGING, // Charging moves, like solar beam
        MULTIHIT_2_MOVE, // 2-hit moves like bonemerang
        MULTIHIT_3_MOVE, // 3-hit move like triple dive
        MULTIHIT_2_TO_5_MOVE, // The common multi hit 2-5 hits
        MULTIHIT_ACC_BASED_3_HIT, // Triple axel
        MULTIHIT_ACC_BASED_10_HIT, // Good ol Pop Bomb
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MoveCategory
    {
        PHYSICAL,
        SPECIAL,
        STATUS
    }
    public class Move
    {
        public string Name { get; set; } = "";
        public PokemonType Type { get; set; }
        public MoveCategory Category { get; set; }
        public int Bp { get; set; }
        public int Acc { get; set; }
        public HashSet<MoveFlag> Flags { get; set; } = new HashSet<MoveFlag>();
        public override string ToString()
        {
            return Name;
        }
    }
}
