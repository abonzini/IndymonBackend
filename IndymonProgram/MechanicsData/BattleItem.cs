using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BattleItemFlag
    {
        NO_ITEM, // Item that is actualyl no item, helps acrobatics
        ALL_ITEMS, // Any item has this tag (except NO_ITEM of course)
        BERRY, // Berry
        CONSUMABLE, // Unburden, acro
        BAD_ITEM, // Items that are normally useless or bad/negative
        MOVE_BOOSTING_ITEM, // Items that boost are meant to boost the moves somehow, e.g. leftovers is good no matter what but choice scarf needs to boost to be useful, otherwise marked as useless
    }
    public class BattleItem
    {
        public string Name { get; set; } = "";
        public PokemonType OffensiveBoostType { get; set; } = PokemonType.NONE;
        public PokemonType DefensiveBoostType { get; set; } = PokemonType.NONE;
        public HashSet<BattleItemFlag> Flags { get; set; } = new HashSet<BattleItemFlag>(); /// Flags that an item may have
        public override string ToString()
        {
            return Name;
        }
    }
}
