using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Calculates the offensive type coverage given an attacking type into many defensive multitypes
        /// </summary>
        /// <param name="attackingType">Type of attacking move</param>
        /// <param name="defenderTypes">All the types that defend</param>
        /// <param name="ignoresImmunity">If a move hits immunity, is this ignored?</param>
        /// <param name="doubleNotEffectiveDamage">If hits a not very effective, is the result doubled?</param>
        /// <param name="seAgainstWater">Will the move hit water for double damage instead of typechart value?</param>
        /// <returns>The maximum dmaage multiplier for this type/defender combo</returns>
        static List<double> CalculateOffensiveTypeCoverage(PokemonType attackingType, List<(PokemonType, PokemonType)> defenderTypes, bool ignoresImmunity, bool doubleNotEffectiveDamage, bool seAgainstWater)
        {
            List<double> result = new List<double>();
            foreach ((PokemonType, PokemonType) defenderType in defenderTypes)
            {
                static double damageFromType(PokemonType attackingType, PokemonType defendingType, bool ignoresImmunity, bool seAgainstWater)
                {
                    if (attackingType == PokemonType.NONE) return 1; // Typeless moves just hit
                    if (seAgainstWater && defendingType == PokemonType.WATER) return 2; // Skip the whole damage calc idc
                    double result = MechanicsDataContainers.GlobalMechanicsData.DefensiveTypeChart[defendingType][attackingType];
                    if (ignoresImmunity && result == 0) result = 1;
                    return result;
                }
                double damageT1 = 1, damageT2 = 1;
                if (defenderType.Item1 != PokemonType.NONE)
                {
                    damageT1 = damageFromType(attackingType, defenderType.Item1, ignoresImmunity, seAgainstWater);
                }
                if (defenderType.Item2 != PokemonType.NONE)
                {
                    damageT2 = damageFromType(attackingType, defenderType.Item2, ignoresImmunity, seAgainstWater);
                }
                double resultingMultiplier = damageT1 * damageT2;
                if (doubleNotEffectiveDamage && resultingMultiplier < 1) resultingMultiplier *= 2; // Tinted lens doubles not very effective dmg
                result.Add(resultingMultiplier);
            }
            return result;
        }
        /// <summary>
        /// For a lot of attackers, assume you're being attacked stab for all types, assuming no abilities. Gets you a list of all damage modifiers received, defender may have abilities too
        /// </summary>
        /// <param name="defendingType">Type of defending mon</param>
        /// <param name="attackingTypes">Attacking types of attacking mons, will try all stabs</param>
        /// <param name="ModifiedTypeEffectiveness">All the mods that would affect defending type effectiveness</param>
        /// <returns>A list with all stab damage multipliers</returns>
        static List<double> CalculateDefensiveTypeStabCoverage((PokemonType, PokemonType) defendingType, List<(PokemonType, PokemonType)> attackingTypes, HashSet<(StatModifier, string)> ModifiedTypeEffectiveness)
        {
            List<double> result = new List<double>();
            foreach ((PokemonType, PokemonType) attackingType in attackingTypes)
            {
                // Consider this as 2 separate attacks now, first T1 and then T2. Get basic damage, multiply, then decide if nullify
                static double DefensiveTypeCheck(PokemonType attackingType, (PokemonType, PokemonType) defendingType, HashSet<(StatModifier, string)> ModifiedTypeEffectiveness)
                {
                    if (attackingType == PokemonType.NONE) return 1; // Typeless moves just hit
                    double damage = CalculateOffensiveTypeCoverage(attackingType, [defendingType], false, false, false)[0]; // Reuse the attackign formula, check how much this messes me up, don''t know enemy abilities so all false
                    // SE checks here before extra modifiers
                    if (damage > 1) // SE!
                    {
                        if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_SE_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                        foreach ((StatModifier, string) alterSeCase in ModifiedTypeEffectiveness.Where(m => m.Item1 == StatModifier.ALTER_RECV_SE_DAMAGE)) // Can be any number so I need to search by key
                        {
                            damage *= double.Parse(alterSeCase.Item2);
                        }
                    }
                    else // non SE!
                    {
                        foreach ((StatModifier, string) alterSeCase in ModifiedTypeEffectiveness.Where(m => m.Item1 == StatModifier.ALTER_RECV_NON_SE_DAMAGE)) // Can be any number so I need to search by key
                        {
                            damage *= double.Parse(alterSeCase.Item2);
                        }
                    }
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.NULLIFIES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.HALVES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 0.5;
                    if (ModifiedTypeEffectiveness.Contains((StatModifier.DOUBLES_RECV_DAMAGE_OF_TYPE, attackingType.ToString()))) damage *= 2;
                    return damage;
                }
                // Calculate and add for valid types, incorporate stab mult too
                if (attackingType.Item1 != PokemonType.NONE) result.Add(1.5 * DefensiveTypeCheck(attackingType.Item1, defendingType, ModifiedTypeEffectiveness));
                if (attackingType.Item2 != PokemonType.NONE) result.Add(1.5 * DefensiveTypeCheck(attackingType.Item2, defendingType, ModifiedTypeEffectiveness));
            }
            return result;
        }
    }
}
