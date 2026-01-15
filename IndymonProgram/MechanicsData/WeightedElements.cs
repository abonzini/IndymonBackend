using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechanicsData
{
    /// <summary>
    /// Things that weight or are weighted for setbuilding. The "type" of element
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ElementType
    {
        POKEMON, /// The pokemon. Can't be affected, but affects stuff
        POKEMON_TYPE, /// The pokemon type. Can't be affected, but affects stuff
        POKEMON_HAS_EVO, /// The flag of whether the pokemon has an evo or not (only way to make eviolite happen)
        ARCHETYPE, /// Certain archetypes such as rain, terrains, etc. Not weighted but enabled, and can weight strategies
        BATTLE_ITEM, /// Affects a specific named item
        BATTLE_ITEM_FLAGS, /// Affects items with that flag
        MOD_ITEM, /// A mod item is affecting stuf, normally the change stats or sth
        ABILITY, /// Affects an ability
        MOVE, /// Affects a specific move by name
        EFFECT_FLAGS, /// Affects moves or abilities carrying a specific flag
        DAMAGING_MOVE_OF_TYPE, /// Affects damaging moves of specific types (most damage related stuff!)
        MOVE_CATEGORY, /// Affects phy/spe/status (e.g. prankster, AV)
        ANY_DAMAGING_MOVE, /// Affects every single damaging move (e.g. normalize)
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// What stat is going to be modified and how
    public enum StatModifier
    {
        // The base multiplier of stuff *X, caused by abilities, items etc
        ATTACK_MULTIPLIER, /// Things that multiply attack
        DEFENSE_MULTIPLIER, /// Things that multiply defense (body press calc i.g.)
        SPECIAL_ATTACK_MULTIPLIER, /// Things that multiply special attack
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
        // Things that affect the mon (damage calc wise)
        HP_EV, /// EVs
        ATK_EV, /// EVs
        DEF_EV, /// EVs
        SPATK_EV, /// EVs
        SPDEF_EV, /// EVs
        SPEED_EV, /// EVs
        NATURE, /// Nature name
        TYPE_1, /// Changes first type
        TYPE_2, /// Changes second type
        TERA, /// Changes tera
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// How specific moves are affected
    public enum MoveModifier
    {
        // Things that affect moves for damage calculation
        MOVE_BP_MOD, /// A specific's move BP is affected
        MOVE_ACC_MOD, /// A specific's move accuracy is affected
        MOVE_TYPE_MOD, /// A specific's move type is affected
    }
}
