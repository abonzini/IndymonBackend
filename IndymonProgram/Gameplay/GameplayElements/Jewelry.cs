using Utilities;

namespace Gameplay.GameplayElements
{
    public class Jewelry : SimulationElement
    {
        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<Jewelry> GetAllJewelry()
        {
            // Amulet coin, while equipped, all IMP gains are doubled
            yield return new Jewelry()
            {
                Name = "Amulet Coin",
                _description = "All IMP gains are doubled as long as this is equipped."
            };
            // Also the basic pendants, boost stats by 10% of all X-type mons for each X-type mon in lineup
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                string typeString = GeneralUtilities.ApaCapitalize(type.ToString());
                yield return new Jewelry()
                {
                    Name = $"{GeneralUtilities.ApaCapitalize(type.ToString())} Pendant",
                    _description = $"All {typeString}-type Pokemon in the field gain a 10% boost to their stats for each {typeString}-type Pokemon in the field."
                };
            }
        }
    }
}
