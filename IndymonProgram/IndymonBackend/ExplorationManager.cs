using ParsersAndData;
using ShowdownBot;

namespace IndymonBackend
{
    public enum ExplorationStepType
    {
        NOP, // No operation, just a pause
        PRINT_STRING,
        CLEAR_CONSOLE,
        DRAW_ROOM,
        MOVE_CHARACTER,
        CONNECT_ROOMS_PASSAGE,
        CONNECT_ROOMS_SHORTCUT,
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
        public string Message { get; set; } // For printing string
        public string Param1 { get; set; } // Strings containing $1 will be replaced by this
        public (int, int) SourceCoord { get; set; } // Coord of the source room when moving
        public (int, int) DestCoord { get; set; } // Coord of destination room when moving
        public int MillisecondsWait { get; set; }
        public override string ToString()
        {
            return Type.ToString();
        }
    }
    public class ExplorationManager
    {
        Random _rng = new Random();
        DataContainers _backEndData = null;
        public string Dungeon { get; set; }
        public string Trainer { get; set; }
        public string NextDungeon { get; set; }
        public bool ExplorationFinished { get; set; } = false;
        public List<ExplorationStep> ExplorationSteps { get; set; }
        Dungeon _dungeonDetails = null;
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
            List<string> options = _backEndData.Dungeons.Keys.ToList();
            Console.WriteLine("Creating a brand new exploration, which dungeon?");
            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"{i + 1}: {options[i]}, ");
            }
            Console.WriteLine("");
            Dungeon = options[int.Parse(Console.ReadLine()) - 1];
            // Then which player
            options = _backEndData.TrainerData.Keys.ToList();
            Console.WriteLine("Which trainer?");
            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"{i + 1}: {options[i]}, ");
            }
            Trainer = options[int.Parse(Console.ReadLine()) - 1];
            // Finally, try to define teamsheet
            TrainerData trainerData = _backEndData.TrainerData[Trainer];
            trainerData.ConfirmSets(_backEndData, int.MaxValue, true, true); // Gets the team for everyone, this time it has no mon limit, and mons initialised in exploration mode (with HP and status)
        }
        public void InitializeNextDungeon()
        {
            if (NextDungeon == "") throw new Exception("NO NEXT DUNGEON!");
            ExplorationSteps = new List<ExplorationStep>();
            Dungeon = NextDungeon;
            // Keep trainer as is
        }
        class ExplorationPrizes
        {
            public Dictionary<string, int>[] MonsFound =
            // 4 levels of mon, from 1-4, associated with floor and a pokeball type
            [
                new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>()
            ];
            public Dictionary<string, int> ItemsFound = new Dictionary<string, int>();
        }
        const int STANDARD_MESSAGE_PAUSE = 3000; // Show text for this amount of time
        const int DRAW_ROOM_PAUSE = 1000; // Show text for this amount of time
        const int SHINY_CHANCE = 500; // Chance for a shiny (1 in 500)
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
            ExplorationPrizes prizes = new ExplorationPrizes();
            // Beginning of expl and event queue
            (int, int) prevCoord = (-1, 0); // Starts from outside i guess
            bool usedShortcut = false; // If a shortcut was used to the new room
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
                    bool roomSuccess = false;
                    if (room == 0) // Room 0 is always the beginning of floor, camping event followed by shortcut check
                    {
                        if (floor == 0) // Beginning of adventure!
                        {
                            string auxString = $"Beginning of {trainerData.Name}'s exploration in {Dungeon}";
                            Console.WriteLine(auxString);
                            GenericMessageCommand(auxString); // Begin of exploration string
                        }
                        roomSuccess = ExecuteEvent(_dungeonDetails.CampingEvent, floor, prizes, trainerData); // Executes camping event
                        // After, need to also check shortcut
                        Console.WriteLine(_dungeonDetails.Floors[floor].ShortcutClue);
                        GenericMessageCommand(_dungeonDetails.Floors[floor].ShortcutClue);
                        if (VerifyShortcutConditions(_dungeonDetails.Floors[floor].ShortcutConditions, trainerData)) // If shortcut activated
                        {
                            // After, need to also check shortcut
                            Console.WriteLine(_dungeonDetails.Floors[floor].ShortcutResolution);
                            GenericMessageCommand(_dungeonDetails.Floors[floor].ShortcutResolution);
                            if (floor == DUNGEON_NUMBER_OF_FLOORS - 1) // If dungeon was done in last floor, then the dungeon is over...
                            {
                                DrawConnectRoomCommand(floor, room, floor + 1, room, true); // Connects to invisible next dungeon, shortcut
                                DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                                string auxString = $"You move onward...";
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
                    }
                    else if (room == 1) // And first room always a wild pokemon encounter
                    {
                        RoomEvent pokemonEvent = new RoomEvent()
                        {
                            EventType = RoomEventType.POKEMON_BATTLE,
                            PreEventString = "Suddenly, wild pokemon attack!",
                            PostEventString = $"You won the battle and obtained multiple items that the wild Pokemon were holding ($1)."
                        }; // Wild pokemon encounter event
                        roomSuccess = ExecuteEvent(pokemonEvent, floor, prizes, trainerData);
                    }
                    else if ((room == DUNGEON_ROOMS_PER_FLOOR - 1) && (floor == DUNGEON_NUMBER_OF_FLOORS - 1)) // Last room is always boss event
                    {
                        roomSuccess = ExecuteEvent(_dungeonDetails.BossEvent, floor, prizes, trainerData); // BOSS
                        if (roomSuccess) // If has been beaten, then dungeon is also over
                        {
                            DrawConnectRoomCommand(floor, room, floor + 1, room, false); // Connects to invisible next dungeon, no shortcut
                            DrawMoveCharacterCommand(floor, room, floor + 1, room); // Character dissapears
                            string auxString = $"You move onward...";
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
                        RoomEvent nextEvent = possibleEvents[_rng.Next(possibleEvents.Count)]; // Get a random event
                        Console.WriteLine($"Event: {nextEvent.ToString()}");
                        roomSuccess = ExecuteEvent(nextEvent, floor, prizes, trainerData);
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
            Console.WriteLine("Items: ");
            foreach (KeyValuePair<string, int> kvp in prizes.ItemsFound)
            {
                Console.Write($"{kvp.Key} x{kvp.Value}, ");
            }
            Console.WriteLine("");
            Console.WriteLine("Mons");
            if (prizes.MonsFound[0].Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in prizes.MonsFound[0])
                {
                    Console.Write($"{kvp.Key} x{kvp.Value}, ");
                }
                Console.WriteLine("(POKE BALL)");
            }
            if (prizes.MonsFound[1].Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in prizes.MonsFound[1])
                {
                    Console.Write($"{kvp.Key} x{kvp.Value}, ");
                }
                Console.WriteLine("(GREAT BALL)");
            }
            if (prizes.MonsFound[2].Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in prizes.MonsFound[2])
                {
                    Console.Write($"{kvp.Key} x{kvp.Value}, ");
                }
                Console.WriteLine("(ULTRA BALL)");
            }
            if (prizes.MonsFound[3].Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in prizes.MonsFound[3])
                {
                    Console.Write($"{kvp.Key} x{kvp.Value}, ");
                }
                Console.WriteLine("(MASTER BALL)");
            }
        }
        /// <summary>
        /// Executes an event of the many possible in room
        /// </summary>
        /// <param name="roomEvent">Event to simulate</param>
        /// <param name="floor">Floor where event happens</param>
        /// <param name="prizes">Place where to store the things won in this room (mons, items, etc)</param>
        /// <param name="trainerData">Data about the trainer</param>
        /// <returns></returns>
        bool ExecuteEvent(RoomEvent roomEvent, int floor, ExplorationPrizes prizes, TrainerData trainerData)
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
                        string itemFound = _dungeonDetails.RareItems[_rng.Next(_dungeonDetails.RareItems.Count)]; // Find a random rare item
                        Console.WriteLine($"Finds {itemFound}");
                        string itemString = roomEvent.PreEventString.Replace("$1", itemFound);
                        GenericMessageCommand(itemString); // Prints the message but we know it could have a $1
                        GenericMessageCommand(roomEvent.PostEventString);
                        AddItemPrize(itemFound, prizes);
                    }
                    break;
                case RoomEventType.BOSS: // Boss fight, identical to alpha, will fetch next floor anyway, which if floor 3, it's boss
                case RoomEventType.ALPHA: // Find a frenzied mon from a floor above, boss will have a rare item if defeated
                    {
                        string item = _dungeonDetails.RareItems[_rng.Next(_dungeonDetails.RareItems.Count)].Trim().ToLower(); // Get a random rare item
                        List<string> pokemonNextFloor = _dungeonDetails.PokemonEachFloor[floor + 1]; // Find the possible mons next floor
                        string pokemonSpecies = pokemonNextFloor[_rng.Next(pokemonNextFloor.Count)].Trim().ToLower(); // Get a random one of these
                        Console.WriteLine($"Alpha {pokemonSpecies} holding {item}");
                        string alphaString = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        bool isShiny = (_rng.Next(SHINY_CHANCE) == 1); // Will be shiny if i get a 1 dice roll
                        PokemonSet alphaPokemon = new PokemonSet()
                        {
                            Species = pokemonSpecies,
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
                        alphaTeam.ConfirmSets(_backEndData, int.MaxValue, false, false); // Randomize enemy team (movesets, etc)
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
                            alphaString = roomEvent.PostEventString.Replace("$1", item);
                            GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                            AddItemPrize(item, prizes); // Add item to prizes
                            AddPokemonPrize(pokemonSpecies, floor + 1, isShiny, prizes); // Add alpha mon too
                        }
                    }
                    break;
                case RoomEventType.EVO:
                    Console.WriteLine("Event tile, consists of only text and resolves. MAY INVOLVE A FEW EXTRA ITEMS");
                    GenericMessageCommand(roomEvent.PreEventString);
                    AddItemPrize("EVOLUTION OPPORTUNITY", prizes);
                    GenericMessageCommand(roomEvent.PostEventString);
                    break;
                case RoomEventType.RESEARCHER:
                    Console.WriteLine("Event tile, consists of only text and resolves. MAY INVOLVE A FEW EXTRA ITEMS");
                    GenericMessageCommand(roomEvent.PreEventString);
                    AddItemPrize("RANDOM PLATE", prizes);
                    GenericMessageCommand(roomEvent.PostEventString);
                    break;
                case RoomEventType.PARADOX:
                    Console.WriteLine("Event tile, consists of only text and resolves. MAY INVOLVE A FEW EXTRA ITEMS");
                    string obtainedDisk = _backEndData.MoveItemData.Keys.ToList()[_rng.Next(_backEndData.MoveItemData.Count)]; // Get random move disk
                    string messageString = roomEvent.PreEventString.Replace("$1", obtainedDisk);
                    GenericMessageCommand(messageString);
                    AddItemPrize("obtainedDisk", prizes);
                    messageString = roomEvent.PostEventString.Replace("$1", obtainedDisk);
                    GenericMessageCommand(messageString);
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
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.STATUS_TRAP:
                    {
                        Console.WriteLine("A trap that has will status one mon");
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
                            statusedMon = possibleMons[_rng.Next(possibleMons.Count - 1)];
                        }
                        else
                        {
                            statusedMon = trainerData.Teamsheet[_rng.Next(trainerData.Teamsheet.Count)];
                        }
                        statusedMon.ExplorationStatus.NonVolatileStatus = status;
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.POKEMON_BATTLE:
                    // Similar to aplha but there's 6 enemy mons
                    {
                        Console.WriteLine("Pokemon battle");
                        // Items obtained during the fight (commons)
                        int itemCount = _rng.Next(2, 4); // Either 2 or 3 items
                        List<string> items = new List<string>();
                        List<string> itemSlots = ["", "", "", "", "", ""]; // These'll be the item slots
                        for (int i = 0; i < itemCount; i++)
                        {
                            // Prize pool will contain common items
                            string item = _dungeonDetails.CommonItems[_rng.Next(_dungeonDetails.CommonItems.Count)].Trim().ToLower();
                            itemSlots[i] = item;
                            Console.WriteLine($"Item: {item}");
                            items.Add(item);
                        }
                        // Shuffle the items so they go into random slots
                        Utilities.ShuffleList(itemSlots, 0, 6, _rng); // Shuffle the list
                        // Add pokemon, some will have random items
                        List<string> pokemonThisFloor = _dungeonDetails.PokemonEachFloor[floor]; // Find the possible mons this floor
                        List<PokemonSet> encounterPokemon = new List<PokemonSet>();
                        for (int i = 0; i < 6; i++) // Generate party of 6 random mons
                        {
                            string pokemonSpecies = pokemonThisFloor[_rng.Next(pokemonThisFloor.Count)].Trim().ToLower();
                            bool isShiny = (_rng.Next(SHINY_CHANCE) == 1);
                            PokemonSet pokemon = new PokemonSet()
                            {
                                Species = pokemonSpecies,
                                Shiny = isShiny,
                                Item = (itemSlots[i] != "") ? new Item() { Name = itemSlots[i], Uses = 1 } : null, // Give item if has one
                            };
                            Console.WriteLine($"Mon: {pokemonSpecies}");
                            encounterPokemon.Add(pokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        TrainerData wildMonTeam = new TrainerData() // Create the blank trainer
                        {
                            Avatar = "unknown",
                            Name = "wild mons",
                            AutoItem = false,
                            AutoTeam = true,
                            Teamsheet = encounterPokemon,
                        };
                        wildMonTeam.ConfirmSets(_backEndData, int.MaxValue, false, false); // Randomize enemy team (movesets, etc)
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
                            string postMessage = roomEvent.PostEventString.Replace("$1", string.Join(',', items));
                            GenericMessageCommand(postMessage);
                            foreach (string item in items) // Add all items to prizes
                            {
                                AddItemPrize(item, prizes);
                            }
                            foreach (PokemonSet pokemonSpecies in encounterPokemon)
                            {
                                AddPokemonPrize(pokemonSpecies.Species, floor, pokemonSpecies.Shiny, prizes); // Add all mons
                            }
                        }
                    }
                    break;
                case RoomEventType.JOINER:
                    // Mon that joins the team for adventures, always catchable
                    {
                        List<string> pokemonThisFloor = _dungeonDetails.PokemonEachFloor[floor]; // Find the possible mons this floor
                        string pokemonSpecies = pokemonThisFloor[_rng.Next(pokemonThisFloor.Count)]; // Get a random one of these
                        Console.WriteLine($"Joiner {pokemonSpecies}");
                        string alphaString = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        bool isShiny = (_rng.Next(SHINY_CHANCE) == 1); // Will be shiny if i get a 1 dice roll
                        PokemonSet joiner = new PokemonSet()
                        {
                            Species = pokemonSpecies,
                            Shiny = isShiny,
                        };
                        joiner.RandomizeMon(_backEndData, false, 7); // Random set of moves, 7% switch standard
                        Console.Write("Added to team");
                        trainerData.Teamsheet.Add(joiner);
                        GenericMessageCommand(roomEvent.PostEventString);
                        AddPokemonPrize(pokemonSpecies, 0, isShiny, prizes); // Add mon to always capturable
                    }
                    break;
                case RoomEventType.NPC_BATTLE:
                    {
                        TrainerData randomNpc = _backEndData.NpcData.Values.ToList()[_rng.Next(_backEndData.NpcData.Values.Count)]; // Get random npc
                        randomNpc.ConfirmSets(_backEndData, 3, true, false); // Get into a trainer fight
                        Console.WriteLine($"Fighting {randomNpc.Name}");
                        string npcString = roomEvent.PreEventString.Replace("$1", randomNpc.Name);
                        GenericMessageCommand(npcString); // Prints the message but we know it could have a $1
                        Console.Write("Encounter resolution: ");
                        int remainingMons = ResolveEncounter(trainerData, randomNpc, 3); // 3 Mon only for opp
                        if (remainingMons == 0) // Means player lost
                        {
                            Console.WriteLine("Player lost");
                            roomCleared = false; // Failure at clearing room
                            GenericMessageCommand($"You blacked out...");
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            AddItemPrize("Some 2 IMP", prizes);
                            GenericMessageCommand(roomEvent.PostEventString);
                        }
                    }
                    break;
                default:
                    break;
            }
            return roomCleared;
        }
        /// <summary>
        /// Verifies whether a trainer fills the consitions to use a shortcut
        /// </summary>
        /// <param name="conditions">Shortcut conditions</param>
        /// <param name="trainerData">Data of trainer</param>
        /// <returns>Whether shortcut activates or not</returns>
        bool VerifyShortcutConditions(List<ShortcutCondition> conditions, TrainerData trainerData)
        {
            bool canTakeShortcut = false;
            foreach (ShortcutCondition condition in conditions)
            {
                string valueToCheck = condition.Which.ToLower();
                switch (condition.ConditionType)
                {
                    case ShortcutConditionType.MOVE:
                        foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                        {
                            if (pokemon.Moves.Contains(valueToCheck)) // move found
                            {
                                canTakeShortcut = true;
                                break;
                            }
                            if (_backEndData.MoveItemData.ContainsKey(pokemon.Item.Name.ToLower()))
                            {
                                if (_backEndData.MoveItemData[pokemon.Item.Name.ToLower()].Contains(valueToCheck))
                                {
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                        }
                        break;
                    case ShortcutConditionType.ABILITY:
                        foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                        {
                            if (pokemon.Ability == valueToCheck) // ability found
                            {
                                canTakeShortcut = true;
                                break;
                            }
                        }
                        break;
                    case ShortcutConditionType.POKEMON:
                        foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                        {
                            if (pokemon.Species == valueToCheck) // species found
                            {
                                canTakeShortcut = true;
                                break;
                            }
                        }
                        break;
                    case ShortcutConditionType.TYPE:
                        foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                        {
                            if (_backEndData.Dex[pokemon.Species].Types.Contains(valueToCheck)) // type of pokemon found
                            {
                                canTakeShortcut = true;
                                break;
                            }
                        }
                        break;
                    case ShortcutConditionType.ITEM:
                        foreach (PokemonSet pokemon in trainerData.Teamsheet) // If a mon has move, all good
                        {
                            if (pokemon.Item.Name == valueToCheck) // type of pokemon found
                            {
                                canTakeShortcut = true;
                                break;
                            }
                        }
                        break;
                    default:
                        break;
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
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.PRINT_STRING,
                Message = message,
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
        /// <summary>
        /// Resolves an encounter between players, no mon limit
        /// </summary>
        /// <param name="epxlorer">P1</param>
        /// <param name="encounter">P2</param>
        /// <param name="nMons">P2 number of mons</param>
        /// <returns>How many mons P1 has left (0 means defeat)</returns>
        int ResolveEncounter(TrainerData epxlorer, TrainerData encounter, int nMons = int.MaxValue)
        {
            (int cursorX, int cursorY) = Console.GetCursorPosition(); // Just in case I need to write in same place
            Console.Write("About to simulate bots...");
            Console.ReadLine();
            BotBattle automaticBattle = new BotBattle(_backEndData); // Generate bot host
            string challengeString = "gen9customgame@@@!Team Preview,OHKO Clause,Evasion Moves Clause,Accuracy Moves Clause,Moody Clause";
            (int explorerLeft, _) = automaticBattle.SimulateBotBattle(epxlorer, encounter, int.MaxValue, nMons, challengeString); // Initiate battle
            Console.SetCursorPosition(cursorX, cursorY);
            Console.Write($"Explorer left with {explorerLeft} mons. GET THE REPLAY");
            return explorerLeft;
        }
        /// <summary>
        /// Adds item to prize pool
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="prizes">Prizes container</param>
        void AddItemPrize(string item, ExplorationPrizes prizes)
        {
            // Add the item to prizes
            if (prizes.ItemsFound.ContainsKey(item)) prizes.ItemsFound[item]++;
            else prizes.ItemsFound.Add(item, 1);
        }
        /// <summary>
        /// Add pokemon to prize pool
        /// </summary>
        /// <param name="mon">Which mon</param>
        /// <param name="floor">Which floor to add (0-4), also corresponds to pokeball</param>
        /// <param name="shiny">Mon is shiny</param>
        /// <param name="prizes">Prizes list</param>
        void AddPokemonPrize(string mon, int floor, bool shiny, ExplorationPrizes prizes)
        {
            if (shiny) mon += "★"; // Add shiny tag too
            Dictionary<string, int> monList = prizes.MonsFound[floor];
            if (monList.ContainsKey(mon)) monList[mon]++;
            else monList.Add(mon, 1);
        }
        #endregion
        #region ANIMATION
        int ROOM_WIDTH = 3;
        int ROOM_HEIGHT = 3;
        /// <summary>
        /// Animates the resulting exploration
        /// </summary>
        public void AnimateExploration()
        {
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
                        Console.WriteLine($"> {nextStep.Message}"); // Write message
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
            roomY = 1 + ((1 + ROOM_HEIGHT) * floor); // Correct Positioning of room Y tile
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
        bool IsRoomValid(int floor, int room)
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
            // Draw 8 things
            Console.SetCursorPosition(X, Y);
            Console.Write(floorData.NwWallTile);
            Console.SetCursorPosition(X + 1, Y);
            Console.Write(floorData.NWallTile);
            Console.SetCursorPosition(X + 2, Y);
            Console.Write(floorData.NeWallTile);
            Console.SetCursorPosition(X, Y + 1);
            Console.Write(floorData.WWallTile);
            Console.SetCursorPosition(X + 2, Y + 1);
            Console.Write(floorData.EWallTile);
            Console.SetCursorPosition(X, Y + 2);
            Console.Write(floorData.SwWallTile);
            Console.SetCursorPosition(X + 1, Y + 2);
            Console.Write(floorData.SWallTile);
            Console.SetCursorPosition(X + 2, Y + 2);
            Console.Write(floorData.SeWallTile);
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
            Console.ForegroundColor = ConsoleColor.White; // Set color back to white, for character
            // Delete current character first
            if (IsRoomValid(sourceFloor, sourceRoom))
            {
                (int fromX, int fromY) = GetRoomCoords(sourceFloor, sourceRoom);
                Console.SetCursorPosition(fromX + 1, fromY + 1);
                Console.Write(" ");
            }
            // Draw new character now
            if (IsRoomValid(destFloor, destRoom))
            {
                (int toX, int toY) = GetRoomCoords(destFloor, destRoom);
                Console.SetCursorPosition(toX + 1, toY + 1);
                Console.Write("☺");
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
                if (fromX < toX) { Console.SetCursorPosition(fromX + 2, fromY + 1); Console.Write(sourceFloorData.EWallPassageTile); } // Conenct east
                else if (fromX > toX) { Console.SetCursorPosition(fromX, fromY + 1); Console.Write(sourceFloorData.WWallPassageTile); } // Connect west
                else if (fromY < toY) { Console.SetCursorPosition(fromX + 1, fromY + 2); Console.Write(sourceFloorData.SWallPassageTile); } // Connect south
                else if (fromY > toY) { Console.SetCursorPosition(fromX + 1, fromY); Console.Write(sourceFloorData.NWallPassageTile); } // Connect north
                else { } // Should never happen
            }
            // Same for dest room
            if (IsRoomValid(destFloor, destRoom))
            {
                destFloorData = _dungeonDetails.Floors[destFloor];
                Console.ForegroundColor = destFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                if (toX < fromX) { Console.SetCursorPosition(toX + 2, toY + 1); Console.Write(destFloorData.EWallPassageTile); } // Conenct east
                else if (toX > fromX) { Console.SetCursorPosition(toX, toY + 1); Console.Write(destFloorData.WWallPassageTile); } // Connect west
                else if (toY < fromY) { Console.SetCursorPosition(toX + 1, toY + 2); Console.Write(destFloorData.SWallPassageTile); } // Connect south
                else if (toY > fromY) { Console.SetCursorPosition(toX + 1, toY); Console.Write(destFloorData.NWallPassageTile); } // Connect north
                else { } // Should never happen
            }
            // Finally, if the passage occurs between floors, need also to connect them as floors have a gap
            if (fromY != toY)
            {
                int topCoord = Math.Min(fromY, toY); // Which one is higher, don't really care, just connect them
                DungeonFloor floorDataToUse = sourceFloorData ?? destFloorData; // Use always the source floor unless it didn't exist in which case use the other one idk
                Console.ForegroundColor = floorDataToUse.PassageColor;
                Console.SetCursorPosition(fromX + 1, topCoord + 3); // X should be same for both rooms (?!?!?!) and then draw ourside of room, use top room for ref
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
                if (fromY < toY) { Console.SetCursorPosition(fromX + 1, fromY + 2); Console.Write(sourceFloorData.SWallShortcutTile); } // Connect south
                else if (fromY > toY) { Console.SetCursorPosition(fromX + 1, fromY); Console.Write(sourceFloorData.NWallShortcutTile); } // Connect north
                else { } // Should never happen
            }
            // Same for dest room
            if (IsRoomValid(destFloor, destRoom))
            {
                destFloorData = _dungeonDetails.Floors[destFloor];
                Console.ForegroundColor = destFloorData.RoomColor;
                // Room is valid, so I need to modify it's corresponding wall depending where it moves to
                // This time, it's only up-down so just compare floors
                if (toY < fromY) { Console.SetCursorPosition(toX + 1, toY + 2); Console.Write(destFloorData.SWallShortcutTile); } // Connect south
                else if (toY > fromY) { Console.SetCursorPosition(toX + 1, toY); Console.Write(destFloorData.NWallShortcutTile); } // Connect north
                else { } // Should never happen
            }
            // Finally, if the passage occurs between floors, need also to connect them as floors have a gap
            if (fromY != toY)
            {
                DungeonFloor floorDataToUse = sourceFloorData ?? destFloorData; // Use always the source floor unless it didn't exist in which case use the other one idk
                int shortcutY = Math.Min(fromY, toY) + 3; // Shorcut to be between rooms (floors)
                int leftShortcutX = Math.Min(fromX, toX) + 1;
                int rightShortcutX = Math.Max(fromX, toX) + 1;
                char firstTile, lastTile, middleTile;
                if (fromX < toX) // Left -> right
                {
                    firstTile = floorDataToUse.NwShortcutTile;
                    lastTile = floorDataToUse.SeShortcutTile;
                    middleTile = floorDataToUse.HorizontalShortcutTile;
                }
                else if (fromX > toX) // Right -> left
                {
                    firstTile = floorDataToUse.SwShortcutTile;
                    lastTile = floorDataToUse.NeShortcutTile;
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
                    if (Console.CursorLeft == leftShortcutX) Console.WriteLine(firstTile);
                    else if (Console.CursorLeft == rightShortcutX) Console.WriteLine(lastTile);
                    else Console.WriteLine(middleTile);
                }
            }
        }
        #endregion
    }
}
