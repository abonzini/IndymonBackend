using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Checks whether a mon fills property or not
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="elementToCheck">What property to check for</param>
        /// <param name="elementToCheckName">Name of property to look for</param>
        /// <returns></returns>
        static bool ValidateMonProperty(TrainerPokemon mon, ElementType elementToCheck, string elementToCheckName)
        {
            Pokemon pokemonData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species]; // Obtain mon data
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out ItemFlag battleItemFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            return elementToCheck switch // Some won't apply
            {
                ElementType.POKEMON => pokemonData.Name == elementToCheckName,
                ElementType.POKEMON_TYPE => (pokemonData.Types.Item1 == typeToCheck || pokemonData.Types.Item2 == typeToCheck),
                ElementType.POKEMON_HAS_EVO => pokemonData.Evos.Count > 0,
                ElementType.BATTLE_ITEM => mon.BattleItem?.Name == elementToCheckName,
                ElementType.ITEM_FLAGS => mon.BattleItem?.Flags.Contains(battleItemFlagToCheck) == true,
                ElementType.MOD_ITEM => mon.ModItem?.Name == elementToCheckName,
                ElementType.ABILITY => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Name == elementToCheckName), // If has ability or set item adds it
                ElementType.MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Name == elementToCheckName), // If has move or set item adds it
                // Complex one because both moves and abilities may have it!
                ElementType.EFFECT_FLAGS => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Flags.Contains(effectFlagToCheck) == true) ||
                    pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Flags.Contains(effectFlagToCheck) == true),
                ElementType.DAMAGING_MOVE_OF_TYPE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category != MoveCategory.STATUS && m?.Type == typeToCheck),
                ElementType.MOVE_CATEGORY => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category == moveCategoryToCheck),
                ElementType.ANY_DAMAGING_MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category != MoveCategory.STATUS),
                _ => false,
            };
        }
        /// <summary>
        /// Checks whether ability fulfills property or not
        /// </summary>
        /// <param name="ability">Ability to check</param>
        /// <param name="elementToCheck">Element to check</param>
        /// <param name="elementToCheckName">Name of element to check</param>
        /// <returns>True if the ability satisfies property</returns>
        static bool ValidateAbilityProperty(Ability ability, ElementType elementToCheck, string elementToCheckName)
        {
            if (ability == null) return false;
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            return elementToCheck switch
            {
                ElementType.ABILITY => ability.Name == elementToCheckName,
                ElementType.EFFECT_FLAGS => ability.Flags.Contains(effectFlagToCheck) == true,
                _ => false,
            };
        }
        /// <summary>
        /// Checks whether move fulfills property or not
        /// </summary>
        /// <param name="move">Move to check</param>
        /// <param name="elementToCheck">Element to check</param>
        /// <param name="elementToCheckName">Name of element to check</param>
        /// <returns>True if the ability satisfies property</returns>
        static bool ValidateMoveProperty(Move move, ElementType elementToCheck, string elementToCheckName)
        {
            if (move == null) return false;
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            return elementToCheck switch // Some won't apply
            {
                ElementType.MOVE => move.Name == elementToCheckName,
                ElementType.EFFECT_FLAGS => move.Flags.Contains(effectFlagToCheck),
                ElementType.DAMAGING_MOVE_OF_TYPE => move.Category != MoveCategory.STATUS && move.Type == typeToCheck,
                ElementType.MOVE_CATEGORY => move.Category == moveCategoryToCheck,
                ElementType.ANY_DAMAGING_MOVE => move.Category != MoveCategory.STATUS,
                _ => false,
            };
        }
    }
}
