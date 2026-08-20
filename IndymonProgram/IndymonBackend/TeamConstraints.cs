using Gameplay.GameplayElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IndymonBackendProgram
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConstraintOperation
    {
        OR,
        AND
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConstraintType
    {
        NONE,
        POKEMON,
        POKEMON_TYPE,
        POKEMON_HAS_PREVO,
        POKEMON_HAS_EVO
    }
    public class Constraint
    {
        public List<(ConstraintType, string)> AllConstraints { get; set; } = new List<(ConstraintType, string)>();
        public ConstraintOperation Operation { get; set; }
        /// <summary>
        /// Checks if this constraint is satisfied by a pokemon
        /// </summary>
        /// <param name="mon">The mon to check</param>
        /// <returns></returns>
        public bool IsSatisfiedByPokemon(PokemonSpecies mon)
        {
            if (AllConstraints.Count == 0) return true; // No constraints needed
            // Now check constraint
            foreach ((ConstraintType, string) constraintToCheck in AllConstraints)
            {
                ConstraintType constraintType = constraintToCheck.Item1;
                string constraintValue = constraintToCheck.Item2;
                bool checkPassed = false; // Will need to see if this check passes
                switch (constraintType)
                {
                    case ConstraintType.POKEMON:
                        checkPassed = mon.Name == constraintValue;
                        break;
                    case ConstraintType.POKEMON_TYPE:
                        {
                            PokemonType typeToCheck = Enum.Parse<PokemonType>(constraintValue);
                            checkPassed = (mon.Types.Item1 == typeToCheck || mon.Types.Item2 == typeToCheck);
                            break;
                        }
                    case ConstraintType.POKEMON_HAS_PREVO:
                        {
                            bool boolToCheck = bool.Parse(constraintValue);
                            checkPassed = (boolToCheck == (mon.Prevo != null));
                            break;
                        }
                    case ConstraintType.POKEMON_HAS_EVO:
                        {
                            bool boolToCheck = bool.Parse(constraintValue);
                            checkPassed = (boolToCheck == (mon.Evos.Count > 0));
                            break;
                        }
                    default:
                        throw new Exception($"Constraint element type {constraintType} not implemented");
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
