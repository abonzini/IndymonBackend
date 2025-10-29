using ParsersAndData;
using System.Text;

namespace IndymonBackend
{
    public class DataContainers
    {
        public string MasterDirectory = "";
        public Dictionary<string, Pokemon> Dex { get; set; } = null;
        public Dictionary<string, Dictionary<string, float>> TypeChart { get; set; } = null;
        public Dictionary<string, Move> MoveData { get; set; } = null;
        public Dictionary<string, HashSet<string>> OffensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> DefensiveItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> NatureItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> EvItemData { get; set; } = null;
        public Dictionary<string, HashSet<string>> TeraItemData { get; set; } = null;
        public Dictionary<string, TrainerData> TrainerData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NpcData { get; set; } = new Dictionary<string, TrainerData>();
        public Dictionary<string, TrainerData> NamedNpcData { get; set; } = new Dictionary<string, TrainerData>();
        public TournamentManager TournamentManager { get; set; } = null;
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
    public class PokemonSet
    {
        public string Name { get; set; }
        public bool Shiny { get; set; }
        public string Ability { get; set; }
        public string[] Moves { get; set; } = new string[4];
        public Item Item { get; set; }

        public override string ToString()
        {
            return $"{Name}: {Ability}, {Moves[0]}-{Moves[1]}-{Moves[2]}-{Moves[3]}, {Item?.Name}";
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
            Pokemon pokemonBackendData = backendData.Dex[Name];
            HashSet<string> legalAbilities = pokemonBackendData.Abilities.ToHashSet();
            HashSet<string> legalMoves = pokemonBackendData.Moves.ToHashSet();
            HashSet<string> legalStabs = pokemonBackendData.DamagingStabs.ToHashSet();
            if (smart)
            {
                legalAbilities.ExceptWith(pokemonBackendData.AiAbilityBanlist);
                legalMoves.ExceptWith(pokemonBackendData.AiMoveBanlist);
                legalStabs.ExceptWith(pokemonBackendData.AiMoveBanlist);
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
        /// Gets pokepaste for this mon set
        /// </summary>
        /// <param name="backEndData">Backend data to check stuff like item effects</param>
        /// <returns>Pokepaste string</returns>
        public string GetPokepaste(DataContainers backEndData)
        {
            // Add all relevant data here
            bool hasItemEquipped = false;
            string itemEffectString = ""; // Strings that may or may not be there depending on mon's item
            string evString = "";
            HashSet<string> auxItemSet;
            if (Item != null) // If _some_ item is there, then something's going on
            {
                if (backEndData.NatureItemData.TryGetValue(Item.Name, out auxItemSet)) // If the item is a nature-setting item
                {
                    itemEffectString = $"{auxItemSet.FirstOrDefault()} Nature\n";
                }
                else if (backEndData.TeraItemData.TryGetValue(Item.Name, out auxItemSet)) // Maybe tera?
                {
                    itemEffectString = $"Tera Type: {auxItemSet.FirstOrDefault()}\n";
                }
                else if (backEndData.EvItemData.TryGetValue(Item.Name, out auxItemSet)) // Maybe ev?
                {
                    List<string> allEvStrings = new List<string>();
                    foreach (string statName in auxItemSet)
                    {
                        string statEvString = statName.ToLower() switch
                        {
                            "attack" => "Atk",
                            "defense" => "Def",
                            "special attack" => "SpA",
                            "special defense" => "SpD",
                            "hp" => "HP",
                            "speed" => "Spe",
                            _ => ""
                        };
                        allEvStrings.Add($"50 {statEvString}");
                    }
                    evString = $"EVs: {string.Join(" / ", allEvStrings)}\n";
                }
                else // If no weird item, then it's just a usable item I guess
                {
                    hasItemEquipped = true;
                }
            }
            if (evString == "") // If ev string has not been used, then add 1 ev to avoid showdown bitching
            {
                evString = $"EVs: 1 HP\n";
            }
            // Now to assemble the final pokepaste
            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.Append(hasItemEquipped ? $"{Name} @ {Item.Name}\n" : $"{Name}\n");
            resultBuilder.Append(itemEffectString);
            resultBuilder.Append(evString);
            resultBuilder.Append($"Ability: {Ability}\n");
            if (Shiny) resultBuilder.Append("Shiny: Yes\n");
            foreach (string move in Moves)
            {
                resultBuilder.Append($"- {move}\n");
            }
            return resultBuilder.ToString();
        }
    }
    public class TrainerData
    {
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
                    Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Name];
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
                                    Pokemon pokemonBackendData = backEndData.Dex[pokemonSet.Name]; // Get the mon
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
    }
}
