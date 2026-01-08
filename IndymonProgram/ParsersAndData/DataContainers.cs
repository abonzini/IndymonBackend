using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Security.Cryptography;
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
        public int[] MovePp { get; set; } = [1000, 1000, 1000, 1000]; // Starts with a big value idc
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
        public string NickName { get; set; } = "";
        public string Species { get; set; } = "";
        public bool Shiny { get; set; } = false;
        public string Ability { get; set; } = "";
        public string[] Moves { get; set; } = ["", "", "", ""];
        public int Level { get; set; } = 100;
        public Item Item { get; set; } = null;
        public ExplorationStatus ExplorationStatus { get; set; } = null;
        /// <summary>
        /// How is the mon called in "normal conversation"
        /// </summary>
        /// <returns>How the mon will appear in screens</returns>
        public string GetInformalName()
        {
            return (NickName != "") ? NickName : Species;
        }
        public override string ToString()
        {
            return $"{Species}: {Ability}, {string.Join('-', Moves)}, {Item?.Name}";
        }
        /// <summary>
        /// Triggers randomizing of mon but with some extra manual checks and confirmations. Allows to randomize single mon instrad of whole team
        /// </summary>
        /// <param name="backEndData">Back end data needed to choose things accurately</param>
        /// <param name="settings">Battle settings to ensure team and moveset is up to standard</param>
        public void RandomizeAndVerify(DataContainers backEndData, TeambuildSettings settings)
        {
            bool acceptedMon = false;
            Pokemon pokemonBackendData = backEndData.Dex[Species];
            while (!acceptedMon)
            {
                // Randomize mon, with a 10% chance of each move being empty (for 7% was 1->18%, 2->1.3%, 3->0.003% but we changed the method to generate movesets since then)
                RandomizeMon(backEndData, settings, 10); // Randomize mon
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
        /// <param name="backEndData">Data to get mon's moves, etc</param>
        /// <param name="settings">Smart randomizer avoids using moves in the AI banlist</param>
        /// <param name="switchChance">Chance that the last move is empty (switch)</param>
        public void RandomizeMon(DataContainers backEndData, TeambuildSettings settings, int switchChance)
        {
            Pokemon pokemonBackendData;
            if (Species.ToLower().Contains("unown")) // Weird case because the species is always unown even if many aesthetic formes that are not in the dex
            {
                pokemonBackendData = backEndData.Dex["unown"];
            }
            else
            {
                pokemonBackendData = backEndData.Dex[Species];
            }
            // Get data, gets a smart set or just legal depending if randomizing is smart
            HashSet<string> legalAbilities = settings.HasFlag(TeambuildSettings.SMART) ? pokemonBackendData.GetSmartAbilities() : pokemonBackendData.GetLegalAbilities();
            HashSet<string> legalMoves = settings.HasFlag(TeambuildSettings.SMART) ? pokemonBackendData.GetSmartMoves() : pokemonBackendData.GetLegalMoves();
            HashSet<string> legalStabs = [.. pokemonBackendData.DamagingStabs.Intersect(legalMoves)]; // Legal stabs are the stabs that are legal
            if (GetTera(backEndData) != "" && pokemonBackendData.Moves.Contains("tera blast")) // Mons that can tera will be able to use tera blast regardless if previously banned move
            {
                legalMoves.Add("tera blast");
            }
            if (Species.ToLower().Contains("unown")) // And then again, weird mechanic because I can only allow the moves that start with the unown letter
            {
                char letter = Species.ToLower().Last();
                legalMoves.IntersectWith(legalMoves.Where(m => m.StartsWith(letter))); // Reduce  
            }
            // First, get the mon an ability
            Ability = legalAbilities.ElementAt(RandomNumberGenerator.GetInt32(legalAbilities.Count)); // Get a random one
            // Moves 1-4, just get random shit with a chance to switch
            for (int i = 0; i < 4; i++)
            {
                Moves[i] = ""; // Clean move first
                if (legalMoves.Count == 0) continue; // If no more moves, continue so I can clear the rest of the moveset but im done
                if (RandomNumberGenerator.GetInt32(0, 100) > switchChance) // Means the next move is not a switch, add something
                {
                    Moves[i] = legalMoves.ElementAt(RandomNumberGenerator.GetInt32(legalMoves.Count));
                    legalMoves.Remove(Moves[i]);
                }
            }
            // If mon has a move disk equipped, ensure it's in the moveset at this point, add at beginning in this case since they need to remain "safe" and not be overwritten
            int safeMoveIndex = 0; // Moves below this index are safe
            if ((Item != null) && (backEndData.MoveItemData.TryGetValue(Item.Name, out HashSet<string> overwrittenMoves)))
            {
                foreach (string newMove in overwrittenMoves)
                {
                    Move moveBackEndData = backEndData.MoveData[newMove]; // Find the move
                    if (moveBackEndData.Damaging && pokemonBackendData.Types.Contains(moveBackEndData.Type)) // If it's a damaging stab, make sure to include it!
                    {
                        legalStabs.Add(newMove);
                    }
                    Moves[safeMoveIndex] = newMove; // Replace last with move disk's
                    safeMoveIndex++; // Protect move disk move
                }
            }
            // Finally, given moveset I now need to verify it fills the requirements and moves are overwritten accordingly
            // Mechanism, theres an index where every move behind that is locked and not replaced, if  anew important move is found, it's moved to the index and saved
            // Stab check, ensure stab is safe
            if (!Moves.Any(legalStabs.Contains)) // If my moveset doesn't have an element found in legal stabs may need to add one
            {
                if (legalStabs.Count > 0) // But make sure a valid stab exists
                {
                    // Replace a move with a stab then
                    legalMoves.Add(Moves[safeMoveIndex]); // Re-add the move to the pool
                    Moves[safeMoveIndex] = legalStabs.ElementAt(RandomNumberGenerator.GetInt32(legalStabs.Count)); // Add a random stab then
                    legalMoves.Remove(Moves[safeMoveIndex]);
                    safeMoveIndex++; // Make the STAB move safe
                }
            }
            else // There's a stab, so I need to make sure it's safe by moving it if not
            {
                int firstStabIndex = Array.IndexOf(Moves, Moves.First(legalStabs.Contains)); // First the index of the first legal stab
                if (firstStabIndex > safeMoveIndex) // Ensure its protected by safe move index
                {
                    (Moves[firstStabIndex], Moves[safeMoveIndex]) = (Moves[safeMoveIndex], Moves[firstStabIndex]); // Swap
                }
                safeMoveIndex++; // STAB is safe now
            }
            // Next, check possible sets for filtering, e.g. dancer
            if (settings.HasFlag(TeambuildSettings.DANCERS))
            {
                HashSet<string> dancingAbilities = SpecificSets.GetDancingAbilities();
                HashSet<string> dancingMoves = SpecificSets.GetDancingMoves();
                // Check if the ability or move is already there
                if (dancingAbilities.Contains(Ability) || dancingMoves.Overlaps(Moves))
                {
                    if (!dancingAbilities.Contains(Ability)) // Then it's a move, protect the move
                    {
                        int dancingMoveIndex = Array.IndexOf(Moves, Moves.First(dancingMoves.Contains)); // First the index of the first legal dancing move
                        if (dancingMoveIndex > safeMoveIndex) // Ensure its protected by safe move index
                        {
                            (Moves[dancingMoveIndex], Moves[safeMoveIndex]) = (Moves[safeMoveIndex], Moves[dancingMoveIndex]); // Swap
                        }
                        safeMoveIndex++; // Dancing move is safe now
                    }
                }
                else // Need to add it then
                {
                    // What can I add?
                    HashSet<string> abilitiesICanUse = [.. legalAbilities.Intersect(dancingAbilities)];
                    HashSet<string> movesICanUse = [.. legalMoves.Intersect(dancingMoves)];
                    // I'll try ability first since dancer is good
                    if (abilitiesICanUse.Count > 0)
                    {
                        Ability = abilitiesICanUse.ElementAt(RandomNumberGenerator.GetInt32(abilitiesICanUse.Count));
                    }
                    else // Just replace a move then, replace whatever's in the safe move index (everything below is protected)
                    {
                        legalMoves.Add(Moves[safeMoveIndex]);
                        Moves[safeMoveIndex] = movesICanUse.ElementAt(RandomNumberGenerator.GetInt32(movesICanUse.Count));
                        legalMoves.Remove(Moves[safeMoveIndex]);
                        safeMoveIndex++;
                    }
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
            int[] evs = [1, 1, 1, 1, 1, 1]; // All ev's 1 so the thing doesnt annoy me
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
            //NICKNAME|SPECIES|ITEM|ABILITY|MOVES|NATURE|EVS|GENDER|IVS|SHINY|LEVEL|HAPPINESS,POKEBALL,HIDDENPOWERTYPE,GIGANTAMAX,DYNAMAXLEVEL,TERATYPE
            List<string> packedStrings = new List<string>();
            packedStrings.Add(NickName);
            packedStrings.Add(Species);
            packedStrings.Add(GetBattleItem(backEndData));
            packedStrings.Add(Ability);
            List<string> movesWithUses = new List<string>();
            if (ExplorationStatus != null) // In this case, moves have PP
            {
                for (int i = 0; i < Moves.Length; i++)
                {
                    movesWithUses.Add($"{Moves[i]}#{ExplorationStatus.MovePp[i]}"); // Add move with the number of recorded uses
                }
            }
            else // Otherwise just good old moves
            {
                movesWithUses = [.. Moves];
            }
            packedStrings.Add(string.Join(",", movesWithUses));
            packedStrings.Add(GetNature(backEndData));
            packedStrings.Add(string.Join(",", GetEvs(backEndData)));
            packedStrings.Add("");
            packedStrings.Add(""); // No IVs I don't care
            packedStrings.Add(Shiny ? "S" : ""); // Depending if shiny
            packedStrings.Add(Level.ToString()); // Mon level is "usually" 100
            string lastPackedString = $",,,,,{GetTera(backEndData)}"; // Add the "remaining" useless stuff needed for tera, etc
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
        DANCERS = 8, /// Every mon must have -dance aqua step or clangorous soul
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
        /// <param name="minNMons">Minimum needed</param>
        /// <param name="maxNMons">How many mons to perform this operation on</param>
        /// <param name="settings">Settings for teambuilding</param>
        /// <returns>All of the possible valid team comps that could be chosen for this format. May have more members than nMons so it may need to be shuffled around</returns>
        public List<List<PokemonSet>> GetValidTeamComps(DataContainers backEndData, int minNMons, int maxNMons, TeambuildSettings settings)
        {
            // Mon number check
            if (Teamsheet.Count < minNMons)
            {
                return new List<List<PokemonSet>>();
            }
            else // Valid, find out how many I actually have tho
            {
                maxNMons = Math.Min(maxNMons, Teamsheet.Count);
            }
            // Check if need to try absolutely everythign (npc) or only the first N
            List<List<PokemonSet>> possibleComps = [AutoTeam ? [.. Teamsheet] : Teamsheet.GetRange(0, maxNMons),]; // Will contain all posible comps
            // Now we filter the lists condition by condition
            // Monotype check, this will be basically monotype at random
            if (settings.HasFlag(TeambuildSettings.MONOTYPE))
            {
                List<List<PokemonSet>> newPossibleComps = new List<List<PokemonSet>>(); // Will contain all resulting possible comps
                foreach (List<PokemonSet> comp in possibleComps) // Check comp by comp
                {
                    Dictionary<string, List<PokemonSet>> typeContainingMons = new Dictionary<string, List<PokemonSet>>(); // Will filter by types
                    foreach (PokemonSet mon in comp) // Check each mon in this comp
                    {
                        foreach (string type in backEndData.Dex[mon.Species].Types) // Aggregate the types
                        {
                            // Add mon to the set
                            if (typeContainingMons.TryGetValue(type, out List<PokemonSet> foundType))
                            {
                                foundType.Add(mon);
                            }
                            else
                            {
                                typeContainingMons[type] = [mon];
                            }
                        }
                    }
                    // Then, get the ones that can be used, for nMons, need to find the list that can be used as monotype, add to the possible comps
                    newPossibleComps.AddRange([.. typeContainingMons.Values.Where(l => l.Count >= minNMons)]); // Add all monotypes
                }
                possibleComps = newPossibleComps; // Replace old filtered with new
            }
            if (settings.HasFlag(TeambuildSettings.DANCERS))
            {
                HashSet<string> abilitiesToVerify = SpecificSets.GetDancingAbilities();
                HashSet<string> movesToVerify = SpecificSets.GetDancingMoves();
                List<List<PokemonSet>> newPossibleComps = new List<List<PokemonSet>>(); // Will contain all resulting possible comps
                foreach (List<PokemonSet> comp in possibleComps) // Check comp by comp
                {
                    List<PokemonSet> filteredComp = new List<PokemonSet>();
                    foreach (PokemonSet mon in comp) // Mon is valid in certain conditions
                    {
                        bool monIsValid = false;
                        // In here, we need to determine if mon already has a moveset defined or not!
                        if (AutoTeam) // This means the pokemon will be potentially randomized later so just need to check species data
                        {
                            // Check if mon contains ability, move or move disk, considering auto team tries to be "smart"
                            Pokemon monData = backEndData.Dex[mon.Species];
                            HashSet<string> validAbilities = abilitiesToVerify.Intersect(monData.GetSmartAbilities()).ToHashSet();
                            HashSet<string> validMoves = movesToVerify.Intersect(monData.GetSmartMoves()).ToHashSet();
                            monIsValid |= validAbilities.Count > 0;
                            monIsValid |= validMoves.Count > 0;
                            // In any case, even if not naturally learned, the mon may have a move disk equipped that will give it
                            if (mon.Item != null && !AutoItem && backEndData.MoveItemData.TryGetValue(mon.Item.Name, out HashSet<string> moveDiskMoves))
                            {
                                monIsValid |= movesToVerify.Overlaps(moveDiskMoves);
                            }
                        }
                        else // Mon already has a defined set (including move items) so just check if any is present
                        {
                            monIsValid |= abilitiesToVerify.Contains(mon.Ability);
                            monIsValid |= movesToVerify.Overlaps(mon.Moves);
                        }
                        // Finished checking dance moves
                        if (monIsValid)
                        {
                            filteredComp.Add(mon);
                        }
                    }
                    if (filteredComp.Count >= minNMons) // If the resulting filtered comp can form a team, then add it to result
                    {
                        newPossibleComps.Add(filteredComp);
                    }
                }
                possibleComps = newPossibleComps; // Replace old filtered with new
            }
            return possibleComps; // Return all generated possible comps
        }
        /// <summary>
        /// Defines a team's team (e.g. movesets, etc), randomizes depending on auto-settings. ASSUMES THE TEAM IS VALID FOR THE DESIRED FORMAT
        /// </summary>
        /// <param name="backEndData"></param>
        /// <param name="minNMons">Minimum needed</param>
        /// <param name="maxNMons">How many mons to perform this operation on</param>
        /// <param name="settings">Settings for teambuilding</param>
        public void ConfirmSets(DataContainers backEndData, int minNMons, int maxNMons, TeambuildSettings settings)
        {
            Console.WriteLine($"Checking {Name}'s team");
            bool defined = false;
            maxNMons = Math.Min(maxNMons, Teamsheet.Count); // No need to deal with infinity numbers if I know how many mons I have max
            while (!defined)
            {
                if (AutoItem)
                {
                    // First, need to remove all mon's items, as they are used for some specific checks
                    foreach (PokemonSet monSet in Teamsheet)
                    {
                        Item monsItem = monSet.Item;
                        if (monsItem != null)
                        {
                            BattleItems.Add(monSet.Item);
                            monSet.Item = null;
                        }
                    }
                }
                // Shuffle teams and sets if auto-team
                if (AutoTeam)
                {
                    // First, get all the possible team comps that are legal for this format, choose a random one, and then shuffle the mons
                    List<List<PokemonSet>> legalComps = GetValidTeamComps(backEndData, minNMons, maxNMons, settings);
                    List<PokemonSet> chosenSet = legalComps[RandomNumberGenerator.GetInt32(legalComps.Count)];
                    Utilities.ShuffleList(chosenSet, 0, chosenSet.Count);
                    // Now make sure the sets have the mons in order
                    for (int i = 0; i < chosenSet.Count; i++)
                    {
                        int currentIndex = Teamsheet.IndexOf(chosenSet[i]); // Find where mon currently at, will become i
                        if (i != currentIndex)
                        {
                            (Teamsheet[i], Teamsheet[currentIndex]) = (Teamsheet[currentIndex], Teamsheet[i]); // Swap
                        }
                    }
                    // Finally, for each mon, will randomize their sets
                    for (int i = 0; i < maxNMons; i++)
                    {
                        PokemonSet pokemonSet = Teamsheet[i];
                        pokemonSet.RandomizeAndVerify(backEndData, settings);
                    }
                }
                // Shuffle items if auto-item
                if (AutoItem)
                {
                    // Then, shuffle all items
                    Utilities.ShuffleList(BattleItems, 0, BattleItems.Count);
                    // Each item will be accepted with a probability P so that the system tries to ensure a specific desired amount (e.g. 4)
                    // However if items is less that this, still try to use them sometimes with a set probability
                    const int DESIRED_FINAL_NUMBER_OF_ITEMS = 4;
                    const int BASE_ACCEPTANCE_CHANCE = 20;
                    int itemAcceptanceChance;
                    if (BattleItems.Count <= DESIRED_FINAL_NUMBER_OF_ITEMS) itemAcceptanceChance = BASE_ACCEPTANCE_CHANCE; // Minimum chance to always use something, sometimes
                    else if ((BattleItems.Count - DESIRED_FINAL_NUMBER_OF_ITEMS) > maxNMons) itemAcceptanceChance = 100; // Since even if all mons equipped it won't reach the desired, just guarantee use
                    else itemAcceptanceChance = 100 * (1 - (DESIRED_FINAL_NUMBER_OF_ITEMS / BattleItems.Count)); // Otherwise the chance is given so around DESIRED_FINAL_NUMBER_OF_ITEMS remains
                    // Now go mon by mon, each mon has the same chance of having an item, will go item by item after
                    for (int i = 0; i < maxNMons; i++)
                    {
                        PokemonSet pokemonSet = Teamsheet[i];
                        Console.WriteLine($"\tItem for {pokemonSet.Species}");
                        if (BattleItems.Count > 0)
                        {
                            int roll = RandomNumberGenerator.GetInt32(0, 100);
                            if (roll < itemAcceptanceChance) // Randomly, try to assign one of the items to the pokemon
                            {
                                Item itemCandidate = null;
                                for (int itemIdx = 0; itemIdx < BattleItems.Count; itemIdx++) // Will try an item, one by one
                                {
                                    itemCandidate = BattleItems[itemIdx];
                                    // So if the item is ok, ill allow it
                                    if (IsItemUseful(itemCandidate.Name, pokemonSet, backEndData))
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
                                    // If I just equipped an item that changes set, need to add it
                                    if (backEndData.MoveItemData.TryGetValue(itemCandidate.Name, out HashSet<string> overwrittenMoves))
                                    {
                                        int overWrittenMoveSlot = 3; // Start with last
                                        foreach (string newMove in overwrittenMoves)
                                        {
                                            pokemonSet.Moves[overWrittenMoveSlot] = newMove; // Replace last with move disk's
                                            overWrittenMoveSlot--;
                                        }
                                    }
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
                for (int i = 0; i < maxNMons; i++)
                {
                    PokemonSet mon = Teamsheet[i];
                    mon.ExplorationStatus = settings.HasFlag(TeambuildSettings.EXPLORATION) ? new ExplorationStatus() : null;
                    Console.WriteLine($"\tSet for {mon}");
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
        static bool IsItemUseful(string itemName, PokemonSet pokemonSet, DataContainers backEndData)
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
            List<string> usefulMoves;
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
                case "binding band":
                    usefulMoves = ["bind", "clamp", "fire spin", "infestation", "magma storm", "sand tomb", "snap trap", "thunder cage", "whirlpool", "wrap"];
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
        /// <summary>
        /// After an event end, lists what items were consumed and the number of remaining uses
        /// </summary>
        /// <param name="nMons">First N mons to check</param>
        /// <returns>The string helping indymon organiser to update the excel sheets</returns>
        public string ListConsumedItems(int nMons)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < nMons && i < Teamsheet.Count; i++)
            {
                PokemonSet pokemonSet = Teamsheet[i];
                if (pokemonSet.Item != null)
                {
                    if (pokemonSet.Item.Uses > 1)
                    {
                        builder.AppendLine($"{pokemonSet.Species}'s {pokemonSet.Item.Name} now has {pokemonSet.Item.Uses - 1} uses left");
                    }
                    else
                    {
                        builder.AppendLine($"{pokemonSet.Species}'s {pokemonSet.Item.Name} is now consumed");
                    }
                }
            }
            return builder.ToString();
        }
    }
}
