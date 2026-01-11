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
    }
    public class RoomEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RoomEventType EventType { get; set; }
        public string PreEventString { get; set; }
        public string PostEventString { get; set; }
        public string SpecialParams { get; set; }
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
        public ShortcutConditionType ConditionType { get; set; }
        public List<string> Which { get; set; } = new List<string>();
        public override string ToString()
        {
            return $"{ConditionType} -> {Which}";
        }
    }
    public class DungeonFloor
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor RoomColor { get; set; } = ConsoleColor.White;
        public char NeWallTile { get; set; }
        public char NwWallTile { get; set; }
        public char SeWallTile { get; set; }
        public char SwWallTile { get; set; }
        public char EWallTile { get; set; }
        public char WWallTile { get; set; }
        public char NWallTile { get; set; }
        public char SWallTile { get; set; }
        public char EWallPassageTile { get; set; }
        public char WWallPassageTile { get; set; }
        public char NWallPassageTile { get; set; }
        public char SWallPassageTile { get; set; }
        public char NWallShortcutTile { get; set; }
        public char SWallShortcutTile { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor PassageColor { get; set; } = ConsoleColor.White;
        public char VerticalPassageTile { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor ShortcutColor { get; set; } = ConsoleColor.White;
        public char NeShortcutTile { get; set; }
        public char NwShortcutTile { get; set; }
        public char SeShortcutTile { get; set; }
        public char SwShortcutTile { get; set; }
        public char HorizontalShortcutTile { get; set; }
        public char VerticalShortcutTile { get; set; }
        public List<ShortcutCondition> ShortcutConditions { get; set; } = new List<ShortcutCondition>();
        public string ShortcutClue { get; set; }
        public string ShortcutResolution { get; set; }
    }
    public class Dungeon
    {
        public string Name { get; set; }
        public bool GoesDownwards { get; set; }
        public List<RoomEvent> Events { get; set; }
        public List<List<string>> PokemonEachFloor { get; set; }
        public List<string> CommonItems { get; set; }
        public List<string> RareItems { get; set; }
        public string BossItem { get; set; }
        public List<DungeonFloor> Floors { get; set; }
        public RoomEvent BossEvent { get; set; }
        public RoomEvent CampingEvent { get; set; }
        public RoomEvent PreBossEvent { get; set; }
        public RoomEvent PostBossEvent { get; set; }
        public string NextDungeon { get; set; }
        public string NextDungeonShortcut { get; set; }
        public List<string> CustomShowdownRules { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
