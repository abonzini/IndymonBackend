using GameData;
using MechanicsData;
using MechanicsDataContainer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomatedTeamBuilder
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConstraintOperation
    {
        OR,
        AND
    }
    public class Constraint
    {
        public List<(ElementType, string)> AllConstraints { get; set; } = new List<(ElementType, string)>();
        public ConstraintOperation Operation { get; set; }
        /// <summary>
        /// Checks if this constraint could be potentially satisfied by a pokemon
        /// </summary>
        /// <param name="mon">The mon to check</param>
        /// <param name="potentialConstraintVerification">To see if the mon could potentially fill this constraint</param>
        /// <returns></returns>
        public bool SatisfiedByMon(TrainerPokemon mon, bool potentialConstraintVerification)
        {
            if (mon.PokeBall == "Heavy Ball") return false; // Heavy ball mon never ever allowed
            if (AllConstraints.Count == 0) return true; // No constraints needed
            Pokemon pokemonData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species]; // Obtain mon data
            // Obtain moveset and ability of mon
            List<Ability> monAbilities;
            if (mon.SetItem != null && mon.SetItem.AddedAbility != null)
            {
                monAbilities = [mon.SetItem.AddedAbility];
            }
            else if (potentialConstraintVerification)
            {
                monAbilities = pokemonData.Abilities;
            }
            else if (mon.ChosenAbility != null)
            {
                monAbilities = [mon.ChosenAbility];
            }
            else
            {
                monAbilities = [];
            }
            List<Move> monMoves;
            if (potentialConstraintVerification) // This would imply the set moves plus some of the possible mon moves
            {
                if (mon.SetItem != null && mon.SetItem.AddedMoves.Count > 0)
                {
                    monMoves = [.. mon.SetItem.AddedMoves];
                }
                else
                {
                    monMoves = [];
                }
                if (monMoves.Count < 4) // If set item didn't completely fill the moveset, then other moves would be able to fit
                {
                    monMoves.AddRange(pokemonData.Moveset);
                }
            }
            else
            {
                // In this case, I believe the moveset would already contain the moves (but remove hard switch from this)
                monMoves = [.. mon.ChosenMoveset.Where(m => m != null)];
            }
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                // Elements that may be of use when checking stuff
                _ = Enum.TryParse(elementName, true, out PokemonType typeToCheck);
                _ = Enum.TryParse(elementName, true, out ItemFlag itemFlagToCheck);
                _ = Enum.TryParse(elementName, true, out EffectFlag effectFlagToCheck);
                _ = Enum.TryParse(elementName, true, out MoveCategory moveCategoryToCheck);
                _ = bool.TryParse(elementName.ToLower(), out bool boolToCheck);
                switch (elementType)
                {
                    case ElementType.POKEMON:
                        checkPassed = mon.Species == elementName;
                        break;
                    case ElementType.POKEMON_TYPE:
                        checkPassed = (pokemonData.Types.Item1 == typeToCheck || pokemonData.Types.Item2 == typeToCheck);
                        break;
                    case ElementType.POKEMON_HAS_PREVO:
                        checkPassed = (boolToCheck == (pokemonData.Prevo != null));
                        break;
                    case ElementType.POKEMON_HAS_EVO:
                        checkPassed = (boolToCheck == (pokemonData.Evos.Count > 0));
                        break;
                    case ElementType.BATTLE_ITEM:
                        checkPassed = mon.BattleItem?.Name == elementName;
                        break;
                    case ElementType.ITEM_FLAGS:
                        HashSet<ItemFlag> itemFlags = new HashSet<ItemFlag>();
                        if (mon.ModItem != null) itemFlags.UnionWith(mon.ModItem.Flags);
                        if (mon.BattleItem != null) itemFlags.UnionWith(mon.BattleItem.Flags);
                        checkPassed = itemFlags.Contains(itemFlagToCheck);
                        break;
                    case ElementType.MOD_ITEM:
                        checkPassed = mon.ModItem?.Name == elementName;
                        break;
                    case ElementType.ABILITY: // Verify mon has ability in set item or would have ability
                        checkPassed |= monAbilities.Any(a => a?.Name == elementName);
                        break;
                    case ElementType.MOVE:
                        checkPassed |= monMoves.Any(m => m?.Name == elementName);
                        break;
                    case ElementType.EFFECT_FLAGS:
                        checkPassed |= monMoves.Any(m => m.Flags.Contains(effectFlagToCheck)) || monAbilities.Any(a => a.Flags.Contains(effectFlagToCheck));
                        break;
                    case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    case ElementType.DAMAGING_MOVE_OF_TYPE:
                        checkPassed |= monMoves.Any(m => m.Category != MoveCategory.STATUS && m.Type == typeToCheck);
                        break;
                    case ElementType.MOVE_CATEGORY:
                        checkPassed |= monMoves.Any(m => m.Category == moveCategoryToCheck);
                        break;
                    case ElementType.ANY_DAMAGING_MOVE:
                        checkPassed |= monMoves.Any(m => m.Category != MoveCategory.STATUS);
                        break;
                    case ElementType.ANY_MOVE:
                        checkPassed |= monMoves.Count > 0;
                        break;
                    case ElementType.POKEBALL:
                        checkPassed |= mon.PokeBall == elementName;
                        break;
                    case ElementType.ARCHETYPE:
                    case ElementType.WEATHER:
                    case ElementType.TERRAIN:
                        checkPassed = false; // Mon can't guarantee this
                        break;
                    default:
                        throw new Exception($"Constraint element type {elementType} not implemented");
                }
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        /// <summary>
        /// Tells me if this set item would verify constraints
        /// </summary>
        /// <param name="item">Which item</param>
        /// <returns>Whether it satisfies or not</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool SatisfiedBySetItem(SetItem item)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                // Elements that may be of use when checking stuff
                Enum.TryParse(elementName, true, out PokemonType typeToCheck);
                Enum.TryParse(elementName, true, out EffectFlag effectFlagToCheck);
                Enum.TryParse(elementName, true, out MoveCategory moveCategoryToCheck);
                switch (elementType)
                {
                    case ElementType.ABILITY: // Verify mon has ability in set item or would have ability
                        checkPassed |= item.AddedAbility?.Name == elementName;
                        break;
                    case ElementType.MOVE:
                        checkPassed |= item.AddedMoves.Any(m => m.Name == elementName);
                        break;
                    case ElementType.EFFECT_FLAGS:
                        // This is horrible but works because if an item doesnt have moves it should have ability
                        checkPassed |= item.AddedMoves.Any(m => m.Flags.Contains(effectFlagToCheck)) || item.AddedAbility.Flags.Contains(effectFlagToCheck);
                        break;
                    case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    case ElementType.DAMAGING_MOVE_OF_TYPE:
                        checkPassed |= item.AddedMoves.Any(m => m.Category != MoveCategory.STATUS && m.Type == typeToCheck);
                        break;
                    case ElementType.MOVE_CATEGORY:
                        checkPassed |= item.AddedMoves.Any(m => m.Category == moveCategoryToCheck);
                        break;
                    case ElementType.ANY_DAMAGING_MOVE:
                        checkPassed |= item.AddedMoves.Any(m => m.Category != MoveCategory.STATUS);
                        break;
                    case ElementType.POKEMON:
                    case ElementType.POKEMON_TYPE:
                    case ElementType.POKEMON_HAS_PREVO:
                    case ElementType.POKEMON_HAS_EVO:
                    case ElementType.BATTLE_ITEM:
                    case ElementType.ITEM_FLAGS:
                    case ElementType.MOD_ITEM:
                    case ElementType.ARCHETYPE:
                    case ElementType.POKEBALL:
                        checkPassed = false; // No pass
                        break;
                    default:
                        throw new Exception($"Constraint element type {elementType} not implemented");
                }
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        /// <summary>
        /// Checks if this constraint could be potentially satisfied by an item (mod or battle)
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns></returns>
        public bool SatisfiedByItem(Item item)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                // Elements that may be of use when checking stuff
                Enum.TryParse(elementName, true, out ItemFlag itemFlagToCheck);
                bool checkPassed = elementType switch
                {
                    ElementType.BATTLE_ITEM => MechanicsDataContainers.GlobalMechanicsData.BattleItems.ContainsKey(item.Name) && item.Name == elementName,
                    ElementType.ITEM_FLAGS => item.Flags.Contains(itemFlagToCheck),
                    ElementType.MOD_ITEM => MechanicsDataContainers.GlobalMechanicsData.ModItems.ContainsKey(item.Name) && item.Name == elementName,
                    _ => false,// No pass
                };
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        /// <summary>
        /// Tells me if this ability would verify constraints
        /// </summary>
        /// <param name="ability">Which item</param>
        /// <returns>Whether it satisfies or not</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool SatisfiedByAbility(Ability ability)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                // Elements that may be of use when checking stuff
                Enum.TryParse(elementName, true, out EffectFlag effectFlagToCheck);
                switch (elementType)
                {
                    case ElementType.ABILITY: // Verify mon has ability in set item or would have ability
                        checkPassed |= ability.Name == elementName;
                        break;
                    case ElementType.EFFECT_FLAGS:
                        // This is horrible but works because if an item doesnt have moves it should have ability
                        checkPassed |= ability.Flags.Contains(effectFlagToCheck);
                        break;
                    case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    case ElementType.DAMAGING_MOVE_OF_TYPE:
                    case ElementType.MOVE_CATEGORY:
                    case ElementType.ANY_DAMAGING_MOVE:
                    case ElementType.MOVE:
                    case ElementType.POKEMON:
                    case ElementType.POKEMON_TYPE:
                    case ElementType.POKEMON_HAS_PREVO:
                    case ElementType.POKEMON_HAS_EVO:
                    case ElementType.BATTLE_ITEM:
                    case ElementType.ITEM_FLAGS:
                    case ElementType.MOD_ITEM:
                    case ElementType.ARCHETYPE:
                    case ElementType.WEATHER:
                    case ElementType.TERRAIN:
                    case ElementType.POKEBALL:
                        checkPassed = false; // No pass
                        break;
                    default:
                        throw new Exception($"Constraint element type {elementType} not implemented");
                }
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        /// <summary>
        /// Tells me if this move would verify constraints
        /// </summary>
        /// <param name="move">Which move</param>
        /// <returns>Whether it satisfies or not</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool SatisfiedByMove(Move move)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                // Elements that may be of use when checking stuff
                Enum.TryParse(elementName, true, out PokemonType typeToCheck);
                Enum.TryParse(elementName, true, out EffectFlag effectFlagToCheck);
                Enum.TryParse(elementName, true, out MoveCategory moveCategoryToCheck);
                switch (elementType)
                {
                    case ElementType.MOVE:
                        checkPassed |= move.Name == elementName;
                        break;
                    case ElementType.EFFECT_FLAGS:
                        // This is horrible but works because if an item doesnt have moves it should have ability
                        checkPassed |= move.Flags.Contains(effectFlagToCheck);
                        break;
                    case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    case ElementType.DAMAGING_MOVE_OF_TYPE:
                        checkPassed |= move.Category != MoveCategory.STATUS && move.Type == typeToCheck;
                        break;
                    case ElementType.MOVE_CATEGORY:
                        checkPassed |= move.Category == moveCategoryToCheck;
                        break;
                    case ElementType.ANY_DAMAGING_MOVE:
                        checkPassed |= move.Category != MoveCategory.STATUS;
                        break;
                    case ElementType.ABILITY: // Verify mon has ability in set item or would have ability
                    case ElementType.POKEMON:
                    case ElementType.POKEMON_TYPE:
                    case ElementType.POKEMON_HAS_PREVO:
                    case ElementType.POKEMON_HAS_EVO:
                    case ElementType.BATTLE_ITEM:
                    case ElementType.ITEM_FLAGS:
                    case ElementType.MOD_ITEM:
                    case ElementType.ARCHETYPE:
                    case ElementType.WEATHER:
                    case ElementType.TERRAIN:
                        checkPassed = false; // No pass
                        break;
                    default:
                        throw new Exception($"Constraint element type {elementType} not implemented");
                }
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        /// <summary>
        /// Verifies whether constraint is satisfied by the context of this mon set
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <returns>Whether constraint satisfied or not</returns>
        public bool SatisfiedByContext(PokemonBuildContext ctx)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ElementType, string) elementToCheck in AllConstraints)
            {
                ElementType elementType = elementToCheck.Item1;
                string elementName = elementToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                // Elements that may be of use when checking stuff
                Enum.TryParse(elementName, true, out TeamArchetype archetypeToCheck);
                Enum.TryParse(elementName, true, out Weather weatherToCheck);
                Enum.TryParse(elementName, true, out Terrain terrainToCheck);
                switch (elementType)
                {
                    case ElementType.WEATHER:
                        checkPassed |= ctx.CurrentWeather == weatherToCheck;
                        break;
                    case ElementType.TERRAIN:
                        checkPassed |= ctx.CurrentTerrain == terrainToCheck;
                        break;
                    case ElementType.ARCHETYPE:
                        checkPassed |= ctx.AdditionalArchetypes.Contains(archetypeToCheck);
                        break;
                    case ElementType.MOVE:
                    case ElementType.EFFECT_FLAGS:
                    case ElementType.ORIGINAL_TYPE_OF_MOVE:
                    case ElementType.DAMAGING_MOVE_OF_TYPE:
                    case ElementType.MOVE_CATEGORY:
                    case ElementType.ANY_DAMAGING_MOVE:
                    case ElementType.ABILITY: // Verify mon has ability in set item or would have ability
                    case ElementType.POKEMON:
                    case ElementType.POKEMON_TYPE:
                    case ElementType.POKEMON_HAS_PREVO:
                    case ElementType.POKEMON_HAS_EVO:
                    case ElementType.BATTLE_ITEM:
                    case ElementType.ITEM_FLAGS:
                    case ElementType.MOD_ITEM:
                    case ElementType.POKEBALL:
                        checkPassed = false; // No pass
                        break;
                    default:
                        throw new Exception($"Constraint element type {elementType} not implemented");
                }
                if (Operation == ConstraintOperation.OR && checkPassed)
                {
                    return true; // A single check passing will be fine
                }
                if (Operation == ConstraintOperation.AND && !checkPassed)
                {
                    return false; // A single check passing will be over
                }
            }
            if (Operation == ConstraintOperation.OR)
            {
                return false; // Failed because not a single constraint passed
            }
            if (Operation == ConstraintOperation.AND)
            {
                return true; // Passed because not a single constraint failed
            }
            throw new NotImplementedException("Unreachable Code");
        }
        public override string ToString()
        {
            return string.Join(",", AllConstraints);
        }
    }
}
