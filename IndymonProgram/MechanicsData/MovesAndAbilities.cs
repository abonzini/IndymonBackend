using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EffectFlag
    {
        BANNED, // Forbidden moves
        DOUBLES_ONLY, // Moves that are good in doubles only, do not use in singles
        DRAIN, // Draining moves, benefit from root
        HEAL, // Moves that heal the user (triage)
        CHANCE, // Moves with a chance of something nice, for serene grace
        SECONDARY_EFFECT, // Moves with a chance (can be 100%) of secondary effect, for sheer force
        PIVOT, // Pivot moves (but not stuff like memento)
        SOUND, // Sound moves
        OPP_ATTACK_DAMAGE, // Foul play lol
        DEFENSE_DAMAGE, // Body press lol
        OTHER_DEFENSE_STAT, // Uses the other defensive stat (e.g. psyshock)
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
        SLEEP_INDUCING, // Moves that cause sleep
        OPP_SLEEP, // Moves that need opponent to be asleep
        PARALYSIS_INDUCING, // Moves that cause paralysis
        POISON_INDUCING, // Moves that cause poison
        BURN_INDUCING, // Moves that cause burn
        GIVE_ITEM, // Moves that give item to enemy and/or discard item from owner,
        CRITICAL, // Sniper, self debuff
        NEED_OPP_ITEM, // Moves that need opp to have an item to be useful
        POSITIVE_STAT_BOOST, // Moves that appreciate many stat boosts
        SETUP_DAMAGING, // Damaging moves that increase stat
        SELF_DEBUFF, // Debuff user (contrary, sure crit)
        SETUP_STATUS, // Collection of all the status setup moves
        SETUP_OFF, // Setup status moves that increase offensive stat
        SETUP_DEF, // Setup status moves that increase defensive stat
        SETUP_SPEED, // Setup status moves that increase speed
        DAMAGE_PROP_WEIGTH_DIFFERENCE, // Damage increases with weight difference
        DAMAGE_PROP_OPP_WEIGTH, // Damage increases solely w opp weight
        DAMAGE_PROP_SPEED_DIFFERENCE, // Damage proportional with speed (e.g. electro ball)
        DAMAGE_INV_SPEED_DIFFERENCE, // Damage inverse with speed (e.g. gyro ball)
        FIXED_DAMAGE, // Seismic toss, dragon rage
        TRAPPING, // Trapping moves
        HIGH_CRIT, // Like sure crit but not sure
        SUB_60, // Moves under 60 damage will be increased by technician
        CHARGING, // Charging moves, like solar beam
        RECHARGING, // Moves that recharge like hyper beam (not so good as they seem)
        MULTIHIT_2_MOVE, // 2-hit moves like bonemerang
        MULTIHIT_3_MOVE, // 3-hit move like triple dive
        MULTIHIT_2_TO_5_MOVE, // The common multi hit 2-5 hits
        MULTIHIT_ACC_BASED_3_HIT, // Triple axel
        MULTIHIT_ACC_BASED_10_HIT, // Good ol Pop Bomb
        SUN_SETTER, // Obvious
        RAIN_SETTER, // Obvious
        SNOW_SETTER, // Obvious
        SAND_SETTER, // Obvious
        ELE_TERRAIN_SETTER, // Obvious
        GRASSY_TERRAIN_SETTER, // Obvious
        PSYCHIC_TERRAIN_SETTER, // Obvious
        MISTY_TERRAIN_SETTER, // Obvious
        ABILITY_MANIPULATING, // Deal with abilities, either giving or taking abilities
        BERRY_DEPENDANT, // Depends on mon having a berry on
        SWITCH_OPPONENT, // Switches opponent out
        USES_STRONGEST_STAT, // Uses strongest stat for damage calculation
        BYPASSES_IMMUNITY, // Move bypasses immunities thay would've made it do 0 damage
        GOOD_FIRST_MON, // Moves that are only really interesting on the first mon of the team (i.e. hazard setting)
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
        public double Bp { get; set; }
        public double Acc { get; set; }
        public HashSet<EffectFlag> Flags { get; set; } = new HashSet<EffectFlag>();
        public override string ToString()
        {
            return Name;
        }
    }
    public class Ability
    {
        public string Name { get; set; } = "";
        public HashSet<EffectFlag> Flags { get; set; } = new HashSet<EffectFlag>();
        public override string ToString()
        {
            return Name;
        }
    }
}
