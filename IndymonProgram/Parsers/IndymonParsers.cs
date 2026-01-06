using MechanicsData;

namespace Parsers
{
    public static class IndymonParsers
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
        /// Parses a sheet (link+tab) into the typechart
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="sheetTab">The tab where typechart is contained</param>
        /// <returns>The typechart (type matchups)</returns>
        public static TypeChart GetTypeChart(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Typechart");
            TypeChart result = new TypeChart();
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
                    result.DefensiveChart.Add(nextType, new Dictionary<PokemonType, float>()); // Add this type
                    for (int j = 1; j < fields.Length; j++)
                    {
                        PokemonType whatType = columnTags[j];
                        float multiplier = float.Parse(fields[j].Trim());
                        result.DefensiveChart[nextType].Add(whatType, multiplier); // Add the multiplier
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Parses a sheet (link+tab) into move list
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="sheetTab">The tab where move data is contained</param>
        /// <returns>The move list</returns>
        public static Dictionary<string, Move> GetMoveDictionary(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Moves");
            Dictionary<string, Move> result = new Dictionary<string, Move>();
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
                result.Add(nextMove.Name, nextMove);
            }
            return result;
        }
        /// <summary>
        /// Parses a sheet (link+tab) into ability list
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="sheetTab">The tab where ability data is contained</param>
        /// <returns>The ability list</returns>
        public static Dictionary<string, Ability> GetAbilityDictionary(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Moves");
            Dictionary<string, Ability> result = new Dictionary<string, Ability>();
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
                result.Add(nextAbility.Name, nextAbility);
            }
            return result;
        }
        /// <summary>
        /// Parses a sheet (link+tab) into pokedex
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="dexSheetTab">The tab where pokedex is contained</param>
        /// <param name="learnsetSheetTab">The tab where learnset is contained</param>
        /// <param name="moves">Dictionary with known moves, for validation</param>
        /// <returns>The pokemon list</returns>
        public static Dictionary<string, Pokemon> GetPokemonDictionary(string sheetId, string dexSheetTab, string learnsetSheetTab, Dictionary<string, Move> moves)
        {
            Console.WriteLine("Parsing Pokedex");
            Dictionary<string, Pokemon> result = new Dictionary<string, Pokemon>();
            // Parse csv
            string pokemonCsv = GetCsvFromGoogleSheets(sheetId, dexSheetTab);
            string learnsetCsv = GetCsvFromGoogleSheets(sheetId, learnsetSheetTab);
            string[] pokemonLines = pokemonCsv.Split('\n');
            // First pass, add all pokemon into the dictionary with an empty Pokemon, this is to first know the existance of all mons
            for (int i = 1; i < pokemonLines.Length; i++)
            {
                int indexUntilComma = pokemonLines[i].IndexOf(',');
                string pokemonName = pokemonLines[i][..indexUntilComma]; // Get only mon name
                result.Add(pokemonName, new Pokemon()); // Start with a default mon, just to add to list
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
                Pokemon thePokemon = result[nextPokemonName];
                thePokemon.Name = nextPokemonName;
                PokemonType theType = Enum.Parse<PokemonType>(fields[TYPE_1_FIELD].Trim().ToUpper());
                thePokemon.Types.Add(theType);
                theType = Enum.Parse<PokemonType>(fields[TYPE_2_FIELD].Trim().ToUpper());
                if (theType != PokemonType.NONE) thePokemon.Types.Add(theType);
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
                    result[nextEvo].Prevo = nextPokemonName; // Also register it in the evolution
                    thePokemon.Evos.Add(nextEvo);
                }
            }
            // Next step, process the learnset
            string[] learnsetLines = learnsetCsv.Split('\n');
            foreach (string learnsetLine in learnsetLines) // This one doesnt have labels
            {
                string[] fields = learnsetLine.Split(","); // Csv
                string pokemonName = fields[0]; // First field is the move
                Pokemon thePokemon = result[pokemonName]; // Retrieve from DB, it HAS to be there
                for (int i = 1; i < fields.Length; i++) // Then the moves
                {
                    string theMove = fields[i].Trim();
                    if (theMove == "") break; // Finished this mon's moveset
                    if (!moves.ContainsKey(theMove)) throw new Exception("Move in learnset not found"); // Move doesn't exist, need to revise
                    thePokemon.Moves.Add(theMove);
                    if (theMove == "Sketch")
                    {
                        thePokemon.Moves.UnionWith(moves.Keys.ToHashSet()); // If sketch, add all existing moves too
                    }
                }
            }
            // Final step, ensure each mon has a learnset (validation)
            foreach (Pokemon mon in result.Values)
            {
                if (mon.Moves.Count == 0) throw new Exception("This mon has no moveset");
            }
            return result;
        }
        /// <summary>
        /// Parses a sheet (link+tab) into mod items
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="sheetTab">The tab where mod item data is contained</param>
        /// <returns>The mod item list of special mod items</returns>
        public static Dictionary<string, ModItem> GetModItemDictionary(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Mod Item List");
            Dictionary<string, ModItem> result = new Dictionary<string, ModItem>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int EFFECTS_COL = 1; // Contains all effect keys of this particular item (starting)
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                ModItem nextModItem = new ModItem
                {
                    Name = fields[NAME_COL]
                };
                for (int j = EFFECTS_COL; j < fields.Length; j++)
                {
                    string modEffect = fields[j].Trim();
                    if (modEffect == "") break; // No more effects
                    string[] modParts = modEffect.Split(":");
                    ModItemExtraFlag action = Enum.Parse<ModItemExtraFlag>(modParts[0].Trim());
                    string value = modParts[1].Trim();
                    nextModItem.Mods.Add((action, value));
                }
                // Move parsed, add
                result.Add(nextModItem.Name, nextModItem);
            }
            return result;
        }
        /// <summary>
        /// Parses a sheet (link+tab) into battle item list
        /// </summary>
        /// <param name="sheetId">Google sheets link</param>
        /// <param name="sheetTab">The tab where battle item data is contained</param>
        /// <returns>The battle item data with special properties</returns>
        public static Dictionary<string, BattleItem> GetBattleItemDictionary(string sheetId, string sheetTab)
        {
            Console.WriteLine("Parsing Battle Items");
            Dictionary<string, BattleItem> result = new Dictionary<string, BattleItem>();
            // Parse csv
            string csv = GetCsvFromGoogleSheets(sheetId, sheetTab);
            const int NAME_COL = 0;
            const int OFF_TYPE_COL = 1;
            const int DEF_TYPE_COL = 2;
            const int FLAGS_COL = 7; // Contains all effect keys of this particular item
            string[] lines = csv.Split("\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(","); // Csv
                BattleItem nextItem = new BattleItem
                {
                    Name = fields[NAME_COL]
                };
                string typeField = fields[OFF_TYPE_COL].Trim().ToUpper();
                if (typeField != "")
                {
                    nextItem.OffensiveBoostType = Enum.Parse<PokemonType>(typeField);
                }
                typeField = fields[DEF_TYPE_COL].Trim().ToUpper();
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
                // Move parsed, add
                result.Add(nextItem.Name, nextItem);
            }
            return result;
        }
    }
}
