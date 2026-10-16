using Gameplay.GameEngine;
using Gameplay.GameplayElements;
using Gameplay.GameplayElementsContainer;
using GameSimulator;
using Utilities;

namespace IndymonBackendProgram
{
    class ExplorationPrizeList
    {
        public Dictionary<string, int> ItemPrizes = [];
        public Dictionary<int, Dictionary<string, int>> MonsFound = []; // Mons divided by rank
    }
    /// <summary>
    /// Contains all the dynamic ongoing dungeon things and useful functions
    /// </summary>
    class ExplorationContext
    {
        List<(Sandwich, int)> _sandwichEffects = [];
        public MessageLogger ExplorationMessage = new MessageLogger();
        public ExplorationPrizeList Prizes = new ExplorationPrizeList();
        public Dungeon DungeonData;
        public TrainerEntity TrainerEntity;
        public TrainerInstance TrainerInstance;
        public TrainerInstance FollowerInstance = null; // Follower if any
        public Random Rng;
        public int CurrentFloor;
        public HashSet<PokemonSpecies> EncounterTable = [];
        public List<ItemDrop> DropTable = [];
        /// <summary>
        /// Adds a sandwich effect to the current context
        /// </summary>
        /// <param name="sandwich"></param>
        public void AddSandwichEffect(Sandwich sandwich)
        {
            _sandwichEffects.Add((sandwich, sandwich.Duration));
        }
        // The sandwiches with more duration are better because they last for bigger battles so the variance needs to be retought and be more negative, shouldnt keep the mean as is, but instead move the mean around
        /// <summary>
        /// Does a random pull with the current game effects to try and get an extra pokemon
        /// </summary>
        /// <returns>How many extra pokemon are found in this encounter. Can be negative</returns>
        public int GetExtraWildPokemon()
        {
            // Lvl 5 will add 5 mons, etc. Result will be an int so need to round
            double extraMonCount = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.ENEMY_NUMBER).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double max = sando.Level;
                double min = max - 0.5 * max * (sando.Duration - 1); // Reaches -MAX at duration 5
                double randomFloat = min + ((max - min) * Rng.NextDouble());
                extraMonCount += randomFloat;
            }
            return (int)Math.Round(extraMonCount); // Always round to closest integer
        }
        /// <summary>
        /// Does a random pull with how much will be the item multiplier
        /// </summary>
        /// <returns>Item multiplier</returns>
        public double GetItemMult()
        {
            // Similar but the additive nature is to a mult which will need to be rounded
            double itemMult = 1; // Base item multiplier
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.ITEM_DROP).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double max = sando.Level;
                double min = max - 0.5 * max * (sando.Duration - 1); // Reaches -MAX at duration 5
                double randomFloat = min + ((max - min) * Rng.NextDouble());
                itemMult += randomFloat;
            }
            return itemMult;
        }
        /// <summary>
        /// Does a random pull with how much will be the extra healing in int percentage
        /// </summary>
        /// <returns>Percentage healing</returns>
        public double GetExtraHealing()
        {
            // This sando heals 5% to 25% at lvl 5. Max is always that and variance just opens amplitude below as the duration approaches 5. Mean value actually decreases but because healing a potential of 25% is the cumul part when duration is there
            double totalHealingPercentage = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.POST_HEALING).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double max = 0.05 * sando.Level; // Max healing
                double min = max - 0.5 * max * (sando.Duration - 1); // Reaches -MAX at duration 5
                double randomFloat = min + ((max - min) * Rng.NextDouble());
                totalHealingPercentage += randomFloat;
            }
            return totalHealingPercentage;
        }
        /// <summary>
        /// Does a random pull with how much will be the shiny chance for a fight
        /// </summary>
        /// <returns>Shiny chance</returns>
        public int GetShinyChance()
        {
            // This one is kind of inverse. Being shiny chance of 1 in 500. A lvl 5 will be 50 times more likely (i.e. 1 in 10) and the others are all the way inbetween
            // This moves the mean, and more sandwiches then will jus oscilate similarly around the new mean
            double shinyChance = GameplayElementsContainer.SHINY_CHANCE; // Base shiny chance (1 in)
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.SHINY_CHANCE).Select(s => s.Item1)]; // Get the valid sandwiches
            if (validSandwichEffects.Count > 0)
            {
                foreach (Sandwich sando in validSandwichEffects)
                {
                    double bestCaseShinyMean = shinyChance / 50; // 50 times more likely is the hardcoded best chance
                    double shinyVariance = (bestCaseShinyMean - shinyChance) * (sando.Level - 1) / 4; // Meaning a lvl 5 will reach this and lvl 1 etc not so much
                    double min = bestCaseShinyMean - shinyVariance; // This is the good shit, better shiny chances
                    double max = bestCaseShinyMean + shinyVariance * (sando.Duration - 1) / 4;
                    // Kind of, I guess...
                    // So now I roll
                    shinyChance = min + ((max - min) * Rng.NextDouble()); // New mean, next rolls will be around here, may be insanely good or drift worse and worse (realistically it should usually be just 1 sandwich anyway)
                }
            }
            int resultingChance = (int)shinyChance; // Get the chance 1/X, round to closest integer
            if (resultingChance < 1) resultingChance = 1; // Chance needs to keep positive otherwise the roll goes to fuck
            return resultingChance;
        }
        /// <summary>
        /// Does a random pull of extra level gain
        /// </summary>
        /// <returns>The lvl gain for a mon</returns>
        public double GetExtraStatMult()
        {
            // Similarly to healing and stuff, each levels increases stats by 5% (so a mult of 1.05 to 1.25 but this mult should be additive i'd say?)
            double totalLevelGain = 0; // Will be double to stack many double effects
            List<Sandwich> validSandwichEffects = [.. _sandwichEffects.Where(s => s.Item1.Effect == SandwichEffectType.STAT_MULT).Select(s => s.Item1)]; // Get the valid sandwiches
            foreach (Sandwich sando in validSandwichEffects)
            {
                double max = 0.05 * sando.Level; // Max healing
                double min = max - 0.5 * max * (sando.Duration - 1); // Reaches -MAX at duration 5
                double randomFloat = min + ((max - min) * Rng.NextDouble());
                totalLevelGain += randomFloat;
            }
            return totalLevelGain;
        }
        /// <summary>
        /// Uses the effect of a sandwich type
        /// </summary>
        /// <param name="effect">The effect that will be used</param>
        /// <param name="expirationAction">Container for expiration messages</param>
        /// <returns>A list of messages of the sandwiches that have been consumed</returns>
        public void UseSandwichEffect(SandwichEffectType effect, Action<string> expirationAction)
        {
            List<(Sandwich, int)> expiredSandwiches = [.. _sandwichEffects.Where(s => s.Item1.Effect == effect && s.Item2 == 1)]; // Get the valid sandwiches that will be exausted here
            foreach (string sando in expiredSandwiches.Select(s => s.Item1.Name)) // print the expired ones
            {
                expirationAction($"The {sando} effect has worn off");
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
        // Private data to retrieve stuff
        readonly ExplorationContext _context = new ExplorationContext();
        // Public data (saved)
        public string Dungeon { get; set; }
        public int ExplorationSeed { get; set; }
        public (string, int, int) TrainerAndSeeds { get; set; } // Trainer seed, mon seed
        public string DirectoryPath { get; set; } = "";
        /// <summary>
        /// Sets up exploration basics to later run it
        /// </summary>
        /// <param name="directoryPath">Path where stuff will be put</param>
        public void InitializeExploration(string directoryPath)
        {
            DirectoryPath = directoryPath;
            Random rng = GameplayElementsContainer.GlobalData.CommonRng;
            // First ask organizer to choose dungeon
            List<string> dungeonOptions = [.. GameplayElementsContainer.GlobalData.Dungeons.Keys];
            Console.WriteLine($"Creating a brand new exploration, which dungeon? (something else for random)\n{string.Join(", ", dungeonOptions)}");
            string Dungeon = Console.ReadLine();
            if (!GameplayElementsContainer.GlobalData.Dungeons.ContainsKey(Dungeon)) // If invalid, pick a random dungeon then
            {
                Dungeon = GeneralUtilities.GetRandomPick(dungeonOptions, rng);
            }
            Console.WriteLine(Dungeon);
            // Then which player
            List<string> validTrainers = [.. GameplayElementsContainer.GlobalData.Trainers.Keys];
            Console.WriteLine($"Which trainer will explore?\n{string.Join(", ", validTrainers)}");
            TrainerAndSeeds = (Console.ReadLine(), rng.Next(), rng.Next());
        }
        /// <summary>
        /// Begins an exploration, starts a simulation
        /// </summary>
        public void ExecuteExploration()
        {
            _context.Rng = new Random(ExplorationSeed);
            _context.ExplorationMessage.Clear(); // This is a new event, so I will clear whatever thet was there before
            // Just to help debug
            Console.Clear();
            // Initialize from beginning
            _context.DungeonData = GameplayElementsContainer.GlobalData.Dungeons[Dungeon];
            _context.TrainerEntity = GameplayElementsContainer.GlobalData.Trainers[TrainerAndSeeds.Item1]; // Find the trainer data
            // Initialize all trainer/mon instances. Will be stored in context
            if (_context.TrainerEntity.AutoTeam)
            {
                Random trainerRng = new Random(TrainerAndSeeds.Item2); // Only case where this is used really. Shuffles the Pokemon around, only remaining RNG stuff
                GeneralUtilities.ShuffleList(_context.TrainerEntity.Pokemon, trainerRng);
            }
            int monSeed = TrainerAndSeeds.Item3;
            Random pokemonRng = new Random(monSeed);
            _context.TrainerInstance = new TrainerInstance()
            {
                Name = _context.TrainerEntity.Name,
                Team = Side.BOTTOM,
                MaxMonsInField = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                // No limit for max mon used, will start with 3-10 and then who knows
                ActiveJewelry = _context.TrainerEntity.EquippedJewelry,
                PokemonInTeam = [.. _context.TrainerEntity.Pokemon.Select(p => SimulatorHandler.InstancePokemon(p, new Random(pokemonRng.Next())))]
            };
            // Check expl ticket
            bool explTicket = false;
            KeyItem explTicketItem = GameplayElementsContainer.GlobalData.KeyItems["Exploration Ticket"];
            if (_context.TrainerEntity.KeyItems.ContainsKey(explTicketItem)) // Fetch the expl ticket key item
            {
                Console.WriteLine("Use ticket? y/N");
                explTicket = Console.ReadLine().ToLower() == "y";
            }
            if (explTicket) // Remove ticket and also different message
            {
                GeneralUtilities.AddtemToCountDictionary(_context.TrainerEntity.KeyItems, explTicketItem, -1); // Remove a ticket
                _context.ExplorationMessage.EventTitle = $"<@{_context.TrainerEntity.DiscordId}> Meanwhile, {_context.TrainerEntity.Name} used their Exploration Ticket for a free trip to {Dungeon}.";
            }
            else
            {
                _context.ExplorationMessage.EventTitle = $"<@{_context.TrainerEntity.DiscordId}> Meanwhile, {_context.TrainerEntity.Name} went on to explore the {Dungeon}.";
            }
            // Check if trainer will use an exploration assistance favour
            if (_context.TrainerEntity.Favours.Count > 0)
            {
                Console.WriteLine($"Use Favour? {string.Join(", ", _context.TrainerEntity.Favours.Keys.Select(t => t.Name))}");
                string favourChosen = Console.ReadLine();
                NpcTrainer favourTrainer = _context.TrainerEntity.Favours.Keys.Where(t => t.Name == favourChosen).FirstOrDefault();
                if (favourTrainer != null)
                {
                    Console.WriteLine($"{favourTrainer.Name} selected");
                    GeneralUtilities.AddtemToCountDictionary(_context.TrainerEntity.Favours, favourTrainer, -1); // Take favour
                    TrainerEntity followerEntity = GameplayElementsContainer.GlobalData.CreateNpcEntity(favourTrainer, _context.TrainerEntity.Pokemon.Count, _context.Rng.Next(), 1, GameplayElementsContainer.SHINY_CHANCE, []);
                    _context.FollowerInstance = new TrainerInstance()
                    {
                        Name = followerEntity.Name,
                        Team = Side.BOTTOM, // Same team as player
                        MaxMonsInField = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                        // No limit for max mon used, will start with 3-10 and then who knows
                        ActiveJewelry = followerEntity.EquippedJewelry,
                        PokemonInTeam = [.. followerEntity.Pokemon.Select(p => SimulatorHandler.InstancePokemon(p, new Random(_context.Rng.Next())))] // Instance the mons too
                    };
                }
            }
            // Get the mon table for this dungeon (weather and ALL), need to also use the wild alternatives!
            HashSet<string> allWeatherMons = [.. _context.DungeonData.WeatherMons[_context.DungeonData.CurrentWeather.Name], .. _context.DungeonData.WeatherMons["ALL"]];
            foreach (string weatherMon in allWeatherMons)
            {
                PokemonSpecies monSpecies = GameplayElementsContainer.GlobalData.Dex[weatherMon];
                _context.EncounterTable.Add(monSpecies);
                _context.EncounterTable.UnionWith(monSpecies.WildAlternatives); // Also add the variants
            }
            // Also get item drop table for lookup
            _context.DropTable = [.. _context.DungeonData.CommonDrops, .. _context.DungeonData.RareDrops];
            // Exploration loop begin
            List<Encounter> pastEncounters = [];
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine($"Beginning of {_context.TrainerEntity.Name}'s exploration in {Dungeon}"));
            for (_context.CurrentFloor = 0; _context.CurrentFloor < GameplayElementsContainer.DUNGEON_NUM_FLOORS; _context.CurrentFloor++) // Begin the iteration of all floors
            {
                bool isFinalFloor = (_context.CurrentFloor == GameplayElementsContainer.DUNGEON_NUM_FLOORS - 1);
                for (int room = 0; room < GameplayElementsContainer.DUNGEON_ROOMS_PER_FLOOR; room++) // Begin the iteration of floor depth
                {
                    // Choose potential rooms to pick from
                    List<Encounter> possibleEncountersThisRoom;
                    if (room == GameplayElementsContainer.DUNGEON_ENCOUNTER_ROOM)
                    {
                        // Need to generate a procedural wild battle encounter
                        possibleEncountersThisRoom = [
                            new Encounter() // This encounter is simple because there's no scripted overrides just a Pokemon battle with some shit going on
                            {
                                Type = EncounterType.BATTLE_AND_PRIZE,
                                Category = EncounterCategory.DANGER,
                                PreEncounterString = "Suddenly, wild Pokemon attack!",
                                EncounterSuccessString = "You defeated the wild Pokemon and gathered some items that were dropped: $2", // Prints the prizes
                                EncounterFailureString = "The Pokemon in this area are quite tough...",
                                PrizeType = EncounterItemType.EQUIPPED_ITEMS,
                                UseEnemyCountSandwich = true,
                                Enemies = [
                                    new EnemyParams() // Standard enemy params besides these (3 pokemon but variabel length and catchable
                                    {
                                        Type = EncounterEnemyType.CURRENT_FLOOR,
                                        EquipType = EncounterItemType.COMMON_ITEM_POOL,
                                        EquipChance = GameplayElementsContainer.NPC_ITEM_CHANCE,
                                        Number = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                                        MaxSimultaneousMons = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                                        Catchable = true
                                    }
                                ]
                            }
                        ];
                    }
                    else if (isFinalFloor && room == GameplayElementsContainer.DUNGEON_FINAL_BOSS_ROOM)
                    {
                        possibleEncountersThisRoom = [_context.DungeonData.FinalBossEncounter]; // Simply the final boss encounter to choose from
                    }
                    else if (!isFinalFloor && room == GameplayElementsContainer.DUNGEON_BOSS_ROOM)
                    {
                        possibleEncountersThisRoom = [GeneralUtilities.GetRandomPick(_context.DungeonData.BossEncounters, _context.Rng)]; // Choose one random boss battle
                    }
                    else
                    {
                        // Otherwise, random pick of encounters, given their absolute weights and then a bit of shenanigans to softmax weights with some rng, sorts the resulting list (bit of black magic ngl)
                        possibleEncountersThisRoom = [.. _context.DungeonData.EncounterPool.OrderBy(e => -Math.Log(_context.Rng.NextDouble() / GetRoomEncounterWeight(e, room, pastEncounters)))];
                        int numberOfRooms = GeneralUtilities.GetRandomDecayingNumber(1, GameplayElementsContainer.MAX_SIMULTANEOUS_ROOM_PICKS, 2, _context.Rng); // Choose between 1-5 rooms
                        possibleEncountersThisRoom = possibleEncountersThisRoom[..numberOfRooms]; // Splice only the first n
                    }
                    Encounter roomEncounter = GeneralUtilities.GetRandomPick(possibleEncountersThisRoom, _context.Rng); // Feels wasteful but now choose a random one of these encounters (all this was for animation sake)
                    // Now, attempt to do the room's encounter
                    ExecuteEncounter(roomEncounter);
                    // Checking for the case where trainer or their friend has no more usable Pokemon
                    if (!_context.TrainerInstance.PokemonInTeam.Any(p => p.Availability == BattleAvailability.READY)
                        && !_context.FollowerInstance.PokemonInTeam.Any(p => p.Availability == BattleAvailability.READY))
                    {
                        Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine($"Trainer blacked out..."));
                        goto DungeonEnd; // Just go directly to end
                    }
                }
            }
        DungeonEnd: // Best way to break from a 2 damn nested loops I think
            ;
            // Exploration end part
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine("Exploration end."));
            GameplayElementsContainer.ConsumeTrainersItems(_context.TrainerEntity); // Consume all items for all mons
            GameplayElementsContainer.ConsumeTrainersJewelry(_context.TrainerEntity); // Consume jew too
            _context.TrainerEntity.SaveTrainerCsv(DirectoryPath);
            SaveExplorationOutcome(); // Replace with message stuff
        }
        /// <summary>
        /// Calculates the weight of each encounter room given a couple of extra params
        /// </summary>
        /// <param name="encounter">Encounter to check</param>
        /// <param name="room">Current room within floor</param>
        /// <param name="previousEncounters">List of previous encounters for history purposes</param>
        /// <returns></returns>
        double GetRoomEncounterWeight(Encounter encounter, int room, List<Encounter> previousEncounters)
        {
            double weight = encounter.BaseWeight; // Start with base weight (i.e. rarity)
            weight *= Math.Pow(0.1, previousEncounters.Count(x => x == encounter)); // Each time an encounter already happened, it's 90% less rare, prioritises events that hadn't happened yet
            if (previousEncounters.Count > 0 && previousEncounters.Last().Category == encounter.Category) weight *= 0.5; // Slightly reduces chance of multiple categories in a row
            // Danger chance multiplier depending on the difficulty level of the dungeon
            if (_context.DungeonData.Difficulty >= 5)
            {
                weight *= encounter.Category switch // Good things less likely, bad things more likely but also more prizes
                {
                    EncounterCategory.DANGER => 1.5,
                    EncounterCategory.REFRESH => 0.75,
                    EncounterCategory.PRIZE => 1.2,
                    _ => 1
                };
            }
            else if (encounter.Category == EncounterCategory.DANGER && _context.DungeonData.Difficulty >= 3)
            {
                weight *= encounter.Category switch // Good things less likely, bad things more likely but also more prizes (less so)
                {
                    EncounterCategory.DANGER => 1.2,
                    EncounterCategory.REFRESH => 0.85,
                    EncounterCategory.PRIZE => 1.1,
                    _ => 1
                };
            }
            else
            {
                weight *= 1; // Otherwise these low diffs dont multiply chances
            }
            // Room 4 (5th room) always try to prioritize a treasure or a healing (give insanely low score to anything else)
            if (room == 4 && (encounter.Category != EncounterCategory.PRIZE || encounter.Category != EncounterCategory.REFRESH))
            {
                weight *= 0.01;
            }
            // Finally, boost certain specific rooms or not depending on additional stuff (more specifics)
            switch (encounter.Type)
            {
                case EncounterType.CAMPING:
                    weight *= (_context.TrainerEntity.Sandwiches.Count > 0) ? 1.2 : 0.8; // Less chance of eating sandwich if there's not in the inventory
                    break;
                case EncounterType.EVO:
                    weight *= (_context.TrainerEntity.EvoPlates.Count > 0 && _context.TrainerEntity.Pokemon.Any(p => p.Species.Evos.Count > 0)) ? 1.2 : 0.8; // Less chance of eating evo event if no mon can use it (or no plates)
                    break;
                case EncounterType.HP_HEAL:
                    weight *= 1 + _context.TrainerInstance.PokemonInTeam.Count(p => p.HpPercent < 0.66) * 0.05; // Add a 5% for each mon below 66%
                    break;
                case EncounterType.FULL_RESTORE:
                    weight *= _context.TrainerInstance.PokemonInTeam.Any(p => p.HpPercent < 0.25) ? 1.2 : 0.8; // If mon heavily hurt there's more chance
                    break;
                case EncounterType.REVIVE:
                    weight *= _context.TrainerInstance.PokemonInTeam.Any(p => p.Availability == BattleAvailability.FAINT) ? 1.2 : 0.8; // If mon dead
                    break;
                case EncounterType.STATUS_CURE:
                    weight *= _context.TrainerInstance.PokemonInTeam.Any(p => p.NonVolatileStatusConditions.Count > 0) ? 1.2 : 0.8; // If any mon status'd
                    break;
                // These events are always base chance
                case EncounterType.BATTLE_AND_PRIZE:
                case EncounterType.JOINER:
                case EncounterType.DAMAGE_TRAP:
                case EncounterType.STATUS_TRAP:
                case EncounterType.KILL_TRAP:
                case EncounterType.NPC_BATTLE:
                case EncounterType.UNOWN:
                    break;
                default:
                    throw new Exception("Unimplemented event type in exploration");
            }
            return weight;
        }
        /// <summary>
        /// Executes an encounter of the many possible in dungeon
        /// </summary>
        /// <param name="encounter">Encounter to simulate</param>
        void ExecuteEncounter(Encounter encounter)
        {
            // Success or failure, they assemble the string, and then the string is printed in the end once loaded
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine($"{encounter.Type} - {encounter.PreEncounterString}"));
            // Now execute encounter
            switch (encounter.Type)
            {
                case EncounterType.BATTLE_AND_PRIZE:
                    ResolveBattleAndPrizeEncounter(encounter); // Works kinda for treasures and other stuff too lol
                    break;
                case EncounterType.CAMPING:
                    break;
                case EncounterType.EVO:
                    break;
                case EncounterType.HP_HEAL:
                    break;
                case EncounterType.FULL_RESTORE:
                    break;
                case EncounterType.REVIVE:
                    break;
                case EncounterType.STATUS_CURE:
                    break;
                case EncounterType.JOINER:
                    break;
                case EncounterType.DAMAGE_TRAP:
                    break;
                case EncounterType.STATUS_TRAP:
                    break;
                case EncounterType.KILL_TRAP:
                    break;
                case EncounterType.NPC_BATTLE:
                    break;
                case EncounterType.UNOWN:
                    break;
                default:
                    throw new Exception($"This encounter type {encounter.Type} is not known");
            }
            /*
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
                        string equippedItem = _dungeonData.BossItem.Name;
                        IndymonUtilities.RewardType itemType = IndymonUtilities.GetRewardType(equippedItem);
                        while (!(itemType == IndymonUtilities.RewardType.MOD || itemType == IndymonUtilities.RewardType.BATTLE || itemType == IndymonUtilities.RewardType.SET))
                        {
                            equippedItem = GeneralUtilities.GetRandomPick(_dungeonData.RareItems).Name; // Pick new item from rare pool until it's an equippable one
                            itemType = IndymonUtilities.GetRewardType(equippedItem);
                        }
                        if (equippedItem == "Sun Stone") // Bosses are immune to sun stone's negative effect
                        {
                            equippedItem = "Boss Sun Stone";
                        }
                        Trainer bossTrainer = GenerateEnemyTrainer("BossEncounter", [enemySpecies], [equippedItem], 100, 100, true);
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
                        List<Move> validMoves = [.. MechanicsDataContainers.GlobalMechanicsData.Moves.Values.Where(m => !m.Flags.Contains(EffectFlag.NORMALLY_UNAVAILABLE) && !m.Flags.Contains(EffectFlag.BANNED))];
                        string chosenMove = GeneralUtilities.GetRandomPick(validMoves).Name;
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
                        numberOfMons += _context.GetExtraWildPokemon(); // How many extra mons this fight will have
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
                        GenericMessageCommand($"Pokemon encountered: {string.Join(", ", pokemonThisFloor)}");
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
                        numberOfMons += _context.GetExtraWildPokemon(); // How many extra mons this fight will have
                        numberOfMons = Math.Clamp(numberOfMons, 1, 24); // Absolute showdown limits
                        List<string> pokemonThisFloor = [];
                        for (int i = 0; i < numberOfMons; i++) // Generate party of random mons always from 0
                        {
                            string nextPokemon = GeneralUtilities.GetRandomPick(_dungeonData.PokemonEachFloor[0]);
                            pokemonThisFloor.Add(nextPokemon); // Add mon to the set
                        }
                        GenericMessageCommand(roomEvent.PreEventString);
                        GenericMessageCommand($"Pokemon encountered: {string.Join(", ", pokemonThisFloor)}");
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
                        joinerString = roomEvent.PostEventString.Replace("$1", pokemonSpecies);
                        GenericMessageCommand(joinerString);
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
                        randomNpc.RestoreAll();
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
                case RoomEventType.REGIDRAGO: // Dramatic drawing of regigas eyes (53,10)
                    ClearScreenCommand();
                    DrawRegiEye(53, 11, 1000);
                    DrawRegiEye(53, 12, 0);
                    DrawRegiEye(52, 10, 0);
                    DrawRegiEye(54, 10, 1000);
                    DrawRegiEye(51, 9, 0);
                    DrawRegiEye(53, 9, 0);
                    DrawRegiEye(55, 9, 1000);
                    NopWithWaitCommand(1000);
                    break;
                default:
                    break;
            }
            return roomCleared;
            */
        }
        /// <summary>
        /// Function that digs until the end to find every single relative of the desired Pokemon Species (i.e. it finds all alternates and prevos)
        /// </summary>
        /// <param name="speciesCollection">The species to look for family</param>
        /// <returns>The complete family tree but no evos!</returns>
        static HashSet<PokemonSpecies> GetFamilyHash(IEnumerable<PokemonSpecies> speciesCollection)
        {
            HashSet<PokemonSpecies> resultingSpecies = [];
            foreach (PokemonSpecies species in speciesCollection)
            {
                resultingSpecies.Add(species);
                foreach (PokemonSpecies alt in species.WildAlternatives)
                {
                    resultingSpecies.UnionWith(GetFamilyHash([alt]));
                }
                resultingSpecies.UnionWith(GetFamilyHash([species.Prevo]));
            }
            return resultingSpecies;
        }
        /// <summary>
        /// Resolves an encounter scripted battle. Similar to the wild battle one but this is way more custom. Doesn't use enemy quant sandwiches tho
        /// </summary>
        /// <param name="encounter"></param>
        void ResolveBattleAndPrizeEncounter(Encounter encounter)
        {
            int paramsIndex = 0; // Index that will navigate through scripted battle's params
            List<TrainerInstance> allEnemyInstances = []; // Contains all generated enemy instances that will participate in this fight
            // Potential prize tables, if the event allows it, both pokemon and items but only filled if catchable
            List<PokemonInstance> catchableEnemyMons = [];
            List<string> potentialPrizes = [];
            // Assemble all sides, enemy sides
            HashSet<PokemonSpecies> speciesUsed = []; // Species that have been used by these enemies
            foreach (EnemyParams enemy in encounter.Enemies) // Instance, one by one
            {
                List<string> itemsUsed = []; // Items that have been used by enemy, there can be duplicates
                // Assemble what the enemy can be, and what can it have
                HashSet<PokemonSpecies> availablePokemonSpecies = enemy.Type switch
                {
                    EncounterEnemyType.SPECIFIC_POKEMON => [.. encounter.Params[paramsIndex++].Split(",").Select(p => GameplayElementsContainer.GlobalData.Dex[p.Trim()])], // Gets (all the) specific pokemon in the params, separated by commas
                    EncounterEnemyType.NEXT_FLOOR => [.. _context.EncounterTable.Where(m => m.Rank == Math.Clamp(_context.CurrentFloor + 2, 1, 3))], // Make sure floor not out of bounds!
                    EncounterEnemyType.CURRENT_FLOOR => [.. _context.EncounterTable.Where(m => m.Rank == Math.Clamp(_context.CurrentFloor + 1, 1, 3))], // Make sure floor not out of bounds!
                    EncounterEnemyType.PREVIOUS_FLOOR => [.. _context.EncounterTable.Where(m => m.Rank == Math.Clamp(_context.CurrentFloor + 0, 1, 3))], // Make sure floor not out of bounds!
                    EncounterEnemyType.FIRST_FLOOR => [.. _context.EncounterTable.Where(m => m.Rank == 1)],
                    EncounterEnemyType.FAMILY => [.. GetFamilyHash(speciesUsed)],
                    EncounterEnemyType.NONE => throw new Exception("Enemy was not defined properly and still has NONE as type"),
                    _ => throw new Exception("Unknown scripted enemy type"),
                };
                List<string> availableEquippableItems = enemy.EquipType switch
                {
                    EncounterItemType.NONE => [],
                    EncounterItemType.SPECIFIC_ITEM => [.. encounter.Params[paramsIndex++].Split(",")], // Just get it from params ig
                    EncounterItemType.COMMON_ITEM_POOL => [.. _context.DungeonData.CommonDrops.Select(d => d.Name)],
                    EncounterItemType.RARE_ITEM_POOL => [.. _context.DungeonData.RareDrops.Select(d => d.Name)],
                    EncounterItemType.ALL_ITEM_POOLS => [.. _context.DungeonData.CommonDrops.Select(d => d.Name), .. _context.DungeonData.RareDrops.Select(d => d.Name)],
                    EncounterItemType.DISKS or EncounterItemType.IMP or EncounterItemType.EVO_PLATE or EncounterItemType.SANDWICH or EncounterItemType.ENEMIES or EncounterItemType.EQUIPPED_ITEMS
                        => throw new Exception("These item types are not a valid descriptor of what items a mon can equip. These are only for prizes. Try again"),
                    _ => throw new Exception("Unknown scripted item type"),
                };
                // Then, time to assemble the enemy "NPC trainer"
                NpcTrainer enemyTrainer = new NpcTrainer()
                {
                    // No name or picture, would be left blank in renderer
                    TrainerRank = TrainerRank.WILD_MONS,
                    FullyLoadedData = true, // It's generated here...
                    AvailablePokemon = [.. availablePokemonSpecies],
                    AvailableItems = availableEquippableItems // Adds the potential items by name, the actual drop amount and prize type would be considered later (if the battle is won!)
                };
                // How many mons will appear?
                int nMons = enemy.Number;
                if (encounter.UseEnemyCountSandwich) // Check if the sandwich can alter the count
                {
                    nMons += _context.GetExtraWildPokemon();
                    if (nMons < 1) // Always at least one enemy...
                    {
                        nMons = 1;
                    }
                }
                // Next, the trainer entity, i.e. from NPC data into an actual entity
                TrainerEntity wildTrainerEntity = GameplayElementsContainer.GlobalData.CreateNpcEntity(enemyTrainer, nMons, _context.Rng.Next(), enemy.EquipChance, _context.GetShinyChance(), itemsUsed); // For now the shiny chance is identical to all mons, scripted or not
                // Finally, the actual instance for game sim, both the trainer AND the mons
                // Create instance of all mons
                TrainerInstance wildTrainerInstance = new TrainerInstance()
                {
                    Name = wildTrainerEntity.Name,
                    Team = Side.TOP,
                    MaxMonsInField = enemy.MaxSimultaneousMons,
                    // No limit for max mon used, will start with 3-10 and then who knows
                    ActiveJewelry = wildTrainerEntity.EquippedJewelry,
                    // Will add mons one by one later due to potential customizations
                };
                foreach (PokemonEntity enemyMon in wildTrainerEntity.Pokemon) // Instance one by one
                {
                    PokemonInstance nextMonInstance = SimulatorHandler.InstancePokemon(enemyMon, _context.Rng);
                    nextMonInstance.BaseStatMult = [.. enemy.StatMult.Select(i => i * GetDifficultyStatMult())]; // Its stats do need to be multiplied depending on danger level and the event's mult
                    wildTrainerInstance.PokemonInTeam.Add(nextMonInstance);
                }
                // Now that the trainer has been assembled, if catchable, need to put into the lists
                speciesUsed.UnionWith(wildTrainerEntity.Pokemon.Select(p => p.Species)); // Add to species used, useful for inter-enemy logic
                if (enemy.Catchable)
                {
                    potentialPrizes.AddRange(itemsUsed);
                    catchableEnemyMons.AddRange(wildTrainerInstance.PokemonInTeam); // Add also the mons if catchable
                }
                allEnemyInstances.Add(wildTrainerInstance); // Enemy is in now!
            }
            // Assembling of procedural strings
            List<string> prizes = encounter.PrizeType switch // Define the prize pool first, since the prizes will be important for assembling procedural strings
            {
                EncounterItemType.NONE => [], // No prize is no prize
                EncounterItemType.SPECIFIC_ITEM => [.. encounter.Params[paramsIndex++].Split(",")], // All the csv prizes in the next param
                EncounterItemType.COMMON_ITEM_POOL => [GeneralUtilities.GetRandomPick([.. _context.DungeonData.CommonDrops.Select(d => d.Name)], _context.Rng)], // One item of the common item pool
                EncounterItemType.RARE_ITEM_POOL => [GeneralUtilities.GetRandomPick([.. _context.DungeonData.RareDrops.Select(d => d.Name)], _context.Rng)], // One item of the rare item pool
                EncounterItemType.ALL_ITEM_POOLS => [GeneralUtilities.GetRandomPick([.. _context.DropTable.Select(d => d.Name)], _context.Rng)], // Any item from the pool
                EncounterItemType.EQUIPPED_ITEMS => [.. potentialPrizes], // All the items that were equipped by the enemies
                EncounterItemType.ENEMIES => [], // Nothing in prizes then (the 0-rank mon appears after)
                EncounterItemType.IMP => [$"{GeneralUtilities.GetRandomNumber(2, 4, _context.Rng)} IMP"], // Between 2-4 IMP
                EncounterItemType.DISKS => [GameplayElementsContainer.GlobalData.GetMoveDiskByMove(GeneralUtilities.GetRandomPick([.. GameplayElementsContainer.GlobalData.Moves.Values], _context.Rng)).Name,
                    GameplayElementsContainer.GlobalData.GetMoveDiskByType(GeneralUtilities.GetRandomPick([.. Enum.GetValues<PokemonType>()], _context.Rng)).Name,
                    "Blank Disk"], // Returns one move disk, type disk, and a set of blank disks (quantity to be determined by droprate)
                EncounterItemType.EVO_PLATE => [GeneralUtilities.GetRandomPick([.. GameplayElementsContainer.GlobalData.EvoPlates.Keys], _context.Rng)], // Random evo plate
                EncounterItemType.SANDWICH => [GameplayElementsContainer.GlobalData.GetSandwichByEffect(
                    GeneralUtilities.GetRandomPick([.. Enum.GetValues<SandwichEffectType>()], _context.Rng),
                    GeneralUtilities.GetRandomDecayingNumber(1,5,2,_context.Rng),
                    GeneralUtilities.GetRandomNumber(1,5,_context.Rng)
                    ).Name], // Generate a random sandwich on the spot with a random effect, random duration and decaying power
                _ => throw new Exception("Unknown scripted item type for prizes"),
            };
            List<string> proceduralStrings = []; // Will be assembled in some cases like when a random boss is found or sth
            if (catchableEnemyMons.Count > 0)
            {
                proceduralStrings.Add(string.Join(", ", [.. catchableEnemyMons.Select(p => p.Species.Name).Distinct().Order()])); // Add string containing ALL enemies species (only once)
            }
            if (potentialPrizes.Count > 0)
            {
                proceduralStrings.Add(string.Join(", ", [.. potentialPrizes.Distinct().Order()])); // And also all items
            }
            string encounterStartString = encounter.PreEncounterString;
            string encounterWinString = encounter.EncounterSuccessString;
            string encounterLoseString = encounter.EncounterFailureString;
            for (int i = 1; i <= proceduralStrings.Count; i++) // Now assemble all possible strings
            {
                encounterStartString = encounterStartString.Replace($"${i}", proceduralStrings[i]);
                encounterWinString = encounterWinString.Replace($"${i}", proceduralStrings[i]);
                encounterLoseString = encounterLoseString.Replace($"${i}", proceduralStrings[i]);
            }
            // Pre combat text
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine(encounterStartString)); // TODO: Also animation later on ig
            // Resolve the outcome, either via combat or some other event style
            bool playerWon = true; // Determines if the event was a success (true if no combat)
            if (allEnemyInstances.Count > 0)
            {
                GameOutcome outcome = ResolveEncounter(allEnemyInstances, GetGameParameters());
                playerWon = outcome.WinningTeam == Side.BOTTOM; // Trainer won if bottom won
            }
            if (playerWon) // If won, can then assign the prizes and the capture tables
            {
                bool rolledItems = false;
                if (encounter.PrizeType == EncounterItemType.ENEMIES) // Special situation, add to 0-rank mons
                {
                    foreach (PokemonInstance mon in allEnemyInstances.SelectMany(e => e.PokemonInTeam))
                    {
                        AddMonToCatchTable(mon, 1, 0); // Add mon to catchtable, but override the rank to 0
                    }
                }
                // Give the prizes then
                foreach (string item in prizes) // Assign item + count for this dung
                {
                    ItemDrop foundItem = _context.DropTable.FirstOrDefault(i => i.Name == item);
                    double count;
                    if (foundItem == null) // Item not in the drop table, no other choice but to give 1 I guess (may be a mon too)
                    {
                        count = 1;
                    }
                    else
                    {
                        // Then I can actually roll this
                        rolledItems = true;
                        count = GeneralUtilities.GetRandomNumber(foundItem.Min, foundItem.Max + 1, _context.Rng);
                        count *= _context.GetItemMult(); // Add the mult of course
                        if (count < 0) count = 0; // Can't remove items!
                    }
                    int totalCount = (int)Math.Round(count);
                    GameplayElementsContainer.GlobalData.GivePrizeToTrainer(item, _context.TrainerEntity, totalCount);
                    GeneralUtilities.AddtemToCountDictionary(_context.Prizes.ItemPrizes, item, totalCount); // Add also to log message
                }
                // If sandwich for item drop was used, then I roll it
                if (rolledItems)
                {
                    _context.UseSandwichEffect(SandwichEffectType.ITEM_DROP, s => { }); // Need to actually notify renderer about this later on
                }
                // Then, the mon tables. Check the CatchableSpecies, which should've been modified during battle
                foreach (PokemonInstance mon in catchableEnemyMons.Where(m => m.Availability == BattleAvailability.FAINT)) // Can only capture fainted mons (idk how non fainted here would happen)
                {
                    AddMonToCatchTable(mon, 1); // Add mon to catchtable
                }
            }
            // If a mon count or shiny sandwich was in effect, consume it
            _context.UseSandwichEffect(SandwichEffectType.ENEMY_NUMBER, s => { });
            _context.UseSandwichEffect(SandwichEffectType.SHINY_CHANCE, s => { });
            // Final things, check which string to output
            string encounterEndString = playerWon ? encounterWinString : encounterLoseString;
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine(encounterEndString)); // TODO: anim
        }
        /// <summary>
        /// Saves exploration outcome in a txt file for quick copying
        /// </summary>
        void SaveExplorationOutcome()
        {
            string explFile = $"{_context.TrainerEntity.Name.ToUpper()}_EXPLORATION.expl";
            _context.ExplorationMessage.EventText.Clear();
            Console.Write("Fate flavour text: ");
            string fateFlavourText = Console.ReadLine();
            if (fateFlavourText != "")
            {
                _context.ExplorationMessage.EventText.AppendLine($"Fate: ||{fateFlavourText}||");
                _context.ExplorationMessage.EventText.AppendLine();
            }
            _context.ExplorationMessage.EventText.AppendLine($"__{_context.TrainerEntity.Name} has obtained the following items:__");
            _context.ExplorationMessage.EventText.AppendLine();
            // List of items ordered
            List<string> prizes = [.. _context.Prizes.ItemPrizes.Select(p => $"{p.Key}{((p.Value > 1) ? $" x{p.Value}" : "")}").Order()];
            _context.ExplorationMessage.EventText.AppendLine($"**Items Found: **||{string.Join(", ", prizes)}||");
            // The mon printing is weird
            _context.ExplorationMessage.EventText.AppendLine($"__Catchable Pokemon (As many as you want as long as you have the corresponding Poke Ball for the Pokemon rank):__");
            _context.ExplorationMessage.EventText.AppendLine();
            List<int> ranks = [.. _context.Prizes.MonsFound.Keys.Order()];
            _context.ExplorationMessage.EventText.Append($"||"); // Beginning of rank Spoilered text
            foreach (int rank in ranks)
            {
                string monsOfRank = string.Join(", ", _context.Prizes.MonsFound[rank].Select(p => $"{p.Key}{((p.Value > 1) ? $" x{p.Value}" : "")}").Order());
                _context.ExplorationMessage.EventText.Append($"**RANK {rank}:** {monsOfRank}. ");
            }
            _context.ExplorationMessage.EventText.Append($"||"); // Close rank-spoilered text
            // Check if any warning for items
            string warningString = GameplayElementsContainer.GetTrainerInventoryWarning(_context.TrainerEntity);
            if (warningString != "")
            {
                _context.ExplorationMessage.PostEventText.AppendLine($"**Note: **||{warningString}||");
            }
            _context.ExplorationMessage.Signature = "";
            // Save
            string filePath = Path.Combine(DirectoryPath, explFile);
            _context.ExplorationMessage.SaveToFile(filePath);
        }
        /// <summary>
        /// Applies all pre-fight traine rmods (i.e. team level)
        /// </summary>
        void PreFightTrainerMods()
        {
            foreach (PokemonInstance mon in _context.TrainerInstance.PokemonInTeam)
            {
                for (int i = 0; i < mon.BaseStatMult.Length; i++)
                {
                    mon.BaseStatMult[i] = _context.GetExtraStatMult();
                }
            }
        }
        /// <summary>
        /// Apllies all trainer post-fight events, including healing and reset of lvl
        /// </summary>
        void PostFightTrainerMods()
        {
            foreach (PokemonInstance mon in _context.TrainerInstance.PokemonInTeam)
            {
                for (int i = 0; i < mon.BaseStatMult.Length; i++)
                {
                    mon.BaseStatMult[i] = 1;
                }
                double postBattleHealing = _context.GetExtraHealing();
                if (postBattleHealing > 0)
                {
                    Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine($"{mon.Name} healed {postBattleHealing * 100}%"));
                    // Later on, put in renderer queue/state machine
                    mon.HpPercent += postBattleHealing;
                    mon.HpPercent = Math.Clamp(mon.HpPercent, 0, 1);
                }
                List<string> expirationMessages = [];
                // TODO: Add renderer action
                _context.UseSandwichEffect(SandwichEffectType.POST_HEALING, s => { });
                _context.UseSandwichEffect(SandwichEffectType.STAT_MULT, s => { });
                foreach (string msg in expirationMessages)
                {
                    Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.WriteLine(msg));
                    // Later on, put in renderer queue/state machine
                }
            }
        }
        /// <summary>
        /// Returns, for a specific dungeon, the stat multiplier that needs to apply
        /// </summary>
        /// <returns>The resulting difficulty stat mmultiplier</returns>
        double GetDifficultyStatMult()
        {
            double mult = 1;
            if (_context.DungeonData.Difficulty > 1)
            {
                mult += _context.DungeonData.Difficulty * 0.05; // Multiplier will go from 1.10 to 1.3 depending on the diff
            }
            return mult;
        }
        /// <summary>
        /// Resolves an encounter between explorer and enemy instance, ready to go
        /// </summary>
        /// <param name="enemies">All enemy side instances with all the mons and shit ready</param>
        /// <param name="parameters">The game parameters for this game</param>
        /// <returns>The Game Outcome</returns>
        GameOutcome ResolveEncounter(List<TrainerInstance> enemies, GameParameters parameters)
        {
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.Write($"Pre wild vs enemies"));
            PreFightTrainerMods();
            List<TrainerInstance> allParticipants = [.. enemies];
            allParticipants.Add(_context.TrainerInstance);
            if (_context.FollowerInstance != null) // Also add follower if any
            {
                allParticipants.Add(_context.FollowerInstance);
            }
            GameOutcome outcome = SimulatorHandler.SimulateGame(allParticipants, parameters, _context.Rng); // Simulates the game
            Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.Write($"Post wild vs enemies"));
            PostFightTrainerMods();
            return outcome;
        }
        /// <summary>
        /// Gets the standard game params to use in an exploration battle
        /// </summary>
        /// <returns>The params to be used</returns>
        GameParameters GetGameParameters()
        {
            GameParameters parameters = new GameParameters()
            {
                ForceWin = false, // These can actually end in a tie (player continues advancing forward)
                Weathers = [GameplayElementsContainer.GlobalData.Weathers[_context.DungeonData.CurrentWeather.Name]], // Fetch the weather
                FocusedSide = Side.BOTTOM // Player is the star here
            };
            return parameters;
        }
        /// <summary>
        /// Adds a pokemon (+ count +overriden rank) to catch table
        /// </summary>
        /// <param name="mon">Which mon (instance) to add</param>
        /// <param name="count">How many (can also remove if needed)</param>
        /// <param name="rankOverride">Use species rank by default, define if there's override needed</param>
        void AddMonToCatchTable(PokemonInstance mon, int count, int rankOverride = -1)
        {
            int rank = (rankOverride < 0) ? mon.Species.Rank : rankOverride; // Check if special rank override
            string monName = mon.Species.Name + ((mon.IsShiny) ? " ★" : "");
            if (!_context.Prizes.MonsFound.TryGetValue(rank, out Dictionary<string, int> thisRankDict)) // Add for this rank if nothing yet
            {
                thisRankDict = [];
                _context.Prizes.MonsFound[rank] = thisRankDict;
            }
            GeneralUtilities.AddtemToCountDictionary(thisRankDict, monName, count);
            if (thisRankDict.Count == 0) // If somehow the rank was emptied, just remove it
            {
                _context.Prizes.MonsFound.Remove(rank);
            }
        }
    }
}
