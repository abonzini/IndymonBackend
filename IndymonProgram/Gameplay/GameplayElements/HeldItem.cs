using Utilities;

namespace Gameplay.GameplayElements
{
    public class HeldItem : SimulationElement
    {
        // Properties
        public PokemonType CramType = PokemonType.NONE;

        // Massive list of all of them and their effects (less friendly than storing in Json but allow for complex behaviours)
        public static IEnumerable<HeldItem> GetAllHeldItems()
        {
            // Type-repeated items
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                string berryName = type switch
                {
                    PokemonType.NORMAL => "Chilan",
                    PokemonType.FIGHTING => "Chople",
                    PokemonType.FLYING => "Coba",
                    PokemonType.POISON => "Kebia",
                    PokemonType.GROUND => "Shuca",
                    PokemonType.ROCK => "Charti",
                    PokemonType.BUG => "Tanga",
                    PokemonType.GHOST => "Kasib",
                    PokemonType.STEEL => "Babiri",
                    PokemonType.FIRE => "Occa",
                    PokemonType.WATER => "Passho",
                    PokemonType.GRASS => "Rindo",
                    PokemonType.ELECTRIC => "Wacan",
                    PokemonType.PSYCHIC => "Payapa",
                    PokemonType.ICE => "Yache",
                    PokemonType.DRAGON => "Haban",
                    PokemonType.DARK => "Colbur",
                    PokemonType.FAIRY => "Roseli",
                    _ => throw new Exception("Invalid pokemon type")
                };
                yield return new HeldItem()
                {
                    Name = $"{berryName} Berry",
                    CramType = type,
                    _description = $"Halves the damage of the first {type}-type move that strikes this Pokemon in a battle."
                };
            }
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                string typeString = GeneralUtilities.ApaCapitalize(type.ToString());
                yield return new HeldItem()
                {
                    Name = $"{typeString} Gem",
                    CramType = type,
                    _description = $"The first {type}-type move used by this Pokemon in battle will have its power boosted by 50%."
                };
            }
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                if (type == PokemonType.NONE) continue;
                string itemName = type switch
                {
                    PokemonType.NORMAL => "Silk Scarf",
                    PokemonType.FIGHTING => "Black Belt",
                    PokemonType.FLYING => "Sharp Beak",
                    PokemonType.POISON => "Poison Barb",
                    PokemonType.GROUND => "Soft Sand",
                    PokemonType.ROCK => "Hard Stone",
                    PokemonType.BUG => "Silver Powder",
                    PokemonType.GHOST => "Spell Tag",
                    PokemonType.STEEL => "Metal Coat",
                    PokemonType.FIRE => "Charcoal",
                    PokemonType.WATER => "Mystic Water",
                    PokemonType.GRASS => "Miracle Seed",
                    PokemonType.ELECTRIC => "Magnet",
                    PokemonType.PSYCHIC => "Twisted Spoon",
                    PokemonType.ICE => "Never-Melt Ice",
                    PokemonType.DRAGON => "Dragon Fang",
                    PokemonType.DARK => "Black Glasses",
                    PokemonType.FAIRY => "Fairy Feather",
                    _ => throw new Exception("Invalid pokemon type")
                };
                yield return new HeldItem()
                {
                    Name = itemName,
                    CramType = type,
                    _description = $"Boosts the power of {type}-type moves by 20%."
                };
            }
            // All other items
            yield return new HeldItem()
            {
                Name = "Cell Battery",
                CramType = PokemonType.ELECTRIC,
                _description = $"Decide effect later."
            };
            yield return new HeldItem()
            {
                Name = "Black Sludge",
                CramType = PokemonType.POISON,
                _description = $"Decide effect later."
            };
            yield return new HeldItem()
            {
                Name = "Eject Button",
                CramType = PokemonType.STEEL,
                _description = $"Decide effect later."
            };
            yield return new HeldItem()
            {
                Name = "Eject Pack",
                CramType = PokemonType.STEEL,
                _description = $"Decide effect later."
            };
        }
    }
}
