using Gameplay.GameplayElements;
using Utilities;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
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
                        if (Enum.TryParse(field.Trim().ToUpper(), out PokemonType type))
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
                Dex.Add(pokemonName, new PokemonSpecies()); // Start with a default mon, just to add to list
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
                PokemonSpecies thePokemon = Dex[nextPokemonName];
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
                    PokemonSpecies thePreevo = Dex[preevo];
                    thePokemon.Prevo = thePreevo; // Add each other
                    thePreevo.Evos.Add(thePokemon);
                }
                // Alternate wild
                string alternateOf = fields[ALTERNATE_OF_FIELD].Trim();
                if (alternateOf != "") // Mon has prevo
                {
                    PokemonSpecies theBaseMon = Dex[alternateOf];
                    thePokemon.AlternativeOf = theBaseMon; // Add each other
                    theBaseMon.WildAlternatives.Add(thePokemon);
                }
                // Image Url
                thePokemon.ImageUrl = fields[IMAGE_URL_FIELD].Trim();
            }
            // Next step, process the learnset
            string[] learnsetLines = learnsetCsv.Split('\n');
            Dictionary<string, PokemonSpecies> pokemonToCalculateStill = Dex.ToDictionary(e => e.Key, e => e.Value);
            foreach (string learnsetLine in learnsetLines) // This one doesnt have labels
            {
                string[] fields = learnsetLine.Split(","); // Csv
                string pokemonName = fields[0]; // First field is the move
                PokemonSpecies thePokemon = pokemonToCalculateStill[pokemonName]; // Retrieve from DB, it HAS to be there
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
        /// Fills NPC data for existing NPCs given a place in the spreadseet
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
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
                            ItemType itemType = GetItemType(nextItem);
                            // TODO: If item type == unknown should throw, but not right now because not all items are defined yet
                            theTrainer.AvailableItems.Add(nextItem);
                        }
                    }
                }
                theTrainer.FullyLoadedData = true; // Now all the trainer's data has been loaded
            }
        }
        /// <summary>
        /// Fills data of all boxed mons
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void FillBoxedMonData(string sheetId, string sheetTab)
        {
            Console.WriteLine("Filling Boxed Mon data");
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");

            for (int line = 1; line < lines.Length; line++) // First line has legend so it's not needed
            {
                string[] fields = lines[line].Trim().Split(',');
                string id = fields[0];
                string species = fields[1];
                PokemonEntity nextBoxedMon = GenerateBlankPokemon(species); // Generate a mon of this kind but make it have randomized values in case is the first instancing
                nextBoxedMon.Nickname = fields[2];
                nextBoxedMon.PokeBall = PokeBalls[fields[3]];
                nextBoxedMon.Moves[0] = (fields[4] != "") ? Moves[fields[4]] : null;
                nextBoxedMon.Moves[1] = (fields[5] != "") ? Moves[fields[5]] : null;
                nextBoxedMon.Nature = Natures[fields[6]];
                nextBoxedMon.Abilities[0] = (fields[7] != "") ? Abilities[fields[7]] : null;
                nextBoxedMon.Abilities[1] = (fields[8] != "") ? Abilities[fields[8]] : null;
                nextBoxedMon.Abilities[2] = (fields[9] != "") ? Abilities[fields[9]] : null;
                BoxedMons.Add(id, nextBoxedMon);
            }
        }
        /// <summary>
        /// Fills data of all player trainers
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void FillPlayers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Filling Player Trainer data");
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] rows = csv.Split("\n");
            string[] sampleCols = rows[0].Trim().Split(","); // Just for some alignment and number check

            // Iterate for all trainers, keeping in mind the dimensions of row/col
            const int TRANER_CARD_WIDTH = 21; // Width is 20 but the margin to the right is considered the trainer's
            const int TRANER_CARD_HEIGHT = 45; // Height is 44 (?) but the margin to the bottom is considered the trainer's
            for (int i = 1; i < rows.Length; i += TRANER_CARD_HEIGHT) // Ignore the first row/col since these are just formatting spaces
            {
                for (int j = 1; j < sampleCols.Length; j += TRANER_CARD_WIDTH)
                {
                    // Standing on next trainer's trainer card (backend)
                    // Row 1, name, data, some IMP and config
                    string[] nextLine = rows[i + 0].Trim().Split(',');
                    string name = nextLine[j + 2];
                    if (name == "") continue; // If no name, trainer is empty, move to next card
                    TrainerEntity newTrainer = new TrainerEntity
                    {
                        Name = name,
                        PictureUrl = nextLine[j + 4],
                        DiscordId = nextLine[j + 6],
                        Imp = int.Parse(nextLine[j + 12]),
                        AutoTeam = bool.Parse(nextLine[j + 19].ToLower())
                    };
                    // Row 2, just jewelry
                    nextLine = rows[i + 1].Trim().Split(',');
                    if (nextLine[j + 12] != "Empty Jewelry Slot") // Only empty slot is allowed as no jewelry to ensure assert on typos
                    {
                        newTrainer.EquippedJewelry = Jewelries[nextLine[j + 12]];
                        newTrainer.EquippedJewelryUses = int.Parse(nextLine[j + 19]);
                    }
                    // Next is the 2 macro-rows of Pokemon 5x2
                    const int POKEMON_WIDTH = 4;
                    const int POKEMON_HEIGHT = 10;
                    for (int monY = 0; monY < 2; monY++)
                    {
                        for (int monX = 0; monX < 5; monX++)
                        {
                            // Species line, contains species of mon, will instantiate a new Pokemon if there's a valid species
                            nextLine = rows[i + 4 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            string monField = nextLine[j + 2 + (monX * POKEMON_WIDTH)]; // In this case, the species
                            if (monField == "") continue;
                            PokemonEntity newMon = GenerateBlankPokemon(monField);
                            // Nickname line
                            nextLine = rows[i + 3 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            monField = nextLine[j + 2 + (monX * POKEMON_WIDTH)]; // Nickname
                            if (monField != "Nickname") newMon.Nickname = monField;
                            // Pokeball + Nature Line
                            nextLine = rows[i + 5 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            monField = nextLine[j + 0 + (monX * POKEMON_WIDTH)]; // Pokeball
                            newMon.PokeBall = PokeBalls[monField];
                            monField = nextLine[j + 2 + (monX * POKEMON_WIDTH)]; // Nature
                            newMon.Nature = Natures[monField];
                            // 2 Moves line
                            nextLine = rows[i + 6 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            monField = nextLine[j + 0 + (monX * POKEMON_WIDTH)]; // M1
                            if (monField != "No Move") newMon.Moves[0] = Moves[monField];
                            monField = nextLine[j + 2 + (monX * POKEMON_WIDTH)]; // M2
                            if (monField != "No Move") newMon.Moves[1] = Moves[monField];
                            // 2 Move Disk line
                            nextLine = rows[i + 7 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            monField = nextLine[j + 0 + (monX * POKEMON_WIDTH)]; // Disk1
                            if (monField != "No Move Disk")
                            {
                                newMon.MoveDisks[0] = GetMoveDisk(monField);
                            }
                            monField = nextLine[j + 2 + (monX * POKEMON_WIDTH)]; // Disk2
                            if (monField != "No Move Disk")
                            {
                                newMon.MoveDisks[1] = GetMoveDisk(monField);
                            }
                            // Abilities line
                            for (int abilityIndex = 0; abilityIndex < newMon.Abilities.Length; abilityIndex++)
                            {
                                nextLine = rows[i + 8 + abilityIndex + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                                monField = nextLine[j + 0 + (monX * POKEMON_WIDTH)]; // Gets the ability
                                if (monField != "No Ability")
                                {
                                    newMon.Abilities[abilityIndex] = Abilities[monField];
                                    monField = nextLine[j + 3 + (monX * POKEMON_WIDTH)]; // Check if ability is active
                                    if (abilityIndex != 0)
                                    {
                                        // Non-first ability are active and consumed if gummy used
                                        if (bool.Parse(monField.ToLower()))
                                        {
                                            newMon.AbilityActive[abilityIndex] = true;
                                        }
                                    }
                                    else
                                    {
                                        // First one always active no gummy needed
                                        newMon.AbilityActive[abilityIndex] = true;
                                    }
                                }
                            }
                            // Held item line
                            nextLine = rows[i + 11 + (monY * POKEMON_HEIGHT)].Trim().Split(',');
                            monField = nextLine[j + 0 + (monX * POKEMON_WIDTH)]; // Held Item
                            if (monField != "Empty Item Slot")
                            {
                                newMon.HeldItem = HeldItems[monField];
                            }
                            // Finally, add mon
                            newTrainer.Pokemon.Add(newMon);
                        }
                    }
                    // Now begins all the boxes and things
                    void parseBagFields<T>(int row, Dictionary<T, int> dictToAdd, Dictionary<string, T> lookupSource)
                    {
                        string field;
                        T item;
                        int count;
                        for (int y = 0; y < 2; y++) // 2 lines
                        {
                            int xInitialValue = y == 0 ? 2 : 0;
                            nextLine = rows[i + row + y].Trim().Split(',');
                            for (int x = xInitialValue; x < 10; x += 2) // 10 item+count "columns"
                            {
                                field = nextLine[j + x];
                                if (field != "") // Theres an item in this bag
                                {
                                    item = lookupSource[field];
                                    count = int.Parse(nextLine[j + x + 1]);
                                    GeneralUtilities.AddtemToCountDictionary(dictToAdd, item, count, TrainerEntity.MAX_NUMBER_BAG);
                                }
                            }
                        }
                    }
                    parseBagFields(22, newTrainer.Gummies, Gummies); // Row 23, gummies
                    parseBagFields(24, newTrainer.MoveDisks, MoveDiskLookup); // Row 25, move disk
                    parseBagFields(26, newTrainer.HeldItems, HeldItems); // Row 27, held items
                    parseBagFields(28, newTrainer.Mints, Mints); // Row 29, mints
                    parseBagFields(30, newTrainer.EvoPlates, EvoPlates); // Row 31, evo plates
                    parseBagFields(32, newTrainer.Essences, Essences); // Row 33, essences
                    parseBagFields(34, newTrainer.KeyItems, KeyItems); // Row 35, key items
                    parseBagFields(36, newTrainer.PokeBalls, PokeBalls); // Row 37, key items
                    parseBagFields(40, newTrainer.Favours, AllNpcTrainers); // Row 41, favours
                    // Some weird special ones, the sandwiches (order-important) and the boxed (the lookup is the key and not the value)
                    { // 39, sandwiches
                        string field;
                        Sandwich sammy;
                        for (int y = 0; y < 2; y++) // 2 lines
                        {
                            int xInitialValue = y == 0 ? 2 : 0;
                            nextLine = rows[i + 38 + y].Trim().Split(',');
                            for (int x = xInitialValue; x < 10; x += 2) // 10 item+count "columns"
                            {
                                field = nextLine[j + x];
                                if (field != "" && newTrainer.Sandwiches.Count < TrainerEntity.MAX_NUMBER_BAG) // Theres a sandwich and i have space
                                {
                                    sammy = GetSandwich(field);
                                    newTrainer.Sandwiches.Add(sammy);
                                }
                            }
                        }
                    }
                    { // 43, boxed mons
                        string field;
                        for (int y = 0; y < 2; y++) // 2 lines
                        {
                            int xInitialValue = y == 0 ? 2 : 0;
                            nextLine = rows[i + 42 + y].Trim().Split(',');
                            for (int x = xInitialValue; x < 10; x += 2) // 10 item+count "columns"
                            {
                                field = nextLine[j + x];
                                if (field != "" && BoxedMons.Count < TrainerEntity.MAX_NUMBER_BAG)
                                {
                                    if (BoxedMons.ContainsKey(field)) // Theres a mon with that id in the box and i have space
                                    {
                                        newTrainer.BoxedMons.Add(field);
                                    }
                                    else
                                    {
                                        throw new Exception($"{newTrainer.Name}'s box references a mon not in global box list");
                                    }
                                }
                            }
                        }
                    }
                    // Finally, add trainer
                    Trainers.Add(name, newTrainer);
                }
            }
        }
    }
}
