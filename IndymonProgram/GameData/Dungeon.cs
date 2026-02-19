using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ParsersAndData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoomEventType
    {
        NONE, // No event
        POKEMON_BATTLE, // Normal found in pokemon room lol
        CAMPING, // Break room/camping
        TREASURE, // Player gets a treasure from rare item pool
        ALPHA, // Mon from 1 floor above holding rare item
        BOSS, // Boss event
        EVO, // Evolution crystal
        HEAL, // Heals 50% party hp
        CURE, // Cure party status
        JOINER, // A mon from 1st floor joins you for adventures
        DAMAGE_TRAP, // Trap deals 25% hp damage
        STATUS_TRAP, // Each mon 33% has a chance of getting a status effect
        NPC_BATTLE, // A trainer npc battles you
        RESEARCHER, // RESEARCHER gives you a random plate
        PARADOX, // Paradox Team member gives you a TR
        SWARM, // Fight 6 lvl 60-75 from first floor
        BIG_HEAL, // One specific mon heals a lot
        PP_HEAL, // 3PP heal
        UNOWN, // Unown event, 6 random unown with wacky moves
        FIRELORD, // Fire legendary event, summon a legendary of lower lvl
        GIANT_POKEMON, // Underwater event with a mon from same floor but lvl 110-125
        MIRROR_MATCH, // Mirror match vs your own party but lvl 80-90
        PLOT_CLUE, // New text color, say cyan, for storyline clue that may lead you to special event
        IMP_GAIN, // Gives IMP instead of items
        REGISTEEL, // Draws registeel face
        REGIROCK, // Draws regirock face
        REGICE, // Draws regice face
        REGIELEKI, // Draws regieleki face
    }
    public class RoomEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RoomEventType EventType;
        public string PreEventString;
        public string PostEventString;
        public string SpecialParams;
        public override string ToString()
        {
            return EventType.ToString();
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShortcutConditionType
    {
        MOVE, // Need a specific move
        ABILITY, // Need a specific ability
        POKEMON, // Need a specific pokemon
        TYPE, // Need a specific pokemon type
        ITEM, // Need a specific item
        MOVE_DISK, // Needs any move disk (item ending in move disk)
    }
    public class ShortcutCondition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ShortcutConditionType ConditionType;
        public List<string> Which = new List<string>();
        public override string ToString()
        {
            return $"{ConditionType} -> {Which}";
        }
    }
    public class DungeonFloor
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor RoomColor = ConsoleColor.White;
        public char NeWallTile;
        public char NwWallTile;
        public char SeWallTile;
        public char SwWallTile;
        public char EWallTile;
        public char WWallTile;
        public char NWallTile;
        public char SWallTile;
        public char EWallPassageTile;
        public char WWallPassageTile;
        public char NWallPassageTile;
        public char SWallPassageTile;
        public char NWallShortcutTile;
        public char SWallShortcutTile;
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor PassageColor = ConsoleColor.White;
        public char VerticalPassageTile;
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor ShortcutColor = ConsoleColor.White;
        public char NeShortcutTile;
        public char NwShortcutTile;
        public char SeShortcutTile;
        public char SwShortcutTile;
        public char HorizontalShortcutTile;
        public char VerticalShortcutTile;
        public List<ShortcutCondition> ShortcutConditions = new List<ShortcutCondition>();
        public string ShortcutClue;
        public string ShortcutResolution;
    }
    public class Dungeon
    {
        public string Name;
        public bool GoesDownwards;
        public List<RoomEvent> Events;
        public List<List<string>> PokemonEachFloor;
        public List<string> CommonItems;
        public List<string> RareItems;
        public string BossItem;
        public List<DungeonFloor> Floors;
        public RoomEvent BossEvent;
        public RoomEvent CampingEvent;
        public RoomEvent PreBossEvent;
        public RoomEvent PostBossEvent;
        public string NextDungeon;
        public string NextDungeonShortcut;
        public List<string> CustomShowdownRules;
        public override string ToString()
        {
            return Name;
        }
    }
}
