using Gameplay.GameEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gameplay.GameplayElements
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncounterType
    {
        UNKNOWN, /// This is an error
        POKEMON_BATTLE, /// Starts a Pokemon Battle with mons of the floor and generic layout
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
        NPC_BATTLE, /// A trainer npc battles you to 3v3. Heals your current first 3 (or some random fainted if can't do 3). Losing battle doesn't end dungeon though. Different to other wild battles due to the forced 3v3 natures and instancing of an NPC. Additionally, the NPC battle is proportional to the danger lvl
        PLATE, /// Find a random evo plate
        MOVE_DISK, /// Find a random Move Disk as well as a type disk and some blanks
        BABIES, /// Fight 6 mons from first floor but with 0.75 stat mult
        SWARM, /// Fight 6 mons from current floor but with 0.75 stat mult
        UNOWN, /// Unown event, they form a word. You fight them and get the item if win. Unowns have a dynamic stat mult that scales
        IMP_GAIN, /// Gives IMP
        SANDWICH, /// Gives Sandwich
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncounterCategory
    {
        SCRIPTED, // Scripted rooms, would never appear normally unless scripted
        REFRESH, // Heal/refresh rooms
        PRIZE, // Prize rooms
        NEUTRAL, // Neutral rooms
        DANGER, // Dangerous rooms
    }
    public class WeatherForecast
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Chance { get; set; }
        public override string ToString()
        {
            return $"{Name} ({Chance * 100}%)";
        }
    }
    public class ItemDrops
    {
        public string Name { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public override string ToString()
        {
            return $"{Name} ({Min}-{Max})";
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// Defines for a specific sripted encounter, which type it is
    public enum EncounterEnemyType
    {
        NONE, /// No encounter for this category (e.g. no boss, etc)
        SPECIFIC_POKEMON, /// A specific Pokemon in Params. If / it can be any either (or rerolled if multiple)
        NEXT_FLOOR, /// Any Pokemon from a floor up (or current if max floor)
        CURRENT_FLOOR, /// Any Pokemon from current floor (or current if max floor)
        PREVIOUS_FLOOR, /// Any Pokemon from a floor down (or current if min floor)
        FIRST_FLOOR, /// Any Pokemon from first floor specifically
        PREVOS_1, /// All the prevos of any mon in #1 (if applicable), naturally this only makes sense for enemy 2
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
        ENEMY_1, /// Enemy 1 willing to join you as rank 0
        ENEMY_2, /// Enemy 1 willing to join you as rank 0
    }
    /// <summary>
    /// Defines a specific scripted encounter, all the elements which are encounter-specific and not used for the dungeon at large
    /// </summary>
    public class Encounter
    {
        public EncounterType Type { get; set; } = EncounterType.UNKNOWN; /// What type of encounter it is (good for scoring and for illustrating)
        public EncounterCategory Category = EncounterCategory.SCRIPTED; /// What type of cateogry to be bunched in probabilistics
        // For strings, $X uses the corresponding string param
        public string PreEncounterString { get; set; } = ""; /// String that shows in beginning of encounter
        public string EncounterSuccessString { get; set; } = ""; /// String that shows in end of encounter (if event was succesful)
        public string EncounterFailureString { get; set; } = ""; /// String that shows in end of encounter (if event unsuccesful)
        public List<string> Params { get; set; } = []; /// Params in order of how they're used, really depends on the event type, documented on the switch case of dungeon exec
        public EncounterItemType PrizeType { get; set; } = EncounterItemType.NONE; /// The prize you get if the result was a success
        public double BaseWeight { get; set; } = 1; /// Weight of event to be compared with others, to create "rare" events
        public List<WeatherForecast> WeatherOverride { get; set; } = [];
        // For battle events, there's Enemy1 and Enemy2, this allows for both Boss + Followers (1 and 2 respectively) or 2 separate group of enemies in some fights
        public Side Enemy1Side { get; set; } = Side.TOP; // By default, enemy and all in same team
        public bool Is1Boss { get; set; } = false; // Making it a boss means the game is over when it dies without defeating the rest
        public double[] Enemy1StatMult { get; set; } = [1, 1, 1, 1, 1, 1];
        public EncounterEnemyType Enemy1Type { get; set; } = EncounterEnemyType.NONE;
        public EncounterItemType Enemy1EquipType { get; set; } = EncounterItemType.NONE;
        public double Enemy1EquipChance { get; set; } = 0;
        public int Enemy1Number { get; set; } = 0; /// How many enemies to add to this (Boss not counted if a boss fight)
        public Side Enemy2Side { get; set; } = Side.TOP; // By default, enemy and all in same team
        public double[] Enemy2StatMult { get; set; } = [1, 1, 1, 1, 1, 1];
        public EncounterEnemyType Enemy2Type { get; set; } = EncounterEnemyType.NONE;
        public EncounterItemType Enemy2EquipType { get; set; } = EncounterItemType.NONE;
        public double Enemy2EquipChance { get; set; } = 0;
        public int Enemy2Number { get; set; } = 0; /// How many enemies to add to this (Boss not counted if a boss fight)
        public int EncounterMaxSimultaneousEnemyMons { get; set; } = GameplayElementsContainer.GameplayElementsContainer.DEFAULT_BATTLE_NMONS; /// How many enemies will appear at the same time on one side of the battle
        public override string ToString()
        {
            return Type.ToString();
        }
    }
    public class Floor
    {
        public Dictionary<string, HashSet<string>> WeatherMons { get; set; } /// Contains which mons are found for each weather (key). There's an ALL keyword with mons that appear in all weathers
    }
    public class Dungeon
    {
        public string Name { get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public int Difficulty { get; set; }
        public List<WeatherForecast> PossibleWeathers { get; set; }
        public WeatherForecast CurrentWeather { get; set; }
        public double GeneralItemDropChance { get; set; }
        public List<ItemDrops> CommonDrops { get; set; }
        public List<ItemDrops> RareDrops { get; set; }
        public List<Floor> Floors { get; set; }
        public List<Encounter> EncounterPool { get; set; }
        public List<Encounter> BossEncounters { get; set; }
        public Encounter FinalBossEncounter { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
