using MechanicsData;
using Utilities;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        /// <summary>
        /// Parses the type chart
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseTypeChart(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Typechart");
            DefensiveTypeChart.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            List<PokemonType> columnTags = new List<PokemonType>();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                if (i == 0) // First line, need to add columns in order
                {
                    foreach (string field in fields)
                    {
                        if (Enum.TryParse<PokemonType>(field.Trim().ToUpper(), out PokemonType type))
                        {
                            columnTags.Add(type);
                        }
                        else
                        {
                            columnTags.Add(PokemonType.NONE);
                        }
                    }
                }
                else
                {
                    PokemonType nextType = Enum.Parse<PokemonType>(fields[0].Trim().ToUpper()); // First one is the type (try)
                    DefensiveTypeChart.Add(nextType, new Dictionary<PokemonType, double>()); // Add this type
                    for (int j = 1; j < fields.Length; j++)
                    {
                        PokemonType whatType = columnTags[j];
                        double multiplier = double.Parse(fields[j].Trim());
                        DefensiveTypeChart[nextType].Add(whatType, multiplier); // Add the multiplier
                    }
                }
            }
        }
        /// <summary>
        /// Parses the move data found in json files
        /// </summary>
        /// <param name="folder">Path to base folder for all jsons</param>
        void ParseMoves(string folder)
        {
            Console.WriteLine("Parsing Moves");
            Moves.Clear();
            // Parse all json files
            foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
            {
                // Will find the jsons, deserialize here, and then add to dictionary
                //Moves.Add(...);
            }
        }
        /// <summary>
        /// Parses the ability data found in json files
        /// </summary>
        /// <param name="folder">Path to base folder for all jsons</param>
        void ParseAbilities(string folder)
        {
            Console.WriteLine("Parsing Moves");
            Abilities.Clear();
            // Parse all json files
            foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
            {
                // Will find the jsons, deserialize here, and then add to dictionary
                //Abilities.Add(...);
            }
        }
        /// <summary>
        /// Parses all pokemon related info. This requires move data to be there already as it'll validate them
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="dexSheetTab">Which tab has the data</param>
        /// <param name="learnsetSheetTab">Which tab has learnsets</param>
        void ParsePokemonData(string sheetId, string dexSheetTab, string learnsetSheetTab)
        {
            Console.WriteLine("Parsing Pokedex");
            Dex.Clear();
            // Parse csv
            string pokemonCsv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, dexSheetTab);
            string learnsetCsv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, learnsetSheetTab);
            string[] pokemonLines = pokemonCsv.Split('\n');
            // First pass, add all pokemon into the dictionary with an empty Pokemon, this is to first know the existance of all mons
            for (int i = 1; i < pokemonLines.Length; i++)
            {
                int indexUntilComma = pokemonLines[i].IndexOf(',');
                string pokemonName = pokemonLines[i][..indexUntilComma]; // Get only mon name
                Dex.Add(pokemonName, new Pokemon()); // Start with a default mon, just to add to list
            }
            // Second pass, parse the actual mon data
            for (int i = 1; i < pokemonLines.Length; i++)
            {
                string[] fields = pokemonLines[i].Split(','); // Csv
                const int NAME_FIELD = 0;
                const int TYPE_1_FIELD = 1;
                const int TYPE_2_FIELD = 2;
                const int HP_FIELD = 3;
                const int ATK_FIELD = 4;
                const int DEF_FIELD = 5;
                const int SPATK_FIELD = 6;
                const int SPDEF_FIELD = 7;
                const int SPEED_FIELD = 8;
                const int WEIGHT_FIELD = 10;
                const int HEIGHT_FIELD = 11;
                const int ABILITY_1_FIELD = 12;
                const int ABILITY_2_FIELD = 13;
                const int ABILITY_3_FIELD = 14;
                const int PREEVO_FIELD = 15;
                const int ALTERNATE_OF_FIELD = 16;
                const int IMAGE_URL_FIELD = 17;
                string nextPokemonName = fields[NAME_FIELD];
                Pokemon thePokemon = Dex[nextPokemonName];
                thePokemon.Name = nextPokemonName;
                PokemonType theType = Enum.Parse<PokemonType>(fields[TYPE_1_FIELD].Trim().ToUpper());
                thePokemon.Types = (theType, thePokemon.Types.Item2);
                theType = Enum.Parse<PokemonType>(fields[TYPE_2_FIELD].Trim().ToUpper());
                thePokemon.Types = (thePokemon.Types.Item1, theType);
                // Stats
                double Hp = int.Parse(fields[HP_FIELD]);
                double Attack = int.Parse(fields[ATK_FIELD]);
                double Defense = int.Parse(fields[DEF_FIELD]);
                double SpecialAttack = int.Parse(fields[SPATK_FIELD]);
                double SpecialDefense = int.Parse(fields[SPDEF_FIELD]);
                double Speed = int.Parse(fields[SPEED_FIELD]);
                thePokemon.Stats = [Hp, Attack, Defense, SpecialAttack, SpecialDefense, Speed];
                thePokemon.Weight = double.Parse(fields[WEIGHT_FIELD]);
                thePokemon.Height = double.Parse(fields[HEIGHT_FIELD]);
                // Ability
                string theAbility = fields[ABILITY_1_FIELD].Trim();
                if (theAbility != "" && Abilities.TryGetValue(theAbility, out Ability nextValidAbility)) thePokemon.Abilities.Add(nextValidAbility);
                theAbility = fields[ABILITY_2_FIELD].Trim();
                if (theAbility != "" && Abilities.TryGetValue(theAbility, out nextValidAbility)) thePokemon.Abilities.Add(nextValidAbility);
                theAbility = fields[ABILITY_3_FIELD].Trim();
                if (theAbility != "" && Abilities.TryGetValue(theAbility, out nextValidAbility)) thePokemon.Abilities.Add(nextValidAbility);
                if (thePokemon.Name.ToLower().Contains("unown")) // Unown will also carryall other abilities
                {
                    char letter = (thePokemon.Name == "Unown") ? 'a' : thePokemon.Name.ToLower().Last();
                    thePokemon.Abilities.UnionWith([.. Abilities.Values.Where(a => a.Name.ToLower().StartsWith(letter))]); // Add moves filtering by unown letter
                }
                // Prevos
                string preevo = fields[PREEVO_FIELD].Trim();
                if (preevo != "") // Mon has prevo
                {
                    Pokemon thePreevo = Dex[preevo];
                    thePokemon.Prevo = thePreevo; // Add each other
                    thePreevo.Evos.Add(thePokemon);
                }
                // Alternate wild
                string alternateOf = fields[ALTERNATE_OF_FIELD].Trim();
                if (alternateOf != "") // Mon has prevo
                {
                    Pokemon theBaseMon = Dex[alternateOf];
                    thePokemon.AlternativeOf = theBaseMon; // Add each other
                    theBaseMon.WildAlternatives.Add(thePokemon);
                }
                // Image Url
                thePokemon.ImageUrl = fields[IMAGE_URL_FIELD].Trim();
            }
            // Next step, process the learnset
            string[] learnsetLines = learnsetCsv.Split('\n');
            Dictionary<string, Pokemon> pokemonToCalculateStill = Dex.ToDictionary(e => e.Key, e => e.Value);
            foreach (string learnsetLine in learnsetLines) // This one doesnt have labels
            {
                string[] fields = learnsetLine.Split(","); // Csv
                string pokemonName = fields[0]; // First field is the move
                Pokemon thePokemon = pokemonToCalculateStill[pokemonName]; // Retrieve from DB, it HAS to be there
                for (int i = 1; i < fields.Length; i++) // Then the moves
                {
                    string moveName = fields[i].Trim();
                    if (moveName == "") break; // Finished this mon's moveset
                    if (Moves.TryGetValue(moveName, out Move move))
                    {
                        if (moveName == "Sketch")
                        {
                            thePokemon.Moveset.Clear(); // Remove whatever was there before, i can learn all anyway
                            if (thePokemon.Name.ToLower().Contains("unown"))
                            {
                                char letter = (thePokemon.Name == "Unown") ? 'a' : thePokemon.Name.ToLower().Last();
                                thePokemon.Moveset.UnionWith([.. Moves.Values.Where(m => m.Name.ToLower().StartsWith(letter))]); // Add moves filtering by unown letter
                            }
                            else
                            {
                                thePokemon.Moveset.UnionWith([.. Moves.Values]); // Just add all
                            }
                            break; // Stop the rest because have all moves anyway lol
                        }
                        else
                        {
                            thePokemon.Moveset.Add(move);
                        }
                    }
                }
                pokemonToCalculateStill.Remove(pokemonName);
            }
            // Check who I forgor
            if (pokemonToCalculateStill.Count > 0)
            {
                throw new Exception($"These mons don't have learnset: {string.Join(",", pokemonToCalculateStill.Keys)}");
            }
        }
        /// <summary>
        /// Parses the unown lookup from symbol code to reward
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseUnownLookup(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Unown Lookup");
            UnownLookup.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                UnownLookup.Add(fields[0], fields[1].Trim()); // This should be unique
            }
        }
        /// <summary>
        /// Gets the list of trainers and their rank
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseNpcTrainerList(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing NPC Trainers available in the game");
            AllNpcTrainers.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                NpcTrainer newTrainer = new NpcTrainer()
                {
                    Name = fields[0],
                    TrainerRank = Enum.Parse<TrainerRank>(fields[1]),
                    FullyLoadedData = false // Not yet, this is in another tab
                };
                AllNpcTrainers.Add(newTrainer.Name, newTrainer);
            }
        }
        /// <summary>
        /// Gets the list of evolution plates
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseEvoPlateList(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Evolution Plates");
            EvoPlates.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                EvoPlate newPlate = new EvoPlate()
                {
                    Name = fields[0],
                    Type = Enum.Parse<PokemonType>(fields[1].Trim().ToUpper()),
                };
                EvoPlates.Add(newPlate.Name, newPlate);
            }
        }
        /// <summary>
        /// Gets the list of key items
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseKeyItemList(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Key Items");
            KeyItems.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++) // Has a header so ignore first item
            {
                string[] fields = lines[i].Split(","); // Csv
                KeyItem newItem = new KeyItem()
                {
                    Name = fields[0],
                    CramType = Enum.Parse<PokemonType>(fields[1].Trim().ToUpper()),
                };
                KeyItems.Add(newItem.Name, newItem);
            }
        }
        /// <summary>
        /// Fills NPC data for existing NPCs given a place in the spreadseet
        /// </summary>
        /// <param name="sheetId"></param>
        /// <param name="sheetTab"></param>
        void FillNpcData(string sheetId, string sheetTab)
        {
            Console.WriteLine("Filling NPC data");
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");

            const int NPC_CARD_HEIGHT = 4;
            const int NPC_CARD_NAME_FIELD = 2;
            const int NPC_CARD_POKEMON_FIELD_START = 6;
            const int NPC_CARD_POKEMON_FIELD_END = 17;
            const int NPC_CARD_ITEM_FIELD_START = 18;
            const int NPC_CARD_ITEM_FIELD_END = 20;

            string[] cols = lines[0].Trim().Split(","); // Keep this because the csv may be smaller if no data for long amount of time
            for (int line = 1; line < lines.Length; line += NPC_CARD_HEIGHT) // First line has a space so it's not needed (second space is already counted by the += 4)
            {
                string[] nameFields = lines[line].Trim().Split(","); // Funnily enough most of the data is in the first line and most of this is ignored
                string trainerName = nameFields[NPC_CARD_NAME_FIELD];
                NpcTrainer theTrainer = AllNpcTrainers[trainerName]; // This will (and has to) throw if trainer is not found
                for (int monIndex = NPC_CARD_POKEMON_FIELD_START; monIndex <= NPC_CARD_POKEMON_FIELD_END && monIndex < cols.Length; monIndex++) // Load all mons
                {
                    string pokemonName = nameFields[monIndex];
                    if (pokemonName == "") continue; // Skip, nothing here
                    theTrainer.AvailablePokemon.Add(Dex[pokemonName]); // Again this will throw if doesnt exist
                }
                // Finally all items which is a bit more annoying because it's in multiple levels
                for (int i = 0; i < NPC_CARD_HEIGHT && line + i < lines.Length; i++)
                {
                    string[] itemLines = lines[line + i].Trim().Split(",");
                    for (int j = NPC_CARD_ITEM_FIELD_START; j <= NPC_CARD_ITEM_FIELD_END && j < cols.Length; j++)
                    {
                        string nextItem = itemLines[j];
                        if (nextItem != "")
                        {
                            theTrainer.AvailableItems.Add(nextItem);
                        }
                    }
                }
                theTrainer.FullyLoadedData = true; // Now all the trainer's data has been loaded
            }
        }
    }
}
