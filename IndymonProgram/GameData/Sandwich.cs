namespace GameData
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
        public string Name = "";
        public int Level = 0;
        public int Duration = 0;
        public SandwichEffectType Effect = SandwichEffectType.NONE;
        public override string ToString()
        {
            return Name;
        }
        // Parser
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
        public static bool TryParse(string sandwichName, out Sandwich sandwich)
        {
            bool success;
            sandwich = null;
            try
            {
                sandwich = Parse(sandwichName);
                success = true;
            }
            catch
            {
                success = false;
            }
            return success;
        }
    }
}
