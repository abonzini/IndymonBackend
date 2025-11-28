using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ParsersAndData
{
    public class DataContainers
    {
        public Dictionary<string, Pokemon> Dex { get; set; } = null;
        public Dictionary<string, Dictionary<string, float>> TypeChart { get; set; } = null;
        public Dictionary<string, Move> MoveData { get; set; } = null;
        public Dictionary<string, HashSet<string>> OffensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> DefensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> NatureItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> EvItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> TeraItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> MoveItemData { get; set; } = null;
        public Dictionary<string, TrainerData> TrainerData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NpcData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NamedNpcData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, Dungeon> Dungeons { get; set; } = new Dictionary<string, Dungeon>();
    }
    public class Item
    {
        public string Name { get; set; }
        public int Uses { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public class ExplorationStatus
    {
        public int HealthPercentage { get; set; } = 100;
        public string NonVolatileStatus { get; set; } = "";
        public void SetStatus(string status)
        {
            if (status.ToLower() == "0 fnt")
            {
                HealthPercentage = 1; // Would be cool to res mons at 1% no matter what
                NonVolatileStatus = "";
            }
            else
            {
                string[] splitStatus = status.Split(' ');
                string[] splitHealth = splitStatus[0].Split("/");
                HealthPercentage = (100 * int.Parse(splitHealth[0])) / int.Parse(splitHealth[1]);
                if (HealthPercentage == 0) HealthPercentage = 1; // Can never be 0 because otherwise it'd be fainted
                NonVolatileStatus = (splitStatus.Length == 2) ? splitStatus[1] : "";
            }
        }
        public override string ToString()
        {
            return $"{HealthPercentage}% {NonVolatileStatus}";
        }
    }
    public class PokemonSet
    {
        public string NickName { get; set; }
        public string Species { get; set; }
        public bool Shiny { get; set; }
        public string Ability { get; set; }
        public string[] Moves { get; set; } = new string[4];
        public Item Item { get; set; }
        public ExplorationStatus ExplorationStatus { get; set; }

        public override string ToString()
        {
            return $"{Species}: {Ability}, {string.Join('-', Moves)}, {Item?.Name}";
        }
        /// <summary>
        /// Triggers randomizing of mon but with some extra manual checks and confirmations. Allows to randomize single mon instrad of whole team
        /// </summary>
        /// <param name="backEndData">Back end data needed to choose things accurately</param>
        /// <param name="smart">Smart randomization uses the AI list</param>
        public void RandomizeAndVerify(DataContainers backEndData, bool smart)
        {
            bool acceptedMon = false;
            Pokemon pokemonBackendData = backEndData.Dex[Species];
            while (!acceptedMon)
            {
                // Randomize mon, with a 7% chance of each move being empty (1->18%, 2->1.3%, 3->0.003%)
                RandomizeMon(backEndData, smart, 7); // Randomize mon
                // Show it to user, user will decide if redo or revise (banning sets for the future)
                Console.WriteLine($"\tSet for {ToString()}");
                Console.WriteLine("\tTo modify AI for future: 5: blacklist ability. 1-4 blacklist moves. Otherwise this mon is approved. 0 to reroll the whole thing");
                string inputString = Console.ReadLine().ToLower();
                switch (inputString)
                {
                    case "5":
                        pokemonBackendData.AiAbilityBanlist.Add(Ability);
                        break;
                    case "1":
                        pokemonBackendData.AiMoveBanlist.Add(Moves[0]);
                        break;
                    case "2":
                        pokemonBackendData.AiMoveBanlist.Add(Moves[1]);
                        break;
                    case "3":
                        pokemonBackendData.AiMoveBanlist.Add(Moves[2]);
                        break;
                    case "4":
                        pokemonBackendData.AiMoveBanlist.Add(Moves[3]);
                        break;
                    case "0":
                        break; // Rejects te mon
                    default:
                        acceptedMon = true;
                        break;
                }
            }
        }
        /// <summary>
        /// Randomizes this mon's sets (ability+moves)
        /// </summary>
        /// <param name="backendData">Data to get mon's moves, etc</param>
        /// <param name="smart">Smart randomizer avoids using moves in the AI banlist</param>
        /// <param name="switchChance">Chance that the last move is empty (switch)</param>
        public void RandomizeMon(DataContainers backendData, bool smart, int switchChance)
        {
            Random _rng = new Random();
            Pokemon pokemonBackendData = backendData.Dex[Species];
            // Get data, remove hardcoded banned stuff
            HashSet<string> legalAbilities = pokemonBackendData.Abilities.ToHashSet();
            RemoveBannedAbilities(legalAbilities);
            HashSet<string> legalMoves = pokemonBackendData.Moves.ToHashSet();
            RemoveBannedMoves(legalMoves);
            HashSet<string> legalStabs = pokemonBackendData.DamagingStabs.ToHashSet();
            RemoveBannedMoves(legalStabs);
            if (smart)
            {
                legalAbilities.ExceptWith(pokemonBackendData.AiAbilityBanlist);
                RemoveUselessAbilities(legalAbilities);
                legalMoves.ExceptWith(pokemonBackendData.AiMoveBanlist);
                RemoveUselessMoves(legalMoves);
                legalStabs.ExceptWith(pokemonBackendData.AiMoveBanlist);
                RemoveUselessMoves(legalStabs);
            }
            if (GetTera(backendData) != "") // Mons that can tera will be able to use tera blast always, regardless if previously banned move
            {
                legalMoves.Add("tera blast");
            }
            // First, get the mon an ability
            Ability = legalAbilities.ElementAt(_rng.Next(legalAbilities.Count)); // Get a random one
            // Then, get the mon a stab (move 1)
            Moves[0] = legalStabs.ElementAt(_rng.Next(legalStabs.Count));
            legalMoves.Remove(Moves[0]);
            // Moves 2-4, just get random shit with a chance to switch
            for (int i = 1; i <= 3; i++)
            {
                if (_rng.Next(0, 100) < switchChance) // Empty
                {
                    Moves[i] = "";
                }
                else // Otherwise a normal move i guess
                {
                    Moves[i] = legalMoves.ElementAt(_rng.Next(legalMoves.Count));
                    legalMoves.Remove(Moves[1]);
                }
            }
        }
        /// <summary>
        /// Removes banned (clause) abilities from a set
        /// </summary>
        /// <param name="abilities">Set with abilities</param>
        static void RemoveBannedAbilities(HashSet<string> abilities)
        {
            abilities.Remove("moody");
        }
        /// <summary>
        /// Removes banned moves (clause) from a set
        /// </summary>
        /// <param name="moves">Set with moves</param>
        static void RemoveBannedMoves(HashSet<string> moves)
        {
            moves.Remove("sand attack");
            moves.Remove("double team");
            moves.Remove("minimize");
            moves.Remove("hidden power");
            moves.Remove("flash");
            moves.Remove("kinesis");
            moves.Remove("mud-slap");
            moves.Remove("smokescreen");
        }
        /// <summary>
        /// Removes useless abilities from a set (so I don't need to blacklist it for every mon)
        /// </summary>
        /// <param name="abilities">Set with abilities</param>
        static void RemoveUselessAbilities(HashSet<string> abilities)
        {
            // Useless
            abilities.Remove("pickup");
            abilities.Remove("ball fetch");
            abilities.Remove("honey gather");
            abilities.Remove("run away");
            abilities.Remove("telepathy");
        }
        /// <summary>
        /// Removes usaless moves from a set (so I don't need to blacklist it for every mon)
        /// </summary>
        /// <param name="moves">Set with moves</param>
        static void RemoveUselessMoves(HashSet<string> moves)
        {
            // Removed because useless
            moves.Remove("frustration");
            moves.Remove("splash");
            moves.Remove("celebrate");
            moves.Remove("hold hands");
        }
        /// <summary>
        /// Gets the pokemon nature (if any)
        /// </summary>
        /// <param name="backEndData">Back End data to check properties</param>
        /// <returns>The nature name or ""</returns>
        string GetNature(DataContainers backEndData)
        {
            if (Item != null) // Teras are determined by item
            {
                if (backEndData.NatureItemData.TryGetValue(Item.Name, out HashSet<string> auxItemSet)) // If the item is a nature-setting item
                {
                    return auxItemSet.FirstOrDefault(); // Found the nature set by item
                }
            }
            return "";
        }
        /// <summary>
        /// Gets the EVs of this mon
        /// </summary>
        /// <param name="backEndData">Back end data</param>
        /// <returns>Array of the 6ev in order, HP, Atk, Def, SpA, SpD, Spe</returns>
        int[] GetEvs(DataContainers backEndData)
        {
            int[] evs = { 1, 1, 1, 1, 1, 1 }; // All ev's 1 so the thing doesnt annoy me
            if (Item != null) // EVs also by item
            {
                if (backEndData.EvItemData.TryGetValue(Item.Name, out HashSet<string> auxItemSet)) // If the item is a nature-setting item
                {
                    foreach (string evStat in auxItemSet)
                    {
                        int evIndex = evStat.ToLower() switch
                        {
                            "hp" => 0,
                            "attack" => 1,
                            "defense" => 2,
                            "special attack" => 3,
                            "special defense" => 4,
                            "speed" => 5,
                            _ => 6
                        };
                        evs[evIndex] = 50; // This stat gets 50
                    }
                }
            }
            return evs;
        }
        /// <summary>
        /// Gets the pokemon tera type
        /// </summary>
        /// <param name="backEndData">Back End data to check properties</param>
        /// <returns>The tera type or ""</returns>
        public string GetTera(DataContainers backEndData)
        {
            if (Item != null) // Natures are determined by item
            {
                if (backEndData.TeraItemData.TryGetValue(Item.Name, out HashSet<string> auxItemSet)) // If the item is a nature-setting item
                {
                    return auxItemSet.FirstOrDefault(); // Found the nature set by item
                }
            }
            return "";
        }
        /// <summary>
        /// If the pokemon has an item that will be equipped in battle, return its name
        /// </summary>
        /// <param name="backEndData">Back End data to check properties</param>
        /// <returns>The item name or ""</returns>
        string GetBattleItem(DataContainers backEndData)
        {
            string itemName = "";
            if (Item != null)
            {
                // Ensure it's not one of my special items
                if (backEndData.NatureItemData.ContainsKey(Item.Name))
                {
                    itemName = "spent mint"; // the nature-changing item
                }
                else if (backEndData.TeraItemData.ContainsKey(Item.Name))
                {
                    itemName = "spent tera shard";
                }
                else if (backEndData.EvItemData.ContainsKey(Item.Name))
                {
                    itemName = "pretty feather";
                }
                else if (backEndData.MoveItemData.ContainsKey(Item.Name))
                {
                    itemName = "blank disk";
                }
                else
                {
                    itemName = Item.Name;
                }
            }
            return itemName;
        }
        /// <summary>
        /// Gets pokemon string in showdown packed format
        /// </summary>
        /// <param name="backEndData">Backend data to check stuff like item effects</param>
        /// <returns>Pokemon string in showdown packed format</returns>
        public string GetPacked(DataContainers backEndData)
        {
            // First, keep in mind it may have a move-altering item
            List<string> setMoves = [.. Moves];
            if ((Item != null) && (backEndData.MoveItemData.TryGetValue(Item.Name, out HashSet<string> overritenMoves)))
            {
                int moveSlot = 3; // Start with last
                foreach (string newMove in overritenMoves)
                {
                    setMoves[moveSlot] = newMove;
                    moveSlot--;
                }
            }
            //NICKNAME|SPECIES|ITEM|ABILITY|MOVES|NATURE|EVS|GENDER|IVS|SHINY|LEVEL|HAPPINESS,POKEBALL,HIDDENPOWERTYPE,GIGANTAMAX,DYNAMAXLEVEL,TERATYPE
            List<string> packedStrings = new List<string>();
            packedStrings.Add(NickName);
            packedStrings.Add(Species);
            packedStrings.Add(GetBattleItem(backEndData));
            packedStrings.Add(Ability);
            packedStrings.Add(string.Join(",", setMoves));
            packedStrings.Add(GetNature(backEndData));
            packedStrings.Add(string.Join(",", GetEvs(backEndData)));
            packedStrings.Add("");
            packedStrings.Add(""); // No IVs I don't care
            packedStrings.Add(Shiny ? "S" : ""); // Depending if shiny
            packedStrings.Add(""); // Always lvl 100
            string lastPackedString = $",,,,,{GetTera(backEndData)}"; // Add the "remaining" useless stuff needed for  tera, etc
            if (ExplorationStatus != null) // This is new, status will be ,%health,status condition too after the tera, so it can be picked up from a modified showdown
            {
                lastPackedString += $",{ExplorationStatus.HealthPercentage}";
                lastPackedString += $",{ExplorationStatus.NonVolatileStatus}";
            }
            else
            {
                lastPackedString += ",,"; // Otherwise empty
            }
            packedStrings.Add(lastPackedString);
            return string.Join("|", packedStrings); // Join them together with |
        }
    }
    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum TeambuildSettings
    {
        NONE = 0,
        EXPLORATION = 1,
        SMART = 2,
        MONOTYPE = 4, /// Share one specific type
        DANCE_OFF = 8 /// Every mon must have -dance aqua step or clangorous soul
    }
    public class TrainerData
    {
        public string Avatar { get; set; }
        public string Name { get; set; }
        public bool AutoItem { get; set; }
        public bool AutoTeam { get; set; }
        public List<Item> BattleItems { get; set; } = new List<Item>();
        public List<PokemonSet> Teamsheet { get; set; } = new List<PokemonSet>(6);
        public override string ToString()
        {
            return Name;
        }
        /// <summary>
        /// Determines whether trainer could participate in a challenge with certain settings
        /// </summary>
        /// <param name="backEndData"></param>
        /// <param name="nMons">How many mons to perform this operation on</param>
        /// <param name="settings">Settings for teambuilding</param>
        /// <returns></returns>
        public bool CanParticipate(DataContainers backEndData, int nMons, TeambuildSettings settings)
        {
            // TODO Redo this to give a list of valid mons, add bool param of like whether it's all the teamsheets (reorder) or first N
            // Mon number check
            if (Teamsheet.Count < nMons) return false;
            // Monotype check
            if (settings.HasFlag(TeambuildSettings.MONOTYPE))
            {
                Dictionary<string, List<PokemonSet>> validMons = new Dictionary<string, List<PokemonSet>>();
                foreach (PokemonSet mon in Teamsheet) // Check each mon
                {
                    foreach (string type in backEndData.Dex[mon.Species].Types) // Aggregate types
                    {
                        // Add mon to the set
                        if (validMons.ContainsKey(type))
                        {
                            validMons[type].Add(mon);
                        }
                        else
                        {
                            validMons[type] = [mon];
                        }
                    }
                }
                // Then, get the ones that can be used, for nMons, need to find the list that can be used as monotype
                List<List<PokemonSet>> validMonotypes = validMons.Values.Where(l => l.Count >= nMons).ToList();
                if (validMonotypes.Count == 0) return false; // Can't monotype
                else return true; // Can monotype
            }
            // No more checks, team is OK
            return true;
        }
        /// <summary>
        /// Defines a team's team (e.g. movesets, etc), randomizes depending on auto-settings
        /// </summary>
        /// <param name="backEndData"></param>
        /// <param name="nMons">How many mons to perform this operation on</param>
        /// <param name="settings">Settings for teambuilding</param>
        public void ConfirmSets(DataContainers backEndData, int nMons, TeambuildSettings settings)
        {
            Console.WriteLine($"Checking {Name}'s team");
            bool defined = false;
            Random _rng = new Random();
            while (!defined)
            {
                // TODO: Redo flow. Instead of shuffling-getting and re-shufflign just get once and shuffle in place
                // If not auto team, would verify and check anyway
                // Mon randomizer now takes wthe whole enum, so it can fine-randomize

                // Shuffle teams and sets if auto-team
                if (AutoTeam)
                {
                    // First, shuffle the mons
                    Utilities.ShuffleList(Teamsheet, 0, Teamsheet.Count);
                    // In monotype, need to do a quick searching to find which mons can be monotype, and then put random nMons in top
                    if (settings.HasFlag(TeambuildSettings.MONOTYPE))
                    {
                        Dictionary<string, List<PokemonSet>> validMons = new Dictionary<string, List<PokemonSet>>();
                        foreach (PokemonSet mon in Teamsheet) // Check each mon
                        {
                            foreach (string type in backEndData.Dex[mon.Species].Types) // Aggregate types
                            {
                                // Add mon to the set
                                if (validMons.ContainsKey(type))
                                {
                                    validMons[type].Add(mon);
                                }
                                else
                                {
                                    validMons[type] = [mon];
                                }
                            }
                        }
                        // Then, get the ones that can be used, for nMons, need to find the list that can be used as monotype
                        List<List<PokemonSet>> validMonotypes = validMons.Values.Where(l => l.Count >= nMons).ToList();
                        if (validMonotypes.Count == 0) throw new Exception("This player can't be monotype!");
                        // Finally, choose a random type
                        List<PokemonSet> chosenTypeSet = validMonotypes[_rng.Next(validMonotypes.Count)]; // Chose random type
                        Utilities.ShuffleList(chosenTypeSet, 0, chosenTypeSet.Count, _rng); // Shuffle the mons
                        // Finally, reorder teamsheet with those mons first
                        for (int i = 0; i < chosenTypeSet.Count; i++)
                        {
                            int currentIndex = Teamsheet.IndexOf(chosenTypeSet[i]); // Find where mon currently at, will become i
                            if (i != currentIndex)
                            {
                                (Teamsheet[i], Teamsheet[currentIndex]) = (Teamsheet[currentIndex], Teamsheet[i]); // Swap
                            }
                        }
                    }
                    // Then, for each mon, will randomize their sets
                    for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                    {
                        PokemonSet pokemonSet = Teamsheet[i];
                        pokemonSet.RandomizeAndVerify(backEndData, settings.HasFlag(TeambuildSettings.SMART));
                    }
                }
                else
                {
                    // Some verifications for some specific game modes
                    if (settings.HasFlag(TeambuildSettings.MONOTYPE)) // Need to ensure that nMons are monotype
                    {
                        Dictionary<string, int> typeCount = new Dictionary<string, int>();
                        for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                        {
                            foreach (string type in backEndData.Dex[Teamsheet[i].Species].Types) // Aggregate types
                            {
                                // Add mon to the set
                                if (typeCount.ContainsKey(type)) typeCount[type]++;
                                else typeCount[type] = 1;
                            }
                            // Then, check if there's any type with a count of nMons (neither more or less?)
                            if (!typeCount.ContainsValue(nMons)) throw new Exception("This player can't be monotype!");
                            // Otherwise all good
                        }
                    }
                }
                // Shuffle items if auto-item
                if (AutoItem)
                {
                    // First, need to remove all mon's items
                    foreach (PokemonSet monSet in Teamsheet)
                    {
                        Item monsItem = monSet.Item;
                        if (monsItem != null)
                        {
                            BattleItems.Add(monSet.Item);
                            monSet.Item = null;
                        }
                    }
                    // Then, shuffle all items
                    Utilities.ShuffleList(BattleItems, 0, BattleItems.Count, _rng);
                    // Each item will be accepted with a probability P so that the system tries to ensure a specific desired amount (e.g. 4)
                    // However if items is less that this, still try to use them sometimes with a set probability
                    const int DESIRED_FINAL_NUMBER_OF_ITEMS = 4;
                    const int BASE_ACCEPTANCE_CHANCE = 20;
                    int itemAcceptanceChance;
                    if ((BattleItems.Count - DESIRED_FINAL_NUMBER_OF_ITEMS) > nMons) itemAcceptanceChance = 100; // Since even if all mons equipped it won't reach the desired, just guarantee use
                    if (BattleItems.Count <= DESIRED_FINAL_NUMBER_OF_ITEMS) itemAcceptanceChance = BASE_ACCEPTANCE_CHANCE; // Minimum chance to always use something, sometimes
                    else itemAcceptanceChance = 100 * (1 - (DESIRED_FINAL_NUMBER_OF_ITEMS / BattleItems.Count)); // Otherwise the chance is given so around DESIRED_FINAL_NUMBER_OF_ITEMS remains
                    // Now go mon by mon, each mon has the same chance of having an item, will go item by item after
                    for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                    {
                        PokemonSet pokemonSet = Teamsheet[i];
                        Console.WriteLine($"\tItem for {pokemonSet.Species}");
                        if (BattleItems.Count > 0)
                        {
                            int roll = _rng.Next(0, 100);
                            if (roll < itemAcceptanceChance) // Randomly, try to assign one of the items to the pokemon
                            {
                                Item itemCandidate = null;
                                for (int itemIdx = 0; itemIdx < BattleItems.Count; itemIdx++) // Will try an item, one by one
                                {
                                    itemCandidate = BattleItems[itemIdx];
                                    // So if the item is ok, ill allow it
                                    if (IstemUseful(itemCandidate.Name, pokemonSet, backEndData))
                                    {
                                        break; // Otherwise I found it
                                    }
                                    else
                                    {
                                        Console.WriteLine($"\t\t{itemCandidate.Name} not useful for {pokemonSet.Species}");
                                        itemCandidate = null; // Won't use it
                                    }
                                }
                                if (itemCandidate != null)
                                {
                                    Console.WriteLine($"\t\t{pokemonSet.Species} will use {itemCandidate}");
                                    BattleItems.Remove(itemCandidate);
                                    pokemonSet.Item = itemCandidate;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"\t\t{pokemonSet.Species} wont take an item (roll {roll}<{itemAcceptanceChance})");
                            }
                        }
                        else
                        {
                            break; // Done with the items
                        }
                    }
                }
                Console.WriteLine("");
                // Finally, just print all and define some extra stuff needed regardless of auto-
                for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                {
                    PokemonSet mon = Teamsheet[i];
                    mon.ExplorationStatus = settings.HasFlag(TeambuildSettings.EXPLORATION) ? new ExplorationStatus() : null;
                    Console.WriteLine($"\tSet for {mon.ToString()}");
                    Console.WriteLine("");
                }
                Console.WriteLine("Do you approve of this team? Y/n");
                string input = Console.ReadLine();
                if (input.Trim().ToLower() != "n")
                {
                    defined = true;
                }
            }
        }
        /// <summary>
        /// Checks if an item is useful for a specific set
        /// </summary>
        /// <param name="itemName">Item to check</param>
        /// <param name="pokemonSet">Set of candidate mon</param>
        /// <param name="backEndData">Back end where extra data is obtained</param>
        /// <returns>If item would be useful</returns>
        static bool IstemUseful(string itemName, PokemonSet pokemonSet, DataContainers backEndData)
        {
            // If offensive item, (i.e. item that boosts a type), then ensure mon is packing a move of that type...
            if (backEndData.OffensiveItemData.TryGetValue(itemName, out HashSet<string> offensiveTypes))
            {
                foreach (string move in pokemonSet.Moves)
                {
                    if (move != "")
                    {
                        // Check the move
                        Move moveData = backEndData.MoveData[move];
                        if (moveData.Damaging && offensiveTypes.Contains(moveData.Type))
                        {
                            return true;
                        }
                    }
                }
            }
            // Otherwise, it may be a defensive item, which may help resisting super effective types...
            else if (backEndData.DefensiveItemData.TryGetValue(itemName, out HashSet<string> defensiveTypes))
            {
                Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Species]; // Get the mon
                foreach (string damageType in defensiveTypes) // Ensure it will be useful for at least 1 type??
                {
                    float damageTaken = 1.0f; // Damage i'd take for this type
                    foreach (string type in pokemonBackendData.Types)
                    {
                        damageTaken *= backEndData.TypeChart[type][damageType];
                    }
                    if (damageTaken > 1.1f) // If damage from this type is super effective...
                    {
                        return true;
                    }
                }
            }
            // Otherwise, it may be a moveset item, which only makes sense if the mon doesn't learn naturally (or already has it!)...
            else if (backEndData.MoveItemData.TryGetValue(itemName, out HashSet<string> learnedMoves))
            {
                Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Species];
                foreach (string learnedMove in learnedMoves) // Check if the move(s) added...
                {
                    if (!pokemonBackendData.Moves.Contains(learnedMove) && // ...are not originally learned anyway
                        !pokemonBackendData.AiMoveBanlist.Contains(learnedMove) && // ...are not banned
                        !pokemonSet.Moves.Contains(learnedMove)) // ...is not already there (???how)
                    {
                        return true; // Then this can be used
                    }
                }
            }
            // Otherwise there's no other checks, will do some personalized checks and go for it
            else
            {
                return IsSpecificItemUseful(itemName, pokemonSet, backEndData);
            }
            return false; // No specific check passed so the item is deemed useless
        }
        /// <summary>
        /// Manual hardcoded check for specific items
        /// </summary>
        /// <param name="itemName">Item Name</param>
        /// <param name="pokemonSet">Pokemon we're checking</param>
        /// <returns>Whether the item would be useful or not</returns>
        static bool IsSpecificItemUseful(string itemName, PokemonSet pokemonSet, DataContainers backEndData)
        {
            List<string> usefulMoves = new List<string>();
            List<string> usefulAbilities = new List<string>();
            switch (itemName.ToLower())
            {
                case "power herb":
                    usefulMoves = ["bounce", "dig", "dive", "electro shot", "fly", "freeze shock", "geomancy", "ice burn", "meteor beam", "phantom force", "razor wind", "shadow force", "skull bash", "sky attack", "sky drop", "solar beam", "solar blade"];
                    break;
                case "terrain extender":
                    usefulMoves = ["electric terrain", "grassy terrain", "psychic terrain", "misty terrain"];
                    usefulAbilities = ["electric surge", "hadron engine", "grassy surge", "seed sower", "misty surge", "psychic surge"];
                    break;
                case "big root":
                    usefulMoves = ["absorb", "bitter blade", "bouncy bubble", "drain punch", "draining kiss", "dream eater", "giga drain", "horn leech", "leech life", "leech seed", "matcha gotcha", "mega drain", "oblivion wing", "parabolic charge", "strength sap"];
                    break;
                case "eviolite": // This one is special, just checks if evos
                    if (backEndData.Dex[pokemonSet.Species].Evos.Count > 0) // Mon can evolve, all good
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case "flame orb":
                    usefulMoves = ["facade", "psycho shift", "trick", "switcheroo", "fling"];
                    usefulAbilities = ["flare boost", "guts", "marvel scale", "quick feet", "klutz"];
                    if (backEndData.Dex[pokemonSet.Species].Types.Contains("fire")) { return false; } // Fire types have no use for this even if guts
                    break;
                case "toxic orb":
                    usefulMoves = ["facade", "psycho shift", "trick", "switcheroo", "fling"];
                    usefulAbilities = ["poison heal", "guts", "marvel scale", "quick feet", "toxic boost", "klutz"];
                    if (backEndData.Dex[pokemonSet.Species].Types.Contains("poison")) { return false; } // Poison types have no use for this even if guts
                    break;
                case "iron ball":
                    usefulMoves = ["trick", "switcheroo", "fling", "trick room"];
                    usefulAbilities = ["klutz"];
                    break;
                case "lagging tail":
                    usefulMoves = ["trick", "switcheroo", "fling"];
                    usefulAbilities = ["klutz"];
                    break;
                case "damp rock":
                    usefulMoves = ["rain dance"];
                    usefulAbilities = ["drizzle"];
                    break;
                case "heat rock":
                    usefulMoves = ["sunny day"];
                    usefulAbilities = ["drought", "orichalcum pulse"];
                    break;
                case "icy rock":
                    usefulMoves = ["hail", "snowscape", "chilly reception"];
                    usefulAbilities = ["snow warning"];
                    break;
                case "smooth rock":
                    usefulMoves = ["sandstorm"];
                    usefulAbilities = ["sand stream", "sand spit"];
                    break;
                default:
                    return true; // Item wasn't singled out so it means its always useful
            }
            // If reach here, check all move/abilities candidates, see if item would help the pokemon somehow
            foreach (string move in usefulMoves)
            {
                if (pokemonSet.Moves.Contains(move))
                {
                    return true;
                }
            }
            foreach (string ability in usefulAbilities)
            {
                if (pokemonSet.Ability == ability)
                {
                    return true;
                }
            }
            return false; // Singled out item that didn't satisfy checks is useless
        }
        /// <summary>
        /// Gets team showdown packed data
        /// </summary>
        /// <param name="backEndData">Back end data</param>
        /// <param name="nMons">first N mons to include</param>
        /// <returns>The packed string</returns>
        public string GetPacked(DataContainers backEndData, int nMons)
        {
            List<string> eachMonPacked = new List<string>();
            for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
            {
                PokemonSet mon = Teamsheet[i];
                eachMonPacked.Add(mon.GetPacked(backEndData));
            }
            return string.Join("]", eachMonPacked); // Returns the packed data joined with ]
        }
    }
}
