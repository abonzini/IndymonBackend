namespace Gameplay.GameplayElements
{
    public class KeyItem : GameplayElement
    {
        public PokemonType CramType = PokemonType.NONE;
        /// <summary>
        /// Full constructor as this is the only one that is fully defined by the spreadsheet as they have no other effects
        /// </summary>
        /// <param name="name">Name of item</param>
        /// <param name="cramType">Type</param>
        /// <param name="description">Item description</param>
        public KeyItem(string name, PokemonType cramType, string description)
        {
            Name = name;
            CramType = cramType;
            _description = description;
        }
    }
}
