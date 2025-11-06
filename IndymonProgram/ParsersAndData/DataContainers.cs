using System.Text;

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
                HealthPercentage = 0;
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
    }
    public class PokemonSet
    {
        public string NickName { get; set; }
        public string Species { get; set; }
        public string Gender { get; set; }
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
        /// Randomizes this mon's sets (ability+moves)
        /// </summary>
        /// <param name="backendData">Data to get mon's moves, etc</param>
        /// <param name="smart">Smart randomizer avoids using moves in the AI banlist</param>
        /// <param name="switchChance">Chance that the last move is empty (switch)</param>
        public void RandomizeMon(DataContainers backendData, bool smart, int switchChance)
        {
            Random _rng = new Random();
            Pokemon pokemonBackendData = backendData.Dex[Species];
            HashSet<string> legalAbilities = pokemonBackendData.Abilities.ToHashSet();
            HashSet<string> legalMoves = pokemonBackendData.Moves.ToHashSet();
            HashSet<string> legalStabs = pokemonBackendData.DamagingStabs.ToHashSet();
            if (smart)
            {
                legalAbilities.ExceptWith(pokemonBackendData.AiAbilityBanlist);
                legalMoves.ExceptWith(pokemonBackendData.AiMoveBanlist);
                legalStabs.ExceptWith(pokemonBackendData.AiMoveBanlist);
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
        /// Gets pokepaste for this mon set
        /// </summary>
        /// <param name="backEndData">Backend data to check stuff like item effects</param>
        /// <returns>Pokepaste string</returns>
        public string GetPokepaste(DataContainers backEndData)
        {
            // Now to assemble the final pokepaste
            StringBuilder resultBuilder = new StringBuilder();
            string pokemonName = (NickName != "") ? $"{NickName} ({Species})" : Species;
            string nameString = (Gender != "") ? $"{pokemonName} ({Gender.ToUpper()})" : pokemonName;
            resultBuilder.Append((GetBattleItem(backEndData) != "") ? $"{nameString} @ {GetBattleItem(backEndData)}\n" : $"{nameString}\n"); // Name
            if (GetNature(backEndData) != "") resultBuilder.Append($"{GetNature(backEndData)} Nature\n"); // Add nature if there
            if (GetTera(backEndData) != "") resultBuilder.Append($"Tera Type: {GetTera(backEndData)}\n"); // Add tera if there
            int[] evs = GetEvs(backEndData); // Load ev one by one
            List<string> allEvStrings = new List<string>();
            for (int i = 0; i < evs.Length; i++)
            {
                string evName = i switch
                {
                    0 => "HP",
                    1 => "Atk",
                    2 => "Def",
                    3 => "SpA",
                    4 => "SpD",
                    5 => "Spe",
                    _ => ""
                };
                allEvStrings.Add($"{evs[i]} {evName}");
            }
            resultBuilder.Append($"EVs: {string.Join(" / ", allEvStrings)}\n");
            resultBuilder.Append($"Ability: {Ability}\n"); // Ability
            if (Shiny) resultBuilder.Append("Shiny: Yes\n"); // Shiny
            foreach (string move in Moves) // Moves
            {
                resultBuilder.Append($"- {move}\n");
            }
            return resultBuilder.ToString();
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
            if (backEndData.MoveItemData.TryGetValue(Item.Name, out HashSet<string> overritenMoves))
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
            packedStrings.Add(Gender);
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
        /// Defines a team's team (e.g. movesets, etc), randomizes depending on auto-settings
        /// </summary>
        /// <param name="backEndData"></param>
        /// <param name="nMons">How many mons to perform this operation on</param>
        public void DefineSets(DataContainers backEndData, int nMons)
        {
            Console.WriteLine($"\tChecking {Name}'s team");
            // Shuffle teams and sets if auto-team
            if (AutoTeam)
            {
                Random _rng = new Random();
                // First, shuffle the mons
                int n = Teamsheet.Count;
                while (n > 1) // Fischer yates
                {
                    n--;
                    int k = _rng.Next(n + 1);
                    (Teamsheet[k], Teamsheet[n]) = (Teamsheet[n], Teamsheet[k]); // Swap
                }
                // Then, for each mon, will randomize sets
                for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                {
                    PokemonSet pokemonSet = Teamsheet[i];
                    bool acceptedMon = false;
                    Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Species];
                    while (!acceptedMon)
                    {
                        // Randomize mon, with a 7% chance of each move being empty (1->18%, 2->1.3%, 3->0.003%)
                        pokemonSet.RandomizeMon(backEndData, true, 7); // Randomize mon
                        // Show it to user, user will decide if redo or revise (banning sets for the future)
                        Console.WriteLine($"\t\tSet for {pokemonSet.ToString()}");
                        Console.WriteLine("\t\tTo modify AI for future: 5: ban ability. 1-4 ban moves. Otherwise this mon is approved. 0 to reroll the whole thing");
                        string inputString = Console.ReadLine().ToLower();
                        switch (inputString)
                        {
                            case "5":
                                pokemonBackendData.AiAbilityBanlist.Add(pokemonSet.Ability);
                                break;
                            case "1":
                                pokemonBackendData.AiMoveBanlist.Add(pokemonSet.Moves[0]);
                                break;
                            case "2":
                                pokemonBackendData.AiMoveBanlist.Add(pokemonSet.Moves[1]);
                                break;
                            case "3":
                                pokemonBackendData.AiMoveBanlist.Add(pokemonSet.Moves[2]);
                                break;
                            case "4":
                                pokemonBackendData.AiMoveBanlist.Add(pokemonSet.Moves[3]);
                                break;
                            case "0":
                                break; // Rejects te mon
                            default:
                                acceptedMon = true;
                                break;
                        }
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
                Random _rng = new Random();
                int n = BattleItems.Count;
                while (n > 1) // Fischer yates
                {
                    n--;
                    int k = _rng.Next(n + 1);
                    (BattleItems[k], BattleItems[n]) = (BattleItems[n], BattleItems[k]); // Swap
                }
                for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
                {
                    PokemonSet pokemonSet = Teamsheet[i];
                    if (BattleItems.Count > 0)
                    {
                        int itemAcceptanceChance = BattleItems.Count * 20; // 20% per item, so that at 5 items it always tries to use one to keep around 4 ish
                        if (_rng.Next(0, 100) < itemAcceptanceChance) // Randomly, try to assign one of the items to the pokemon
                        {
                            Item itemCandidate = null;
                            for (int itemIdx = 0; itemIdx < BattleItems.Count; itemIdx++) // Will try an item, one by one
                            {
                                itemCandidate = BattleItems[itemIdx];
                                bool itemIsUseless = true; // Item starts useless
                                // If offensive item, (i.e. item that boosts a type), then ensure mon is packing a move of that type...
                                if (backEndData.OffensiveItemData.TryGetValue(itemCandidate.Name, out HashSet<string> offensiveTypes))
                                {
                                    foreach (string move in pokemonSet.Moves)
                                    {
                                        if (move != "")
                                        {
                                            // Check the move
                                            Move moveData = backEndData.MoveData[move];
                                            if (moveData.Damaging && offensiveTypes.Contains(moveData.Type))
                                            {
                                                itemIsUseless &= false; // Flag item as useful now
                                                break;
                                            }
                                        }
                                    }
                                }
                                // Otherwise, it may be a defensive item, which may help resisting super effective types...
                                else if (backEndData.DefensiveItemData.TryGetValue(itemCandidate.Name, out HashSet<string> defensiveTypes))
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
                                            itemIsUseless &= false;
                                            break;
                                        }
                                    }
                                }
                                // Otherwise, it may be a moveset item, which only makes sense if the mon doesn't learn naturally (or already has it!)...
                                else if (backEndData.MoveItemData.TryGetValue(itemCandidate.Name, out HashSet<string> learnedMoves))
                                {
                                    Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Species];
                                    foreach (string learnedMove in learnedMoves) // Check if the move(s) added...
                                    {
                                        if (!pokemonBackendData.Moves.Contains(learnedMove) && // ...are not originally learned anyway
                                            !pokemonBackendData.AiMoveBanlist.Contains(learnedMove) && // ...are not banned
                                            !pokemonSet.Moves.Contains(learnedMove)) // ...is not already there (???how)
                                        {
                                            itemIsUseless &= false; // Then this can be used
                                        }

                                    }
                                }
                                // Otherwise there's no other checks, item has to be decent
                                else
                                {
                                    itemIsUseless &= false;
                                }
                                // So if the item is ok, ill allow it
                                if (itemIsUseless)
                                {
                                    itemCandidate = null; // Won't use it
                                }
                                else
                                {
                                    break; // Otherwise I found it
                                }
                            }
                            if (itemCandidate != null)
                            {
                                BattleItems.Remove(itemCandidate);
                                pokemonSet.Item = itemCandidate;
                            }
                        }
                    }
                    else
                    {
                        break; // Done with the items
                    }
                }
            }
            // Finally, just print all
            for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
            {
                PokemonSet mon = Teamsheet[i];
                Console.WriteLine($"\t\tSet for {mon.ToString()}");
                Console.WriteLine("");
            }
        }
        /// <summary>
        /// Gets team pokepaste
        /// </summary>
        /// <param name="backEndData">Back end data</param>
        /// <param name="nMons">first N mons to include</param>
        /// <returns>The pokepaste string</returns>
        public string GetPokepaste(DataContainers backEndData, int nMons)
        {
            StringBuilder resultBuilder = new StringBuilder();
            for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
            {
                PokemonSet mon = Teamsheet[i];
                resultBuilder.AppendLine(mon.GetPokepaste(backEndData));
            }
            return resultBuilder.ToString();
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
