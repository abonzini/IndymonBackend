namespace MechanicsData
{
    public enum SandwichEffectType
    {
        NONE,
        ENEMY_NUMBER,
        ITEM_DROP,
        SHINY_CHANCE,
        POST_HEALING,
        LEVEL
    }
    public class Sandwich
    {
        // Const
        const string ENEMY_NUMBER_FLAVOUR = "Sweet";
        const string ITEM_DROP_FLAVOUR = "Sour";
        const string SHINY_CHANCE_FLAVOUR = "Salty";
        const string POST_HEALING_FLAVOUR = "Bitter";
        const string LEVEL_FLAVOUR = "Spicy";
        // Data
        public string Name { get; set; } = "";
        public int Level = 0;
        public int Duration = 0;
        public SandwichEffectType Effect = SandwichEffectType.NONE;
        public PokemonType CramType = PokemonType.NONE;
        public string GetDescription()
        {
            string result = "";
            switch (Effect)
            {
                case SandwichEffectType.ENEMY_NUMBER:
                    result += "Increases the number of wild Pokemon in battles (and therefore the chance of getting drop items).";
                    break;
                case SandwichEffectType.POST_HEALING:
                    result += "Heals your Pokemon at the end of every battle.";
                    break;
                case SandwichEffectType.ITEM_DROP:
                    result += "When you get items after a battle, you get more.";
                    break;
                case SandwichEffectType.LEVEL:
                    result += "Increases the level (and therefore stats) of your Pokemon during battles.";
                    break;
                case SandwichEffectType.SHINY_CHANCE:
                    result += "Increases the chance of finding Shiny Pokemon.";
                    break;
                case SandwichEffectType.NONE:
                default:
                    return "";
            }
            if (Level > 1)
            {
                result += $" Effect is x{Level} times stronger.";
            }
            result += $" Lasts for {Duration} turns.";
            return result;
        }
        // Parser
        /// <summary>
        /// Parses a sandwich by its name
        /// </summary>
        /// <param name="sandwichName">Name of sandwich to parse (contains all the data)</param>
        /// <returns>The Parsed sandwich</returns>
        public static Sandwich Parse(string sandwichName)
        {
            Sandwich resultingSandwich = new Sandwich
            {
                Name = sandwichName
            };
            string[] nameParts = sandwichName.Split(' ');
            if (nameParts.Length != 4) throw new Exception("Sandwich name doesn't contain 4 words");
            if (nameParts[3] != "Sandwich") throw new Exception("4th word isn't sandwich!");
            // Checks boost type
            resultingSandwich.Effect = nameParts[0] switch
            {
                ENEMY_NUMBER_FLAVOUR => SandwichEffectType.ENEMY_NUMBER,
                ITEM_DROP_FLAVOUR => SandwichEffectType.ITEM_DROP,
                SHINY_CHANCE_FLAVOUR => SandwichEffectType.SHINY_CHANCE,
                POST_HEALING_FLAVOUR => SandwichEffectType.POST_HEALING,
                LEVEL_FLAVOUR => SandwichEffectType.LEVEL,
                _ => throw new Exception($"Sandwich flavour {nameParts[0]} not implemented"),
            };
            resultingSandwich.CramType = nameParts[0] switch
            {
                ENEMY_NUMBER_FLAVOUR => PokemonType.BUG,
                ITEM_DROP_FLAVOUR => PokemonType.GRASS,
                SHINY_CHANCE_FLAVOUR => PokemonType.ROCK,
                POST_HEALING_FLAVOUR => PokemonType.DARK,
                LEVEL_FLAVOUR => PokemonType.FIRE,
                _ => throw new Exception($"Sandwich flavour {nameParts[0]} not implemented"),
            };
            // Check level (roman numeral calculator lmao)
            resultingSandwich.Level = 0;
            char prevLetter = ' ';
            foreach (char letter in nameParts[2])
            {
                resultingSandwich.Level += letter switch
                {
                    'I' => 1,
                    'V' => 5,
                    'X' => 10,
                    _ => throw new Exception($"Unrecognised roman numeral {letter}")
                };
                if (letter != prevLetter && prevLetter == 'I') // In the case I'm subtracting ones
                {
                    resultingSandwich.Level -= 2; // Remove the I and the number from the current letter, e.g. IX is 9 not 11
                }
            }
            // Finally, check duration/chaos
            resultingSandwich.Duration = nameParts[2] switch
            {
                "Single" => 1,
                "Double" => 2,
                "Triple" => 3,
                "Quadruple" => 4,
                "Quintuple" => 5,
                _ => throw new Exception($"Unrecognized duration {nameParts[2]}")
            };
            // Sandwich finished parsing
            return resultingSandwich;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
