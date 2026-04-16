using AutomatedTeamBuilder;
using GameData;
using GameDataContainer;
using MechanicsData;
using MechanicsDataContainer;
using ShowdownBot;
using System.Text;
using Utilities;

namespace IndymonBackendProgram
{
    public enum ExplorationStepType
    {
        NOP, // No operation, just a pause
        // Data-based
        DEFINE_DUNGEON,
        // Text-Based
        CLEAR_CONSOLE,
        PRINT_STRING,
        PRINT_CLUE,
        PRINT_PLOT,
        PRINT_EVOLUTION,
        // Info table
        ADD_INFO_COLUMN,
        ADD_INFO_VALUE,
        // Graphics
        MOVE_CHARACTER,
        DRAW_EVENT,
        DRAW_MAP,
        CLEAR_SCREEN,
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
        public int MillisecondsWait { get; set; }
        public (int, int) CoordParam { get; set; } // When drawing coords or something that is placed somewhere
        public ConsoleColor FgParam { get; set; } // Colors when requesting to draw a specific thing
        public ConsoleColor BgParam { get; set; }
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
            trainer.Imp += ImpFound;
        }
    }
    public class ExplorationContext
    {
        int _baseShinyChance = 500; // Chance for a shiny (1 in 500 is the base one)
        List<(Sandwich, int)> _sandwichEffects = [];
        const int RNG_FLOAT_RESOLUTION = 1000000;
        /// <summary>
        /// Modifies the base shiny chance
        /// </summary>
        /// <param name="mult">How much to modify for</param>
        public void ModifyBaseShinyChance(double mult)
        {
            _baseShinyChance = (int)(_baseShinyChance * mult);
        }
        /// <summary>
        /// Adds a sandwich effect to the current context
        /// </summary>
        /// <param name="sandwich"></param>
        public void AddSandwichEffect(Sandwich sandwich)
        {
            _sandwichEffects.Add((sandwich, sandwich.Duration));
        }
        /// <summary>
        /// Does a random pull with the current game effects to try and get an extra pokemon
        /// </summary>
        /// <returns>How many extra pokemon are found in this encounter</returns>
        public int GetExtraPokemon()
        {
            double extraMonCount = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.ENEMY_NUMBER).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double min = sando.Level * (1 - (sando.Duration - 1) / 3);
                double max = (2 * sando.Level) - min;
                double randomFloat = min + ((max - min) * GeneralUtilities.GetRandomNumber(RNG_FLOAT_RESOLUTION) / RNG_FLOAT_RESOLUTION);
                extraMonCount += randomFloat;
            }
            return (int)(extraMonCount + 0.5); // Always round up
        }
        /// <summary>
        /// Does a random pull with how much will be the item multiplier
        /// </summary>
        /// <returns>Item multiplier</returns>
        public double GetItemMult()
        {
            double itemMult = 1; // Base item multiplier
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.ITEM_DROP).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double min = sando.Level * (1 - (sando.Duration - 1) / 3);
                double max = (2 * sando.Level) - min;
                min *= 0.5; max *= 0.5; // Item multilpier make it so that it has a theoretical max of *10 if no delta at lvl 20
                double randomFloat = min + ((max - min) * GeneralUtilities.GetRandomNumber(RNG_FLOAT_RESOLUTION) / RNG_FLOAT_RESOLUTION);
                itemMult += randomFloat;
            }
            return itemMult;
        }
        /// <summary>
        /// Does a random pull with how much will be the extra healing in int percentage
        /// </summary>
        /// <returns>Percentage healing</returns>
        public int GetExtraHealing()
        {
            double totalHealingPercentage = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.POST_HEALING).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double min = sando.Level * (1 - (sando.Duration - 1) / 3);
                double max = (2 * sando.Level) - min;
                min *= 2.5; max *= 2.5; // Each lvl of sandwich heals 2.5%
                double randomFloat = min + ((max - min) * GeneralUtilities.GetRandomNumber(RNG_FLOAT_RESOLUTION) / RNG_FLOAT_RESOLUTION);
                totalHealingPercentage += randomFloat;
            }
            return (int)(totalHealingPercentage + 0.5); // Always round up
        }
        /// <summary>
        /// Does a random pull with how much will be the shiny chance for a fight
        /// </summary>
        /// <returns>Shiny chance</returns>
        public int GetShinyChance()
        {
            double shinyChanceMultiplier = 1; // Base shiny chance
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.SHINY_CHANCE).Select(s => s.Item1)]; // Get the valid sandwiches
            if (validSandwichEffects.Count > 0)
            {
                foreach (Sandwich sando in validSandwichEffects)
                {
                    double shinyMean = (21.0 - sando.Level) / 21; // This is the mean of the whole thing
                    double delta = (sando.Duration - 1.0) * (1 - shinyMean) / 4; // Will be a delta of this
                    double min = shinyMean - delta;
                    double max = shinyMean + delta;
                    min *= 0.5; max *= 0.5; // Item multilpier make it so that it has a theoretical max of *10 if no delta at lvl 20
                    double randomFloat = min + ((max - min) * GeneralUtilities.GetRandomNumber(RNG_FLOAT_RESOLUTION) / RNG_FLOAT_RESOLUTION);
                    shinyChanceMultiplier += randomFloat;
                }
                shinyChanceMultiplier /= validSandwichEffects.Count; // Its the average shiny chance because I didn't find a better way to do it
            }
            int resultingChance = (int)((_baseShinyChance * shinyChanceMultiplier) + 0.5); // Get the chance 1/500
            if (resultingChance < 1) resultingChance = 1; // Chance needs to keep positive
            return resultingChance;
        }
        /// <summary>
        /// Does a random pull of extra level gain
        /// </summary>
        /// <returns>The lvl gain for a mon</returns>
        public int GetExtraLevel()
        {
            double totalLevelGain = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.LEVEL).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double min = sando.Level * (1 - (sando.Duration - 1) / 3);
                double max = (2 * sando.Level) - min;
                double randomFloat = min + ((max - min) * GeneralUtilities.GetRandomNumber(RNG_FLOAT_RESOLUTION) / RNG_FLOAT_RESOLUTION);
                totalLevelGain += randomFloat;
            }
            return (int)(totalLevelGain + 0.5); // Always round up
        }
        /// <summary>
        /// Uses the effect of a sandwich type
        /// </summary>
        /// <param name="effect">The effect that will be used</param>
        /// <param name="logger">Where to print expiration message</param>
        /// <returns>A list of messages of the sandwiches that have been consumed</returns>
        public void UseSandwichEffect(SandwichEffectType effect, Action<string> logger)
        {
            List<(Sandwich, int)> expiredSandwiches = [.. _sandwichEffects.Where(s => s.Item1.Effect == effect && s.Item2 == 1)]; // Get the valid sandwiches that will be exausted here
            foreach (string sando in expiredSandwiches.Select(s => s.Item1.Name)) // print the expired ones
            {
                logger($"The {sando} effect has worn off");
            }
            _sandwichEffects = [.. _sandwichEffects.Except(expiredSandwiches)]; // Remove expired
            for (int i = 0; i < _sandwichEffects.Count; i++)
            {
                (Sandwich, int) item = _sandwichEffects[i];
                if (item.Item1.Effect == effect)
                {
                    item.Item2--;
                    _sandwichEffects[i] = item; // Reduce duration by 1
                }
            }
        }
    }
    public class ExplorationManager
    {
        // Private data
        Dungeon _dungeonData = null;
        readonly ExplorationPrizes _prizes = new ExplorationPrizes();
        readonly ExplorationContext _context = new ExplorationContext();
        Trainer _trainer;
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
        const int MIN_MESSAGE_PAUSE = 2000; // Show text for this amount of time min
        const int MESSAGE_PAUSE_PER_WORD = 300; // 0.3s per word looks reasonable
        const int DRAW_DELAY = 1000; // Time to wait until showing next section
        const int MAX_TRAINER_MONS = 6; // How many mons trainer can have max (at beginning of expl)
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
            ExplorationSteps.Clear();
            _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
            _trainer = IndymonUtilities.GetTrainerByName(TrainerAndSeed.Item1); // Find the trainer data
            List<PossibleTeamBuild> possibleBuilds = TeamBuilder.GetTrainersPossibleBuilds(_trainer, MAX_TRAINER_MONS, [new Constraint()], true); // Since there's no constraint, will get a build consisting of all my mons (max 6)
            TeamBuilder.AssembleTrainersBattleTeam(_trainer, MAX_TRAINER_MONS, possibleBuilds, true, TrainerAndSeed.Item2); // Chooses one of the sets, prepares the mons
            // Now, make the trainer choose it's team sets!
            HashSet<Pokemon> enemyMons = new HashSet<Pokemon>();
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[0].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]); // Add all mons from all floors
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[1].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[2].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            enemyMons.UnionWith([.. _dungeonData.PokemonEachFloor[3].Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m])]);
            TeamBuilder.DefineTrainerSets(_trainer, true, _dungeonData.DungeonArchetypes, _dungeonData.DungeonWeather, _dungeonData.DungeonTerrain, new Constraint(), [.. enemyMons], TrainerAndSeed.Item2); // Team build but with the dungeon's weather and such 
            _trainer.RestoreAll(); // Begin expl at full (and also inits the PP thing, weirdly enough)
            // Beginning of expl and event queue
            List<RoomEvent> possibleEvents = [.. _dungeonData.Events]; // These are the possible events for this dungeon
            if (_trainer.BattleTeam.Any(m => m.ModItem?.Name == "Shiny Stone")) // Shiny stone is a special item that will modify the base shiny chance to 1 in 25
            {
                _context.ModifyBaseShinyChance(1 / 20); // 20 times more likely
            }
            SetDungeonCommand(Dungeon); // Need to do first of all otherwise all goes to hell
            AddInfoColumnCommand("Pokemon", 18);
            AddInfoColumnCommand("Health", 6);
            AddInfoColumnCommand("Status", 6);
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventTitle = $"<@{_trainer.DiscordNumber}> Meanwhile, {_trainer.Name} went on to explore the {Dungeon}.";
            bool explorationFinished;
            do
            {
                HashSet<char> mapExplorationProgress = ['0']; // Stores which parts of the map have been discovered, 0 is always discovered
                explorationFinished = true;
                SetDungeonCommand(Dungeon);
                ClearScreenCommand(); // Just in case, just clear the current screen (if coming from another exploration)
                ClearConsoleCommand(); // Clears mini console to leave space for new event's message
                DrawMapCommand(mapExplorationProgress);
                string auxString = $"Beginning of {_trainer.Name}'s exploration in {Dungeon}";
                GenericMessageCommand(auxString); // Begin of exploration string
                // Adds status table. Pokemon have a max of 18 characters, health and status max 3. Fill the info too
                UpdateTrainerDataInfo();
                for (int floor = 0; floor < _dungeonData.NFloors; floor++) // Begin the iteration of all floors
                {
                    for (int depth = 0; depth < _dungeonData.NRoomsPerFloor; depth++) // Begin the iteration of floor depth
                    {
                    NewRoomLanding:
                        ClearConsoleCommand(); // Clears mini console to leave space for new event's message
                        int roomNumber = _dungeonData.GetRoomNumber(floor, depth);
                        mapExplorationProgress.Add((char)('a' + roomNumber)); // Add the current room to map
                        DrawMapCommand(mapExplorationProgress);
                        MoveCharacterCommand(roomNumber); // Put character there
                        NopWithWaitCommand(DRAW_DELAY);
                        bool roomSuccess;
                        // Check if there's any shortcut, first check the dungeon one, otherwise a room shortcut
                        Shortcut currentShortcut = null;
                        bool shortcutWouldBeFinal = false;
                        int shortcutNumber = 0; // If shortcut shows something in map, this will be the value
                        if (_dungeonData.DungeonShortcut.RoomNumber == roomNumber)
                        {
                            currentShortcut = _dungeonData.DungeonShortcut;
                            shortcutWouldBeFinal = true;
                            shortcutNumber = _dungeonData.RoomShortcuts.Count + 1; // Final shortcut equivalent to the last shortcut
                        }
                        else
                        {
                            currentShortcut = _dungeonData.RoomShortcuts.Where(s => s.RoomNumber == roomNumber).FirstOrDefault();
                            shortcutNumber = _dungeonData.RoomShortcuts.IndexOf(currentShortcut) + 1; // Mark the shortcut index too
                        }
                        if (currentShortcut != null) // There's a shortcut, verify
                        {
                            ClueMessageCommand(currentShortcut.Clue);
                            if (VerifyShortcutConditions(currentShortcut.Conditions, out string message))
                            {
                                // Shortcut taken, time to print shortcut and skip floor, show this shortcut too
                                mapExplorationProgress.Add((char)('0' + shortcutNumber)); // Add the current shortcut to map too
                                string shortcutString = currentShortcut.Resolution.Replace("$1", message);
                                GenericMessageCommand(shortcutString);
                                if (shortcutWouldBeFinal)
                                {
                                    MoveCharacterCommand(-1); // Remove character from screen
                                    GenericMessageCommand("You move onward...");
                                    NopWithWaitCommand(1000);
                                    explorationFinished = false; // Will repeat exploration loop now
                                    Dungeon = _dungeonData.NextDungeonShortcut; // Go to the dungeon indicated by shortcut
                                    _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
                                    possibleEvents = [.. _dungeonData.Events];
                                    goto DungeonEnd; // Just break these loops
                                }
                                else
                                {
                                    (floor, depth) = _dungeonData.GetRoomCoords(currentShortcut.RoomDestination); // Will teleport here immediately
                                    goto NewRoomLanding;
                                }
                            }
                        }
                        // Otherwise, I'm safely in a room, do something
                        if (depth == 0) // First of floor is camping
                        {
                            roomSuccess = ExecuteEvent(_dungeonData.CampingEvent, floor);
                        }
                        else if (depth == 1) // Second room of floor always a wild pokemon encounter
                        {
                            roomSuccess = ExecuteEvent(_dungeonData.WildMonsEvent, floor);
                        }
                        else if ((depth == _dungeonData.NRoomsPerFloor - 1) && (floor == _dungeonData.NFloors - 1)) // Last room is always boss event
                        {
                            ExecuteEvent(_dungeonData.PreBossEvent, floor); // Pre boss event
                            DrawMapCommand([]); // Show whole map, deserved
                            MoveCharacterCommand(roomNumber); // Also put character because the preboss event may remove it or something
                            roomSuccess = ExecuteEvent(_dungeonData.BossEvent, floor); // BOSS
                            if (roomSuccess) // If has been beaten, then dungeon is also over
                            {
                                ExecuteEvent(_dungeonData.PostBossEvent, floor); // Post boss event
                                // Also the typical backend stuff
                                if (_dungeonData.NextDungeon != "")
                                {
                                    MoveCharacterCommand(-1); // Remove character from screen
                                    GenericMessageCommand("You move onward...");
                                    NopWithWaitCommand(1000);
                                    explorationFinished = false; // Will repeat exploration loop now
                                    Dungeon = _dungeonData.NextDungeon; // Go to the next dungeon
                                    _dungeonData = GameDataContainers.GlobalGameData.Dungeons[Dungeon];
                                    possibleEvents = [.. _dungeonData.Events];
                                }
                                else
                                {
                                    GenericMessageCommand("End of exploration.");
                                }
                                goto DungeonEnd;
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
                        {
                            GenericMessageCommand($"You blacked out...");
                            goto DungeonEnd; // Just go directly to end
                        }
                    }
                }
            DungeonEnd: // Best way to break from a 2 damn nested loops I think
                ;
            } while (!explorationFinished); // Will do once unless need to continue with another dungeon
            // Return to normal
            Console.CursorVisible = false;
            Console.WriteLine("Exploration end.");
            // Add items from prizes -> inventory
            _prizes.TransferToTrainer(_trainer);
            // Consume items
            IndymonUtilities.ConsumeTrainersItems(_trainer);
            // Save the copiable exploration file
            IndymonUtilities.WarnTrainer(_trainer);//Warn trainer of the exceeded items
            SaveExplorationOutcome(); // Replace with message stuff
            string trainerFilePath = Path.Combine(DirectoryPath, $"{_trainer.Name.ToUpper().Replace(" ", "").Replace("?", "")}.trainer");
            _trainer.SaveTrainerCsv(trainerFilePath);
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
            // Visual, will draw the event here
            DrawEventCommand(roomEvent); // It's position will always draw relative to the character
            // Mechanics, actually execute the event
            switch (roomEvent.EventType)
            {
                case RoomEventType.CAMPING:
                    GenericMessageCommand(roomEvent.PreEventString);
                    // Camping logic: eat a random sandwich
                    if (_trainer.Sandwiches.Count > 0)
                    {
                        Sandwich sando = _trainer.Sandwiches[0];
                        _context.AddSandwichEffect(sando);
                        GenericMessageCommand($"Your team has eaten {sando.Name}");
                        _trainer.Sandwiches.RemoveAt(0); // Consume sandwich
                    }
                    GenericMessageCommand(roomEvent.PostEventString);
                    break;
                case RoomEventType.TREASURE: // Find an item in the floor, free rare item
                    {
                        ItemReward itemFound = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Find a random rare item
                        int amount = GeneralUtilities.GetRandomNumber(itemFound.Min, itemFound.Max + 1); // How many were found
                        amount = (int)((amount * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                        if (amount < 1) amount = 1;
                        Console.WriteLine($"Finds {itemFound.Name}");
                        string itemString = roomEvent.PreEventString.Replace("$1", $"{itemFound.Name} (x{amount})");
                        GenericMessageCommand(itemString); // Prints the message but we know it could have a $1
                        itemString = roomEvent.PostEventString.Replace("$1", $"{itemFound.Name} (x{amount})");
                        GenericMessageCommand(itemString);
                        _prizes.AddReward(itemFound.Name, amount);
                        _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                    }
                    break;
                case RoomEventType.BOSS: // Boss fight
                    {
                        int enemyFloor = 3; // Last floor is where bosses are
                        int itemCount = GeneralUtilities.GetRandomNumber(_dungeonData.BossItem.Min, _dungeonData.BossItem.Max + 1); // How much of an item I gained
                        itemCount = (int)((itemCount * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                        if (itemCount < 1) itemCount = 1;
                        string enemySpecies = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[enemyFloor]); // Boss will be a random one
                        // Find what item the enemy will have, ensure boss has something atleast
                        string item = _dungeonData.BossItem.Name;
                        IndymonUtilities.RewardType itemType = IndymonUtilities.GetRewardType(item);
                        while (!(itemType == IndymonUtilities.RewardType.MOD || itemType == IndymonUtilities.RewardType.BATTLE || itemType == IndymonUtilities.RewardType.SET))
                        {
                            item = GeneralUtilities.GetRandomPick(_dungeonData.RareItems).Name; // Pick new item from rare pool until it's an equippable one
                            itemType = IndymonUtilities.GetRewardType(item);
                        }
                        Trainer bossTrainer = GenerateEnemyTrainer("BossEncounter", [enemySpecies], [item], 100, 100, true);
                        DefineEnemySet(bossTrainer, 24, true); // Defines the enemy set (smart for a final boss challenge!)
                        string bossString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                        // Fight and conclusion
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, bossTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            bossString = roomEvent.PostEventString.Replace("$1", _dungeonData.BossItem.Name);
                            GenericMessageCommand(bossString); // Prints the message but we know it could have a $1
                            _prizes.AddReward(_dungeonData.BossItem.Name, itemCount);
                            _prizes.AddMons(bossTrainer.BattleTeam, enemyFloor + 1); // Rank 4
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.ALPHA: // Find a smart and frenzied mon from a floor above, boss will have a rare item if defeated
                    {
                        ItemReward item = GeneralUtilities.GetRandomPick(_dungeonData.RareItems); // Get a random rare item
                        int itemAmount = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1);
                        itemAmount = (int)((itemAmount * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                        if (itemAmount < 1) itemAmount = 1;
                        int enemyFloor = (floor + 1 >= _dungeonData.NFloors) ? floor : floor + 1; // Find enemy of next floor if possible
                        string enemySpecies = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[enemyFloor]); // Get a random one of these
                        string alphaString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                        Trainer alphaTrainer = GenerateEnemyTrainer("Alpha", [enemySpecies], [item.Name], 100, 100, true);
                        DefineEnemySet(alphaTrainer, 24, true); // Defines the enemy set (smart for an alpha challenge)
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, alphaTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            alphaString = roomEvent.PostEventString.Replace("$1", item.Name);
                            GenericMessageCommand(alphaString); // Prints the message but we know it could have a $1
                            _prizes.AddReward(item.Name, itemAmount);
                            _prizes.AddMons(alphaTrainer.BattleTeam, enemyFloor + 1);
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.EVO:
                    {
                        EvolutionMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
                        {
                            Pokemon baseMon = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
                            bool error;
                            if (baseMon.Evos.Count > 0) // Mon has evos, ask for each
                            {
                                do
                                {
                                    try // Very easy to fuck up
                                    {
                                        Console.WriteLine($"Evolve {mon} ? y/N. Consider:");
                                        Console.WriteLine($"Mon Items: (0) Set: {mon.SetItem}, (1) Mod: {mon.ModItem}, (2) Battle: {mon.BattleItem}");
                                        Console.WriteLine($"(3) Set Items In Bag: {string.Join(", ", _trainer.SetItems.Keys.Select(i => i.Name))}");
                                        Console.WriteLine($"(4) Mod Items In Bag: {string.Join(", ", _trainer.ModItems.Keys.Select(i => i.Name))}");
                                        Console.WriteLine($"(5) Battle Items In Bag: {string.Join(", ", _trainer.BattleItems.Keys.Select(i => i.Name))}");
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
                                                if (choice == 3) collectionDict = _trainer.SetItems;
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
                                                if (choice == 4) collectionDict = _trainer.ModItems;
                                                else if (choice == 5) collectionDict = _trainer.BattleItems;
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
                                        error = false;
                                    }
                                    catch
                                    {
                                        error = true;
                                    }
                                } while (error);
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
                        int blankDiskCount = GeneralUtilities.GetRandomNumber(2, 5); // 2 to 4 blank disks
                        string giftString = $"{diskName} and {blankDiskCount} Blank Disks";
                        string messageString = roomEvent.PreEventString.Replace("$1", giftString);
                        GenericMessageCommand(messageString);
                        _prizes.AddReward(diskName, 1);
                        _prizes.AddReward("Blank Disk", blankDiskCount);
                        messageString = roomEvent.PostEventString.Replace("$1", giftString);
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
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
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
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
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
                        GenericMessageCommand(roomEvent.PreEventString);
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
                        {
                            mon.NonVolatileStatus = "";
                        }
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        GenericMessageCommand(roomEvent.PostEventString);
                    }
                    break;
                case RoomEventType.BIG_HEAL:
                    {
                        TrainerPokemon mon = _trainer.BattleTeam.OrderBy(p => p.HealthPercentage).FirstOrDefault();
                        mon.HealthPercentage = 100;
                        string message = roomEvent.PreEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(roomEvent.PreEventString);
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        message = roomEvent.PostEventString.Replace("$1", mon.GetInformalName());
                        GenericMessageCommand(message);
                    }
                    break;
                case RoomEventType.STATUS_TRAP:
                    {
                        string status = roomEvent.SpecialParams;
                        List<TrainerPokemon> possibleMons = [.. _trainer.BattleTeam.Where(m => m.NonVolatileStatus == "")]; // Get mons without status
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
                        int numberOfMons = 3;
                        numberOfMons += _context.GetExtraPokemon(); // How many extra mons this fight will have
                        numberOfMons = Math.Clamp(numberOfMons, 1, 24); // Absolute showdown limits
                        // Items obtained during the fight (commons)
                        int uniqueItems = numberOfMons;
                        List<ItemReward> items = new List<ItemReward>();
                        for (int i = 0; i < uniqueItems; i++)
                        {
                            // Prize pool will contain common items
                            ItemReward nextItem = GeneralUtilities.GetRandomPick(_dungeonData.CommonItems);
                            items.Add(nextItem);
                        }
                        // Add Pokemon, they will have items
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < numberOfMons; i++) // Generate party of random mons
                        {
                            string nextPokemon = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[floor]);
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer wildMonsTrainer = GenerateEnemyTrainer("WildMons", pokemonThisFloor, [.. items.Select(i => i.Name)], 100, 100, true);
                        DefineEnemySet(wildMonsTrainer, 24, false); // Defines the enemy set (dumb mons tho)
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, wildMonsTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", string.Join(',', [.. items.Select(i => i.Name)]));
                            GenericMessageCommand(postMessage);
                            foreach (ItemReward item in items) // Add all items to Prizes
                            {
                                int amount = GeneralUtilities.GetRandomNumber(item.Min, item.Max + 1);
                                amount = (int)((amount * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                                if (amount < 1) amount = 1;
                                _prizes.AddReward(item.Name, amount);
                            }
                            foreach (TrainerPokemon pokemonSpecies in wildMonsTrainer.BattleTeam)
                            {
                                _prizes.AddMon(pokemonSpecies, floor + 1);
                            }
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.SWARM:
                    // Similar to aplha but there's 6 enemy mons
                    {
                        int numberOfMons = 6;
                        numberOfMons += _context.GetExtraPokemon(); // How many extra mons this fight will have
                        numberOfMons = Math.Clamp(numberOfMons, 1, 24); // Absolute showdown limits
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < numberOfMons; i++) // Generate party of random mons always from 0
                        {
                            string nextPokemon = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[0]);
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer swarmTrainer = GenerateEnemyTrainer("Swarm", pokemonThisFloor, [], 60, 75, true); // Lvl between 60-75
                        DefineEnemySet(swarmTrainer, 24, false); // Defines the enemy set (dumb mons tho)
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, swarmTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
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
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.UNOWN:
                    // Weird one, select 6 unowns, give them random moves
                    {
                        KeyValuePair<string, string> unownChosen = GeneralUtilities.GetRandomKvp(MechanicsDataContainers.GlobalMechanicsData.UnownLookup);
                        // Calculate the lvl of these dudes
                        int averageLevel = _trainer.BattleTeam.Select(m => m.Level).Sum();
                        averageLevel /= unownChosen.Key.Length; // Unown level will be dependent on the trainer lvl but toned down if more unown than mons
                        int minLvl = 9 * averageLevel / 10;
                        int maxLvl = 11 * averageLevel / 10;
                        minLvl = Math.Clamp(minLvl, 65, 100); // But always between 65-100
                        maxLvl = Math.Clamp(maxLvl, 65, 100);
                        // Then make the team
                        Console.WriteLine($"Unown battle, will form word {unownChosen.Key} with reward {unownChosen.Value.Trim()}");
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < unownChosen.Key.Length; i++) // Generate party of random unowns
                        {
                            char letter = unownChosen.Key[i]; // Obtain next unown
                            string nextPokemon = (letter == 'A') ? "Unown" : $"Unown-{letter}"; // Get the correct unown
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        Trainer unownTrainer = GenerateEnemyTrainer("Symbols", pokemonThisFloor, [], minLvl, maxLvl, false, [.. unownChosen.Key.Select(c => c.ToString())]); // Unowns are unshuffled so they can form the phrase, they also got nickname
                        DefineEnemySet(unownTrainer, 24, false); // Defines the enemy set (smart unowns was proven to be too good so let's use dumb unowns instead)
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, unownTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            string postMessage = roomEvent.PostEventString.Replace("$1", unownChosen.Value.Trim());
                            GenericMessageCommand(postMessage);
                            foreach (TrainerPokemon pokemonSpecies in unownTrainer.BattleTeam)
                            {
                                _prizes.AddMon(pokemonSpecies, 1); // Rank 1
                            }
                            _prizes.AddReward(unownChosen.Value.Trim(), 1); // Add the unown reward
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
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
                        string message = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(message);
                        Trainer bossTrainer = GenerateEnemyTrainer("Firelord", [pokemonSpecies], [], 50, 65, true);
                        DefineEnemySet(bossTrainer, 24, true); // Smart set
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, bossTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            int rewardQuantity = GeneralUtilities.GetRandomNumber(itemPrize.Min, itemPrize.Max + 1);
                            rewardQuantity = (int)((rewardQuantity * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                            if (rewardQuantity < 1) rewardQuantity = 1;
                            message = roomEvent.PostEventString.Replace("$1", itemPrize.Name);
                            GenericMessageCommand(message);
                            _prizes.AddReward(itemPrize.Name, rewardQuantity);
                            _prizes.AddMons(bossTrainer.BattleTeam, 4); // Add the mon (masterball tho)
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
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
                        string message = roomEvent.PreEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(message);
                        Trainer giantMon = GenerateEnemyTrainer("MutantPokemon", [pokemonSpecies], [], 110, 126, true);
                        DefineEnemySet(giantMon, 24, false); // Not smart
                        giantMon.BattleTeam[0].HealthPercentage = 50;
                        giantMon.BattleTeam[0].NonVolatileStatus = "brn";
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, giantMon);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            int rewardQuantity = GeneralUtilities.GetRandomNumber(itemPrize.Min, itemPrize.Max + 1);
                            rewardQuantity = (int)((rewardQuantity * _context.GetItemMult()) + 0.5); // Item multiplier obtained at random
                            if (rewardQuantity < 1) rewardQuantity = 1;
                            message = roomEvent.PostEventString.Replace("$1", itemPrize.Name);
                            GenericMessageCommand(message);
                            _prizes.AddReward(itemPrize.Name, rewardQuantity);
                            _prizes.AddMons(giantMon.BattleTeam, floor + 1);
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.MIRROR_MATCH:
                    // Fight against yourself but a couple levels lower
                    {
                        Trainer copiedTeam = new Trainer()
                        {
                            Name = "Illusion",
                            Avatar = _trainer.Avatar,
                            BattleTeam = [] // Will add mons later
                        };
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
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
                                Logic = mon.Logic,
                                Level = GeneralUtilities.GetRandomNumber(70, 80 + 1) // Random low level
                            };
                            copiedTeam.BattleTeam.Add(copiedMon);
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, copiedTeam);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            foreach (TrainerPokemon mon in _trainer.BattleTeam)
                            {
                                mon.HealFull(); // Heall all mons as reward
                            }
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            GenericMessageCommand(roomEvent.PostEventString);
                            PostFightTrainerMods();
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
                        DefineEnemySet(placeholderTrainer, 24, false); // Not smart
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
                        _trainer.BattleTeam.Add(joiner);
                        GenericMessageCommand(roomEvent.PostEventString);
                        _prizes.AddMon(joiner, 0); // Add mon to lowest floor (free basically)
                        UpdateTrainerDataInfo(); // Updates numbers in chart
                        _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                    }
                    break;
                case RoomEventType.NPC_BATTLE:
                    {
                        Trainer randomNpc = GeneralUtilities.GetRandomKvp(GameDataContainers.GlobalGameData.NpcData).Value; // Get random npc
                        // Define trainer in the same way we defined our own for exploration
                        List<PossibleTeamBuild> possibleBuilds = TeamBuilder.GetTrainersPossibleBuilds(randomNpc, _trainer.BattleTeam.Count, [new Constraint()], true); // Since there's no constraint, will get a build consisting of all my mons, matched to trainer's
                        TeamBuilder.AssembleTrainersBattleTeam(randomNpc, Math.Min(MAX_TRAINER_MONS, _trainer.BattleTeam.Count), possibleBuilds, true); // Chooses one of the sets, prepares the mons, seed 0
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
                        foreach (TrainerPokemon mon in _trainer.BattleTeam)
                        {
                            mon.HealFull();
                        }
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, randomNpc);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            Console.WriteLine("Player won");
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            _prizes.AddReward(randomNpc.Name, 1);
                            npcString = roomEvent.PostEventString.Replace("$1", randomNpc.Name);
                            GenericMessageCommand(npcString);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.APRICORN: // Bug and apricorns
                    {
                        List<string> possiblePokemon = ["Forretress", "Ambipom", "Scyther", "Scizor", "Pinsir", "Heracross", "Shuckle", "Beedrill", "Arbok", "Butterfree", "Venomoth"];
                        string enemySpecies = GeneralUtilities.GetRandomPick(possiblePokemon); // Get a random one of these
                        string apricornString = roomEvent.PreEventString.Replace("$1", enemySpecies);
                        GenericMessageCommand(apricornString); // Prints the message but we know it could have a $1
                        Trainer alphaTrainer = GenerateEnemyTrainer("Tree Dweller", [enemySpecies], [], 100, 100, true);
                        DefineEnemySet(alphaTrainer, 24, true); // Defines the enemy set (smart for an alpha challenge)
                        Console.Write("Encounter resolution: ");
                        PreFightTrainerMods();
                        int remainingMons = ResolveEncounter(_trainer, alphaTrainer);
                        if (remainingMons == 0) // Means player lost
                        {
                            roomCleared = false; // Failure at clearing room
                        }
                        else
                        {
                            List<string> commonApricorns = ["Red", "Yellow", "Blue", "Green"];
                            List<string> rareApricorns = ["Black", "White"];
                            List<string> obtainedApricorns = [];
                            // 5 commons and 2 rares
                            for (int i = 0; i < 3; i++)
                            {
                                obtainedApricorns.Add(GeneralUtilities.GetRandomPick(commonApricorns));
                            }
                            for (int i = 0; i < 1; i++)
                            {
                                obtainedApricorns.Add(GeneralUtilities.GetRandomPick(rareApricorns));
                            }
                            UpdateTrainerDataInfo(); // Updates numbers in chart
                            apricornString = roomEvent.PostEventString.Replace("$1", string.Join(", ", obtainedApricorns.ToHashSet())); // To hash set to avoid duplicates
                            GenericMessageCommand(apricornString); // Prints the message but we know it could have a $1
                            foreach (string apricorn in obtainedApricorns)
                            {
                                _prizes.AddReward(apricorn + " Apricorn", 1); // Add them
                            }
                            _prizes.AddMons(alphaTrainer.BattleTeam, 2); // All bugs are rank 2 idc
                            _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, GenericMessageCommand);
                            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, GenericMessageCommand);
                            PostFightTrainerMods();
                        }
                    }
                    break;
                case RoomEventType.REGISTEEL: // Dramatic drawing of registeel eyes
                    ClearScreenCommand();
                    DrawRegiEye(35, 0, 1000);
                    DrawRegiEye(37, 0, 1000);
                    DrawRegiEye(38, 1, 500);
                    DrawRegiEye(37, 2, 333);
                    DrawRegiEye(35, 2, 333);
                    DrawRegiEye(34, 1, 200);
                    DrawRegiEye(36, 1, 200);
                    NopWithWaitCommand(3000);
                    break;
                case RoomEventType.REGIROCK: // Dramatic drawing of regirock eyes
                    ClearScreenCommand();
                    DrawRegiEye(37, 4, 2000);
                    DrawRegiEye(38, 4, 0);
                    DrawRegiEye(36, 4, 2000);
                    DrawRegiEye(38, 5, 0);
                    DrawRegiEye(38, 3, 0);
                    DrawRegiEye(36, 5, 0);
                    DrawRegiEye(36, 3, 2000);
                    NopWithWaitCommand(1000);
                    break;
                case RoomEventType.REGICE: // Dramatic drawing of regice eyes
                    ClearScreenCommand();
                    DrawRegiEye(58, 8, 1000);
                    DrawRegiEye(58, 7, 0);
                    DrawRegiEye(58, 9, 1000);
                    DrawRegiEye(60, 8, 0);
                    DrawRegiEye(56, 8, 500);
                    DrawRegiEye(59, 8, 0);
                    DrawRegiEye(57, 8, 500);
                    NopWithWaitCommand(3000);
                    break;
                case RoomEventType.REGIELEKI: // Dramatic drawing of regigas eyes
                    ClearScreenCommand();
                    DrawRegiEye(13, 3, 250);
                    DrawRegiEye(14, 3, 250);
                    DrawRegiEye(12, 3, 250);
                    DrawRegiEye(15, 4, 0);
                    DrawRegiEye(15, 2, 250);
                    DrawRegiEye(11, 2, 0);
                    DrawRegiEye(11, 4, 250);
                    NopWithWaitCommand(3000);
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
            string explFile = $"{_trainer.ToString().ToUpper().Replace("?", "")}_EXPLORATION.expl";
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.Clear();
            Console.Write("Fate flavour text: ");
            string fateFlavourText = Console.ReadLine();
            if (fateFlavourText != "")
            {
                GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"Fate: ||{fateFlavourText}||");
                GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine();
            }
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine($"__{_trainer.Name} has obtained the following items:__");
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.AppendLine();
            // Do collections one by one, of the things gained
            int maxCount = 15;
            static string GetPrizesStrings<T>(Dictionary<T, int> dict)
            {
                List<string> collection = new List<string>();
                foreach (KeyValuePair<T, int> data in dict)
                {
                    collection.Add(data.Value > 1 ? $"{data.Key} x{data.Value}" : $"{data.Key}");
                }
                return string.Join(", ", collection);
            }
            // First set items:
            string setItemString = GetPrizesStrings(_prizes.SetItemsFound);
            maxCount = Math.Max(maxCount, setItemString.Length);
            // Mod
            string modItemString = GetPrizesStrings(_prizes.ModItemsFound);
            maxCount = Math.Max(maxCount, modItemString.Length);
            // Mod
            string battleItemString = GetPrizesStrings(_prizes.BattleItemsFound);
            maxCount = Math.Max(maxCount, battleItemString.Length);
            // Favour
            string favourString = GetPrizesStrings(_prizes.FavoursFound);
            maxCount = Math.Max(maxCount, favourString.Length);
            // Key
            string keyString = GetPrizesStrings(_prizes.KeyItemsFound);
            if (_prizes.ImpFound > 0) keyString = $"{_prizes.ImpFound} IMP, " + _prizes.ImpFound;
            // Analyze string
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
            GameDataContainers.GlobalGameData.CurrentEventMessage.EventText.Append($"||"); // Close rank-spoilered text
            GameDataContainers.GlobalGameData.CurrentEventMessage.Signature = "";
            // Save
            string filePath = Path.Combine(DirectoryPath, explFile);
            GameDataContainers.GlobalGameData.CurrentEventMessage.SaveToFile(filePath);
        }
        /// <summary>
        /// Updates all the trainer data in the exploration info table
        /// </summary>
        void UpdateTrainerDataInfo()
        {
            for (int i = 0; i < _trainer.BattleTeam.Count; i++)
            {
                TrainerPokemon mon = _trainer.BattleTeam[i];
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
        bool VerifyShortcutConditions(List<ShortcutConditions> conditions, out string message)
        {
            bool canTakeShortcut = false;
            message = "";
            foreach (ShortcutConditions condition in conditions)
            {
                foreach (string eachOne in condition.Which)
                {
                    switch (condition.ConditionType)
                    {
                        case ShortcutConditionType.MOVE:
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam) // If a mon has move, all good
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
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam) // If a mon has ability, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered with the right set item
                                if (pokemon.ChosenAbility.Name == eachOne && pokemon.SetItemChosen && pokemon.SetItem.AddedAbility?.Name == eachOne) // ability found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.POKEMON:
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam) // If a mon is there, all good
                            {
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered consciously
                                if (pokemon.Species == eachOne && !_trainer.AutoTeam) // species found
                                {
                                    message = $"{pokemon.GetInformalName()}";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.TYPE:
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam) // If a mon has type, all good
                            {
                                Pokemon monSpecies = MechanicsDataContainers.GlobalMechanicsData.Dex[pokemon.Species];
                                // Shortcuts shouldn't be triggered accindentally, so they can only be triggered consciously
                                if (!_trainer.AutoTeam && (monSpecies.Types.Item1.ToString().ToUpper() == eachOne.ToUpper() || monSpecies.Types.Item2.ToString().ToUpper() == eachOne.ToUpper())) // type of pokemon found
                                {
                                    message = $"{pokemon.GetInformalName()}'s {eachOne} type";
                                    canTakeShortcut = true;
                                    break;
                                }
                            }
                            break;
                        case ShortcutConditionType.ITEM: // This is outdated but refers to the battle item I THINK ?!?!
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam) // If a mon has item, all good
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
                            foreach (TrainerPokemon pokemon in _trainer.BattleTeam)
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
        /// Applies all pre-fight traine rmods (i.e. team level)
        /// </summary>
        void PreFightTrainerMods()
        {
            foreach (TrainerPokemon mon in _trainer.BattleTeam)
            {
                mon.LevelMod = _context.GetExtraLevel(); // Check if mons get extra lvl
                mon.LevelMod = Math.Clamp(mon.LevelMod, -(mon.Level - 1), 9999 - mon.Level);
            }
        }
        /// <summary>
        /// Apllies all trainer post-fight events, including healing and reset of lvl
        /// </summary>
        void PostFightTrainerMods()
        {
            foreach (TrainerPokemon mon in _trainer.BattleTeam)
            {
                mon.LevelMod = 0;
                mon.HealthPercentage += _context.GetExtraHealing(); // Heal mon randomly
                mon.HealthPercentage = Math.Clamp(mon.HealthPercentage, 1, 100);
                _context.UseSandwichEffect(SandwichEffectType.POST_HEALING, GenericMessageCommand);
                _context.UseSandwichEffect(SandwichEffectType.LEVEL, GenericMessageCommand);
            }
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
        void ClearScreenCommand()
        {
            ExplorationSteps.Add(new ExplorationStep() // Clean the rooms
            {
                Type = ExplorationStepType.CLEAR_SCREEN
            });
        }
        /// <summary>
        /// Draws the discovered map so far
        /// </summary>
        /// <param name="shown">Room mask that will be shown, with an empty hash set showing the whole map</param>
        void DrawMapCommand(HashSet<char> shown)
        {
            shown ??= [];
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.DRAW_MAP,
                StringParam = string.Join("", shown)
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
                CoordParam = (x, y),
                MillisecondsWait = milliSecondWait
            });
        }
        /// <summary>
        /// Draws the movement of character to a room
        /// </summary>
        /// <param name="room">New character room, negative number if you want character to disappear</param>
        void MoveCharacterCommand(int room)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.MOVE_CHARACTER,
                IntParam = room
            });
        }
        /// <summary>
        /// Will draw an event in the map
        /// </summary>
        /// <param name="roomEvent">Event to draw</param>
        void DrawEventCommand(RoomEvent roomEvent)
        {
            ExplorationSteps.Add(new ExplorationStep()
            {
                Type = ExplorationStepType.DRAW_EVENT,
                CoordParam = (_dungeonData.EventAnchorX + roomEvent.OffsetAnchorX, _dungeonData.EventAnchorY + roomEvent.OffsetAnchorY),
                StringParam = roomEvent.EventLook,
                FgParam = roomEvent.EventFg,
                BgParam = roomEvent.EventBg
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
                CoordParam = tableCoord,
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
        Trainer GenerateEnemyTrainer(string trainerName, List<string> pokemonList, List<string> itemsHeld, int minLvl, int maxLvl, bool shuffled, List<string> nicknames = null)
        {
            nicknames ??= [];
            int encounterShinyChance = _context.GetShinyChance();
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
                bool isShiny = (GeneralUtilities.GetRandomNumber(encounterShinyChance) == 0); // Will be shiny if i get a 0 dice roll
                int level = GeneralUtilities.GetRandomNumber(minLvl, maxLvl + 1);
                TrainerPokemon nextPokemonInTeam = new TrainerPokemon()
                {
                    Species = pokemonList[i],
                    Nickname = (nicknames.Count > i) ? nicknames[i] : "",
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
                            nextPokemonInTeam.SetItemChosen = true;
                            break;
                        case IndymonUtilities.RewardType.MOD:
                            itemAux = MechanicsDataContainers.GlobalMechanicsData.ModItems[itemName];
                            nextPokemonInTeam.ModItem = itemAux;
                            nextPokemonInTeam.ModItemChosen = true;
                            break;
                        case IndymonUtilities.RewardType.BATTLE:
                            itemAux = MechanicsDataContainers.GlobalMechanicsData.BattleItems[itemName];
                            nextPokemonInTeam.BattleItem = itemAux;
                            nextPokemonInTeam.BattleItemChosen = true;
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
            List<Pokemon> enemyMons = [];
            if (smart) // If smart, build against the trainer
            {
                enemyMons = [.. _trainer.BattleTeam.Select(m => MechanicsDataContainers.GlobalMechanicsData.Dex[m.Species])];
            }
            TeamBuilder.DefineTrainerSets(enemy, smart, _dungeonData.DungeonArchetypes, _dungeonData.DungeonWeather, _dungeonData.DungeonTerrain, new Constraint(), enemyMons); // Team build but with the dungeon's weather and such 
            enemy.RestoreAll(); // "Init" mons
        }
        /// <summary>
        /// Resolves an encounter between players, no mon limit
        /// </summary>
        /// <param name="explorer">P1</param>
        /// <param name="encounter">P2</param>
        /// <returns>How many mons P1 has left (0 means defeat)</returns>
        int ResolveEncounter(Trainer explorer, Trainer encounter)
        {
            GenericMessageCommand($""); // Marker for video sync
            (int cursorX, int cursorY) = Console.GetCursorPosition(); // Just in case I need to write in same place
            Console.Write("About to simulate bots. Enter a number instead to input trainer's outcome...");
            string outcome = Console.ReadLine();
            if (!int.TryParse(outcome, out int explorerLeft))
            {
                // The challenge string may contain dungeon-specific rules (besides the mandatory ones)
                List<string> showdownRules = ["!Team Preview", .. _dungeonData.CustomShowdownRules];
                string challengeString = $"gen9customgame@@@{string.Join(",", showdownRules)}"; // Assemble resulting challenge string
                (explorerLeft, _) = BotBattle.SimulateBotBattle(explorer, encounter, challengeString); // Initiate battle
            }
            Console.SetCursorPosition(cursorX, cursorY);
            Console.Write($"Explorer left with {explorerLeft} mons. GET THE REPLAY");
            return explorerLeft;
        }
        #endregion
        #region ANIMATION
        /// <summary>
        /// Animates the resulting exploration
        /// </summary>
        public void AnimateExploration()
        {
            _trainer = IndymonUtilities.GetTrainerByName(TrainerAndSeed.Item1); // Find the trainer data
            char[] infoTableTemplate = "|".ToCharArray();
            List<int> infoColOffset = [2]; // First element "would" start from here
            List<char[]> infoRows = [[.. infoTableTemplate], new string('-', infoTableTemplate.Length).ToCharArray()]; // Starts with the (empty) table header, and a separator
            Console.WriteLine("Write anything to begin. Better start recording now");
            Console.ReadLine();
            Console.Clear();
            int consoleLineStart = 0; // The initial row of the mini console
            int consoleOffset = consoleLineStart; // Where console currently at
            string emptyLine = new string(' ', Console.WindowWidth);
            int lastKnownCharacterRoom = -1; // Last known location of the character icon
            foreach (ExplorationStep nextStep in ExplorationSteps) // Now, will do event one by one...
            {
                Console.BackgroundColor = ConsoleColor.Black; // The console is always black by default, except on the map tileset artwork
                switch (nextStep.Type)
                {
                    case ExplorationStepType.DEFINE_DUNGEON:
                        _dungeonData = GameDataContainers.GlobalGameData.Dungeons[nextStep.StringParam]; // Loads the dungeon
                        consoleLineStart = _dungeonData.TilemapSizeY + 1; // Dungeon dimensions may have changed, so adjust console
                        consoleOffset = consoleLineStart;
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
                        while ((infoRows.Count - 2) <= nextStep.CoordParam.Item2) // If row not yet present, add new until exist
                        {
                            infoRows.Add([.. infoTableTemplate]); // Copy template into new row
                        }
                        (int, int) actualTableCoord = (nextStep.CoordParam.Item1, nextStep.CoordParam.Item2 + 2); // The Y offset is because the first 2 rows are not data but labels+sep
                        UpdateInfoTableField(nextStep.StringParam, actualTableCoord, infoRows, infoColOffset);
                        // Redraw table
                        RedrawInfoTable(infoRows);
                        break;
                    case ExplorationStepType.MOVE_CHARACTER:
                        RedrawCharacter(lastKnownCharacterRoom, nextStep.IntParam);
                        lastKnownCharacterRoom = nextStep.IntParam; // Register current position for later
                        break;
                    case ExplorationStepType.DRAW_EVENT:
                        DrawEvent(lastKnownCharacterRoom, nextStep.CoordParam, nextStep.StringParam, nextStep.FgParam, nextStep.BgParam);
                        break;
                    case ExplorationStepType.DRAW_MAP:
                        for (int y = 0; y < _dungeonData.TilemapSizeY; y++) // Draw line by line
                        {
                            DrawMapLine(0, y, int.MaxValue, nextStep.StringParam); // Draw line with visible string filter
                        }
                        break;
                    case ExplorationStepType.CLEAR_SCREEN:
                        Console.Clear();
                        break;
                    case ExplorationStepType.DRAW_REGI_EYE:
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Red; // Regi eyes are red
                        Console.SetCursorPosition(nextStep.CoordParam.Item1, nextStep.CoordParam.Item2);
                        Console.Write("●"); // Draw regi eye
                        break;
                    case ExplorationStepType.NOP:
                        break;
                    default:
                        throw new Exception($"Event step {nextStep.Type} not impemented");
                }
                if (nextStep.MillisecondsWait > 0)
                {
                    Thread.Sleep(nextStep.MillisecondsWait);
                }
            }
            Console.ReadLine();
            Console.ResetColor();
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
            int tableStart = _dungeonData.TilemapSizeX + 1; // Where the info table starts (leave 1 room separation)
            // Printing loop
            for (int i = 0; i < rows.Count; i++)
            {
                Console.SetCursorPosition(tableStart, i);
                Console.Write(new string(rows[i]));
            }
        }
        /// <summary>
        /// Draws a line in the map, starting at position X, Y
        /// </summary>
        /// <param name="x">X where to start drawing</param>
        /// <param name="y">Y where to start drawing</param>
        /// <param name="length">How much to draw, empty if </param>
        /// <param name="knownAreas">Known areas to be drawn, default draws the whole map</param>
        void DrawMapLine(int x, int y, int length, string knownAreas = "")
        {
            HashSet<char> knownRegion = []; // Mark the valid tiles to draw, upper and lower invariant
            knownRegion.UnionWith([.. knownAreas.ToLower()]);
            knownRegion.UnionWith([.. knownAreas.ToUpper()]);
            Console.SetCursorPosition(x, y);
            while (x < length && x < _dungeonData.TilemapSizeX)
            {
                if (knownRegion.Count == 0 || knownRegion.Contains(_dungeonData.Markers[y][x]))
                {
                    // Draw map
                    Console.BackgroundColor = _dungeonData.BgMap[y][x];
                    Console.ForegroundColor = _dungeonData.FgMap[y][x];
                    Console.Write(_dungeonData.TileMap[y][x]);
                }
                else
                {
                    // Draw fog of war
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(' ');
                }
                x++;
            }
        }
        /// <summary>
        /// Draws character on map, overrides any tile for the moment
        /// </summary>
        /// <param name="character">Character string</param>
        /// <param name="x">X where to start from</param>
        /// <param name="y">Y where to start from</param>
        void DrawMapCharacter(string character, int x, int y)
        {
            Console.SetCursorPosition(x, y);
            for (int i = 0; i < character.Length && (x + i) < _dungeonData.TilemapSizeX; i++)
            {
                char charToDraw = character[i];
                // Figure the colors and draw character light or dark depending on contrast
                Console.BackgroundColor = _dungeonData.BgMap[y][x + i];
                Console.ForegroundColor = Console.BackgroundColor switch
                {
                    ConsoleColor.Black or
                    ConsoleColor.DarkBlue or
                    ConsoleColor.DarkGreen or
                    ConsoleColor.DarkCyan or
                    ConsoleColor.DarkRed or
                    ConsoleColor.DarkMagenta or
                    ConsoleColor.DarkYellow or
                    ConsoleColor.Red or
                    ConsoleColor.Magenta or
                    ConsoleColor.Blue => ConsoleColor.White,
                    ConsoleColor.Gray or
                    ConsoleColor.DarkGray or
                    ConsoleColor.Green or
                    ConsoleColor.Cyan or
                    ConsoleColor.Yellow or
                    ConsoleColor.White => ConsoleColor.Black,
                    _ => throw new Exception("This color doesn't exist")
                };
                Console.Write(charToDraw);
                x++;
            }
        }
        /// <summary>
        /// Gets the screen coordinate of a specific marker
        /// </summary>
        /// <param name="marker">Marker to find</param>
        /// <returns>Screen coordinate, x and y</returns>
        (int, int) GetMarkerCoordinate(char marker)
        {
            int x = 0, y = 0;
            for (int i = 0; i < _dungeonData.Markers.Count; i++)
            {
                if (_dungeonData.Markers[i].Contains(marker))
                {
                    y = i;
                    x = _dungeonData.Markers[i].IndexOf(marker);
                    break;
                }
            }
            return (x, y);
        }
        /// <summary>
        /// Redraws the character in screen
        /// </summary>
        /// <param name="oldPos">Where the character was located before (if known)</param>
        /// <param name="newPos">Where the character is located now</param>
        void RedrawCharacter(int oldPos, int newPos)
        {
            string characterLook = _trainer.DungeonIdentifier; // This is how the character will look
            // First step, clean where character was
            if (oldPos >= 0) // Valid pos
            {
                char oldPosMarker = (char)('A' + oldPos); // Character should've been standing here, redraw this pixel
                (int x, int y) = GetMarkerCoordinate(oldPosMarker);
                DrawMapLine(x, y, characterLook.Length); // Replaces the player with the map tiles in place
            }
            // Next, draw character in new place
            if (newPos >= 0)
            {
                char newPosMarker = (char)('A' + newPos); // Character should've been standing here, redraw this pixel
                (int x, int y) = GetMarkerCoordinate(newPosMarker);
                DrawMapCharacter(characterLook, x, y); // Draw character on screen
            }
        }
        /// <summary>
        /// Draws an event in a specific marker
        /// </summary>
        /// <param name="markerPosition">Marker to draw event</param>
        /// <param name="markerOffset">Relative position around marker</param>
        /// <param name="look">How event looks</param>
        /// <param name="fg">Fg color of event</param>
        /// <param name="bg">Bg color of event</param>
        void DrawEvent(int markerPosition, (int, int) markerOffset, string look, ConsoleColor fg, ConsoleColor bg)
        {
            char posMarker = (char)('A' + markerPosition);
            (int x, int y) = GetMarkerCoordinate(posMarker);
            x += markerOffset.Item1;
            y += markerOffset.Item2;
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            Console.Write(look);
        }
        enum DungeonTestState
        {
            TEST_MARKERS,
            SET_FG,
            SET_BG,
            SET_MARKERS
        }
        /// <summary>
        /// Tests a dungeon image for debugging and visual tests
        /// </summary>
        public void TestDungeonImage()
        {
            Console.WriteLine($"Which dungeon to test? {string.Join(", ", GameDataContainers.GlobalGameData.Dungeons.Keys)}");
            string dungeon = Console.ReadLine();
            _dungeonData = GameDataContainers.GlobalGameData.Dungeons[dungeon];
            char readChar;
            List<char> assembledVisibility = [];
            Console.Clear();
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, 0); // Set cursor to top left
            List<DungeonTestState> possibleStates = [.. Enum.GetValues(typeof(DungeonTestState)).Cast<DungeonTestState>()];
            int stateIdx = 0;
            int x, y;
            DungeonTestState testState = possibleStates[stateIdx];
            do
            {
                ConsoleKeyInfo pressed = Console.ReadKey(true);
                // Check if mode changed
                if (pressed.Key == ConsoleKey.PageUp)
                {
                    stateIdx--;
                    if (stateIdx < 0) stateIdx = possibleStates.Count - 1;
                }
                else if (pressed.Key == ConsoleKey.PageDown)
                {
                    stateIdx++;
                    stateIdx %= possibleStates.Count;
                }
                else if (pressed.Key >= ConsoleKey.LeftArrow && pressed.Key <= ConsoleKey.DownArrow)
                {
                    (x, y) = Console.GetCursorPosition();
                    switch (pressed.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            x--;
                            break;
                        case ConsoleKey.RightArrow:
                            x++;
                            break;
                        case ConsoleKey.UpArrow:
                            y--;
                            break;
                        case ConsoleKey.DownArrow:
                            y++;
                            break;
                        default:
                            throw new Exception("Unreachable state");
                    }
                    x = Math.Clamp(x, 0, _dungeonData.TilemapSizeX - 1);
                    y = Math.Clamp(y, 0, _dungeonData.TilemapSizeY - 1);
                    Console.SetCursorPosition(x, y);
                }
                else
                {
                }
                if (testState != possibleStates[stateIdx]) // There was a change of state, reset all
                {
                    testState = possibleStates[stateIdx];
                    assembledVisibility.Clear();
                    // Notify user too
                    (x, y) = Console.GetCursorPosition();
                    Console.SetCursorPosition(0, Console.WindowHeight - 1);
                    Console.Write($"Test state currently {testState}");
                    Console.SetCursorPosition(x, y);
                }
                // Then, can check the action that was demanded
                readChar = pressed.KeyChar;
                if (char.IsLetterOrDigit(readChar) && readChar != 'q') // If it's a "valid" action (letter or digit, may change something)
                {
                    switch (testState)
                    {
                        case DungeonTestState.TEST_MARKERS:
                            if (!assembledVisibility.Remove(readChar))
                            {
                                assembledVisibility.Add(readChar);
                            }
                            break;
                        case DungeonTestState.SET_FG:
                        case DungeonTestState.SET_BG:
                            ConsoleColor newColor = GameData.Dungeon.CharToColor(readChar);
                            List<List<ConsoleColor>> listToModify = testState switch
                            {
                                DungeonTestState.SET_FG => _dungeonData.FgMap,
                                DungeonTestState.SET_BG => _dungeonData.BgMap,
                                _ => throw new Exception("Unreachable state")
                            };
                            listToModify[Console.CursorTop][Console.CursorLeft] = newColor;
                            break;
                        case DungeonTestState.SET_MARKERS:
                            _dungeonData.Markers[Console.CursorTop][Console.CursorLeft] = readChar;
                            break;
                        default:
                            throw new Exception("Invalid state reached");
                    }
                    Console.CursorLeft++; // Move cursor
                    if (Console.CursorLeft >= _dungeonData.TilemapSizeX) Console.CursorLeft = 0;
                }
                // Then, draw
                (x, y) = Console.GetCursorPosition();
                Console.CursorVisible = false;
                string visibilityString = string.Join("", assembledVisibility);
                for (int line = 0; line < _dungeonData.TilemapSizeY; line++) // Draw line by line
                {
                    DrawMapLine(0, line, int.MaxValue, visibilityString); // Draw line with visible string filter
                }
                Console.SetCursorPosition(x, y);
                Console.CursorVisible = true;
            } while (readChar != 'q');
            Console.ResetColor();
            Console.CursorVisible = false;
            // Final thing, ask if want to save new thing
            Console.WriteLine("Save modified tilesets? Y/n");
            if (Console.ReadLine().ToLower() != "n")
            {
                string targetDirectory = Path.Combine(DirectoryPath, "dungeons");
                void SaveCharArray(string file, List<List<char>> array)
                {
                    StringBuilder lines = new StringBuilder();
                    foreach (List<char> line in array)
                    {
                        lines.AppendLine(string.Join("", line));
                    }
                    File.WriteAllText(file, lines.ToString());
                }
                void SaveColorArray(string file, List<List<ConsoleColor>> array)
                {
                    StringBuilder lines = new StringBuilder();
                    foreach (List<ConsoleColor> line in array)
                    {
                        foreach (ConsoleColor color in line)
                        {
                            lines.Append(GameData.Dungeon.ColorToChar(color));
                        }
                        lines.AppendLine();
                    }
                    File.WriteAllText(file, lines.ToString());
                }
                string nextFile = Path.Combine(targetDirectory, "new.tile");
                SaveCharArray(nextFile, _dungeonData.TileMap);
                nextFile = Path.Combine(targetDirectory, "new.marker");
                SaveCharArray(nextFile, _dungeonData.Markers);
                nextFile = Path.Combine(targetDirectory, "new.fg");
                SaveColorArray(nextFile, _dungeonData.FgMap);
                nextFile = Path.Combine(targetDirectory, "new.bg");
                SaveColorArray(nextFile, _dungeonData.BgMap);
            }
        }
        #endregion
    }
}
