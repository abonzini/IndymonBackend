using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ParsersAndData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoomEventType
    {
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
        ARCHAEOLOGIST, // Archaeologist gives you a random plate
        PARADOX, // Paradox Team member gives you a TR
    }
    public class RoomEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RoomEventType EventType { get; set; }
        public string PreEventString { get; set; }
        public string PostEventString { get; set; }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShortcutConditionType
    {
        MOVE, // Need a specific move
        ABILITY, // Need a specific ability
        POKEMON, // Need a specific pokemon
        TYPE, // Need a specific pokemon type
        ITEM // Need a specific item
    }
    public class ShortcutCondition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ShortcutConditionType ConditionType { get; set; }
        public string Which { get; set; }
    }
    public class DungeonFloor
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor RoomColor { get; set; } = ConsoleColor.White;
        public char NeCornerTile { get; set; }
        public char NwCornerTile { get; set; }
        public char SeCornerTile { get; set; }
        public char SwCornerTile { get; set; }
        public char EWallTile { get; set; }
        public char WWallTile { get; set; }
        public char NWallTile { get; set; }
        public char SWallTile { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor PassageColor { get; set; } = ConsoleColor.White;
        public char NePassageTile { get; set; }
        public char NwPassageTile { get; set; }
        public char SePassageTile { get; set; }
        public char SwPassageTile { get; set; }
        public char HorizontalPassageTile { get; set; }
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
    }
    public class Dungeon
    {
        public string Name { get; set; }
        public bool GoesDownwards { get; set; }
        public List<RoomEvent> Events { get; set; }
        public List<List<string>> PokemonEachFloor { get; set; }
        public List<string> CommonItems { get; set; }
        public List<string> RareItems { get; set; }
        public List<DungeonFloor> Floors { get; set; }
        public RoomEvent BossEvent { get; set; }
        public RoomEvent CampingEvent { get; set; }
        public string NextDungeon { get; set; }
        public string NextDungeonShortcut { get; set; }
    }
}
