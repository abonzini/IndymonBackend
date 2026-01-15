using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        /// <summary>
        /// Validates whether this element extists in data
        /// </summary>
        /// <param name="type">Type of element</param>
        /// <param name="name">Name of the element to verify</param>
        /// <returns>True if element exists in this data</returns>
        public bool ValidateElementExistance(ElementType type, string name)
        {
            return type switch
            {
                ElementType.POKEMON => Dex.ContainsKey(name),
                ElementType.POKEMON_TYPE => Enum.TryParse<PokemonType>(name, true, out _),
                ElementType.POKEMON_HAS_EVO => bool.TryParse(name, out _),
                ElementType.ARCHETYPE => Enum.TryParse<TeamArchetype>(name, true, out _),
                ElementType.BATTLE_ITEM => BattleItems.ContainsKey(name),
                ElementType.BATTLE_ITEM_FLAGS => Enum.TryParse<BattleItemFlag>(name, true, out _),
                ElementType.MOD_ITEM => ModItems.ContainsKey(name),
                ElementType.ABILITY => Abilities.ContainsKey(name),
                ElementType.MOVE => Moves.ContainsKey(name),
                ElementType.EFFECT_FLAGS => Enum.TryParse<EffectFlag>(name, true, out _),
                ElementType.DAMAGING_MOVE_OF_TYPE => Enum.TryParse<PokemonType>(name, true, out _),
                ElementType.MOVE_CATEGORY => Enum.TryParse<MoveCategory>(name, true, out _),
                ElementType.ANY_DAMAGING_MOVE => true,
                _ => false,
            };
            ;
        }
        /// <summary>
        /// Validates whether this stat mod exists in data
        /// </summary>
        /// <param name="mod">Type of mod</param>
        /// <param name="name">Name of the element to verify</param>
        /// <returns>True if element exists in this data</returns>
        public static bool ValidateStatModExistance(StatModifier mod, string name)
        {
            return mod switch
            {
                StatModifier.ATTACK_MULTIPLIER or StatModifier.DEFENSE_MULTIPLIER or StatModifier.SPECIAL_ATTACK_MULTIPLIER or StatModifier.SPEED_MULTIPLIER or StatModifier.SPECIAL_ACCURACY_MULTIPLIER or StatModifier.PHYSICAL_ACCURACY_MULTIPLIER => float.TryParse(name, out _),
                StatModifier.ATTACK_BOOST or StatModifier.DEFENSE_BOOST or StatModifier.SPECIAL_ATTACK_BOOST or StatModifier.SPECIAL_DEFENSE_BOOST or StatModifier.SPEED_BOOST or StatModifier.HIGHEST_STAT_BOOST or StatModifier.ALL_BOOSTS or StatModifier.HP_EV or StatModifier.ATK_EV or StatModifier.DEF_EV or StatModifier.SPATK_EV or StatModifier.SPDEF_EV or StatModifier.SPEED_EV => int.TryParse(name, out _),
                StatModifier.NATURE => Enum.TryParse<Nature>(name, true, out _),
                StatModifier.TERA or StatModifier.TYPE_1 or StatModifier.TYPE_2 => Enum.TryParse<PokemonType>(name, true, out _),
                _ => false,
            };
        }
        /// <summary>
        /// Validates whether this move mod exists in data
        /// </summary>
        /// <param name="mod">Type of mod</param>
        /// <param name="name">Name of the element to verify</param>
        /// <returns>True if element exists in this data</returns>
        public static bool ValidateMoveModExistance(MoveModifier mod, string name)
        {
            return mod switch
            {
                MoveModifier.MOVE_BP_MOD or MoveModifier.MOVE_ACC_MOD => float.TryParse(name, out _),
                MoveModifier.MOVE_TYPE_MOD => Enum.TryParse<PokemonType>(name, true, out _),
                _ => false,
            };
        }
    }
}
