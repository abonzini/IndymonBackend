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
        /// Parses the move data
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseMoves(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Moves");
            Moves.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int TYPE_COL = 1;
            const int CATEGORY_COL = 2;
            const int BP_COL = 3;
            const int ACC_COL = 4;
            const int FLAGS_COL = 5; // Contains all effect keys of this particular move
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                Move nextMove = new Move
                {
                    Name = fields[NAME_COL].Trim(),
                    Type = Enum.Parse<PokemonType>(fields[TYPE_COL].Trim().ToUpper()),
                    Category = Enum.Parse<MoveCategory>(fields[CATEGORY_COL].Trim().ToUpper()),
                    Bp = double.Parse(fields[BP_COL].Trim()),
                    Acc = double.Parse(fields[ACC_COL].Trim())
                };
                string[] flagsFields = fields[FLAGS_COL].Split(";");
                foreach (string flag in flagsFields)
                {
                    string nextFlag = flag.Trim();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextMove.Flags.Add(Enum.Parse<EffectFlag>(nextFlag.Trim()));
                }
                // Move parsed, add
                Moves.Add(nextMove.Name, nextMove);
            }
        }
        /// <summary>
        /// Parses the ability data
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseAbilities(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Abilities");
            Abilities.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int FLAGS_COL = 1; // Contains all effect keys of this particular ability (more manual...)
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                Ability nextAbility = new Ability
                {
                    Name = fields[NAME_COL].Trim(),
                };
                string[] flagsFields = fields[FLAGS_COL].Split(";");
                foreach (string flag in flagsFields)
                {
                    string nextFlag = flag.Trim();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextAbility.Flags.Add(Enum.Parse<EffectFlag>(nextFlag.Trim()));
                }
                // Ability parsed, add
                Abilities.Add(nextAbility.Name, nextAbility);
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
            // Second pass, parse mon data
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
                const int ABILITY_1_FIELD = 11;
                const int ABILITY_2_FIELD = 12;
                const int ABILITY_3_FIELD = 13;
                const int FIRST_EVO_FIELD = 14;
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
                // Ability
                string theAbility = fields[ABILITY_1_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(Abilities[theAbility]);
                theAbility = fields[ABILITY_2_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(Abilities[theAbility]);
                theAbility = fields[ABILITY_3_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(Abilities[theAbility]);
                // Evo and prevos
                for (int j = FIRST_EVO_FIELD; j < fields.Length; j++)
                {
                    string nextEvo = fields[j].Trim();
                    if (nextEvo == "") break; // No more evos
                    Pokemon evo = Dex[nextEvo];
                    evo.Prevo = thePokemon; // Also register myself in the evolution
                    thePokemon.Evos.Add(evo);
                }
            }
            // Next step, process the learnset
            string[] learnsetLines = learnsetCsv.Split('\n');
            foreach (string learnsetLine in learnsetLines) // This one doesnt have labels
            {
                string[] fields = learnsetLine.Split(","); // Csv
                string pokemonName = fields[0]; // First field is the move
                Pokemon thePokemon = Dex[pokemonName]; // Retrieve from DB, it HAS to be there
                for (int i = 1; i < fields.Length; i++) // Then the moves
                {
                    string theMove = fields[i].Trim();
                    if (theMove == "") break; // Finished this mon's moveset
                    Move move = Moves[theMove];
                    thePokemon.Moveset.Add(move);
                    if (theMove == "Sketch")
                    {
                        thePokemon.Moveset.Clear(); // Remove whatever was there before, i can learn all anyway
                        thePokemon.Moveset.AddRange([.. Moves.Values]); // Just add all
                        break; // Stop the rest
                    }
                }
            }
            // Then, ensure each mon has a learnset (validation)
            foreach (Pokemon mon in Dex.Values)
            {
                if (mon.Moveset.Count == 0) throw new Exception("This mon has no moveset");
            }
            // Final step, calculate average stats
        }
        /// <summary>
        /// Parses all mod items
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseModItems(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Mod Item List");
            ModItems.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int FLAGS_COL = 4; // Contains all effect keys of this particular item
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                Item nextItem = new Item
                {
                    Name = fields[NAME_COL]
                };
                string[] flagsFields = fields[FLAGS_COL].Split(";");
                foreach (string flag in flagsFields)
                {
                    string nextFlag = flag.Trim();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextItem.Flags.Add(Enum.Parse<ItemFlag>(nextFlag.Trim()));
                }
                // Move parsed, add
                ModItems.Add(nextItem.Name, nextItem);
            }
        }
        /// <summary>
        /// Parses all battle items
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseBattleItems(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Battle Items");
            BattleItems.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int FLAGS_COL = 10; // Contains all effect keys of this particular item
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                Item nextItem = new Item
                {
                    Name = fields[NAME_COL]
                };
                string[] flagsFields = fields[FLAGS_COL].Split(";");
                foreach (string flag in flagsFields)
                {
                    string nextFlag = flag.Trim();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextItem.Flags.Add(Enum.Parse<ItemFlag>(nextFlag.Trim()));
                }
                if (!nextItem.Flags.Contains(ItemFlag.ALL_ITEMS)) throw new Exception($"{nextItem} does not have the ALL_ITEMS flag, corruption supsected.");
                // Move parsed, add
                BattleItems.Add(nextItem.Name, nextItem);
            }
        }
        /// <summary>
        /// Parses initial score weights of stuff. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseInitialWeights(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Initial Weights");
            InitialWeights.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int ELEMENT_TYPE_COL = 0;
            const int NAME_COL = 1;
            const int WEIGHT_COL = 2;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType type = Enum.Parse<ElementType>(fields[ELEMENT_TYPE_COL].Trim());
                string name = fields[NAME_COL].Trim();
                double weight = double.Parse(fields[WEIGHT_COL]);
                // Validate if all's good
                AssertElementExistance(type, name);
                InitialWeights.Add((type, name), weight);
            }
        }
        /// <summary>
        /// Parses the enablements of items/strats and assembles also the disabled list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseEnabledOptions(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Enablement List");
            Enablers = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), double>>();
            DisabledOptions.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int ENABLER_TYPE_COL = 0;
            const int ENABLER_NAME_COL = 1;
            const int ENABLED_TYPE_COL = 2;
            const int ENABLED_NAME_COL = 3;
            const int WEIGHT_COL = 4;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType enablerType = Enum.Parse<ElementType>(fields[ENABLER_TYPE_COL].Trim());
                string enablerName = fields[ENABLER_NAME_COL].Trim();
                ElementType enabledType = Enum.Parse<ElementType>(fields[ENABLED_TYPE_COL].Trim());
                string enabledName = fields[ENABLED_NAME_COL].Trim();
                double weight = (fields[WEIGHT_COL].Trim() != "-") ? double.Parse(fields[WEIGHT_COL]) : 1;
                // Validate if all's good
                AssertElementExistance(enablerType, enablerName);
                AssertElementExistance(enabledType, enabledName);
                // Add to the corresponding matrices
                if (!Enablers.TryGetValue((enablerType, enablerName), out Dictionary<(ElementType, string), double> enableds))
                {
                    enableds = new Dictionary<(ElementType, string), double>();
                    Enablers.Add((enablerType, enablerName), enableds);
                }
                enableds.Add((enabledType, enabledName), weight);
                // Also the disableds
                DisabledOptions.Add((enabledType, enabledName));
            }
        }
        /// <summary>
        /// Parses the forced builds list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseForcedBuilds(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Forced Build List");
            ForcedBuilds.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int FORCER_TYPE_COL = 0;
            const int FORCER_NAME_COL = 1;
            const int FORCED_TYPE_COL = 2;
            const int FORCED_NAME_COL = 3;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType forcerType = Enum.Parse<ElementType>(fields[FORCER_TYPE_COL].Trim());
                string forcerName = fields[FORCER_NAME_COL].Trim();
                ElementType forcedType = Enum.Parse<ElementType>(fields[FORCED_TYPE_COL].Trim());
                string forcedName = fields[FORCED_NAME_COL].Trim();
                // Validate if all's good
                AssertElementExistance(forcerType, forcerName);
                AssertElementExistance(forcedType, forcedName);
                // Add to the corresponding matrices
                if (!ForcedBuilds.TryGetValue((forcerType, forcerName), out HashSet<(ElementType, string)> forceds))
                {
                    forceds = new HashSet<(ElementType, string)>();
                    ForcedBuilds.Add((forcerType, forcerName), forceds);
                }
                forceds.Add((forcedType, forcedName));
            }
        }
        /// <summary>
        /// Parses the stat modifiers list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseStatModifiers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Stat Mods List");
            StatModifiers.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int ELEMENT_TYPE_COL = 0;
            const int ELEMENT_NAME_COL = 1;
            const int STAT_MOD_TYPE_COL = 2;
            const int STAT_MOD_VALUE_NAME = 3;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType elementType = Enum.Parse<ElementType>(fields[ELEMENT_TYPE_COL].Trim());
                string elementName = fields[ELEMENT_NAME_COL].Trim();
                StatModifier statModType = Enum.Parse<StatModifier>(fields[STAT_MOD_TYPE_COL].Trim());
                string statModValue = fields[STAT_MOD_VALUE_NAME].Trim();
                // Validate if all's good
                AssertElementExistance(elementType, elementName);
                AssertStatModExistance(statModType, statModValue);
                // Add to the corresponding matrices
                if (!StatModifiers.TryGetValue((elementType, elementName), out HashSet<(StatModifier, string)> statMods))
                {
                    statMods = new HashSet<(StatModifier, string)>();
                    StatModifiers.Add((elementType, elementName), statMods);
                }
                statMods.Add((statModType, statModValue));
            }
        }
        /// <summary>
        /// Parses the move modifiers list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseMoveModifiers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Move Mods List");
            MoveModifiers.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int MODIFIER_TYPE_COL = 0;
            const int MODIFIER_NAME_COL = 1;
            const int MODIFIED_TYPE_COL = 2;
            const int MODIFIED_NAME_COL = 3;
            const int MOD_TYPE_COL = 4;
            const int MOD_NAME_COL = 5;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType modifierType = Enum.Parse<ElementType>(fields[MODIFIER_TYPE_COL].Trim());
                string modifierName = fields[MODIFIER_NAME_COL].Trim();
                ElementType modifiedType = Enum.Parse<ElementType>(fields[MODIFIED_TYPE_COL].Trim());
                string modifiedName = fields[MODIFIED_NAME_COL].Trim();
                MoveModifier modType = Enum.Parse<MoveModifier>(fields[MOD_TYPE_COL].Trim());
                string modName = fields[MOD_NAME_COL].Trim();
                // Validate if all's good
                AssertElementExistance(modifierType, modifierName);
                AssertElementExistance(modifiedType, modifiedName);
                AssertMoveModExistance(modType, modName);
                // Add to the corresponding matrices
                if (!MoveModifiers.TryGetValue((modifierType, modifierName), out Dictionary<(ElementType, string), Dictionary<MoveModifier, string>> modifieds))
                {
                    modifieds = new Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>();
                    MoveModifiers.Add((modifierType, modifierName), modifieds);
                }
                if (!modifieds.TryGetValue((modifiedType, modifiedName), out Dictionary<MoveModifier, string> moveMods))
                {
                    moveMods = new Dictionary<MoveModifier, string>();
                    modifieds.Add((modifiedType, modifiedName), moveMods);
                }
                moveMods.Add(modType, modName);
            }
        }
        /// <summary>
        /// Parses the weight modifiers list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseWeightModifiers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Weight Mods List");
            WeightModifiers.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int MODIFIER_TYPE_COL = 0;
            const int MODIFIER_NAME_COL = 1;
            const int MODIFIED_TYPE_COL = 2;
            const int MODIFIED_NAME_COL = 3;
            const int WEIGHT_COL = 4;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType modifierType = Enum.Parse<ElementType>(fields[MODIFIER_TYPE_COL].Trim());
                string modifierName = fields[MODIFIER_NAME_COL].Trim();
                ElementType modifiedType = Enum.Parse<ElementType>(fields[MODIFIED_TYPE_COL].Trim());
                string modifiedName = fields[MODIFIED_NAME_COL].Trim();
                double weight = double.Parse(fields[WEIGHT_COL]);
                // Validate if all's good
                AssertElementExistance(modifierType, modifierName);
                AssertElementExistance(modifiedType, modifiedName);
                // Add to the corresponding matrices
                if (!WeightModifiers.TryGetValue((modifierType, modifierName), out Dictionary<(ElementType, string), double> modifieds))
                {
                    modifieds = new Dictionary<(ElementType, string), double>();
                    WeightModifiers.Add((modifierType, modifierName), modifieds);
                }
                modifieds.Add((modifiedType, modifiedName), weight); // This should be unique
            }
        }
        /// <summary>
        /// Parses the fixed modifiers list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        void ParseFixedModifiers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Fixed Mods List");
            FlatIncreaseModifiers.Clear();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int MODIFIER_TYPE_COL = 0;
            const int MODIFIER_NAME_COL = 1;
            const int WEIGHT_COL = 2;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType modifierType = Enum.Parse<ElementType>(fields[MODIFIER_TYPE_COL].Trim());
                string modifierName = fields[MODIFIER_NAME_COL].Trim();
                double weight = double.Parse(fields[WEIGHT_COL]);
                // Validate if all's good
                AssertElementExistance(modifierType, modifierName);
                // Add to the corresponding matrices
                FlatIncreaseModifiers.Add((modifierType, modifierName), weight); // This should be unique
            }
        }
    }
}
