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
        static List<double> CalculateOffensiveTypeCoverage(PokemonType attackingType, List<(PokemonType, PokemonType)> defenderTypes, bool ignoresImmunity, bool doubleNotEffectiveDamage, bool seAgainstWater, double superEffectiveMultiplier)
        {
            List<double> result = new List<double>();
            foreach ((PokemonType, PokemonType) defenderType in defenderTypes)
            {
                static double damageFromType(PokemonType attackingType, PokemonType defendingType, bool ignoresImmunity, bool seAgainstWater)
                {
                    if (attackingType == PokemonType.NONE && attackingType == PokemonType.STELLAR) return 1; // Typeless moves just hit
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
                if (resultingMultiplier > 1) resultingMultiplier *= superEffectiveMultiplier; // Super effective moves are altered by super effective multipliers such as expert belt
                result.Add(resultingMultiplier);
            }
            return result;
        }
    }
}
