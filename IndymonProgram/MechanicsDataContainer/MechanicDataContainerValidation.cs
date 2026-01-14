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
                ElementType.ABILITY => Abilities.ContainsKey(name),
                ElementType.MOVE => Moves.ContainsKey(name),
                ElementType.EFFECT_FLAGS => Enum.TryParse<EffectFlag>(name, true, out _),
                ElementType.MOVE_TYPE => Enum.TryParse<PokemonType>(name, true, out _),
                ElementType.MOVE_CATEGORY => Enum.TryParse<MoveCategory>(name, true, out _),
                ElementType.ALL_MOVES => true,
                _ => false,
            };
        }
    }
}
