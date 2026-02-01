using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    /// <summary>
    /// Defines a set of constraints that need to be in a team (or in an ongoing mon set!) Form is a list of lists, the first list is AND and the second a series of OR
    /// E.g. (A+B)*(C+D+E)*F
    /// </summary>
    public class TeamBuildConstraints
    {
        /// Options that could generate a valid team. Many mandatory conditions of optional combos ((A+B)*(C+D+E)*(F))
        public List<List<(ElementType, string)>> AllConstraints = new List<List<(ElementType, string)>>();
        /// <summary>
        /// Adds all monotype constraint options (e.g. a team of one type, each with a possible solution
        /// </summary>
        public TeamBuildConstraints Clone()
        {
            TeamBuildConstraints clone = new TeamBuildConstraints
            {
                AllConstraints = [.. AllConstraints] // Just shallows copies the constraint list, OR constraints are not modified anyway
            };
            return clone;
        }
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Checks whether a mon fills property or not
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="elementToCheck">What property to check for</param>
        /// <param name="elementToCheckName">Name of property to look for</param>
        /// <returns></returns>
        static bool ValidateBasicMonProperty(TrainerPokemon mon, ElementType elementToCheck, string elementToCheckName)
        {
            Pokemon pokemonData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species]; // Obtain mon data
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out ItemFlag itemFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            return elementToCheck switch // Some won't apply
            {
                ElementType.POKEMON => pokemonData.Name == elementToCheckName,
                ElementType.POKEMON_TYPE => (pokemonData.Types.Item1 == typeToCheck || pokemonData.Types.Item2 == typeToCheck),
                ElementType.POKEMON_HAS_EVO => pokemonData.Evos.Count > 0,
                ElementType.BATTLE_ITEM => mon.BattleItem?.Name == elementToCheckName,
                ElementType.ITEM_FLAGS => mon.BattleItem?.Flags.Contains(itemFlagToCheck) == true,
                ElementType.MOD_ITEM => mon.ModItem?.Name == elementToCheckName,
                ElementType.ABILITY => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Name == elementToCheckName), // If has ability or set item adds it
                ElementType.MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Name == elementToCheckName), // If has move or set item adds it
                // Complex one because both moves and abilities may have it!
                ElementType.EFFECT_FLAGS => pokemonData.Abilities.Append(GetSetItemAbility(mon.SetItem)).Any(a => a?.Flags.Contains(effectFlagToCheck) == true) ||
                    pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Flags.Contains(effectFlagToCheck) == true),
                ElementType.DAMAGING_MOVE_OF_TYPE or ElementType.ORIGINAL_TYPE_OF_MOVE => pokemonData.Moveset.Append(GetSetItemMove(mon.SetItem)).Any(m => m?.Category != MoveCategory.STATUS && m?.Type == typeToCheck),
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
        static bool ValidateBasicAbilityProperty(Ability ability, ElementType elementToCheck, string elementToCheckName)
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
        static bool ValidateBasicMoveProperty(Move move, ElementType elementToCheck, string elementToCheckName)
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
                ElementType.DAMAGING_MOVE_OF_TYPE or ElementType.ORIGINAL_TYPE_OF_MOVE => move.Category != MoveCategory.STATUS && move.Type == typeToCheck,
                ElementType.MOVE_CATEGORY => move.Category == moveCategoryToCheck,
                ElementType.ANY_DAMAGING_MOVE => move.Category != MoveCategory.STATUS,
                _ => false,
            };
        }
        /// <summary>
        /// Constrant validation for a mon, but a complex one, assuming mods and stuff
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="monCtx">COntext that adds stuff to the elements</param>
        /// <param name="elementToCheck">Which element we look for</param>
        /// <param name="elementToCheckName">Name of what we look for</param>
        /// <returns></returns>
        static bool ValidateComplexMonProperty(TrainerPokemon mon, PokemonBuildInfo monCtx, ElementType elementToCheck, string elementToCheckName)
        {
            // Elements that may be of use when checking stuff
            Enum.TryParse(elementToCheckName, true, out PokemonType typeToCheck);
            Enum.TryParse(elementToCheckName, true, out TeamArchetype archetypeToCheck);
            Enum.TryParse(elementToCheckName, true, out ItemFlag itemFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out EffectFlag effectFlagToCheck);
            Enum.TryParse(elementToCheckName, true, out MoveCategory moveCategoryToCheck);
            bool anyMoveFulfillsCheck = false; // Move checking is wild because they're heavily modded so this bool is a quick solution to checking "if any move"
            switch (elementToCheck) // Validates but now we know all mods of the mon so can be finer checks
            {
                case ElementType.POKEMON:
                    return mon.Species == elementToCheckName;
                case ElementType.POKEMON_TYPE:
                    return (monCtx.PokemonTypes.Item1 == typeToCheck || monCtx.PokemonTypes.Item2 == typeToCheck);
                case ElementType.POKEMON_HAS_EVO:
                    return MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species].Evos.Count > 0;
                case ElementType.ARCHETYPE:
                    return monCtx.AdditionalArchetypes.Contains(archetypeToCheck);
                case ElementType.BATTLE_ITEM:
                    return mon.BattleItem?.Name == elementToCheckName;
                case ElementType.ITEM_FLAGS:
                    return (mon.BattleItem != null && mon.BattleItem.Flags.Contains(itemFlagToCheck)) || (mon.ModItem != null && mon.ModItem.Flags.Contains(itemFlagToCheck));
                case ElementType.MOD_ITEM:
                    return mon.ModItem?.Name == elementToCheckName;
                case ElementType.ABILITY:
                    return mon.ChosenAbility?.Name == elementToCheckName;
                case ElementType.MOVE:
                    return mon.ChosenMoveset.Any(m => m.Name == elementToCheckName);
                case ElementType.EFFECT_FLAGS: // This one is a bit harder because it can be ability or move, but if it's move it can be added later
                    foreach (Move move in mon.ChosenMoveset) // Check if the moves have flags
                    {
                        anyMoveFulfillsCheck |= ExtractMoveFlags(move, monCtx).Contains(effectFlagToCheck);
                        if (anyMoveFulfillsCheck) break;
                    }
                    return anyMoveFulfillsCheck || (mon.ChosenAbility != null && mon.ChosenAbility.Flags.Contains(effectFlagToCheck));
                case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    return mon.ChosenMoveset.Any(m => m.Type == typeToCheck);
                case ElementType.DAMAGING_MOVE_OF_TYPE:
                    foreach (Move move in mon.ChosenMoveset) // Check if the moves have flags
                    {
                        anyMoveFulfillsCheck |= (move.Category != MoveCategory.STATUS) && (GetModifiedMoveType(move, monCtx) == typeToCheck);
                        if (anyMoveFulfillsCheck) break;
                    }
                    return anyMoveFulfillsCheck;
                case ElementType.MOVE_CATEGORY:
                    return mon.ChosenMoveset.Any(m => m.Category == moveCategoryToCheck);
                case ElementType.ANY_DAMAGING_MOVE:
                    return mon.ChosenMoveset.Any(m => m.Category != MoveCategory.STATUS);
                default:
                    return false;
            }
        }
    }
}
