using AutomatedTeamBuilder;
using GameData;
using GameDataContainer;
using MechanicsData;
using MechanicsDataContainer;
using ShowdownBot;
using Utilities;

namespace IndymonBackendProgram
{
    public enum ExplorationStepType
    {
        NOP, // No operation, just a pause
        DEFINE_DUNGEON,
        PRINT_STRING,
        PRINT_CLUE,
        PRINT_PLOT,
        PRINT_EVOLUTION,
        CLEAR_CONSOLE,
        DRAW_ROOM,
        MOVE_CHARACTER,
        CONNECT_ROOMS_PASSAGE,
        CONNECT_ROOMS_SHORTCUT,
        ADD_INFO_COLUMN,
        ADD_INFO_VALUE,
        CLEAR_ROOMS,
        DRAW_REGI_EYE
    }
    public enum CardinalDirections
    {
        NORTH,
        SOUTH,
        WEST,
        EAST
    }
    public class ExplorationStep // All needed to make an animation
    {
        public ExplorationStepType Type { get; set; } // Which command
        public string StringParam { get; set; } // Parameter to pass. Strings containing $1 will be replaced by this
        public int IntParam { get; set; } // Parameter to pass.
        public (int, int) SourceCoord { get; set; } // Coord of the source room when moving
        public (int, int) DestCoord { get; set; } // Coord of destination room when moving
        public int MillisecondsWait { get; set; }
        public override string ToString()
        {
            return Type.ToString();
        }
    }
    public class ExplorationPrizes
    {
        public Dictionary<int, Dictionary<string, int>> MonsFound = []; // Mons divided by rank
        public Dictionary<SetItem, int> SetItemsFound = new Dictionary<SetItem, int>();
        public Dictionary<Item, int> ModItemsFound = new Dictionary<Item, int>();
        public Dictionary<Item, int> BattleItemsFound = new Dictionary<Item, int>();
        public Dictionary<Trainer, int> FavoursFound = new Dictionary<Trainer, int>();
        public int ImpFound = 0;
        public Dictionary<string, int> KeyItemsFound = new Dictionary<string, int>();
        /// <summary>
        /// Adds a specific number of a specific item. Can also deal with rewards but not ideal, but necessary for when the reward is a random string
        /// </summary>
        /// <param name="rewardName">Item name</param>
        /// <param name="count">How many to add</param>
        public void AddReward(string rewardName, int count)
        {
            switch (IndymonUtilities.GetRewardType(rewardName))
            {
                case IndymonUtilities.RewardType.SET:
                    SetItem theSetItem = IndymonUtilities.GetOrCreateSetItem(rewardName);
                    GeneralUtilities.AddtemToCountDictionary(SetItemsFound, theSetItem, count);
                    break;
                case IndymonUtilities.RewardType.MOD:
                    GeneralUtilities.AddtemToCountDictionary(ModItemsFound, MechanicsDataContainers.GlobalMechanicsData.ModItems[rewardName], count);
                    break;
                case IndymonUtilities.RewardType.BATTLE:
                    GeneralUtilities.AddtemToCountDictionary(BattleItemsFound, MechanicsDataContainers.GlobalMechanicsData.BattleItems[rewardName], count);
                    break;
                case IndymonUtilities.RewardType.FAVOUR:
                    GeneralUtilities.AddtemToCountDictionary(FavoursFound, IndymonUtilities.GetTrainerByName(rewardName), count);
                    break;
                case IndymonUtilities.RewardType.IMP:
                    AddImp(int.Parse(rewardName.ToLower().Replace("imp", ""))); // Imp without IMP
                    break;
                case IndymonUtilities.RewardType.POKEMON: // Pokemon are added as key items to be added immediately to party
                default:
                    GeneralUtilities.AddtemToCountDictionary(KeyItemsFound, rewardName, count);
                    break;
            }
        }
        /// <summary>
        /// Adds imp to the prizes
        /// </summary>
        /// <param name="imp">How much</param>
        public void AddImp(int imp)
        {
            ImpFound += imp;
        }
        /// <summary>
        /// Adds pokemon to prizes
        /// </summary>
        /// <param name="mon">Mon to add</param>
        /// <param name="floor">Floor to add to</param>
        public void AddMon(TrainerPokemon mon, int rank)
        {
            if (!MonsFound.TryGetValue(rank, out Dictionary<string, int> rankMons))
            {
                rankMons = new Dictionary<string, int>();
                MonsFound.Add(rank, rankMons);
            }
            string monName = mon.Species;
            if (mon.IsShiny) monName += "✦"; // Add shiny tag too
            GeneralUtilities.AddtemToCountDictionary(rankMons, monName, 1);
        }
        /// <summary>
        /// Adds pokemon to prizes
        /// </summary>
        /// <param name="mons">Mons to add</param>
        /// <param name="floor">Floor to add to</param>
        public void AddMons(List<TrainerPokemon> mons, int rank)
        {
            foreach (TrainerPokemon mon in mons) AddMon(mon, rank);
        }
        /// <summary>
        /// Transfers everything to trainer
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        public void TransferToTrainer(Trainer trainer)
        {
            foreach (KeyValuePair<SetItem, int> kvp in SetItemsFound) GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, kvp.Key, kvp.Value);
            foreach (KeyValuePair<Item, int> kvp in ModItemsFound) GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, kvp.Key, kvp.Value);
            foreach (KeyValuePair<Item, int> kvp in BattleItemsFound) GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, int> kvp in KeyItemsFound) GeneralUtilities.AddtemToCountDictionary(trainer.KeyItems, kvp.Key, kvp.Value);
            foreach (KeyValuePair<Trainer, int> kvp in FavoursFound) GeneralUtilities.AddtemToCountDictionary(trainer.Favours, kvp.Key, kvp.Value);
        }
    }
    public class ExplorationContext
    {
        public int ShinyChance = 500; // Chance for a shiny (1 in 500)
    }
    public class ExplorationManager
    {
        // Private data
        Dungeon _dungeonData = null;
        readonly ExplorationPrizes _prizes = new ExplorationPrizes();
        readonly ExplorationContext _context = new ExplorationContext();
        Trainer _explorer;
        // Public data (saved)
        public string Dungeon { get; set; }
        public (string, int) TrainerAndSeed { get; set; }
        public List<ExplorationStep> ExplorationSteps { get; set; } = new List<ExplorationStep>();
        public string DirectoryPath { get; set; } = "";
        public void InitializeExploration()
        {
            // First ask organizer to choose dungeon
            List<string> dungeonOptions = [.. GameDataContainers.GlobalGameData.Dungeons.Keys];
            Console.WriteLine("Creating a brand new exploration, which dungeon? (0 for random)");
            for (int i = 0; i < dungeonOptions.Count; i++)
            {
                Console.Write($"{i + 1}: {dungeonOptions[i]}, ");
            }
            Console.WriteLine("");
            int selection = int.Parse(Console.ReadLine());
            if (selection == 0)
            {
                selection = GeneralUtilities.GetRandomNumber(dungeonOptions.Count);
            }
            else
            {
                selection--; // Make it array-indexable
            }
            Dungeon = dungeonOptions[selection];
            Console.WriteLine(Dungeon);
            // Then which player
            List<string> validTrainers = [.. GameDataContainers.GlobalGameData.TrainerData.Keys];
            Console.WriteLine("Which trainer will explore?");
            for (int idx = 1; idx <= validTrainers.Count; idx++)
            {
                Console.Write($"{idx}-{validTrainers[idx - 1]} ");
            }
            int choice = int.Parse(Console.ReadLine());
            TrainerAndSeed = (validTrainers[choice - 1], GeneralUtilities.GetRandomNumber());
        }
        const int MIN_MESSAGE_PAUSE = 5000; // Show text for this amount of time min
        const int MESSAGE_PAUSE_PER_WORD = 300; // 0.3s per word looks reasonable
        const int DRAW_ROOM_PAUSE = 1000; // Show text for this amount of time
        const int DUNGEON_NUMBER_OF_FLOORS = 3; // Hardcoded for now unless we need to make it flexible later on
        const int DUNGEON_ROOMS_PER_FLOOR = 5;
        #region EXECUTION
        /// <summary>
        /// Begins an exploration, starts a simulation
        /// </summary>
        public void ExecuteExploration()
        {
            GameDataContainers.GlobalGameData.CurrentEventMessage.Clear(); // This is a new event, so I will clear whatever thet was there before
            // Just to help debug
            Console.Clear();
            Console.CursorVisible = true;
            // Initialize from beginning
            _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
            _explorer = IndymonUtilities.GetTrainerByName(TrainerAndSeed.Item1); // Find the trainer data
            const int MAX_N_MONS = 6;
            List<PossibleTeamBuild> possibleBuilds = TeamBuilder.GetTrainersPossibleBuilds(_explorer, MAX_N_MONS, [new Constraint()], true); // Since there's no constraint, will get a build consisting of all my mons (max 6)
            TeamBuilder.AssembleTrainersBattleTeam(_explorer, MAX_N_MONS, possibleBuilds, true, TrainerAndSeed.Item2); // Chooses one of the sets, prepares the mons
            // Now, make the trainer choose it's team sets!
            HashSet<Pokemon> enemyMons = new HashSet<Pokemon>();
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[0].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]); // Add all mons from all floors
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[1].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[2].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[3].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            TeamBuilder.DefineTrainerSets(_explorer, true, _dungeonData.DungeonArchetypes, _dungeonData.DungeonWeather, _dungeonData.DungeonTerrain, new Constraint(), [.. enemyMons], TrainerAndSeed.Item2); // Team build but with the dungeon's weather and such 
            List<RoomEvent> possibleEvents = [.. _dungeonData.Events]; // These are the possible events for this dungeon
            // Beginning of expl and event queue
            AddInfoColumnCommand("Pokemon", 18);
            AddInfoColumnCommand("Health", 6);
            AddInfoColumnCommand("Status", 6);
            bool explorationFinished = true;
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventTitle = $"<@{_explorer.DiscordNumber}> Meanwhile, {_explorer.Name} went on to explore the {Dungeon}.";
            do
            {
                SetDungeonCommand(Dungeon);
                ClearRoomsCommand(); // Just in case, just clear the current screen (if coming from another exploration)
                ClearConsoleCommand(); // Clears mini console to leave space for new event's message
                string auxString = $"Beginning of {_explorer.Name}'s exploration in {Dungeon}";
                Console.WriteLine(auxString);
                GenericMessageCommand(auxString); // Begin of exploration string
                (int, int) prevCoord = (-1, 0); // Starts from outside i guess
                bool usedShortcut = false; // If a shortcut was used to the new room
                // Adds status table. Pokemon have a max of 18 characters, health and status max 3. Fill the info too
                UpdateTrainerDataInfo();
                for (int floor = 0; floor < DUNGEON_NUMBER_OF_FLOORS; floor++) // Begin the iteration of all floors
                {
                    for (int room = 0; room < DUNGEON_ROOMS_PER_FLOOR; room++) // Begin the iteration of all rooms
                    {
                        ClearConsoleCommand(); // Clears mini console to leave space for new event's message
                        DrawRoomCommand(floor, room); // Will draw the room
                        DrawConnectRoomCommand(prevCoord.Item1, prevCoord.Item2, floor, room, usedShortcut); // Draws previous connection too
                        DrawMoveCharacterCommand(prevCoord.Item1, prevCoord.Item2, floor, room); // Put character there
                        NopWithWaitCommand(DRAW_ROOM_PAUSE);
                        prevCoord = (floor, room); // For drawing
                        usedShortcut = false; // Reset shortcut
                        bool roomSuccess;
                        if (room == 0) // Room 0 is always the beginning of floor, camping event followed by shortcut check
                        {
                            // Check shortcut first
                            ClueMessageCommand(_dungeonData.Floors[floor].ShortcutClue);
                            if (VerifyShortcutConditions(_dungeonData.Floors[floor].ShortcutConditions, out string message)) // If shortcut activated
                            {
                                // After, need to also check shortcut
                                string shortcutString = _dungeonData.Floors[floor].ShortcutResolution.Replace("$1", message);
                                GenericMessageCommand(shortcutString);
                                if (floor == DUNGEON_NUMBER_OF_FLOORS - 1) // If dungeon was done in last floor, then the dungeon is over...
                                {
                                    DrawConnectRoomCommand(floor, room, floor + 1, room, true); // Connects to invisible next dungeon, shortcut
                                    DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                                    auxString = $"You move onward...";
                                    Console.WriteLine(auxString);
                                    GenericMessageCommand(auxString);
                                    // Also the trypical backend stuff
                                    explorationFinished = false; // Will repeat exploration loop now
                                    Dungeon = _dungeonData.NextDungeonShortcut; // Go to the dungeon indicated by shortcut
                                    _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
                                    goto ExplorationEnd; // Just break these loops
                                }
                                else
                                {
                                    usedShortcut = true;
                                    break; // Floor is done, can skip all the rooms
                                }
                            }
                            // If no shortcut taken, can do a camping event at this stage
                            roomSuccess = ExecuteEvent(_dungeonData.CampingEvent, floor);
                        }
                        else if (room == 1) // And first room always a wild pokemon encounter
                        {
                            RoomEvent pokemonEvent = new RoomEvent()
                            {
                                EventType = RoomEventType.POKEMON_BATTLE,
                                PreEventString = "Suddenly, wild pokemon attack!",
                                PostEventString = $"You won the battle and obtained multiple items that the wild Pokemon were holding ($1)."
                            }; // Wild pokemon encounter event
                            roomSuccess = ExecuteEvent(pokemonEvent, floor);
                        }
                        else if ((room == DUNGEON_ROOMS_PER_FLOOR - 1) && (floor == DUNGEON_NUMBER_OF_FLOORS - 1)) // Last room is always boss event
                        {
                            ExecuteEvent(_dungeonData.PreBossEvent, floor); // Pre boss event
                            roomSuccess = ExecuteEvent(_dungeonData.BossEvent, floor); // BOSS
                            if (roomSuccess) // If has been beaten, then dungeon is also over
                            {
                                ExecuteEvent(_dungeonData.PostBossEvent, floor); // Post boss event
                                DrawConnectRoomCommand(floor, room, floor + 1, room, false); // Connects to invisible next dungeon, no shortcut
                                DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                                auxString = $"You move onward...";
                                Console.WriteLine(auxString);
                                GenericMessageCommand(auxString);
                                // Also the typical backend stuff
                                explorationFinished = false; // Will repeat exploration loop now
                                Dungeon = _dungeonData.NextDungeon; // Go to the next dungeon
                                _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
                                goto ExplorationEnd;
                            }
                        }
                        else // Normal room implies a normal event from the possibility list
                        {
                            RoomEvent nextEvent = GeneralUtilities.GetRandomPick(possibleEvents); // Get a random event
                            Console.WriteLine($"Event: {nextEvent}");
                            roomSuccess = ExecuteEvent(nextEvent, floor);
                            possibleEvents.Remove(nextEvent); // Remove from event pool
                        }
                        if (!roomSuccess) // Player lost during exploration
                            goto ExplorationEnd; // Just go directly to end
                    }
                }
            ExplorationEnd: // Best way to break from a 2 damn nested loops I think
                ;
            } while (!explorationFinished); // Will do once unless need to continue with another dungeon
            // Return to normal
            Console.CursorVisible = false;
            Console.WriteLine("Exploration end.");
            // Add items from prizes -> inventory
            _prizes.TransferToTrainer(_explorer);
            // Consume items
            IndymonUtilities.ConsumeTrainersItems(_explorer);
            // Save the copiable exploration file
            IndymonUtilities.WarnTrainer(_explorer);//Warn trainer of the exceeded items
            SaveExplorationOutcome(); // Replace with message stuff
            string trainerFilePath = Path.Combine(DirectoryPath, $"{_explorer.Name.ToUpper().Replace(" ", "").Replace("?", "")}.trainer");
            _explorer.SaveTrainerCsv(trainerFilePath);
        }
        /// <summary>
        /// Executes an event of the many possible in dungeon
        /// </summary>
        /// <param name="roomEvent">Event to simulate</param>
        /// <param name="floor">Floor where event happens</param>
        /// <returns></returns>
        bool ExecuteEvent(RoomEvent roomEvent, int floor)
        {
            bool roomCleared = true;
            Console.WriteLine(roomEvent.ToString());
            switch (roomEvent.EventType)
            {
                case RoomEventType.CAMPING:
                    Console.WriteLine("Camping tile, nothing yet");
                    GenericMessageCommand(roomEvent.PreEventString);
                    // Camping logic would go here?
                    GenericMessageCommand(roomEvent.PostEventString);
                    break;
                case RoomEventType.TREASURE: // Find an item in the floor, free rare item
                    {
                        ItemReward itemFound = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Find a random rare item
                        int amount = GeneralUtilities.GetRandomNumber(itemFound.Min, itemFound.Max + 1); // How many were found
                        Console.WriteLine($"Finds {itemFound.Name}");
                        string itemString = roomEvent.PreEventString.Replace("$1", $"{itemFound.Name} (x{amount})");
                        GenericMessageCommand(itemString); // Prints the message but we know it could have a $1
                        itemString = roomEvent.PostEventString.Replace("$1", $"{itemFound.Name} (x{amount})");
                        GenericMessageCommand(itemString);
                        _prizes.AddReward(itemFound.Name, amount);
                    }
                    break;
                case RoomEventType.BOSS: // Boss fight
                    {
                        int enemyFloor = 3; // Last floor is where bosses are
                        int itemCount = GeneralUtilities.GetRandomNumber(_dungeonData.BossItem.Min, _dungeonData.BossItem.Max + 1); // How much of an item I gained
                        string enemySpecies = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[enemyFloor]); // Boss will be a random one
                        Trainer bossTrainer = GenerateEnemyTrainer("Boss", [enemySpecies], [_dungeonData.BossItem.Name], 100, 100, true);
                        DefineEnemySet(bossTrainer, int.MaxValue, true); // Defines the enemy set (smart for a final boss challenge!)
                        string bossString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                        // Fight and conclusion
                        int remainingMons = ResolveEncounter(_explorer, bossTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            bossString = roomEvent.PostEventString.Replace("$1", _dungeonData.BossItem.Name);
                            GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                            _prizes.AddReward(_dungeonData.BossItem.Name, itemCount);
                            _prizes.AddMons(bossTrainer.BattleTeam, enemyFloor + 1); // Rank 4
                        }
                    }
                    break;
                case RoomEventType.ALPHA: // Find a smart and frenzied mon from a floor above, boss will have a rare item if defeated
                    {
                        ItemReward item = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Get a random rare item
                        int itemAmount = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1);
                        int enemyFloor = (floor + 1 >= DUNGEON_NUMBER_OF_FLOORS) ? floor : floor + 1; // Find enemy of next floor if possible
                        string enemySpecies = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[enemyFloor]); // Get a random one of these
                        string alphaString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        Trainer alphaTrainer = GenerateEnemyTrainer("Alpha", [enemySpecies], [item.Name], 100, 100, true);
                        DefineEnemySet(alphaTrainer, int.MaxValue, true); // Defines the enemy set (smart for an alpha challenge)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, alphaTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            alphaString = roomEvent.PostEventString.Replace("$1", item.Name);
                            GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                            _prizes.AddReward(item.Name, itemAmount);
                            _prizes.AddMons(alphaTrainer.BattleTeam, enemyFloor + 1);
                        }
                    }
                    break;
                case RoomEventType.EVO:
                    {
                        EvolutionMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            Pokemon baseMon = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
                            if (baseMon.Evos.Count > 0) // Mon has evos, ask for each
                            {
                                Console.WriteLine($"Evolve {mon} ? y/N. Consider:");
                                Console.WriteLine($"Mon Items: (0) Set: {mon.SetItem}, (1) Mod: {mon.ModItem}, (2) Battle: {mon.BattleItem}");
                                Console.WriteLine($"(3) Set Items In Bag: {string.Join(", ", _explorer.SetItems.Keys.Select(i => i.Name))}");
                                Console.WriteLine($"(4) Mod Items In Bag: {string.Join(", ", _explorer.ModItems.Keys.Select(i => i.Name))}");
                                Console.WriteLine($"(5) Battle Items In Bag: {string.Join(", ", _explorer.BattleItems.Keys.Select(i => i.Name))}");
                                Console.WriteLine($"(6) Set Items In Prizes: {string.Join(", ", _prizes.SetItemsFound.Keys.Select(i => i.Name))}");
                                Console.WriteLine($"(7) Mod Items In Prizes: {string.Join(", ", _prizes.ModItemsFound.Keys.Select(i => i.Name))}");
                                Console.WriteLine($"(8) Battle Items In Prizes: {string.Join(", ", _prizes.BattleItemsFound.Keys.Select(i => i.Name))}");
                                if (Console.ReadLine().Trim().ToLower() == "y")
                                {
                                    // Check mon
                                    List<Pokemon> possibleEvos = [.. baseMon.Evos];
                                    Console.Write($"0 RANDOM,");
                                    for (int i = 0; i < possibleEvos.Count; i++)
                                    {
                                        Console.Write($"{i + 1} {possibleEvos[i]},");
                                    }
                                    int choice = int.Parse(Console.ReadLine());
                                    Pokemon chosenMon;
                                    if (choice == 0) // Random evo
                                    {
                                        chosenMon = possibleEvos[GeneralUtilities.GetRandomNumber(possibleEvos.Count)];
                                    }
                                    else
                                    {
                                        chosenMon = possibleEvos[choice - 1];
                                    }
                                    // Check consumed item
                                    Console.WriteLine("Which index of place to remove item?");
                                    choice = int.Parse(Console.ReadLine());
                                    string itemConsumedString;
                                    if (choice == 0)
                                    {
                                        itemConsumedString = $"its equipped {mon.SetItem.Name}";
                                        mon.SetItem = null;
                                    }
                                    else if (choice == 1)
                                    {
                                        itemConsumedString = $"its equipped {mon.ModItem.Name}";
                                        mon.ModItem = null;
                                    }
                                    else if (choice == 2)
                                    {
                                        itemConsumedString = $"its equipped {mon.BattleItem.Name}";
                                        mon.BattleItem = null;
                                    }
                                    else if (choice == 3 || choice == 6) // Set items...
                                    {
                                        Dictionary<SetItem, int> collectionDict;
                                        if (choice == 3) collectionDict = _explorer.SetItems;
                                        else if (choice == 6) collectionDict = _prizes.SetItemsFound;
                                        else throw new Exception("Invalid place to retrieve evo item from!"); // ????
                                        // Now list them and choose
                                        List<SetItem> itemList = [.. collectionDict.Keys];
                                        Console.WriteLine($"Which item to use?");
                                        for (int i = 0; i < itemList.Count; i++) Console.Write($"{i} {itemList[i].Name}, ");
                                        int idx = int.Parse(Console.ReadLine());
                                        GeneralUtilities.AddtemToCountDictionary(collectionDict, itemList[idx], -1, true);
                                        itemConsumedString = $"the {itemList[idx].Name} located in the bag";
                                    }
                                    else
                                    {
                                        Dictionary<Item, int> collectionDict;
                                        itemConsumedString = $"in the bag";
                                        if (choice == 4) collectionDict = _explorer.ModItems;
                                        else if (choice == 5) collectionDict = _explorer.BattleItems;
                                        else if (choice == 7) collectionDict = _prizes.ModItemsFound;
                                        else if (choice == 8) collectionDict = _prizes.BattleItemsFound;
                                        else throw new Exception("Invalid place to retrieve evo item from!");
                                        // Now list them and choose
                                        List<Item> itemList = [.. collectionDict.Keys];
                                        Console.WriteLine($"Which item to use?");
                                        for (int i = 0; i < itemList.Count; i++) Console.Write($"{i} {itemList[i].Name}, ");
                                        int idx = int.Parse(Console.ReadLine());
                                        GeneralUtilities.AddtemToCountDictionary(collectionDict, itemList[idx], -1, true);
                                        itemConsumedString = $"the {itemList[idx].Name} located in the bag";
                                    }
                                    // Notify all
                                    string message = mon.ToString();
                                    message += $" has evolved into {chosenMon} by using " + itemConsumedString + "!";
                                    GenericMessageCommand(message);
                                    // Finally, actually do the deed
                                    mon.Species = chosenMon.Name;
                                    if (!chosenMon.Abilities.Contains(mon.ChosenAbility)) mon.ChosenAbility = GeneralUtilities.GetRandomPick(chosenMon.Abilities); // Get a random ability if now invalid
                                    UpdateTrainerDataInfo(); // Updates numbers in chart
                                }
                            }
                        }
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.RESEARCHER:
                    {
                        List<string> platesList = [.. MechanicsDataContainers.GlobalMechanicsData.BattleItems.Keys.Where(i => i.ToLower().Contains("plate"))];
                        string chosenPlate = GeneralUtilities.GetRandomPick(platesList);
                        string messageString = roomEvent.PreEventString.Replace("$1", chosenPlate);
                        GenericMessageCommand(messageString);
                        _prizes.AddReward(chosenPlate, 1);
                        messageString = roomEvent.PostEventString.Replace("$1", chosenPlate);
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.PARADOX:
                    {
                        string chosenMove = GeneralUtilities.GetRandomPick(MechanicsDataContainers.GlobalMechanicsData.Moves.Keys.ToList());
                        string diskName = $"{chosenMove} {SetItem.ADVANCED_DISK}"; // Create the advanced disk
                        string messageString = roomEvent.PreEventString.Replace("$1", diskName);
                        GenericMessageCommand(messageString);
                        _prizes.AddReward(diskName, 1);
                        messageString = roomEvent.PostEventString.Replace("$1", diskName);
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.IMP_GAIN:
                    {
                        int impGain = GeneralUtilities.GetRandomNumber(2, 4); // 2-3 IMP
                        string messageString = roomEvent.PreEventString.Replace("$1", $"{impGain} IMP");
                        GenericMessageCommand(messageString);
                        _prizes.AddImp(impGain);
                        messageString = roomEvent.PostEventString.Replace("$1", $"{impGain} IMP");
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.HEAL:
                    {
                        Console.WriteLine("A heal of 33% of all mons");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            mon.HealthPercentage += 33;
                            if (mon.HealthPercentage > 100) mon.HealthPercentage = 100;
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.DAMAGE_TRAP:
                    {
                        Console.WriteLine("A damage trap of 25% to all mons");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            mon.HealthPercentage -= 25;
                            if (mon.HealthPercentage <= 0) mon.HealthPercentage = 1;
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.CURE:
                    {
                        Console.WriteLine("Cures all mons status");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            mon.NonVolatileStatus = "";
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.BIG_HEAL:
                    {
                        Console.WriteLine("Single big heal to a mon");
                        TrainerPokemon mon = _explorer.BattleTeam.OrderBy(p => p.HealthPercentage).FirstOrDefault();
                        mon.HealthPercentage = 100;
                        string message = roomEvent.PreEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(roomEvent.PreEventString);
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        message = roomEvent.PostEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(message);
                    }
                    break;
                case RoomEventType.PP_HEAL:
                    {
                        Console.WriteLine("Cures all mons PP");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            for (int i = 0; i < mon.MovePp.Count; i++) // Restores 3 pp to each move
                            {
                                mon.MovePp[i] += 3;
                            }
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.STATUS_TRAP:
                    {
                        Console.WriteLine("A trap that will status one mon");
                        string status = roomEvent.SpecialParams;
                        List<TrainerPokemon> possibleMons = [.. _explorer.BattleTeam.Where(m => m.NonVolatileStatus == "")]; // Get mons without status
                        GenericMessageCommand(roomEvent.PreEventString);
                        if (possibleMons.Count > 0)
                        {
                            TrainerPokemon mon = GeneralUtilities.GetRandomPick(possibleMons);
                            mon.NonVolatileStatus = status;
                            string postMessage = roomEvent.PostEventString.Replace("$1", mon.GetInformalName());
                            GenericMessageCommand(postMessage);
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                    }
                    break;
                case RoomEventType.POKEMON_BATTLE:
                    // Similar to aplha but there's 3 enemy mons
                    {
                        const int NUMBER_OF_WILD_POKEMON = 3; // Time to balance-hardcode this, could be implemented in ctx later
                        const int ITEM_MULTIPLIER = 1;
                        // Items obtained during the fight (commons)
                        int uniqueItems = NUMBER_OF_WILD_POKEMON;
                        List<ItemReward> items = new List<ItemReward>();
                        for (int i = 0; i < uniqueItems; i++)
                        {
                            // Prize pool will contain common items
                            ItemReward nextItem = GeneralUtilities.GetRandomPick(_dungeonData.CommonItems);
                            items.Add(nextItem);
                        }
                        // Add Pokemon, they will have items
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < NUMBER_OF_WILD_POKEMON; i++) // Generate party of random mons
                        {
                            string nextPokemon = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[floor]);
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer wildMonsTrainer = GenerateEnemyTrainer("WildMons", pokemonThisFloor, [.. items.Select(i => i.Name)], 100, 100, true);
                        DefineEnemySet(wildMonsTrainer, int.MaxValue, false); // Defines the enemy set (dumb mons tho)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, wildMonsTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", string.Join(',', [.. items.Select(i => i.Name)]));
                            GenericMessageCommand(postMessage);
                            foreach (ItemReward item in items) // Add all items to Prizes
                            {
                                int amount = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1) * ITEM_MULTIPLIER;
                                _prizes.AddReward(item.Name, amount);
                            }
                            foreach (TrainerPokemon pokemonSpecies in wildMonsTrainer.BattleTeam)
                            {
                                _prizes.AddMon(pokemonSpecies, floor + 1);
                            }
                        }
                    }
                    break;
                case RoomEventType.SWARM:
                    // Similar to aplha but there's 6 enemy mons
                    {
                        const int NUMBER_OF_WILD_POKEMON = 6; // Time to balance-hardcode this
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < NUMBER_OF_WILD_POKEMON; i++) // Generate party of random mons always from 0
                        {
                            string nextPokemon = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[0]);
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer swarmTrainer = GenerateEnemyTrainer("Swarm", pokemonThisFloor, [], 60, 75, true); // Lvl between 60-75
                        DefineEnemySet(swarmTrainer, int.MaxValue, false); // Defines the enemy set (dumb mons tho)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, swarmTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString);
                            foreach (TrainerPokemon pokemonSpecies in swarmTrainer.BattleTeam)
                            {
                                _prizes.AddMon(pokemonSpecies, 0);
                            }
                        }
                    }
                    break;
                case RoomEventType.UNOWN:
                    // Weird one, select 6 unowns, give them random moves
                    {
                        KeyValuePair<string, string> unownChosen = GeneralUtilities.GetRandomKvp(MechanicsDataContainers.GlobalMechanicsData.UnownLookup);
                        Console.WriteLine($"Unown battle, will form word {unownChosen.Key} with reward {unownChosen.Value}");
                        List<string> pokemonThisFloor = [];
                        List<string> itemsThisFloor = [];
                        for (int i = 0; i < unownChosen.Key.Length; i++) // Generate party of random unowns
                        {
                            char letter = unownChosen.Key[i]; // Obtain next unown
                            string nextPokemon = (letter == 'A') ? "Unown" : $"Unown-{letter}"; // Get the correct unown
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                            // Check if mon can have an item (any, idk)
                            List<string> itemCandidates = [.. MechanicsDataContainers.GlobalMechanicsData.BattleItems.Keys.Where(i => i.First() == letter)];
                            if (itemCandidates.Count > 0) { itemsThisFloor.Add(GeneralUtilities.GetRandomPick(itemCandidates)); }
                            else itemsThisFloor.Add(""); // Add no item, leave the space so the list doesn't mismatch
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer unownTrainer = GenerateEnemyTrainer("Symbols", pokemonThisFloor, itemsThisFloor, 100, 100, false); // Unowns are unshuffled so they can form the phrase
                        DefineEnemySet(unownTrainer, int.MaxValue, true); // Defines the enemy set (smart unowns)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, unownTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", unownChosen.Value);
                            GenericMessageCommand(postMessage);
                            foreach (TrainerPokemon pokemonSpecies in unownTrainer.BattleTeam)
                            {
                                _prizes.AddMon(pokemonSpecies, 1); // Rank 1
                            }
                            _prizes.AddReward(unownChosen.Value, 1); // Add the unown reward
                        }
                    }
                    break;
                case RoomEventType.FIRELORD:
                    // Weird one, weakened legendary with rare item
                    {
                        // Which mon will be chosen
                        ItemReward itemPrize = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Get a random rare item
                        List<string> validMons = ["Moltres", "Entei", "Ho-Oh", "Groudon", "Heatran", "Chi-Yu", "Koraidon", "Volcaion", "Blacephalon"];
                        string pokemonSpecies = GeneralUtilities.GetRandomPick(validMons);
                        Trainer bossTrainer = GenerateEnemyTrainer("Firelord", [pokemonSpecies], [], 50, 65, true);
                        DefineEnemySet(bossTrainer, int.MaxValue, true); // Smart set
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, bossTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            int rewardQuantity = GeneralUtilities.GetRandomNumber(itemPrize.Min, itemPrize.Max + 1);
                            string postMessage = roomEvent.PostEventString.Replace("$1", itemPrize.Name);
                            GenericMessageCommand(postMessage);
                            _prizes.AddReward(itemPrize.Name, rewardQuantity);
                            _prizes.AddMons(bossTrainer.BattleTeam, 4); // Add the mon (masterball tho)
                        }
                    }
                    break;
                case RoomEventType.GIANT_POKEMON:
                    // Dumb single pokemon with a rare item but the mon is lvl 110-125
                    {
                        // Which mon will be chosen
                        ItemReward itemPrize = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Get a random rare item
                        List<string> validMons = [.. _dungeonData.PokemonEachFloor[floor]];
                        string pokemonSpecies = GeneralUtilities.GetRandomPick(validMons);
                        Trainer giantMon = GenerateEnemyTrainer("MutantPokemon", [pokemonSpecies], [], 110, 126, true);
                        DefineEnemySet(giantMon, int.MaxValue, false); // Not smart
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, giantMon);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            int rewardQuantity = GeneralUtilities.GetRandomNumber(itemPrize.Min, itemPrize.Max + 1);
                            string postMessage = roomEvent.PostEventString.Replace("$1", itemPrize.Name);
                            GenericMessageCommand(postMessage);
                            _prizes.AddReward(itemPrize.Name, rewardQuantity);
                            _prizes.AddMons(giantMon.BattleTeam, floor + 1);
                        }
                    }
                    break;
                case RoomEventType.MIRROR_MATCH:
                    // Fight against yourself but a couple levels lower
                    {
                        Trainer copiedTeam = new Trainer()
                        {
                            Name = "Illusion",
                            Avatar = _explorer.Avatar,
                            BattleTeam = [] // Will add mons later
                        };
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            int level = GeneralUtilities.GetRandomNumber(75, 91); // Get lvls 75-90
                            TrainerPokemon copiedMon = new TrainerPokemon()
                            {
                                Species = mon.Species,
                                Nickname = mon.Nickname,
                                IsShiny = mon.IsShiny,
                                SetItem = mon.SetItem,
                                ModItem = mon.ModItem,
                                BattleItem = mon.BattleItem,
                                ChosenAbility = mon.ChosenAbility,
                                ChosenMoveset = mon.ChosenMoveset,
                                Evs = mon.Evs,
                                Nature = mon.Nature,
                                TeraType = mon.TeraType,
                                HealthPercentage = mon.HealthPercentage,
                                NonVolatileStatus = mon.NonVolatileStatus,
                                MovePp = mon.MovePp,
                                Logic = mon.Logic
                            };
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        int remainingMons = ResolveEncounter(_explorer, copiedTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            foreach (TrainerPokemon mon in _explorer.BattleTeam)
                            {
                                mon.HealFull(); // Heall all mons as reward
                            }
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString);
                        }
                    }
                    break;
                case RoomEventType.PLOT_CLUE:
                    {
                        PlotMessageCommand(roomEvent.PreEventString);
                        PlotMessageCommand(roomEvent.PostEventString);
                        if (roomEvent.SpecialParams != "")
                        {
                            _prizes.AddReward(roomEvent.SpecialParams, 1); // Add item if plot event has it
                        }
                    }
                    break;
                case RoomEventType.JOINER:
                    // Mon that joins the team for adventures, always catchable
                    {
                        List<string> validMons = [.. _dungeonData.PokemonEachFloor[floor]];
                        string pokemonSpecies = GeneralUtilities.GetRandomPick(validMons);
                        Trainer placeholderTrainer = GenerateEnemyTrainer("Whatever", [pokemonSpecies], [], 100, 100, false);
                        DefineEnemySet(placeholderTrainer, int.MaxValue, false); // Not smart
                        Console.WriteLine($"Joiner {pokemonSpecies}");
                        string joinerString = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(joinerString); // Prints the message but we know it could have a $1
                        string nickName = $"{pokemonSpecies} friend";
                        if (nickName.Length > 18) // Sanitize, name has to be shorter than 19 and no spaces
                        {
                            nickName = nickName[..18].Trim();
                        }
                        TrainerPokemon joiner = placeholderTrainer.BattleTeam.First();
                        joiner.Nickname = nickName;
                        Console.Write("Added to team");
                        _explorer.BattleTeam.Add(joiner);
                        GenericMessageCommand(roomEvent.PostEventString);
                        _prizes.AddMon(joiner, 0); // Add mon to lowest floor (free basically)
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                    }
                    break;
                case RoomEventType.NPC_BATTLE:
                    {
                        Trainer randomNpc = GeneralUtilities.GetRandomKvp(GameDataContainers.GlobalGameData.NpcData).Value; // Get random npc
                        // Define trainer in the same way we defined our own for exploration
                        List<PossibleTeamBuild> possibleBuilds = TeamBuilder.GetTrainersPossibleBuilds(randomNpc, _explorer.BattleTeam.Count, [new Constraint()], true); // Since there's no constraint, will get a build consisting of all my mons, matched to trainer's
                        TeamBuilder.AssembleTrainersBattleTeam(randomNpc, _explorer.BattleTeam.Count, possibleBuilds, true); // Chooses one of the sets, prepares the mons, seed 0
                        // Now, make the trainer choose it's team sets!
                        HashSet<Pokemon> enemyMons = new HashSet<Pokemon>();
                        enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[0].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]); // Add all mons from all floors
                        enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[1].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
                        enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[2].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
                        enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[3].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
                        TeamBuilder.DefineTrainerSets(randomNpc, true, _dungeonData.DungeonArchetypes, _dungeonData.DungeonWeather, _dungeonData.DungeonTerrain, new Constraint(), [.. enemyMons]); // Team build but with the dungeon's weather and such 
                        Console.WriteLine($"Fighting {randomNpc.Name}");
                        string npcString = roomEvent.PreEventString.Replace("$1", randomNpc.Name);
                        GenericMessageCommand(npcString); // Prints the message but we know it could have a $1
                        // Heal first nMons
                        foreach (TrainerPokemon mon in _explorer.BattleTeam)
                        {
                            mon.HealFull();
                        }
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(_explorer, randomNpc);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            _prizes.AddReward(randomNpc.Name, 1);
                            npcString = roomEvent.PostEventString.Replace("$1", randomNpc.Name);
                            GenericMessageCommand(npcString);
                        }
                    }
                    break;
                case RoomEventType.REGISTEEL: // Dramatic drawing of registeel eyes
                    ClearRoomsCommand();
                    DrawRegiEye(-1, -1, 1000);
                    DrawRegiEye(1, -1, 1000);
                    DrawRegiEye(2, 0, 500);
                    DrawRegiEye(1, 1, 333);
                    DrawRegiEye(-1, 1, 333);
                    DrawRegiEye(-2, 0, 333);
                    break;
                case RoomEventType.REGIROCK: // Dramatic drawing of regirock eyes
                    ClearRoomsCommand();
                    DrawRegiEye(0, 0, 2000);
                    DrawRegiEye(1, 0, 0);
                    DrawRegiEye(-1, 0, 2000);
                    DrawRegiEye(1, 1, 0);
                    DrawRegiEye(1, -1, 0);
                    DrawRegiEye(-1, 1, 0);
                    DrawRegiEye(-1, -1, 2000);
                    break;
                case RoomEventType.REGICE: // Dramatic drawing of regice eyes
                    ClearRoomsCommand();
                    DrawRegiEye(0, 0, 1000);
                    DrawRegiEye(0, -1, 0);
                    DrawRegiEye(0, 1, 1000);
                    DrawRegiEye(1, 0, 0);
                    DrawRegiEye(-1, 0, 500);
                    DrawRegiEye(2, 0, 0);
                    DrawRegiEye(-2, 0, 500);
                    break;
                case RoomEventType.REGIELEKI: // Dramatic drawing of regigas eyes
                    ClearRoomsCommand();
                    DrawRegiEye(0, 0, 250);
                    DrawRegiEye(1, 0, 250);
                    DrawRegiEye(-1, 0, 250);
                    DrawRegiEye(2, 1, 250);
                    DrawRegiEye(2, -1, 250);
                    DrawRegiEye(-2, -1, 250);
                    DrawRegiEye(-2, 1, 250);
                    break;
                default:
                    break;
            }
            return roomCleared;
        }
        /// <summary>
        /// Saves exploration outcome in a txt file for quick copying
        /// </summary>
        void SaveExplorationOutcome()
        {
            string explFile = $"{_explorer}_EXPLORATION.txt";
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.Clear();
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"__{_explorer.Name} has obtained the following items:__");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine();
            // Do collections one by one, of the things gained
            int maxCount = 0;
            static string GetPrizesStrings<T>(Dictionary<T, int> dict)
            {
                List<string> collection = new List<string>();
                foreach (KeyValuePair<T, int> data in dict)
                {
                    collection.Add(data.Value > 1 ? $"{data.Key} x{data.Value}" : $"{data.Key}");
                }
                return string.Join(',', collection);
            }
            // First set items:
            string setItemString = GetPrizesStrings(_prizes.SetItemsFound);
            maxCount = Math.Max(maxCount, setItemString.Length);
            // Mod
            string modItemString = GetPrizesStrings(_prizes.SetItemsFound);
            maxCount = Math.Max(maxCount, modItemString.Length);
            // Mod
            string battleItemString = GetPrizesStrings(_prizes.BattleItemsFound);
            maxCount = Math.Max(maxCount, battleItemString.Length);
            // Favour
            string favourString = GetPrizesStrings(_prizes.FavoursFound);
            maxCount = Math.Max(maxCount, favourString.Length);
            // Key
            string keyString = GetPrizesStrings(_prizes.KeyItemsFound);
            maxCount = Math.Max(maxCount, keyString.Length);
            if (setItemString.Length == 0) setItemString = new string('M', GeneralUtilities.GetRandomNumber(9 * maxCount / 10, 11 * maxCount / 10)); // +- 10%
            if (modItemString.Length == 0) modItemString = new string('M', GeneralUtilities.GetRandomNumber(9 * maxCount / 10, 11 * maxCount / 10)); // +- 10%
            if (battleItemString.Length == 0) battleItemString = new string('M', GeneralUtilities.GetRandomNumber(9 * maxCount / 10, 11 * maxCount / 10)); // +- 10%
            if (favourString.Length == 0) favourString = new string('M', GeneralUtilities.GetRandomNumber(3 * maxCount / 10, 4 * maxCount / 10)); // +- 10%
            if (keyString.Length == 0) keyString = new string('M', GeneralUtilities.GetRandomNumber(3 * maxCount / 10, 4 * maxCount / 10)); // +- 10%
            // Now print
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"**Set Items: **||{setItemString}||");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"**Mod Items: **||{modItemString}||");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"**Held Items: **||{battleItemString}||");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"**Favours: **||{favourString}||");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"**Key Items: **||{keyString}||");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine();
            // The mon printing is weird
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"__Catchable Pokemon (As many as you want as long as you have the corresponding Poke Ball for the Pokemon rank):__");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine();
            List<int> ranks = [.. _prizes.MonsFound.Keys.Order()];
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.Append($"||"); // Beginning of rank Spoilered text
            foreach (int rank in ranks)
            {
                string monsOfRank = GetPrizesStrings(_prizes.MonsFound[rank]);
                GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.Append($"**RANK {rank}:** {monsOfRank}. ");
            }
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"||"); // Close rank-spoilered text
            // Save
            string filePath = Path.Combine(DirectoryPath, explFile);
            GameDataContainers.GlobalGameData.CurrentEventMessage.SaveToFile(filePath);
        }
        /// <summary>
        /// Updates all the trainer data in the exploration info table
        /// </summary>
        void UpdateTrainerDataInfo()
        {
            for (int i = 0; i < _explorer.BattleTeam.Count; i++)
            {
                TrainerPokemon mon = _explorer.BattleTeam[i];
                // Print stuff
                ModifyInfoValueCommand(mon.Species, (0, i));
                ModifyInfoValueCommand($"{mon.HealthPercentage}%", (1, i));
                ModifyInfoValueCommand(mon.NonVolatileStatus, (2, i));
            }
        }
        /// <summary>
        /// Verifies whether a trainer fills the consitions to use a shortcut
        /// </summary>
        /// <param name="conditions">Shortcut conditions</param>
        /// <param name="message">An extra return indicating the message used (E.g. X used Y)</param>
        /// <returns>Whether shortcut activates or not</returns>
        bool VerifyShortcutConditions(List<ShortcutCondition> conditions, out string message)
        {
            bool canTakeShortcut = false;
            message = "";
            foreach (ShortcutCondition condition in conditions)
            {
                foreach (string eachOne in condition.Which)
                {
                    switch (condition.ConditionType)
                    {
                        case ShortcutConditionType.MOVE:
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam) // If a mon has move, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered with the right set item
                                if (pokemon.ChosenMoveset.Any(m => m?.Name == eachOne) && pokemon.SetItemChosen && pokemon.SetItem.AddedMoves.Any(m => m?.Name == eachOne)) // move found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.ABILITY:
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam) // If a mon has ability, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered with the right set item
                                if (pokemon.ChosenAbility.Name == eachOne && pokemon.SetItemChosen && pokemon.SetItem.AddedAbility.Name == eachOne) // ability found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.POKEMON:
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam) // If a mon is there, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered consciously
                                if (pokemon.Species == eachOne && !_explorer.AutoTeam) // species found
                                {
                                    message = $"{pokemon.GetInformalName()}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.TYPE:
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam) // If a mon has type, all good
                            {
                                Pokemon monSpecies = MechanicsDataContainers.GlobalMechanicsData.Dex[pokemon.Species];
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered consciously
                                if (!_explorer.AutoTeam && (monSpecies.Types.Item1.ToString().ToUpper() == eachOne.ToUpper() || monSpecies.Types.Item2.ToString().ToUpper() == eachOne.ToUpper())) // type of pokemon found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne} type";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.ITEM: // This is outdated but refers to the battle item I THINK ?!?!
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam) // If a mon has item, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered with the right item
                                if (pokemon.BattleItem?.Name == eachOne && pokemon.BattleItemChosen) // item found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.MOVE_DISK: // This is an insane guess but I think it refers to set item containin "move disk"
                            foreach (TrainerPokemon pokemon in _explorer.BattleTeam)
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered with the right set item
                                if (pokemon.SetItem != null && pokemon.SetItemChosen && pokemon.SetItem.Name.Contains("Disk")) // disk found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {pokemon.SetItem.Name}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    if (canTakeShortcut) break; // Only need to find one
                }
            }
            return canTakeShortcut;
        }
        /// <summary>
        /// Clears all rooms and table (UNRECOVERABLE!)
        /// </summary>
        void SetDungeonCommand(string dungeon)
        {
            ExplorationSteps.Add(new ExplorationStep() // Clean the rooms
            {
                Type = ExplorationStepType.DEFINE_DUNGEON,
                StringParam = dungeon
            });
        }
        /// <summary>
        /// Helper for all messages
        /// </summary>
        /// <param name="message">String to add</param>
        void MessageCommand(string message, ExplorationStepType type)
        {
            Console.WriteLine($"> {message}"); // Important for debug too
            int messagePause = message.Count(c => c == ' ') + 1; // Count spaces (words?)
            messagePause *= MESSAGE_PAUSE_PER_WORD;
            if (messagePause < MIN_MESSAGE_PAUSE) messagePause = MIN_MESSAGE_PAUSE;
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = type,
                StringParam = message,
                MillisecondsWait = messagePause
            });
        }
        /// <summary>
        /// Adds to event queue, a generic message string
        /// </summary>
        /// <param name="message">String to add</param>
        void GenericMessageCommand(string message)
        {
            MessageCommand(message, ExplorationStepType.PRINT_STRING);
        }
        /// <summary>
        /// Adds to event queue, a generic message string. Will be informative (i.e. gives clue to a player)
        /// </summary>
        /// <param name="message">String to add</param>
        void ClueMessageCommand(string message)
        {
            MessageCommand(message, ExplorationStepType.PRINT_CLUE);
        }
        /// <summary>
        /// Adds to event queue, a plot message string. Will be plot-based (i.e. gives clue to a player)
        /// </summary>
        /// <param name="message">String to add</param>
        void PlotMessageCommand(string message)
        {
            MessageCommand(message, ExplorationStepType.PRINT_PLOT);
        }
        /// <summary>
        /// Adds to event queue, a generic message regarding pokemon evolution
        /// </summary>
        /// <param name="message">String to add</param>
        void EvolutionMessageCommand(string message)
        {
            MessageCommand(message, ExplorationStepType.PRINT_EVOLUTION);
        }
        /// <summary>
        /// No command, just meant to do a wait
        /// </summary>
        /// <param name="wait">Wait in milliseconds</param>
        void NopWithWaitCommand(int wait)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.NOP,
                MillisecondsWait = wait
            });
        }
        /// <summary>
        /// Deletes console
        /// </summary>
        void ClearConsoleCommand()
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.CLEAR_CONSOLE,
            });
        }
        /// <summary>
        /// Clears all rooms and table (UNRECOVERABLE!)
        /// </summary>
        void ClearRoomsCommand()
        {
            ExplorationSteps.Add(new ExplorationStep() // Clean the rooms
            {
                Type = ExplorationStepType.CLEAR_ROOMS,
                MillisecondsWait = DRAW_ROOM_PAUSE
            });
        }
        /// <summary>
        /// Commands the system to draw a room box
        /// </summary>
        /// <param name="floor">Floor of room</param>
        /// <param name="room">Room number (0-5)</param>
        void DrawRoomCommand(int floor, int room)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.DRAW_ROOM,
                DestCoord = (floor, room)
            });
        }
        /// <summary>
        /// Draws a regi eye in the relative coords x,y
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        void DrawRegiEye(int x, int y, int milliSecondWait)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.DRAW_REGI_EYE,
                DestCoord = (x, y),
                MillisecondsWait = milliSecondWait
            });
        }
        /// <summary>
        /// Draws the connection of rooms either in normal mode or passage mode, can connect to invisible rooms too (i.e. end of dungeon or beginning)
        /// </summary>
        /// <param name="floor1">coord of source room</param>
        /// <param name="room1">coord of source room</param>
        /// <param name="floor2">coord of dest room</param>
        /// <param name="room2">coord of dest room</param>
        /// <param name="isShortcut">If will draw shortcut or passage</param>
        void DrawConnectRoomCommand(int floor1, int room1, int floor2, int room2, bool isShortcut)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = isShortcut ? ExplorationStepType.CONNECT_ROOMS_SHORTCUT : ExplorationStepType.CONNECT_ROOMS_PASSAGE,
                SourceCoord = (floor1, room1),
                DestCoord = (floor2, room2)
            });
        }
        /// <summary>
        /// Draws the movement of character from one room to another
        /// </summary>
        /// <param name="floor1">coord of source room</param>
        /// <param name="room1">coord of source room</param>
        /// <param name="floor2">coord of dest room</param>
        /// <param name="room2">coord of dest room</param>
        void DrawMoveCharacterCommand(int floor1, int room1, int floor2, int room2)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.MOVE_CHARACTER,
                SourceCoord = (floor1, room1),
                DestCoord = (floor2, room2)
            });
        }
        void AddInfoColumnCommand(string label, int minWidth)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.ADD_INFO_COLUMN,
                IntParam = minWidth,
                StringParam = label
            });
        }
        void ModifyInfoValueCommand(string value, (int, int) tableCoord)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.ADD_INFO_VALUE,
                DestCoord = tableCoord,
                StringParam = value
            });
        }
        /// <summary>
        /// Generates an NPC enemy trainer for wild mons, listing the name and the mons/equip used. Traienr avatar will be unknown always
        /// </summary>
        /// <param name="trainerName">Name of trainer</param>
        /// <param name="pokemonList">Pokemon found</param>
        /// <param name="itemsHeld">Items the mons are holding</param>
        /// <param name="shuffled">Whether to shuffle the mons in the generated team</param>
        /// <returns></returns>
        Trainer GenerateEnemyTrainer(string trainerName, List<string> pokemonList, List<string> itemsHeld, int minLvl, int maxLvl, bool shuffled)
        {
            Trainer enemyTrainer = new Trainer() // Create the blank trainer
            {
                Avatar = "unknown",
                Name = trainerName,
                AutoTeam = shuffled, // Will shuffle shit around unless it's not meant to
                AutoBattleItem = false,
                AutoFavour = false,
                AutoModItem = false,
                AutoSetItem = false,
            };
            if (shuffled)
            {
                GeneralUtilities.ShuffleList(itemsHeld); // Shuffle item list rq
                GeneralUtilities.ShuffleList(pokemonList); // Shuffle mon list rq
            }
            for (int i = 0; i < pokemonList.Count; i++)
            {
                bool isShiny = (GeneralUtilities.GetRandomNumber(_context.ShinyChance) == 0); // Will be shiny if i get a 0 dice roll
                int level = GeneralUtilities.GetRandomNumber(minLvl, maxLvl + 1);
                TrainerPokemon nextPokemonInTeam = new TrainerPokemon()
                {
                    Species = pokemonList[i],
                    IsShiny = isShiny,
                    Level = level,
                };
                // Add item if i got one
                if (itemsHeld.Count > i) // If there's a valid item to give this mon
                {
                    string itemName = itemsHeld[i];
                    IndymonUtilities.RewardType itemType = IndymonUtilities.GetRewardType(itemName); // Find the type of this item
                    Item itemAux;
                    switch (itemType)
                    {
                        case IndymonUtilities.RewardType.SET:
                            if (!GameDataContainers.GlobalGameData.SetItems.TryGetValue(itemName, out SetItem setItem))
                            {
                                setItem = SetItem.Parse(itemName);
                                GameDataContainers.GlobalGameData.SetItems.Add(itemName, setItem);
                            }
                            nextPokemonInTeam.SetItem = setItem; // Equip the item
                            break;
                        case IndymonUtilities.RewardType.MOD:
                            itemAux = MechanicsDataContainers.GlobalMechanicsData.ModItems[itemName];
                            nextPokemonInTeam.ModItem = itemAux;
                            break;
                        case IndymonUtilities.RewardType.BATTLE:
                            itemAux = MechanicsDataContainers.GlobalMechanicsData.BattleItems[itemName];
                            nextPokemonInTeam.ModItem = itemAux;
                            break;
                        default: // These items are not equippable
                            break;
                    }
                }
                enemyTrainer.PartyPokemon.Add(nextPokemonInTeam); // Add mon
            }
            return enemyTrainer;
        }
        /// <summary>
        /// Builds a set for an enemy encounter, which may include a set that can counter the trainer alongside using dungeon as best as possible
        /// </summary>
        /// <param name="enemy">Trainer to build</param>
        /// <param name="maxNMons">How many mons to build it for</param>
        /// <param name="smart">Whether teambuild is smart or not</param>
        void DefineEnemySet(Trainer enemy, int maxNMons, bool smart)
        {
            List<PossibleTeamBuild> possibleBuilds = TeamBuilder.GetTrainersPossibleBuilds(enemy, maxNMons, [new Constraint()], true);
            TeamBuilder.AssembleTrainersBattleTeam(enemy, maxNMons, possibleBuilds, true); // Chooses one of the sets, prepares the mons, any seed
            // Now, make the trainer choose it's team sets!
            List<Pokemon> enemyMons = null;
            if (smart) // If smart, build against the trainer
            {
                enemyMons = [.. _explorer.BattleTeam.Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m.Species])];
            }
            TeamBuilder.DefineTrainerSets(enemy, smart, _dungeonData.DungeonArchetypes, _dungeonData.DungeonWeather, _dungeonData.DungeonTerrain, new Constraint(), enemyMons); // Team build but with the dungeon's weather and such 
        }
        /// <summary>
        /// Resolves an encounter between players, no mon limit
        /// </summary>
        /// <param name="explorer">P1</param>
        /// <param name="encounter">P2</param>
        /// <returns>How many mons P1 has left (0 means defeat)</returns>
        int ResolveEncounter(Trainer explorer, Trainer encounter)
        {
            (int cursorX, int cursorY) = Console.GetCursorPosition(); // Just in case I need to write in same place
            Console.Write("About to simulate bots...");
            Console.ReadLine();
            // The challenge string may contain dungeon-specific rules (besides the mandatory ones)
            List<string> showdownRules = ["!Team Preview", .. _dungeonData.CustomShowdownRules];
            string challengeString = $"gen9customgame@@@{string.Join(",", showdownRules)}"; // Assemble resulting challenge string
            (int explorerLeft, _) = BotBattle.SimulateBotBattle(explorer, encounter, challengeString); // Initiate battle
            Console.SetCursorPosition(cursorX, cursorY);
            Console.Write($"Explorer left with {explorerLeft} mons. GET THE REPLAY");
            return explorerLeft;
        }
        #endregion
        #region ANIMATION
        readonly int ROOM_WIDTH = 5;
        readonly int ROOM_HEIGHT = 5;
        /// <summary>
        /// Animates the resulting exploration
        /// </summary>
        public void AnimateExploration()
        {
            _explorer = IndymonUtilities.GetTrainerByName(TrainerAndSeed.Item1); // Find the trainer data
            char[] infoTableTemplate = "|".ToCharArray();
            List<int> infoColOffset = [2]; // First element "would" start from here
            List<char[]> infoRows = [[.. infoTableTemplate], new string('-', infoTableTemplate.Length).ToCharArray()]; // Starts with the (empty) table header, and a separator
            Console.WriteLine("Write anything to begin. Better start recording now");
            Console.ReadLine();
            Console.Clear();
            int consoleLineStart = (DUNGEON_NUMBER_OF_FLOORS * ROOM_HEIGHT) + DUNGEON_NUMBER_OF_FLOORS + 1; // Rooms + spaces between, above and below
            int consoleOffset = consoleLineStart; // Where console currently at
            string emptyLine = new string(' ', Console.WindowWidth);
            foreach (ExplorationStep nextStep in ExplorationSteps) // Now, will do event one by one...
            {
                switch (nextStep.Type)
                {
                    case ExplorationStepType.DEFINE_DUNGEON:
                        _dungeonData = GameDataContainers.GlobalGameData.Dungeons[nextStep.StringParam]; // Loads the dungeon
                        break;
                    case ExplorationStepType.PRINT_STRING:
                        Console.ForegroundColor = ConsoleColor.White; // Reset console just in case
                        Console.SetCursorPosition(0, consoleOffset);
                        Console.WriteLine($"> {nextStep.StringParam}"); // Write message
                        consoleOffset = Console.CursorTop; // New console location
                        break;
                    case ExplorationStepType.PRINT_CLUE:
                        Console.ForegroundColor = ConsoleColor.Yellow; // Sets info color
                        Console.SetCursorPosition(0, consoleOffset);
                        Console.WriteLine($"> {nextStep.StringParam}"); // Write message
                        consoleOffset = Console.CursorTop; // New console location
                        break;
                    case ExplorationStepType.PRINT_PLOT:
                        Console.ForegroundColor = ConsoleColor.Cyan; // Sets plot color
                        Console.SetCursorPosition(0, consoleOffset);
                        Console.WriteLine($"> {nextStep.StringParam}"); // Write message
                        consoleOffset = Console.CursorTop; // New console location
                        break;
                    case ExplorationStepType.PRINT_EVOLUTION:
                        Console.ForegroundColor = ConsoleColor.Magenta; // Sets info color
                        Console.SetCursorPosition(0, consoleOffset);
                        Console.WriteLine($"> {nextStep.StringParam}"); // Write message
                        consoleOffset = Console.CursorTop; // New console location
                        break;
                    case ExplorationStepType.CLEAR_CONSOLE:
                        for (int line = consoleLineStart; line <= consoleOffset; line++) // Clear all that console had printed until now
                        {
                            Console.SetCursorPosition(0, line);
                            Console.Write(emptyLine);
                        }
                        consoleOffset = consoleLineStart;
                        break;
                    case ExplorationStepType.DRAW_ROOM:
                        DrawRoom(nextStep.DestCoord.Item1, nextStep.DestCoord.Item2);
                        break;
                    case ExplorationStepType.MOVE_CHARACTER:
                        DrawCharacter(nextStep.SourceCoord.Item1, nextStep.SourceCoord.Item2, nextStep.DestCoord.Item1, nextStep.DestCoord.Item2);
                        break;
                    case ExplorationStepType.CONNECT_ROOMS_PASSAGE:
                        ConnectPassage(nextStep.SourceCoord.Item1, nextStep.SourceCoord.Item2, nextStep.DestCoord.Item1, nextStep.DestCoord.Item2);
                        break;
                    case ExplorationStepType.CONNECT_ROOMS_SHORTCUT:
                        ConnectShortcut(nextStep.SourceCoord.Item1, nextStep.SourceCoord.Item2, nextStep.DestCoord.Item1, nextStep.DestCoord.Item2);
                        break;
                    case ExplorationStepType.ADD_INFO_COLUMN:
                        // Add template for next time
                        char[] labelSpaces = new string(' ', nextStep.IntParam).ToCharArray();
                        infoTableTemplate = [.. infoTableTemplate, ' ', .. labelSpaces, ' ', '|']; // Add the new empty template
                        // Extend title with new label spaces and then the label
                        infoRows[0] = [.. infoRows[0], ' ', .. labelSpaces, ' ', '|']; // Add the new empty template
                        // Extend separator
                        infoRows[1] = new string('-', infoTableTemplate.Length).ToCharArray();
                        // Add width data
                        int nextOffset = infoColOffset.Last() + nextStep.IntParam + 3; // Next character would be 3 (for the new _|_ + the len of other label)
                        infoColOffset.Add(nextOffset);
                        // Redraw table
                        UpdateInfoTableField(nextStep.StringParam, (infoColOffset.Count - 2, 0), infoRows, infoColOffset); // Puts the label
                        RedrawInfoTable(infoRows);
                        break;
                    case ExplorationStepType.ADD_INFO_VALUE:
                        // X (field) and Y (row). First, add rows until satisfied
                        while ((infoRows.Count - 2) <= nextStep.DestCoord.Item2) // If row not yet present, add new until exist
                        {
                            infoRows.Add([.. infoTableTemplate]); // Copy template into new row
                        }
                        (int, int) actualTableCoord = (nextStep.DestCoord.Item1, nextStep.DestCoord.Item2 + 2); // The Y offset is because the first 2 rows are not data but labels+sep
                        UpdateInfoTableField(nextStep.StringParam, actualTableCoord, infoRows, infoColOffset);
                        // Redraw table
                        RedrawInfoTable(infoRows);
                        break;
                    case ExplorationStepType.CLEAR_ROOMS:
                        for (int line = 0; line < consoleLineStart; line++) // Clear all until text
                        {
                            Console.SetCursorPosition(0, line);
                            Console.Write(emptyLine);
                        }
                        break;
                    case ExplorationStepType.DRAW_REGI_EYE:
                        Console.ForegroundColor = ConsoleColor.Red; // Regi eyes are red
                        int midpointX = Console.WindowWidth / 2; // Midpoint pixel
                        int midpointY = consoleLineStart / 2;
                        Console.SetCursorPosition(nextStep.DestCoord.Item1 + midpointX, nextStep.DestCoord.Item2 + midpointY);
                        Console.Write($"●"); // Draw regi eye
                        break;
                    default:
                        break;
                }
                if (nextStep.MillisecondsWait > 0)
                {
                    Thread.Sleep(nextStep.MillisecondsWait);
                }
            }
            Console.ReadLine();
        }
        /// <summary>
        /// Get the coordinates in console space for a room
        /// </summary>
        /// <param name="floor">FLoor of room</param>
        /// <param name="room">Room</param>
        /// <returns>The X,Y coord</returns>
        (int, int) GetRoomCoords(int floor, int room)
        {
            // Floor index is 0-2 if goes downwards or 2-0 if upwards
            int roomY = _dungeonData.GoesDownwards ? floor : DUNGEON_NUMBER_OF_FLOORS - floor - 1; // Get the Y coord (vertical)
            roomY = 1 + ((1 + ROOM_HEIGHT) * roomY); // Correct Positioning of room Y tile
            // Clamp floor to valid numbers, to avoid weird looping outside the edges
            if (floor >= DUNGEON_NUMBER_OF_FLOORS) floor = DUNGEON_NUMBER_OF_FLOORS - 1;
            if (floor < 0) floor = 0;
            // (Room always left to right in top/bottom (even floors), right to left in mid)
            int roomX = ((floor % 2) == 0) ? room : DUNGEON_ROOMS_PER_FLOOR - room - 1; // Get the X coord (horiz)
            roomX = 1 + (ROOM_WIDTH * roomX); // This one has no spaces inbetween just an offset if needed
            return (roomX, roomY);
        }
        /// <summary>
        /// Check whether requested coord is valid (inside of picture)
        /// </summary>
        /// <param name="floor">Which floor</param>
        /// <param name="room">Which room</param>
        /// <returns></returns>
        static bool IsRoomValid(int floor, int room)
        {
            if (floor < 0 || floor >= DUNGEON_NUMBER_OF_FLOORS)
            {
                return false;
            }
            if (room < 0 || floor >= DUNGEON_ROOMS_PER_FLOOR)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Draws the required room for this dungeon
        /// </summary>
        /// <param name="floor">Floor room is in</param>
        /// <param name="room">Room number</param>
        void DrawRoom(int floor, int room)
        {
            (int X, int Y) = GetRoomCoords(floor, room);
            // Now i have coord X, Y to draw room
            DungeonFloor floorData = _dungeonData.Floors[floor]; // Obtain room drawing data
            Console.ForegroundColor = floorData.RoomColor; // Set color
            // Draw corners
            Console.SetCursorPosition(X, Y);
            Console.Write(floorData.NwWallTile);
            Console.SetCursorPosition(X + ROOM_WIDTH - 1, Y);
            Console.Write(floorData.NeWallTile);
            Console.SetCursorPosition(X, Y + ROOM_HEIGHT - 1);
            Console.Write(floorData.SwWallTile);
            Console.SetCursorPosition(X + ROOM_WIDTH - 1, Y + ROOM_HEIGHT - 1);
            Console.Write(floorData.SeWallTile);
            // Horizontal Walls
            for (int i = 1; i < ROOM_WIDTH - 1; i++)
            {
                Console.SetCursorPosition(X + i, Y);
                Console.Write(floorData.NWallTile);
                Console.SetCursorPosition(X + i, Y + ROOM_HEIGHT - 1);
                Console.Write(floorData.SWallTile);
            }
            for (int i = 1; i < ROOM_HEIGHT - 1; i++)
            {
                Console.SetCursorPosition(X, Y + i);
                Console.Write(floorData.WWallTile);
                Console.SetCursorPosition(X + ROOM_WIDTH - 1, Y + i);
                Console.Write(floorData.EWallTile);
            }
        }
        /// <summary>
        /// Moves character from one room to another
        /// </summary>
        /// <param name="sourceFloor">Where from (floor)</param>
        /// <param name="sourceRoom">Where from (room)</param>
        /// <param name="destFloor">Where to (floor)</param>
        /// <param name="destRoom">Where to (room)</param>
        void DrawCharacter(int sourceFloor, int sourceRoom, int destFloor, int destRoom)
        {
            string playerSymbol = _explorer.DungeonIdentifier;
            Console.ForegroundColor = ConsoleColor.White; // Set color back to white, for character
            // Delete current character first
            if (IsRoomValid(sourceFloor, sourceRoom))
            {
                (int fromX, int fromY) = GetRoomCoords(sourceFloor, sourceRoom);
                Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY + (ROOM_HEIGHT / 2));
                Console.Write(new string(' ', playerSymbol.Length));
            }
            // Draw new character now
            if (IsRoomValid(destFloor, destRoom))
            {
                (int toX, int toY) = GetRoomCoords(destFloor, destRoom);
                Console.SetCursorPosition(toX + (ROOM_WIDTH / 2), toY + (ROOM_HEIGHT / 2));
                Console.Write(playerSymbol);
            }
        }
        /// <summary>
        /// Connects 2 rooms with passage (typical), from->to
        /// </summary>
        /// <param name="sourceFloor">Where from (floor)</param>
        /// <param name="sourceRoom">Where from (room)</param>
        /// <param name="destFloor">Where to (floor)</param>
        /// <param name="destRoom">Where to (room)</param>
        void ConnectPassage(int sourceFloor, int sourceRoom, int destFloor, int destRoom)
        {
            // In passage, rooms are just altered in a very simple way, by just modifying its wall closest to the other room (always adjacent)
            // A vertical passage is then drawn connecting 2 rooms in different floors
            // First, get room coords (no matter if invalid, just to check who's above what
            (int fromX, int fromY) = GetRoomCoords(sourceFloor, sourceRoom);
            (int toX, int toY) = GetRoomCoords(destFloor, destRoom);
            // Then, draw for first room, if needed
            DungeonFloor sourceFloorData = null, destFloorData = null;
            if (IsRoomValid(sourceFloor, sourceRoom))
            {
                sourceFloorData = _dungeonData.Floors[sourceFloor];
                Console.ForegroundColor = sourceFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                if (fromX < toX) { Console.SetCursorPosition(fromX + ROOM_WIDTH - 1, fromY + (ROOM_HEIGHT / 2)); Console.Write(sourceFloorData.EWallPassageTile); } // Conenct east
                else if (fromX > toX) { Console.SetCursorPosition(fromX, fromY + (ROOM_HEIGHT / 2)); Console.Write(sourceFloorData.WWallPassageTile); } // Connect west
                else if (fromY < toY) { Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY + ROOM_HEIGHT - 1); Console.Write(sourceFloorData.SWallPassageTile); } // Connect south
                else if (fromY > toY) { Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY); Console.Write(sourceFloorData.NWallPassageTile); } // Connect north
                else { } // Should never happen
            }
            // Same for dest room
            if (IsRoomValid(destFloor, destRoom))
            {
                destFloorData = _dungeonData.Floors[destFloor];
                Console.ForegroundColor = destFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                if (toX < fromX) { Console.SetCursorPosition(toX + ROOM_WIDTH - 1, toY + (ROOM_HEIGHT / 2)); Console.Write(destFloorData.EWallPassageTile); } // Conenct east
                else if (toX > fromX) { Console.SetCursorPosition(toX, toY + (ROOM_HEIGHT / 2)); Console.Write(destFloorData.WWallPassageTile); } // Connect west
                else if (toY < fromY) { Console.SetCursorPosition(toX + (ROOM_WIDTH / 2), toY + ROOM_HEIGHT - 1); Console.Write(destFloorData.SWallPassageTile); } // Connect south
                else if (toY > fromY) { Console.SetCursorPosition(toX + (ROOM_WIDTH / 2), toY); Console.Write(destFloorData.NWallPassageTile); } // Connect north
                else { } // Should never happen
            }
            // Finally, if the passage occurs between floors, need also to connect them as floors have a gap
            if (fromY != toY)
            {
                int topCoord = Math.Min(fromY, toY); // Which one is higher, don't really care, just connect them
                DungeonFloor floorDataToUse = sourceFloorData ?? destFloorData; // Use always the source floor unless it didn't exist in which case use the other one idk
                Console.ForegroundColor = floorDataToUse.PassageColor;
                Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), topCoord + ROOM_HEIGHT); // X should be same for both rooms (?!?!?!) and then draw ourside of room, use top room for ref
                Console.Write(floorDataToUse.VerticalPassageTile);
            }
        }
        /// <summary>
        /// Connects 2 rooms with shortcut (special), from->to
        /// </summary>
        /// <param name="sourceFloor">Where from (floor)</param>
        /// <param name="sourceRoom">Where from (room)</param>
        /// <param name="destFloor">Where to (floor)</param>
        /// <param name="destRoom">Where to (room)</param>
        void ConnectShortcut(int sourceFloor, int sourceRoom, int destFloor, int destRoom)
        {
            // In shortcut, rooms are first altered in a very simple way, by just modifying its wall closest to the other room (always adjacent)
            // Shortcut is then drawn. This always involves a change in floors so shortcut will need to be drawn iteratively
            // First, get room coords (no matter if invalid, just to check who's above what)
            (int fromX, int fromY) = GetRoomCoords(sourceFloor, sourceRoom);
            (int toX, int toY) = GetRoomCoords(destFloor, destRoom);
            // Then, draw for first room, if needed
            DungeonFloor sourceFloorData = null, destFloorData = null;
            if (IsRoomValid(sourceFloor, sourceRoom))
            {
                sourceFloorData = _dungeonData.Floors[sourceFloor];
                Console.ForegroundColor = sourceFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                // This time, it's only up-down so just compare floors
                if (fromY < toY) { Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY + ROOM_HEIGHT - 1); Console.Write(sourceFloorData.SWallShortcutTile); } // Connect south
                else if (fromY > toY) { Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY); Console.Write(sourceFloorData.NWallShortcutTile); } // Connect north
                else { } // Should never happen
            }
            // Same for dest room
            if (IsRoomValid(destFloor, destRoom))
            {
                destFloorData = _dungeonData.Floors[destFloor];
                Console.ForegroundColor = destFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                // This time, it's only up-down so just compare floors
                if (toY < fromY) { Console.SetCursorPosition(toX + (ROOM_WIDTH / 2), toY + ROOM_HEIGHT - 1); Console.Write(destFloorData.SWallShortcutTile); } // Connect south
                else if (toY > fromY) { Console.SetCursorPosition(toX + (ROOM_WIDTH / 2), toY); Console.Write(destFloorData.NWallShortcutTile); } // Connect north
                else { } // Should never happen
            }
            // Finally, if the passage occurs between floors, need also to connect them as floors have a gap
            if (fromY != toY)
            {
                DungeonFloor floorDataToUse = sourceFloorData ?? destFloorData; // Use always the source floor unless it didn't exist in which case use the other one idk
                int shortcutY = Math.Min(fromY, toY) + ROOM_HEIGHT; // Shorcut to be between rooms (floors)
                int leftShortcutX = Math.Min(fromX, toX) + (ROOM_WIDTH / 2);
                int rightShortcutX = Math.Max(fromX, toX) + (ROOM_WIDTH / 2);
                char firstTile, lastTile, middleTile;
                if (fromX < toX) // Left -> right
                {
                    firstTile = floorDataToUse.NwShortcutTile;
                    lastTile = floorDataToUse.SeShortcutTile;
                    middleTile = floorDataToUse.HorizontalShortcutTile;
                }
                else if (fromX > toX) // Right -> left
                {
                    firstTile = floorDataToUse.NeShortcutTile;
                    lastTile = floorDataToUse.SwShortcutTile;
                    middleTile = floorDataToUse.HorizontalShortcutTile;
                }
                else // Just up, which is just in end of dungeon
                {
                    firstTile = lastTile = middleTile = floorDataToUse.VerticalShortcutTile;
                }
                // Ok we done, new just draw the shortcut
                Console.ForegroundColor = floorDataToUse.ShortcutColor;
                Console.SetCursorPosition(leftShortcutX, shortcutY);
                while (Console.CursorLeft <= rightShortcutX)
                {
                    if (Console.CursorLeft == leftShortcutX) Console.Write(firstTile);
                    else if (Console.CursorLeft == rightShortcutX) Console.Write(lastTile);
                    else Console.Write(middleTile);
                }
            }
        }
        /// <summary>
        /// Updates the field of an info table
        /// </summary>
        /// <param name="value">Field value</param>
        /// <param name="coord">Coord of field</param>
        /// <param name="rows">Table to modify</param>
        /// <param name="lengths">Item containing table offsets</param>
        static void UpdateInfoTableField(string value, (int, int) coord, List<char[]> rows, List<int> lengths)
        {
            char[] rowToModify = rows[coord.Item2]; // Get the row element (after labels and separator)
            // Overwrite table characters
            int beginningChar = lengths[coord.Item1];
            int endChar = lengths[coord.Item1 + 1];
            for (int i = 0; i < (endChar - beginningChar); i++)
            {
                char charToUse;
                if (i < value.Length) // Still printing string
                {
                    charToUse = value[i];
                }
                else if ((i >= (endChar - beginningChar) - 3)) // column space ended, can finish here
                {
                    break;
                }
                else // Fill the rest blank
                {
                    charToUse = ' ';
                }
                // Ok now print it
                rowToModify[i + beginningChar] = charToUse;
            }
        }
        /// <summary>
        /// Updates info table of current exploration
        /// </summary>
        /// <param name="rows">Table to print</param>
        void RedrawInfoTable(List<char[]> rows)
        {
            int tableStart = (DUNGEON_ROOMS_PER_FLOOR * ROOM_WIDTH) + (ROOM_WIDTH); // Where the info table starts (leave 1 room separation)
            // Printing loop
            for (int i = 0; i < rows.Count; i++)
            {
                Console.SetCursorPosition(tableStart, i);
                Console.Write(new string(rows[i]));
            }
        }
        #endregion
    }
}
