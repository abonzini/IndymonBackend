using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        /// <summary>
        /// Gets a csv from a google sheets id+tab combo
        /// </summary>
        /// <param name="sheetId">Id</param>
        /// <param name="sheetTab">Tab</param>
        /// <returns>The csv</returns>
        static string GetCsvFromGoogleSheets(string sheetId, string sheetTab)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={sheetTab}";
            using HttpClient client = new HttpClient();
            return client.GetStringAsync(url).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Parses the type chart
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseTypeChart(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Typechart");
            TypeChart = new TypeChart();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                    TypeChart.DefensiveChart.Add(nextType, new Dictionary<PokemonType, float>()); // Add this type
                    for (int j = 1; j < fields.Length; j++)
                    {
                        PokemonType whatType = columnTags[j];
                        float multiplier = float.Parse(fields[j].Trim());
                        TypeChart.DefensiveChart[nextType].Add(whatType, multiplier); // Add the multiplier
                    }
                }
            }
        }
        /// <summary>
        /// Parses the move data
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseMoves(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Moves");
            Moves = new Dictionary<string, Move>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                    Bp = int.Parse(fields[BP_COL].Trim()),
                    Acc = int.Parse(fields[ACC_COL].Trim())
                };
                for (int j = FLAGS_COL; j < fields.Length; j++)
                {
                    string nextFlag = fields[j].Replace("\"", "").Trim().ToUpper();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextMove.Flags.Add(Enum.Parse<EffectFlag>(nextFlag));
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
        public void ParseAbilities(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Abilities");
            Abilities = new Dictionary<string, Ability>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int FLAGS_COL = 2; // Contains all effect keys of this particular ability (more manual...)
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                Ability nextAbility = new Ability
                {
                    Name = fields[NAME_COL].Trim(),
                };
                for (int j = FLAGS_COL; j < fields.Length; j++)
                {
                    string nextFlag = fields[j].Replace("\"", "").Trim().ToUpper();
                    if (nextFlag == "") continue; // If flag is invalid, skip
                    nextAbility.Flags.Add(Enum.Parse<EffectFlag>(nextFlag));
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
        public void ParsePokemonData(string sheetId, string dexSheetTab, string learnsetSheetTab)
        {
            Console.WriteLine("Parsing Pokedex");
            Dex = new Dictionary<string, Pokemon>();
            // Parse csv
            string pokemonCsv = GetCsvFromGoogleSheets(sheetId, dexSheetTab);
            string learnsetCsv = GetCsvFromGoogleSheets(sheetId, learnsetSheetTab);
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
                const int ABILITY_1_FIELD = 10;
                const int ABILITY_2_FIELD = 11;
                const int ABILITY_3_FIELD = 12;
                const int FIRST_EVO_FIELD = 13;
                string nextPokemonName = fields[NAME_FIELD];
                Pokemon thePokemon = Dex[nextPokemonName];
                thePokemon.Name = nextPokemonName;
                PokemonType theType = Enum.Parse<PokemonType>(fields[TYPE_1_FIELD].Trim().ToUpper());
                thePokemon.Types[0] = theType;
                theType = Enum.Parse<PokemonType>(fields[TYPE_2_FIELD].Trim().ToUpper());
                thePokemon.Types[1] = theType;
                int Hp = int.Parse(fields[HP_FIELD]);
                int Attack = int.Parse(fields[ATK_FIELD]);
                int Defense = int.Parse(fields[DEF_FIELD]);
                int SpecialAttack = int.Parse(fields[SPATK_FIELD]);
                int SpecialDefense = int.Parse(fields[SPDEF_FIELD]);
                int Speed = int.Parse(fields[SPEED_FIELD]);
                thePokemon.Stats = [Hp, Attack, Defense, SpecialAttack, SpecialDefense, Speed]; // Load stats
                string theAbility = fields[ABILITY_1_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(theAbility);
                theAbility = fields[ABILITY_2_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(theAbility);
                theAbility = fields[ABILITY_3_FIELD].Trim();
                if (theAbility != "") thePokemon.Abilities.Add(theAbility);
                for (int j = FIRST_EVO_FIELD; j < fields.Length; j++)
                {
                    string nextEvo = fields[j].Trim();
                    if (nextEvo == "") break; // No more evos
                    Dex[nextEvo].Prevo = nextPokemonName; // Also register it in the evolution
                    thePokemon.Evos.Add(nextEvo);
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
                    if (!Moves.ContainsKey(theMove)) throw new Exception("Move in learnset not found"); // Move doesn't exist, need to revise
                    thePokemon.Moves.Add(theMove);
                    if (theMove == "Sketch")
                    {
                        thePokemon.Moves.UnionWith(Moves.Keys.ToHashSet()); // If sketch, add all existing moves too
                    }
                }
            }
            // Final step, ensure each mon has a learnset (validation)
            foreach (Pokemon mon in Dex.Values)
            {
                if (mon.Moves.Count == 0) throw new Exception("This mon has no moveset");
            }
        }
        /// <summary>
        /// Parses all mod items
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseModItems(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Mod Item List");
            ModItems = new Dictionary<string, ModItem>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                ModItem nextModItem = new ModItem
                {
                    Name = lines[i].Trim()
                };
                // Move parsed, add
                ModItems.Add(nextModItem.Name, nextModItem);
            }
        }
        /// <summary>
        /// Parses all battle items
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseBattleItems(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Battle Items");
            BattleItems = new Dictionary<string, BattleItem>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int DEF_TYPE_COL = 1;
            const int FLAGS_COL = 8; // Contains all effect keys of this particular item
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                BattleItem nextItem = new BattleItem
                {
                    Name = fields[NAME_COL]
                };
                string typeField = fields[DEF_TYPE_COL].Trim().ToUpper();
                if (typeField != "")
                {
                    nextItem.DefensiveBoostType = Enum.Parse<PokemonType>(typeField);
                }
                for (int j = FLAGS_COL; j < fields.Length; j++)
                {
                    string nextFlag = fields[j].Replace("\"", "").Trim().ToUpper();
                    if (nextFlag == "") continue; // If not valid flag, skip
                    nextItem.Flags.Add(Enum.Parse<BattleItemFlag>(nextFlag));
                }
                if (!nextItem.Flags.Contains(BattleItemFlag.ALL_ITEMS)) throw new Exception($"{nextItem} does not have the ALL_ITEMS flag, corruption supsected.");
                // Move parsed, add
                BattleItems.Add(nextItem.Name, nextItem);
            }
        }
        /// <summary>
        /// Parses initial score weights of stuff. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseInitialWeights(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Initial Weights");
            InitialWeights = new Dictionary<(ElementType, string), float>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int ELEMENT_TYPE_COL = 0;
            const int NAME_COL = 1;
            const int WEIGHT_COL = 2;
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ElementType type = Enum.Parse<ElementType>(fields[ELEMENT_TYPE_COL].Trim());
                string name = fields[NAME_COL].Trim();
                float weight = float.Parse(fields[WEIGHT_COL]);
                // Validate if all's good
                if (!ValidateElementExistance(type, name)) throw new Exception($"{name} is not a valid {type}");
                InitialWeights.Add((type, name), weight);
            }
        }
        /// <summary>
        /// Parses the enablements of items/strats and assembles also the disabled list. This requires all data (moves,dex,etc) to be pre-parsed
        /// </summary>
        /// <param name="sheetId">Sheet to google sheets</param>
        /// <param name="sheetTab">Which tab has the data</param>
        public void ParseEnabledOptions(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Enablement List");
            Enablers = new Dictionary<(ElementType, string), Dictionary<(ElementType, string), float>>();
            DisabledOptions = new HashSet<(ElementType, string)>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                float weight = (fields[WEIGHT_COL].Trim() != "-") ? float.Parse(fields[WEIGHT_COL]) : 1;
                // Validate if all's good
                if (!ValidateElementExistance(enablerType, enablerName)) throw new Exception($"{enablerName} is not a valid {enablerType}");
                if (!ValidateElementExistance(enabledType, enabledName)) throw new Exception($"{enabledName} is not a valid {enabledType}");
                // Add to the corresponding matrices
                if (!Enablers.TryGetValue((enablerType, enablerName), out Dictionary<(ElementType, string), float> enableds))
                {
                    enableds = new Dictionary<(ElementType, string), float>();
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
        public void ParseForcedBuilds(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Forced Build List");
            ForcedBuilds = new Dictionary<(ElementType, string), HashSet<(ElementType, string)>>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                if (!ValidateElementExistance(forcerType, forcerName)) throw new Exception($"{forcerName} is not a valid {forcerType}");
                if (!ValidateElementExistance(forcedType, forcedName)) throw new Exception($"{forcedName} is not a valid {forcedType}");
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
        public void ParseStatModifiers(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Stat Mods List");
            StatModifiers = new Dictionary<(ElementType, string), HashSet<(StatModifier, string)>>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                if (!ValidateElementExistance(elementType, elementName)) throw new Exception($"{elementName} is not a valid {elementType}");
                if (!ValidateStatModExistance(statModType, statModValue)) throw new Exception($"{statModValue} is not a valid {statModType}");
                // Add to the corresponding matrices
                if (!StatModifiers.TryGetValue((elementType, elementName), out HashSet<(StatModifier, string)> statMods))
                {
                    statMods = new HashSet<(StatModifier, string)>();
                    StatModifiers.Add((elementType, elementName), statMods);
                }
                statMods.Add((statModType, statModValue));
            }
        }
    }
}
