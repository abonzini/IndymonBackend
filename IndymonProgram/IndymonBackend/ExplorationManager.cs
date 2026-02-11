using ParsersAndData;
using ShowdownBot;
using System.Text;

namespace IndymonBackendProgram
{
    public enum ExplorationStepType
    {
        NOP, // No operation, just a pause
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
        DRAW_REGI_EYE,
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
        public Dictionary<string, int>[] MonsFound { get; set; } =
        // 4 levels of mon, from 1-4, associated with floor and a pokeball type
        [
            new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>()
        ];
        public Dictionary<string, int> CommonItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RareItems { get; set; } = new Dictionary<string, int>();
    }
    public class ExplorationManager
    {
        DataContainers _backEndData = null;
        public string Dungeon { get; set; }
        public string Trainer { get; set; }
        public string NextDungeon { get; set; }
        public bool ExplorationFinished { get; set; } = false;
        public List<ExplorationStep> ExplorationSteps { get; set; }
        public ExplorationPrizes Prizes { get; set; } = new ExplorationPrizes();
        Dungeon _dungeonDetails = null;
        int _shinyChance = 500; // Chance for a shiny (1 in 500)
        public ExplorationManager(DataContainers backEndData)
        {
            _backEndData = backEndData;
        }
        public ExplorationManager()
        {

        }
        public void SetBackEndData(DataContainers backEndData)
        {
            _backEndData = backEndData;
        }
        public void InitializeExploration()
        {
            ExplorationSteps = new List<ExplorationStep>(); // Start from scratch!
            // First ask organizer to choose dungeon
            List<string> options = [.. _backEndData.Dungeons.Keys];
            Console.WriteLine("Creating a brand new exploration, which dungeon? (0 for random)");
            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"{i + 1}: {options[i]}, ");
            }
            Console.WriteLine("");
            int selection = int.Parse(Console.ReadLine());
            if (selection == 0)
            {
                selection = Utilities.GetRandomNumber(options.Count);
            }
            else
            {
                selection--; // Make it array-indexable
            }
            Dungeon = options[selection];
            Console.WriteLine(Dungeon);
            // Then which player
            TrainerData trainerData = Utilities.ChooseOneTrainerDialog(TeambuildSettings.EXPLORATION | TeambuildSettings.SMART, _backEndData);
            Trainer = trainerData.Name;
            trainerData.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.EXPLORATION | TeambuildSettings.SMART); // Gets the team for everyone, this time it has no mon limit, and mons initialised in exploration mode (with HP and status), if need to randomize, smart
            // At this point I can check the dungeon "mods", for now just the shiny chance
            if (trainerData.Teamsheet.Any(p => p.Item?.Name == "Shiny Stone")) // A shiny stone equipped makes the chances 1 in 5 (100 times more likely)
            {
                _shinyChance /= 100;
            }
        }
        public void InitializeNextDungeon()
        {
            if (NextDungeon == "") throw new Exception("NO NEXT DUNGEON!");
            ExplorationSteps = new List<ExplorationStep>();
            Dungeon = NextDungeon;
            // Keep trainer as is
        }
        const int STANDARD_MESSAGE_PAUSE = 5000; // Show text for this amount of time
        const int DRAW_ROOM_PAUSE = 1000; // Show text for this amount of time
        const int DUNGEON_NUMBER_OF_FLOORS = 3; // Hardcoded for now unless we need to make it flexible later on
        const int DUNGEON_ROOMS_PER_FLOOR = 5;
        #region EXECUTION
        /// <summary>
        /// Begins an exploration, starts a simulation
        /// </summary>
        public void ExecuteExploration()
        {
            // Just to help debug
            Console.Clear();
            Console.CursorVisible = true;
            // Set up exploration
            _dungeonDetails = _backEndData.Dungeons[Dungeon]; // Obtain dungeon back end data
            TrainerData trainerData = _backEndData.TrainerData[Trainer]; // Obtain trainer's data
            List<RoomEvent> possibleEvents = [.. _dungeonDetails.Events]; // These are the possible events for this dungeon
            // Beginning of expl and event queue
            (int, int) prevCoord = (-1, 0); // Starts from outside i guess
            bool usedShortcut = false; // If a shortcut was used to the new room
            // Print initial string
            string auxString = $"Beginning of {trainerData.Name}'s exploration in {Dungeon}";
            Console.WriteLine(auxString);
            GenericMessageCommand(auxString); // Begin of exploration string
            // Adds status table. Pokemon have a max of 18 characters, health and status max 3. Fill the info too
            AddInfoColumnCommand("Pokemon", 18);
            AddInfoColumnCommand("Health", 6);
            AddInfoColumnCommand("Status", 6);
            AddInfoColumnCommand("PP1", 3);
            AddInfoColumnCommand("PP2", 3);
            AddInfoColumnCommand("PP3", 3);
            AddInfoColumnCommand("PP4", 3);
            UpdateTrainerDataInfo(trainerData);
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
                        ClueMessageCommand(_dungeonDetails.Floors[floor].ShortcutClue);
                        if (VerifyShortcutConditions(_dungeonDetails.Floors[floor].ShortcutConditions, trainerData, out string message)) // If shortcut activated
                        {
                            // After, need to also check shortcut
                            string shortcutString = _dungeonDetails.Floors[floor].ShortcutResolution.Replace("$1", message);
                            GenericMessageCommand(shortcutString);
                            if (floor == DUNGEON_NUMBER_OF_FLOORS - 1) // If dungeon was done in last floor, then the dungeon is over...
                            {
                                DrawConnectRoomCommand(floor, room, floor + 1, room, true); // Connects to invisible next dungeon, shortcut
                                DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                                auxString = $"You move onward...";
                                Console.WriteLine(auxString);
                                GenericMessageCommand(auxString);
                                // Also the trypical backend stuff
                                ExplorationFinished = true;
                                NextDungeon = _dungeonDetails.NextDungeonShortcut; // Go to the dungeon indicated by shortcut
                                goto ExplorationEnd; // Just go directly to end
                            }
                            else
                            {
                                usedShortcut = true;
                                break; // Floor is done, can skip all the rooms
                            }
                        }
                        // If no shortcut taken, can do a camping event at this stage
                        roomSuccess = ExecuteEvent(_dungeonDetails.CampingEvent, floor, trainerData);
                    }
                    else if (room == 1) // And first room always a wild pokemon encounter
                    {
                        RoomEvent pokemonEvent = new RoomEvent()
                        {
                            EventType = RoomEventType.POKEMON_BATTLE,
                            PreEventString = "Suddenly, wild pokemon attack!",
                            PostEventString = $"You won the battle and obtained multiple items that the wild Pokemon were holding ($1)."
                        }; // Wild pokemon encounter event
                        roomSuccess = ExecuteEvent(pokemonEvent, floor, trainerData);
                    }
                    else if ((room == DUNGEON_ROOMS_PER_FLOOR - 1) && (floor == DUNGEON_NUMBER_OF_FLOORS - 1)) // Last room is always boss event
                    {
                        ExecuteEvent(_dungeonDetails.PreBossEvent, floor, trainerData); // Pre boss event
                        roomSuccess = ExecuteEvent(_dungeonDetails.BossEvent, floor, trainerData); // BOSS
                        if (roomSuccess) // If has been beaten, then dungeon is also over
                        {
                            ExecuteEvent(_dungeonDetails.PostBossEvent, floor, trainerData); // Post boss event
                            DrawConnectRoomCommand(floor, room, floor + 1, room, false); // Connects to invisible next dungeon, no shortcut
                            DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                            auxString = $"You move onward...";
                            Console.WriteLine(auxString);
                            GenericMessageCommand(auxString);
                            // Also the trypical backend stuff
                            ExplorationFinished = true;
                            NextDungeon = _dungeonDetails.NextDungeon;
                            goto ExplorationEnd;
                        }
                    }
                    else // Normal room implies a normal event from the possibility list
                    {
                        RoomEvent nextEvent = possibleEvents[Utilities.GetRandomNumber(possibleEvents.Count)]; // Get a random event
                        Console.WriteLine($"Event: {nextEvent}");
                        roomSuccess = ExecuteEvent(nextEvent, floor, trainerData);
                        possibleEvents.Remove(nextEvent); // Remove from event pool
                    }
                    if (!roomSuccess) // Player lost during exploration
                    {
                        ExplorationFinished = true;
                        NextDungeon = "";
                        goto ExplorationEnd; // Just go directly to end
                    }
                }
            }
        ExplorationEnd:
            // Return to normal
            Console.CursorVisible = false;
            Console.WriteLine("Exploration end.");
            SaveExplorationOutcome(Prizes);
            // Finally, need to examine and tell if trainer used/ran out of items
            trainerData.ListConsumedItems(int.MaxValue); // No mon limit for explorations...
        }
        /// <summary>
        /// Executes an event of the many possible in room
        /// </summary>
        /// <param name="roomEvent">Event to simulate</param>
        /// <param name="floor">Floor where event happens</param>
        /// <param name="prizes">Place where to store the things won in this room (mons, items, etc)</param>
        /// <param name="trainerData">Data about the trainer</param>
        /// <returns></returns>
        bool ExecuteEvent(RoomEvent roomEvent, int floor, TrainerData trainerData)
        {
            bool roomCleared = true;
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
                        string itemFound = _dungeonDetails.RareItems[Utilities.GetRandomNumber(_dungeonDetails.RareItems.Count)]; // Find a random rare item
                        Console.WriteLine($"Finds {itemFound}");
                        string itemString = roomEvent.PreEventString.Replace("$1", itemFound);
                        GenericMessageCommand(itemString); // Prints the message but we know it could have a $1
                        itemString = roomEvent.PostEventString.Replace("$1", itemFound);
                        GenericMessageCommand(itemString);
                        AddRareItemPrize(itemFound, Prizes);
                    }
                    break;
                case RoomEventType.BOSS: // Boss fight
                    {
                        string item = (_dungeonDetails.BossItem != "") ? _dungeonDetails.BossItem : "";
                        int enemyFloor = 3; // Last floor is where bosses are
                        List<string> possiblePokemon = _dungeonDetails.PokemonEachFloor[enemyFloor]; // Find the possible mons next floor
                        string enemySpecies = possiblePokemon[Utilities.GetRandomNumber(possiblePokemon.Count)].Trim().ToLower(); // Get a random one of these
                        Console.WriteLine($"Boss {enemySpecies} holding {item}");
                        string bossString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                        bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0); // Will be shiny if i get a 0 dice roll
                        PokemonSet bossPokemon = new PokemonSet()
                        {
                            Species = enemySpecies,
                            Shiny = isShiny,
                            Item = (item.ToLower().Contains("titan plate")) ? null : new Item() { Name = item, Uses = 1 } // Ensure battle item is really there
                        };
                        TrainerData bossTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "boss",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = [bossPokemon], // Only mon in the teamsheet
                        };
                        bossTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.SMART); // Randomize enemy team (movesets, etc), boss/alpha is a bit smarter than normal dungeon mon
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, bossTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            bossString = roomEvent.PostEventString.Replace("$1", item);
                            GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                            AddRareItemPrize(item, Prizes); // Add item to Prizes
                            AddPokemonPrize(enemySpecies, enemyFloor, isShiny, Prizes); // Add boss mon too
                        }
                    }
                    break;
                case RoomEventType.ALPHA: // Find a frenzied mon from a floor above, boss will have a rare item if defeated
                    {
                        string item = _dungeonDetails.RareItems[Utilities.GetRandomNumber(_dungeonDetails.RareItems.Count)].Trim().ToLower(); // Get a random rare item
                        int enemyFloor = (floor + 1 >= DUNGEON_NUMBER_OF_FLOORS) ? floor : floor + 1; // Find enemy of next floor if possible
                        List<string> possiblePokemon = _dungeonDetails.PokemonEachFloor[enemyFloor]; // Find the possible mons next floor
                        string enemySpecies = possiblePokemon[Utilities.GetRandomNumber(possiblePokemon.Count)].Trim().ToLower(); // Get a random one of these
                        Console.WriteLine($"Strong {enemySpecies} holding {item}");
                        string alphaString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0); // Will be shiny if i get a 0 dice roll
                        PokemonSet alphaPokemon = new PokemonSet()
                        {
                            Species = enemySpecies,
                            Shiny = isShiny,
                            Item = new Item() { Name = item, Uses = 1 }
                        };
                        TrainerData alphaTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "alpha mon",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = [alphaPokemon], // Only mon in the teamsheet
                        };
                        alphaTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.SMART); // Randomize enemy team (movesets, etc), boss/alpha is a bit smarter than normal dungeon mon
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, alphaTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            alphaString = roomEvent.PostEventString.Replace("$1", item);
                            GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                            AddRareItemPrize(item, Prizes); // Add item to Prizes
                            AddPokemonPrize(enemySpecies, enemyFloor, isShiny, Prizes); // Add alpha mon too
                        }
                    }
                    break;
                case RoomEventType.EVO:
                    {
                        EvolutionMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            Pokemon baseMon = _backEndData.Dex[mon.Species];
                            if (baseMon.Evos.Count > 0) // Mon has evos, ask for each
                            {
                                Console.WriteLine($"Evolve {mon} ? y/N");
                                if (Console.ReadLine().Trim().ToLower() == "y")
                                {
                                    List<string> possibleEvos = [.. baseMon.Evos];
                                    Console.Write($"0 RANDOM,");
                                    for (int i = 0; i < possibleEvos.Count; i++)
                                    {
                                        Console.Write($"{i + 1} {possibleEvos[i]},");
                                    }
                                    int choice = int.Parse(Console.ReadLine());
                                    string chosenMon;
                                    if (choice == 0) // Random evo
                                    {
                                        chosenMon = possibleEvos[Utilities.GetRandomNumber(possibleEvos.Count)];
                                    }
                                    else
                                    {
                                        chosenMon = possibleEvos[choice - 1];
                                    }
                                    // Notify all
                                    string message = mon.NickName != "" ? $"{mon.NickName} ({mon.Species})" : mon.Species;
                                    message += $" has evolved into {chosenMon}!";
                                    GenericMessageCommand(message);
                                    // Finally, actually do the deed
                                    mon.Species = chosenMon.Trim().ToLower();
                                    UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                                }
                            }
                        }
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.RESEARCHER:
                    {
                        List<string> platesList = [.. _backEndData.OffensiveItemData.Keys.Where(i => i.Contains("plate"))];
                        string chosenPlate = platesList[Utilities.GetRandomNumber(platesList.Count)];
                        string messageString = roomEvent.PreEventString.Replace("$1", chosenPlate);
                        GenericMessageCommand(messageString);
                        AddCommonItemPrize(chosenPlate, Prizes);
                        messageString = roomEvent.PostEventString.Replace("$1", chosenPlate);
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.PARADOX:
                    {
                        Console.WriteLine("Event tile, consists of only text and resolves. MAY INVOLVE A FEW EXTRA ITEMS");
                        string obtainedDisk = _backEndData.MoveItemData.Keys.ToList()[Utilities.GetRandomNumber(_backEndData.MoveItemData.Count)]; // Get random move disk
                        string messageString = roomEvent.PreEventString.Replace("$1", obtainedDisk);
                        GenericMessageCommand(messageString);
                        AddCommonItemPrize(obtainedDisk, Prizes);
                        messageString = roomEvent.PostEventString.Replace("$1", obtainedDisk);
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.IMP_GAIN:
                    {
                        int impGain = Utilities.GetRandomNumber(2, 4); // 2-3 IMP
                        string messageString = roomEvent.PreEventString.Replace("$1", $"{impGain} IMP");
                        GenericMessageCommand(messageString);
                        AddCommonItemPrize($"{impGain} IMP", Prizes);
                        messageString = roomEvent.PostEventString.Replace("$1", $"{impGain} IMP");
                        GenericMessageCommand(messageString);
                    }
                    break;
                case RoomEventType.HEAL:
                    {
                        Console.WriteLine("A heal of 33% of all mons");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            mon.ExplorationStatus.HealthPercentage += 33;
                            if (mon.ExplorationStatus.HealthPercentage > 100)
                            {
                                mon.ExplorationStatus.HealthPercentage = 100;
                            }
                        }
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.DAMAGE_TRAP:
                    {
                        Console.WriteLine("A damage trap of 25% to all mons");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            mon.ExplorationStatus.HealthPercentage -= 25;
                            if (mon.ExplorationStatus.HealthPercentage <= 0)
                            {
                                mon.ExplorationStatus.HealthPercentage = 1;
                            }
                        }
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.CURE:
                    {
                        Console.WriteLine("Cures all mons status");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            mon.ExplorationStatus.NonVolatileStatus = "";
                        }
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.BIG_HEAL:
                    {
                        Console.WriteLine("Single big heal to a mon");
                        PokemonSet mon = trainerData.Teamsheet.OrderBy(p => p.ExplorationStatus.HealthPercentage).FirstOrDefault();
                        mon.ExplorationStatus.HealthPercentage = 100;
                        string message = roomEvent.PreEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(roomEvent.PreEventString);
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        message = roomEvent.PostEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(message);
                    }
                    break;
                case RoomEventType.PP_HEAL:
                    {
                        Console.WriteLine("Cures all mons PP");
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            for (int i = 0; i < mon.ExplorationStatus.MovePp.Length; i++) // Restores 3 pp to each move
                            {
                                mon.ExplorationStatus.MovePp[i] += 3;
                            }
                        }
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.STATUS_TRAP:
                    {
                        Console.WriteLine("A trap that will status one mon");
                        string status = roomEvent.SpecialParams;
                        List<PokemonSet> possibleMons = new List<PokemonSet>();
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (PokemonSet mon in trainerData.Teamsheet)
                        {
                            if (mon.ExplorationStatus.NonVolatileStatus == "")
                            {
                                possibleMons.Add(mon);
                            }
                        }
                        // Choose a mon without status as priority otherwise any mon will have it overriden
                        PokemonSet statusedMon;
                        if (possibleMons.Count > 0)
                        {
                            statusedMon = possibleMons[Utilities.GetRandomNumber(possibleMons.Count - 1)];
                        }
                        else
                        {
                            statusedMon = trainerData.Teamsheet[Utilities.GetRandomNumber(trainerData.Teamsheet.Count)];
                        }
                        statusedMon.ExplorationStatus.NonVolatileStatus = status;
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                        string postMessage = roomEvent.PostEventString.Replace("$1", statusedMon.GetInformalName());
                        GenericMessageCommand(postMessage);
                    }
                    break;
                case RoomEventType.POKEMON_BATTLE:
                    // Similar to aplha but there's 3 enemy mons
                    {
                        const int NUMBER_OF_WILD_POKEMON = 3; // Time to balance-hardcode this
                        const int ITEMS_PER_WILD_POKEMON = 1;
                        Console.WriteLine("Pokemon battle");
                        // Items obtained during the fight (commons)
                        int itemCount = NUMBER_OF_WILD_POKEMON * ITEMS_PER_WILD_POKEMON;
                        List<string> items = new List<string>();
                        for (int i = 0; i < itemCount; i++)
                        {
                            // Prize pool will contain common items
                            string item = _dungeonDetails.CommonItems[Utilities.GetRandomNumber(_dungeonDetails.CommonItems.Count)].Trim().ToLower();
                            Console.WriteLine($"Item: {item}");
                            items.Add(item);
                        }
                        // Add Pokemon, they will have items
                        List<string> pokemonThisFloor = _dungeonDetails.PokemonEachFloor[floor]; // Find the possible mons this floor
                        List<PokemonSet> encounterPokemon = new List<PokemonSet>();
                        for (int i = 0; i < NUMBER_OF_WILD_POKEMON; i++) // Generate party of random mons
                        {
                            string pokemonSpecies = pokemonThisFloor[Utilities.GetRandomNumber(pokemonThisFloor.Count)].Trim().ToLower();
                            bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0);
                            PokemonSet pokemon = new PokemonSet()
                            {
                                Species = pokemonSpecies,
                                Shiny = isShiny,
                                Item = (i < itemCount) ? new Item() { Name = items[i], Uses = 1 } : null // If still have items available, give one to mon
                            };
                            Console.WriteLine($"Mon: {pokemonSpecies}");
                            encounterPokemon.Add(pokemon); // Add mon to the set
                        }
                        if (itemCount < NUMBER_OF_WILD_POKEMON)
                        {
                            // If not all Pokemon have items, shuffle the mons so the items are not clumped only at the beginning
                            Utilities.ShuffleList(encounterPokemon, 0, encounterPokemon.Count);
                        }
                        // Ok now begin event
                        GenericMessageCommand(roomEvent.PreEventString);
                        TrainerData wildMonTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "wild mons",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = encounterPokemon,
                        };
                        wildMonTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.NONE); // Randomize enemy team (movesets, etc)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, wildMonTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", string.Join(',', items));
                            GenericMessageCommand(postMessage);
                            foreach (string item in items) // Add all items to Prizes
                            {
                                AddCommonItemPrize(item, Prizes);
                            }
                            foreach (PokemonSet pokemonSpecies in encounterPokemon)
                            {
                                AddPokemonPrize(pokemonSpecies.Species, floor, pokemonSpecies.Shiny, Prizes); // Add all mons
                            }
                        }
                    }
                    break;
                case RoomEventType.SWARM:
                    // Similar to aplha but there's 6 enemy mons
                    {
                        const int NUMBER_OF_WILD_POKEMON = 6; // Time to balance-hardcode this
                        Console.WriteLine("Swarm battle");
                        // Items obtained during the fight (commons)
                        List<string> pokemonThisFloor = _dungeonDetails.PokemonEachFloor[0]; // Always from first floor
                        List<PokemonSet> encounterPokemon = new List<PokemonSet>();
                        for (int i = 0; i < NUMBER_OF_WILD_POKEMON; i++) // Generate party of random mons
                        {
                            string pokemonSpecies = pokemonThisFloor[Utilities.GetRandomNumber(pokemonThisFloor.Count)].Trim().ToLower();
                            bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0);
                            int level = Utilities.GetRandomNumber(60, 76); // Lvl between 60-75
                            PokemonSet pokemon = new PokemonSet()
                            {
                                Species = pokemonSpecies,
                                Shiny = isShiny,
                                Level = level,
                            };
                            Console.WriteLine($"Mon: {pokemonSpecies} lvl {level}");
                            encounterPokemon.Add(pokemon); // Add mon to the set
                        }
                        // Ok now begin event
                        GenericMessageCommand(roomEvent.PreEventString);
                        TrainerData wildMonTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "wild babies",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = encounterPokemon,
                        };
                        wildMonTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.NONE); // Randomize enemy team (movesets, etc)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, wildMonTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString);
                            foreach (PokemonSet pokemonSpecies in encounterPokemon)
                            {
                                AddPokemonPrize(pokemonSpecies.Species, 0, pokemonSpecies.Shiny, Prizes); // Add all mons (they're floor 0)
                            }
                        }
                    }
                    break;
                case RoomEventType.UNOWN:
                    // Weird one, select 6 unowns, give them random moves
                    {
                        const int NUMBER_OF_WILD_POKEMON = 6; // Time to balance-hardcode this
                        Console.WriteLine("Unown battle");
                        // Items obtained during the fight (commons)
                        List<PokemonSet> encounterPokemon = new List<PokemonSet>();
                        HashSet<char> usedLetters = [];
                        for (int i = 0; i < NUMBER_OF_WILD_POKEMON; i++) // Generate party of random mons
                        {
                            char letter;
                            do
                            {
                                letter = (char)Utilities.GetRandomNumber('A', 'Z' + 1); // Shoudl give me a random letter lol
                            } while (usedLetters.Contains(letter)); // Get unique letters please
                            usedLetters.Add(letter);
                            string pokemonSpecies = (letter == 'A') ? $"Unown" : $"Unown-{letter}"; // Unown or Unown-*
                            bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0);
                            PokemonSet pokemon = new PokemonSet()
                            {
                                Species = pokemonSpecies,
                                NickName = letter.ToString().ToUpper(),
                                Shiny = isShiny,
                            };
                            Console.WriteLine($"Mon: {pokemonSpecies}");
                            encounterPokemon.Add(pokemon); // Add mon to the set
                        }
                        // Ok now begin event
                        GenericMessageCommand(roomEvent.PreEventString);
                        TrainerData wildMonTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "symbols",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = encounterPokemon,
                        };
                        wildMonTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.NONE); // Randomize enemy team (movesets, etc)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, wildMonTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString);
                            foreach (PokemonSet pokemonSpecies in encounterPokemon)
                            {
                                AddPokemonPrize(pokemonSpecies.Species, 0, pokemonSpecies.Shiny, Prizes); // Add all unowns (they're floor 0)
                            }
                        }
                    }
                    break;
                case RoomEventType.FIRELORD:
                    // Weird one, weakened legendary with rare item
                    {
                        Console.WriteLine("Firelord battle");
                        // Which mon will be chosen
                        string item = _dungeonDetails.RareItems[Utilities.GetRandomNumber(_dungeonDetails.RareItems.Count)].Trim().ToLower(); // Get a random rare item
                        List<string> validMons = ["moltres", "entei", "ho-oh", "groudon", "heatran", "chi-yu", "koraidon", "volcaion", "blacephalon"];
                        string pokemonSpecies = validMons[Utilities.GetRandomNumber(validMons.Count)];
                        List<PokemonSet> encounterPokemon = new List<PokemonSet>();
                        bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0);
                        PokemonSet pokemon = new PokemonSet()
                        {
                            Species = pokemonSpecies,
                            Shiny = isShiny,
                            Level = Utilities.GetRandomNumber(50, 66),
                            Item = new Item() { Name = item, Uses = 1 }
                        };
                        Console.WriteLine($"Mon: {pokemonSpecies}");
                        encounterPokemon.Add(pokemon); // Add mon to the set
                        // Ok now begin event
                        GenericMessageCommand(roomEvent.PreEventString);
                        TrainerData wildMonTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "firelord",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = encounterPokemon,
                        };
                        wildMonTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.NONE); // Randomize enemy team (movesets, etc)
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, wildMonTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", item);
                            GenericMessageCommand(postMessage);
                            AddPokemonPrize(pokemonSpecies, 3, isShiny, Prizes); // Add the mon (masterball tho)
                        }
                    }
                    break;
                case RoomEventType.GIANT_POKEMON:
                    // Single pokemon with a rare item but the mon is lvl 110-125
                    {
                        string item = _dungeonDetails.RareItems[Utilities.GetRandomNumber(_dungeonDetails.RareItems.Count)].Trim().ToLower(); // Get a random rare item
                        List<string> potentialPokemon = _dungeonDetails.PokemonEachFloor[floor]; // Find the possible mons this floor
                        string pokemonSpecies = potentialPokemon[Utilities.GetRandomNumber(potentialPokemon.Count)].Trim().ToLower(); // Get a random one of these
                        int level = Utilities.GetRandomNumber(110, 126); // Get lvls 110-125
                        string alphaString = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0); // Will be shiny if i get a 0 dice roll
                        PokemonSet giantPokemon = new PokemonSet()
                        {
                            Species = pokemonSpecies,
                            Shiny = isShiny,
                            Level = level,
                            Item = new Item() { Name = item, Uses = 1 }
                        };
                        TrainerData giantTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "giant mon",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = [giantPokemon], // Only mon in the teamsheet
                        };
                        giantTeam.ConfirmSets(_backEndData, 1, int.MaxValue, TeambuildSettings.SMART); // Randomize enemy team (movesets, etc), boss/alpha is a bit smarter than normal dungeon mon
                        int remainingMons = ResolveEncounter(trainerData, giantTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            alphaString = roomEvent.PostEventString.Replace("$1", item);
                            GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                            AddRareItemPrize(item, Prizes); // Add item to Prizes
                            AddPokemonPrize(pokemonSpecies, floor, isShiny, Prizes); // Add giant mon too
                        }
                    }
                    break;
                case RoomEventType.MIRROR_MATCH:
                    // Fight against yourself but a couple levels lower
                    {
                        TrainerData copiedTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = trainerData.Avatar,
                            Name = "illusion",
                            AutoItem = false,
                            AutoTeam = false,
                            Teamsheet = [], // Will add mons after
                        };
                        foreach (PokemonSet set in trainerData.Teamsheet)
                        {
                            int level = Utilities.GetRandomNumber(75, 91); // Get lvls 75-90
                            PokemonSet copiedMon = new PokemonSet()
                            {
                                Species = set.Species,
                                Shiny = set.Shiny,
                                Level = level,
                                Item = set.Item,
                                Ability = set.Ability,
                                Moves = set.Moves,
                                ExplorationStatus = set.ExplorationStatus
                            };
                            copiedTeam.Teamsheet.Add(copiedMon);
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        int remainingMons = ResolveEncounter(trainerData, copiedTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString); // Prints the message but we know it could have a $1
                        }
                    }
                    break;
                case RoomEventType.PLOT_CLUE:
                    {
                        PlotMessageCommand(roomEvent.PreEventString);
                        PlotMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.JOINER:
                    // Mon that joins the team for adventures, always catchable
                    {
                        List<string> pokemonThisFloor = _dungeonDetails.PokemonEachFloor[floor]; // Find the possible mons this floor
                        string pokemonSpecies = pokemonThisFloor[Utilities.GetRandomNumber(pokemonThisFloor.Count)].Trim().ToLower(); // Get a random one of these
                        Console.WriteLine($"Joiner {pokemonSpecies}");
                        string joinerString = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(joinerString); // Prints the message but we know it could have a $1
                        bool isShiny = (Utilities.GetRandomNumber(_shinyChance) == 0); // Will be shiny if i get a 0 dice roll
                        string nickName = $"{pokemonSpecies} friend";
                        if (nickName.Length > 18) // Sanitize, name has to be shorter than 19 and no spaces
                        {
                            nickName = nickName[..18].Trim();
                        }
                        PokemonSet joiner = new PokemonSet()
                        {
                            Species = pokemonSpecies,
                            Shiny = isShiny,
                            NickName = nickName,
                            ExplorationStatus = new ExplorationStatus()
                        };
                        joiner.RandomizeMon(_backEndData, TeambuildSettings.NONE, 10); // Random set of moves, 10% switch new standard
                        Console.Write("Added to team");
                        trainerData.Teamsheet.Add(joiner);
                        GenericMessageCommand(roomEvent.PostEventString);
                        AddPokemonPrize(pokemonSpecies, 0, isShiny, Prizes); // Add mon to always capturable
                        UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                    }
                    break;
                case RoomEventType.NPC_BATTLE:
                    {
                        TrainerData randomNpc = _backEndData.NpcData.Values.ToList()[Utilities.GetRandomNumber(_backEndData.NpcData.Values.Count)]; // Get random npc
                        int nMons = Math.Min(randomNpc.Teamsheet.Count, trainerData.Teamsheet.Count); // Fight with the highest legal fair count
                        randomNpc.ConfirmSets(_backEndData, nMons, nMons, TeambuildSettings.SMART); // Get into a trainer fight (they will bring atleast 1 mon at most 3
                        Console.WriteLine($"Fighting {randomNpc.Name}");
                        string npcString = roomEvent.PreEventString.Replace("$1", randomNpc.Name);
                        GenericMessageCommand(npcString); // Prints the message but we know it could have a $1
                        // Heal first nMons
                        for (int i = 0; i < nMons && i < trainerData.Teamsheet.Count; i++)
                        {
                            trainerData.Teamsheet[i].ExplorationStatus.HealthPercentage = 100;
                            trainerData.Teamsheet[i].ExplorationStatus.NonVolatileStatus = "";
                        }
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, randomNpc, nMons); // 3 Mon both players
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(trainerData); // Updates numbers in chart
                            AddRareItemPrize($"{randomNpc.Name}'s favor", Prizes);
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
                case RoomEventType.REGICE: // Dramatic drawing of regice eyes
                    DrawRegiEye(0, 0, 1000);
                    DrawRegiEye(0, -1, 0);
                    DrawRegiEye(0, 1, 1000);
                    DrawRegiEye(1, 0, 0);
                    DrawRegiEye(-1, 0, 500);
                    DrawRegiEye(2, 0, 0);
                    DrawRegiEye(-2, 0, 500);
                    break;
                case RoomEventType.REGIELEKI: // Dramatic drawing of regigas eyes
                    DrawRegiEye(0, 0, 250);
                    DrawRegiEye(1, 0, 250);
                    DrawRegiEye(-1, 0, 250);
                    DrawRegiEye(2, 0, 250);
                    DrawRegiEye(-2, 0, 250);
                    DrawRegiEye(3, 1, 250);
                    DrawRegiEye(3, -1, 250);
                    DrawRegiEye(-3, -1, 250);
                    DrawRegiEye(-3, 1, 250);
                    break;
                default:
                    break;
            }
            return roomCleared;
        }
        /// <summary>
        /// Saves exploration outcome in a txt file for quick copying
        /// </summary>
        /// <param name="prizeData">Data of obtained stuff</param>
        void SaveExplorationOutcome(ExplorationPrizes prizeData)
        {
            string explFile = $"expl_result.txt";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"__{Trainer} has obtained the following items:__");
            builder.AppendLine();
            List<string> nextCollection = new List<string>();
            // Attach commons (or empty list) items
            foreach (KeyValuePair<string, int> commonItemData in prizeData.CommonItems)
            {
                nextCollection.Add($"{commonItemData.Key} x{commonItemData.Value}");
            }
            builder.Append("**Commons: **||");
            if (nextCollection.Count > 0) builder.Append(string.Join(',', nextCollection));
            else builder.Append(new string('M', Utilities.GetRandomNumber(15, 30)));
            builder.AppendLine("||");
            // Attach rares (or empty list) items
            nextCollection = new List<string>();
            foreach (KeyValuePair<string, int> rareItemData in prizeData.RareItems)
            {
                nextCollection.Add($"{rareItemData.Key} x{rareItemData.Value}");
            }
            builder.Append("**Rares: **||");
            if (nextCollection.Count > 0) builder.Append(string.Join(',', nextCollection));
            else builder.Append(new string('M', Utilities.GetRandomNumber(15, 30)));
            builder.AppendLine("||");
            builder.AppendLine();
            // Then the pokemon one by one
            builder.AppendLine($"__Catchable Pokemon (As many as you want as long as you have the corresponding poke-ball):__");
            builder.AppendLine();
            string[] pokeBallData = ["Poke ball or better", "Great ball or better", "Ultra ball or better", "Master ball"];
            for (int i = 0; i < prizeData.MonsFound.Length; i++)
            {
                nextCollection = new List<string>();
                builder.Append($"**Floor {i + 1} ({pokeBallData[i]}): **||");
                foreach (KeyValuePair<string, int> monData in prizeData.MonsFound[i])
                {
                    nextCollection.Add($"{monData.Key} x{monData.Value}");
                }
                if (nextCollection.Count > 0) builder.Append(string.Join(',', nextCollection));
                else builder.Append(new string('M', Utilities.GetRandomNumber(15, 30)));
                builder.AppendLine("||");
            }
            // String done, save into file
            File.WriteAllText(explFile, builder.ToString());
        }
        /// <summary>
        /// Updates all the trainer data in the exploration info table
        /// </summary>
        /// <param name="trainerData">Trainer containing the mons</param>
        void UpdateTrainerDataInfo(TrainerData trainerData)
        {
            for (int i = 0; i < trainerData.Teamsheet.Count; i++)
            {
                PokemonSet mon = trainerData.Teamsheet[i];
                Console.WriteLine($"{mon.GetInformalName()} status is {mon.ExplorationStatus}");
                // Print stuff
                ModifyInfoValueCommand(mon.Species, (0, i));
                ModifyInfoValueCommand($"{mon.ExplorationStatus.HealthPercentage}%", (1, i));
                ModifyInfoValueCommand(mon.ExplorationStatus.NonVolatileStatus, (2, i));
                ModifyInfoValueCommand((mon.ExplorationStatus.MovePp[0] == 99) ? "??" : mon.ExplorationStatus.MovePp[0].ToString(), (3, i));
                ModifyInfoValueCommand((mon.ExplorationStatus.MovePp[1] == 99) ? "??" : mon.ExplorationStatus.MovePp[1].ToString(), (4, i));
                ModifyInfoValueCommand((mon.ExplorationStatus.MovePp[2] == 99) ? "??" : mon.ExplorationStatus.MovePp[2].ToString(), (5, i));
                ModifyInfoValueCommand((mon.ExplorationStatus.MovePp[3] == 99) ? "??" : mon.ExplorationStatus.MovePp[3].ToString(), (6, i));
            }
        }
        /// <summary>
        /// Verifies whether a trainer fills the consitions to use a shortcut
        /// </summary>
        /// <param name="conditions">Shortcut conditions</param>
        /// <param name="trainerData">Data of trainer</param>
        /// <param name="message">An extra return indicating the message used (E.g. X used Y)</param>
        /// <returns>Whether shortcut activates or not</returns>
        bool VerifyShortcutConditions(List<ShortcutCondition> conditions, TrainerData trainerData, out string message)
        {
            bool canTakeShortcut = false;
            message = "";
            foreach (ShortcutCondition condition in conditions)
            {
                foreach (string eachOne in condition.Which)
                {
                    string valueToCheck = eachOne.ToLower();
                    switch (condition.ConditionType)
                    {
                        case ShortcutConditionType.MOVE:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                            {
                                if (pokemon.Moves.Contains(valueToCheck)) // move found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {valueToCheck}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.ABILITY:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has ability, all good
                            {
                                if (pokemon.Ability == valueToCheck) // ability found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {valueToCheck}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.POKEMON:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon is there, all good
                            {
                                if (pokemon.Species == valueToCheck) // species found
                                {
                                    message = $"{pokemon.GetInformalName()}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.TYPE:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has type, all good
                            {
                                if (_backEndData.Dex[pokemon.Species].Types.Contains(valueToCheck)) // type of pokemon found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {valueToCheck} type";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.ITEM:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has item, all good
                            {
                                if (pokemon.Item != null && pokemon.Item.Name == valueToCheck) // item found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {valueToCheck}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.MOVE_DISK:
                            foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has item, all good
                            {
                                if (pokemon.Item != null && pokemon.Item.Name.ToLower().Contains(" disk")) // disk found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {pokemon.Item.Name}";
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
        /// Adds to event queue, a generic message string
        /// </summary>
        /// <param name="message">String to add</param>
        void GenericMessageCommand(string message)
        {
            Console.WriteLine($"> {message}"); // Important for debug too
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.PRINT_STRING,
                StringParam = message,
                MillisecondsWait = STANDARD_MESSAGE_PAUSE
            });
        }
        /// <summary>
        /// Adds to event queue, a generic message string. Will be informative (i.e. gives clue to a player)
        /// </summary>
        /// <param name="message">String to add</param>
        void ClueMessageCommand(string message)
        {
            Console.WriteLine($"> {message}"); // Important for debug too
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.PRINT_CLUE,
                StringParam = message,
                MillisecondsWait = STANDARD_MESSAGE_PAUSE
            });
        }
        /// <summary>
        /// Adds to event queue, a plot message string. Will be plot-based (i.e. gives clue to a player)
        /// </summary>
        /// <param name="message">String to add</param>
        void PlotMessageCommand(string message)
        {
            Console.WriteLine($"> {message}"); // Important for debug too
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.PRINT_PLOT,
                StringParam = message,
                MillisecondsWait = STANDARD_MESSAGE_PAUSE
            });
        }
        /// <summary>
        /// Adds to event queue, a generic message regarding pokemon evolution
        /// </summary>
        /// <param name="message">String to add</param>
        void EvolutionMessageCommand(string message)
        {
            Console.WriteLine($"> {message}"); // Important for debug too
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.PRINT_EVOLUTION,
                StringParam = message,
                MillisecondsWait = STANDARD_MESSAGE_PAUSE
            });
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
        /// Resolves an encounter between players, no mon limit
        /// </summary>
        /// <param name="epxlorer">P1</param>
        /// <param name="encounter">P2</param>
        /// <param name="nMons">If this encounter has a specific number of mons (default is all v all)</param>
        /// <returns>How many mons P1 has left (0 means defeat)</returns>
        int ResolveEncounter(TrainerData epxlorer, TrainerData encounter, int nMons = int.MaxValue)
        {
            (int cursorX, int cursorY) = Console.GetCursorPosition(); // Just in case I need to write in same place
            Console.Write("About to simulate bots...");
            Console.ReadLine();
            BotBattle automaticBattle = new BotBattle(_backEndData); // Generate bot host
            // The challenge string may contain dungeon-specific rules (besides the mandatory ones)
            List<string> showdownRules = ["!Team Preview", "OHKO Clause", "Evasion Moves Clause", "Moody Clause", .. _dungeonDetails.CustomShowdownRules];
            string challengeString = $"gen9customgame@@@{string.Join(",", showdownRules)}"; // Assemble resulting challenge string
            (int explorerLeft, _) = automaticBattle.SimulateBotBattle(epxlorer, encounter, nMons, nMons, challengeString); // Initiate battle
            Console.SetCursorPosition(cursorX, cursorY);
            Console.Write($"Explorer left with {explorerLeft} mons. GET THE REPLAY");
            return explorerLeft;
        }
        /// <summary>
        /// Adds item to common prize pool
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="prizes">Prizes container</param>
        static void AddCommonItemPrize(string item, ExplorationPrizes prizes)
        {
            // Add the item to prizes
            if (prizes.CommonItems.TryGetValue(item, out int previousCount)) prizes.CommonItems[item] = ++previousCount;
            else prizes.CommonItems.Add(item, 1);
        }
        /// <summary>
        /// Adds item to common prize pool
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="prizes">Prizes container</param>
        static void AddRareItemPrize(string item, ExplorationPrizes prizes)
        {
            // Add the item to prizes
            if (prizes.RareItems.TryGetValue(item, out int previousCount)) prizes.RareItems[item] = ++previousCount;
            else prizes.RareItems.Add(item, 1);
        }
        /// <summary>
        /// Add pokemon to prize pool
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="floor">Which floor to add (0-4), also corresponds to pokeball</param>
        /// <param name="shiny">Mon is shiny</param>
        /// <param name="prizes">Prizes list</param>
        static void AddPokemonPrize(string mon, int floor, bool shiny, ExplorationPrizes prizes)
        {
            if (shiny) mon += "★"; // Add shiny tag too
            Dictionary<string, int> monList = prizes.MonsFound[floor];
            if (monList.TryGetValue(mon, out int count)) monList[mon] = ++count;
            else monList.Add(mon, 1);
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
            char[] infoTableTemplate = "|".ToCharArray();
            List<int> infoColOffset = [2]; // First element "would" start from here
            List<char[]> infoRows = [[.. infoTableTemplate], new string('-', infoTableTemplate.Length).ToCharArray()]; // Starts with the (empty) table header, and a separator
            Console.WriteLine("Write anything to begin. Better start recording now");
            Console.ReadLine();
            Console.Clear();
            _dungeonDetails = _backEndData.Dungeons[Dungeon]; // Obtain dungeon back end data just in case it's not there yet
            int consoleLineStart = (DUNGEON_NUMBER_OF_FLOORS * ROOM_HEIGHT) + DUNGEON_NUMBER_OF_FLOORS + 1; // Rooms + spaces between, above and below
            int consoleOffset = consoleLineStart; // Where console currently at
            string emptyLine = new string(' ', Console.WindowWidth);
            foreach (ExplorationStep nextStep in ExplorationSteps) // Now, will do event one by one...
            {
                switch (nextStep.Type)
                {
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
            int roomY = _dungeonDetails.GoesDownwards ? floor : DUNGEON_NUMBER_OF_FLOORS - floor - 1; // Get the Y coord (vertical)
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
            DungeonFloor floorData = _dungeonDetails.Floors[floor]; // Obtain room drawing data
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
            char playerSymbol = Trainer.ToUpper()[0];
            Console.ForegroundColor = ConsoleColor.White; // Set color back to white, for character
            // Delete current character first
            if (IsRoomValid(sourceFloor, sourceRoom))
            {
                (int fromX, int fromY) = GetRoomCoords(sourceFloor, sourceRoom);
                Console.SetCursorPosition(fromX + (ROOM_WIDTH / 2), fromY + (ROOM_HEIGHT / 2));
                Console.Write(" ");
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
                sourceFloorData = _dungeonDetails.Floors[sourceFloor];
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
                destFloorData = _dungeonDetails.Floors[destFloor];
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
                sourceFloorData = _dungeonDetails.Floors[sourceFloor];
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
                destFloorData = _dungeonDetails.Floors[destFloor];
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
