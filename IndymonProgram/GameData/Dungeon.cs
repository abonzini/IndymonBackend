using MechanicsData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameData
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
        UNOWN, // Unown event, 6 random unown with wacky moves
        FIRELORD, // Fire legendary event, summon a legendary of lower lvl
        GIANT_POKEMON, // Underwater event with a mon from same floor but lvl 110-125
        MIRROR_MATCH, // Mirror match vs your own party but lvl 80-90
        PLOT_CLUE, // New text color, say cyan, for storyline clue that may lead you to special event
        IMP_GAIN, // Gives IMP instead of items
        APRICORN, // Fight vs bug, reward is apricorns
        REGISTEEL, // Draws registeel face
        REGIROCK, // Draws regirock face
        REGICE, // Draws regice face
        REGIELEKI, // Draws regieleki face
        REGIDRAGO, // Draws regidrago face
    }
    public class ItemReward
    {
        public string Name { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public class RoomEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RoomEventType EventType;
        public string PreEventString;
        public string PostEventString;
        public string SpecialParams = "";
        public string EventLook = "";
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor EventFg = ConsoleColor.White;
        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor EventBg = ConsoleColor.Black;
        public int OffsetAnchorX = 0;
        public int OffsetAnchorY = 0;
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
    public class ShortcutConditions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ShortcutConditionType ConditionType;
        public List<string> Which = new List<string>();
    }
    public class Shortcut
    {
        public List<ShortcutConditions> Conditions;
        public string Clue = "";
        public string Resolution = "";
        public int RoomNumber; // Which room shortcut is at
        public int RoomDestination; // Where does it lead to
    }
    public class Dungeon
    {
        public string Name;
        public int NFloors = 3;
        public int NRoomsPerFloor = 5;
        public int EventAnchorX = 0;
        public int EventAnchorY = 0;
        public List<List<char>> TileMap = [];
        public List<List<char>> Markers = [];
        public List<List<ConsoleColor>> FgMap = [];
        public List<List<ConsoleColor>> BgMap = [];
        public int TilemapSizeX = 0;
        public int TilemapSizeY = 0;
        public List<RoomEvent> Events;
        public List<List<string>> PokemonEachFloor;
        public List<ItemReward> CommonItems;
        public List<ItemReward> RareItems;
        public ItemReward BossItem;
        public RoomEvent WildMonsEvent;
        public RoomEvent BossEvent;
        public RoomEvent CampingEvent;
        public RoomEvent PreBossEvent;
        public RoomEvent PostBossEvent;
        public List<Shortcut> RoomShortcuts;
        public string NextDungeon;
        public Shortcut DungeonShortcut;
        public string NextDungeonShortcut;
        public List<string> CustomShowdownRules;
        public HashSet<TeamArchetype> DungeonArchetypes;
        public Weather DungeonWeather;
        public Terrain DungeonTerrain;
        public override string ToString()
        {
            return Name;
        }
        /// <summary>
        /// Gets room number comin from which floor and which depth
        /// </summary>
        /// <param name="floor">Current Floor</param>
        /// <param name="depth">Current Depth within floor</param>
        /// <returns>Absolute room number</returns>
        public int GetRoomNumber(int floor, int depth)
        {
            return NRoomsPerFloor * floor + depth;
        }
        /// <summary>
        /// Gets coords (floor and depth) of a given room
        /// </summary>
        /// <param name="room">Which room</param>
        /// <returns>Coord of room</returns>
        public (int, int) GetRoomCoords(int room)
        {
            int floor = room / NRoomsPerFloor;
            int depth = room % NRoomsPerFloor;
            return (floor, depth);
        }
        /// <summary>
        /// Returns which color a char describes
        /// </summary>
        /// <param name="c">Char describving color</param>
        /// <returns>Which console color it is</returns>
        public static ConsoleColor CharToColor(char c)
        {
            return c switch
            {
                'k' or 'K' => ConsoleColor.Black,
                'B' => ConsoleColor.DarkBlue,
                'G' => ConsoleColor.DarkGreen,
                'C' => ConsoleColor.DarkCyan,
                'R' => ConsoleColor.DarkRed,
                'M' => ConsoleColor.DarkMagenta,
                'Y' => ConsoleColor.DarkYellow,
                'a' => ConsoleColor.Gray,
                'A' => ConsoleColor.DarkGray,
                'b' => ConsoleColor.Blue,
                'g' => ConsoleColor.Green,
                'c' => ConsoleColor.Cyan,
                'r' => ConsoleColor.Red,
                'm' => ConsoleColor.Magenta,
                'y' => ConsoleColor.Yellow,
                'w' or 'W' or 't' or 'T' => ConsoleColor.White,
                _ => ConsoleColor.Black,
            };
        }
        /// <summary>
        /// Transforms a color to a char
        /// </summary>
        /// <param name="color">Which color</param>
        /// <returns>Which char will represent the color</returns>
        public static char ColorToChar(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => 'k',
                ConsoleColor.DarkBlue => 'B',
                ConsoleColor.DarkGreen => 'G',
                ConsoleColor.DarkCyan => 'C',
                ConsoleColor.DarkRed => 'R',
                ConsoleColor.DarkMagenta => 'M',
                ConsoleColor.DarkYellow => 'Y',
                ConsoleColor.Gray => 'a',
                ConsoleColor.DarkGray => 'A',
                ConsoleColor.Blue => 'b',
                ConsoleColor.Green => 'g',
                ConsoleColor.Cyan => 'c',
                ConsoleColor.Red => 'r',
                ConsoleColor.Magenta => 'm',
                ConsoleColor.Yellow => 'y',
                ConsoleColor.White => 'w',
                _ => throw new Exception($"{color} not recognized as a valid color for this color code"),
            };
        }
    }
}
