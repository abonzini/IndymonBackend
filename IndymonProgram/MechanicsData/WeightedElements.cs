using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    /// <summary>
    /// Things that weight or are weighted for setbuilding. The "type" of element
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WeightedElementType
    {
        POKEMON, /// Affects/enables sets
        ARCHETYPE, /// Certain archetypes such as rain, terrains, etc. Not weighted but enabled, and can weight strategies
        BATTLE_ITEM, /// Affects a specific named item
        BATTLE_ITEM_FLAGS, /// Affects items with that flag
        ABILITY, /// Affects an ability
        MOVE, /// Affects a specific move by name
        MOVE_FLAGS, /// Affects moves carrying a specific flag
        MOVE_TYPE, /// Affects moves of specific types
        MOVE_CATEGORY, /// Affects phy/spe/status (e.g. prankster, AV)
        // The base multiplier of stuff *X, caused by abilities, items etc
        ATTACK_MULTIPLIER, /// Things that multiply attack
        DEFENSE_MULTIPLIER, /// Things that multiply defense
        SPECIAL_ATTACK_MULTIPLIER, /// Things that multiply special attack
        SPECIAL_DEFENSE_MULTIPLIER, /// Things that multiply special defense
        SPEED_MULTIPLIER, /// Things that multiply speed
        PHYSICAL_ACCURACY_MULTIPLIER, /// Things that multiply physical accuracy
        SPECIAL_ACCURACY_MULTIPLIER, /// Things that multiply special accuracy
        // Stat changes of stats caused usually by moves or abilities
        ATTACK_BOOST, /// Attack stat changes
        DEFENSE_BOOST, /// Defense stat changes
        SPECIAL_ATTACK_BOOST, /// Special attack stat changes
        SPECIAL_DEFENSE_BOOST, /// Special defense stat changes
        SPEED_BOOST, /// Speed stat changes
        HIGHEST_STAT_BOOST, /// Stat change of the highest stat
        // Thigs that affect the boosts in weird ways
        ALL_BOOSTS, /// All boosts are affected a specific amount
    }
}
