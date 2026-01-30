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
        ORIGINAL_TYPE_OF_MOVE, /// Afftects moves before their type is modified
        DAMAGING_MOVE_OF_TYPE, /// Affects damaging moves of specific types (most damage related stuff!)
        MOVE_CATEGORY, /// Affects phy/spe/status (e.g. prankster, AV)
        ANY_DAMAGING_MOVE, /// Affects every single damaging move (e.g. normalize)
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// What stat is going to be modified and how
    public enum StatModifier
    {
        WEIGHT_MULTIPLIER, /// Things that modify weight
        // Things that change a whole ass mon
        SUFFIX_CHANGE, /// Format is a -Suffix which fetches the mon's bst lol
        // The base multiplier of stuff *X, caused by abilities, items etc
        HP_MULTIPLIER, /// Attack multiplications, usually reductions
        ATTACK_MULTIPLIER, /// Things that multiply attack
        DEFENSE_MULTIPLIER, /// Things that multiply defense (body press calc i.g.)
        SPECIAL_ATTACK_MULTIPLIER, /// Things that multiply special attack
        SPECIAL_DEFENSE_MULTIPLIER, /// Things that multiply special defense
        SPEED_MULTIPLIER, /// Things that multiply speed
        PHYSICAL_ACCURACY_MULTIPLIER, /// Things that multiply physical accuracy
        SPECIAL_ACCURACY_MULTIPLIER, /// Things that multiply special accuracy
        OPP_HP_MULTIPLIER, /// This is weird but it's basically effects that diminish opp max health (i.e hazards)
        OPP_ATTACK_MULTIPLIER,
        OPP_DEFENSE_MULTIPLIER,
        OPP_SPECIAL_ATTACK_MULTIPLIER,
        OPP_SPECIAL_DEFENSE_MULTIPLIER,
        OPP_SPEED_MULTIPLIER,
        // Stat changes of stats caused usually by moves or abilities
        ATTACK_BOOST, /// Attack stat changes
        DEFENSE_BOOST, /// Defense stat changes
        SPECIAL_ATTACK_BOOST, /// Special attack stat changes
        SPECIAL_DEFENSE_BOOST, /// Special defense stat changes
        SPEED_BOOST, /// Speed stat changes
        CRIT_BOOST, /// Boost to critical stage
        OPP_ATTACK_BOOST,
        OPP_DEFENSE_BOOST,
        OPP_SPECIAL_ATTACK_BOOST,
        OPP_SPECIAL_DEFENSE_BOOST,
        OPP_SPEED_BOOST,
        // Thigs that affect the boosts in weird ways
        HIGHEST_STAT_BOOST, /// Stat change of the highest stat
        ALL_BOOSTS, /// All boosts are affected a specific amount
        ALL_OPP_BOOSTS, /// Same but for opponent
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
        NULLIFIES_RECV_DAMAGE_OF_TYPE, // For nullify/absorb abilities
        DOUBLES_RECV_DAMAGE_OF_TYPE, // For abilities that fuck me up
        HALVES_RECV_DAMAGE_OF_TYPE, // For chilan berry
        HALVES_RECV_SE_DAMAGE_OF_TYPE, // For resist berries
        ALTER_RECV_SE_DAMAGE, // Halves all SE damage
        ALTER_RECV_NON_SE_DAMAGE, // For wonder guard!
    }
    [JsonConverter(typeof(StringEnumConverter))]
    /// How specific moves are affected
    public enum MoveModifier
    {
        // Things that affect moves for damage calculation
        MOVE_BP_MOD, /// A specific's move BP is affected
        MOVE_ACC_MOD, /// A specific's move accuracy is affected
        MOVE_TYPE_MOD, /// A specific's move type is affected
        ADD_FLAG, /// Move gains a flag (e.g. poison touch makes contact moves poison inducing). Flags can't add flags, only everything else to avoid weird loops
        REMOVE_FLAG, /// Move loses a flag (e.g. long reach removes contact). Flags can't remove flags, only everything else to avoid weird loops
    }
}
