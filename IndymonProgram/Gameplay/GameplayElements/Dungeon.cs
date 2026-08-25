using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gameplay.GameplayElements
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncounterType
    {
        UNKNOWN, /// This is an error
        POKEMON_BATTLE, /// Starts a Pokemon Battle with mons of the floor and generic layout
        BOSS, /// Starts a boss battle from this floor
        CAMPING, /// Camping event
        TREASURE, /// Player gets a free treasure prize from rare item pool
        GUARDED_TREASURE, /// A treasure but a mon is equipping it, uses generic room layout
        EVO, /// Evolution crystal event
        HP_HEAL, /// Heals percentage of all party hp
        FULL_RESTORE, /// Heals big percentage of a specific mon
        REVIVE, /// Revives a random mon with some health
        STATUS_CURE, /// Cures the status of party
        JOINER, /// A mon from current floor joins you for adventures, will be rank 0 catchable
        DAMAGE_TRAP, /// Trap that deals % hp damage to all mons
        STATUS_TRAP, /// Applies a status effect to a pokemon
        KILL_TRAP, /// A random mon faints
        NPC_BATTLE, /// A trainer npc battles you to 3v3. Heals your current first 3 (or some random fainted if can't do 3). Losing battle doesn't end dungeon though
        PLATE, /// Find a random evo plate
        MOVE_DISK, /// Find a random Move Disk as well as a type disk and some blanks
        BABIES, /// Fight 6 mons from first floor but with 0.75 stat mult
        SWARM, /// Fight 6 mons from current floor but with 0.75 stat mult
        UNOWN, /// Unown event, they form a word. You fight them and get the item if win. Unowns have a dynamic stat mult that scales
        IMP_GAIN, /// Gives IMP
        SANDWICH /// Gives Sandwich
    }
    public class Weather
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Chance { get; set; }
    }
    public class ItemDrops
    {
        public string Name { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// Defines for a specific sripted encounter, which type it is
    public enum EncounterEnemyType
    {
        NONE, /// No encounter for this category (e.g. no boss, etc)
        SPECIFIC_POKEMON, /// A specific Pokemon in BossAuxString
        NEXT_FLOOR, /// Any Pokemon from a floor up (or current if max floor)
        CURRENT_FLOOR, /// Any Pokemon from current floor (or current if max floor)
        PREVIOUS_FLOOR, /// Any Pokemon from a floor down (or current if min floor)
        ONE_OF, /// One of the options in BossAuxString
        FIRST_FLOOR, /// Any Pokemon from first floor specifically
        BOSS_PREVOS, /// All the prevos of a boss (if applicable but will default to f1 if not to avoid making fight too easy)
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// What type of item is used as equips and prizes
    public enum EncounterItemType
    {
        NONE, /// No prize
        SPECIFIC_ITEM, /// A specific item (param)
        COMMON_ITEM_POOL, /// A random item from the common item pool
        RARE_ITEM_POOL, /// A random item from the rare item pool
        ALL_ITEM_POOLS, /// Random item from both item pools
        EQUIPPED_ITEMS, /// The items that have been equipped (really makes sense for prizes only)
        BOSS, /// The boss is willing to join you (rank 0)
    }
    /// <summary>
    /// Defines a specific scripted encounter, all the elements which are encounter-specific and not used for the dungeon at large
    /// </summary>
    public class Encounter
    {
        public EncounterType Type { get; set; } = EncounterType.UNKNOWN; /// What type of encounter it is (good for scoring and for illustrating)
        // For strings, $X uses the corresponding string param
        public string PreEncounterString { get; set; } = ""; /// String that shows in beginning of encounter
        public string PostEncounterString { get; set; } = ""; /// String that shows in end of encounter (if dungeon is not over)
        public List<string> Params { get; set; } = []; /// Params in order of how they're used, really depends on the event type, documented on the switch case of dungeon exec
        public double[] EncounterStatMult { get; set; } = [1, 1, 1, 1, 1, 1]; /// Additional stat mult for encounter (only for boss if boss fight)
        public EncounterEnemyType BossType { get; set; } = EncounterEnemyType.NONE; /// If encounter will have a boss, which type of encounter it is
        public EncounterItemType BossEquipType { get; set; } = EncounterItemType.NONE; /// If boss encounter, check what type of item the boss can equip
        public double BossEquipChance { get; set; } = 0; /// And if so, whats the base equip chance
        public EncounterEnemyType NormalEnemyType { get; set; } = EncounterEnemyType.NONE; /// If boss encounter not none, may have followers, if so, which kind?
        public EncounterItemType NormalEquipType { get; set; } = EncounterItemType.NONE; /// If encounter, check what type of item the mons can equip
        public double NormalEquipChance { get; set; } = 0; /// And if so, whats the base equip chance
        public int EncounterMaxSimultaneousEnemyMons { get; set; } = GameplayElementsContainer.GameplayElementsContainer.DEFAULT_BATTLE_NMONS; /// How many enemies will appear at the same time on one side of the battle
        public int EncounterEnemyNumber { get; set; } = 0; /// How many enemies to add to this (Will be always 1 boss)
        public EncounterItemType PrizeType { get; set; } = EncounterItemType.NONE; /// The prize you get if the result was good
        public double BaseWeight { get; set; } = 1; /// Weight of event to be compared with others, to create "rare" events
    }
    public class Floor
    {
        public Dictionary<string, HashSet<string>> WeatherMons { get; set; } /// Contains which mons are found for each weather (key). There's an ALL keyword with mons that appear in all weathers
        public List<Encounter> BossEncounters { get; set; } /// The possible specific boss encounters for this floor
    }
    public class Dungeon
    {
        public string Name { get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public int Difficulty { get; set; }
        public List<Weather> PossibleWeathers { get; set; }
        public Weather CurrentWeather { get; set; }
        public double GeneralItemDropChance { get; set; }
        public List<ItemDrops> CommonDrops { get; set; }
        public List<ItemDrops> RareDrops { get; set; }
        public List<Floor> Floors { get; set; }
        public List<Encounter> EncounterPool { get; set; }
    }
}
