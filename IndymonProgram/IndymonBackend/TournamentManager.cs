using Gameplay.GameEngine;
using Gameplay.GameplayElements;
using Gameplay.GameplayElementsContainer;
using GameSimulator;
using Utilities;

namespace IndymonBackendProgram
{
    public class TournamentManager
    {
        public Tournament OngoingTournament { get; set; }
        public string DirectoryPath { get; set; } = "";
        /// <summary>
        /// Generates a new tournament, dialog asking for tpy, n players, n mons, and which participants
        /// </summary>
        public void GenerateNewTournament()
        {
            Random rng = GameplayElementsContainer.GlobalData.CommonRng; // Need to use rng for the seed and the sort
            Console.WriteLine("Creation of a new tournament. Which type of tournament? [elim, king, group]");
            string inputString;
            bool validSelection = false;
            do
            {
                inputString = Console.ReadLine();
                switch (inputString.ToLower())
                {
                    case "elim":
                        validSelection = true;
                        OngoingTournament = new ElimTournament();
                        break;
                    case "king":
                        validSelection = true;
                        OngoingTournament = new KingOfTheHillTournament();
                        break;
                    case "group":
                        validSelection = true;
                        OngoingTournament = new GroupStageTournament();
                        break;
                    default:
                        validSelection = false;
                        break;
                }
            } while (!validSelection);
            OngoingTournament.RngSeed = rng.Next();
            Console.WriteLine("How many players will participate?");
            int nPlayers = int.Parse(Console.ReadLine());
            OngoingTournament.RequestAdditionalInfo(nPlayers); // Request tournament-specific info (if needed)
            // Finally, player selection, pre-filter trainers whether they can participate in this event (also all the npcs)
            List<TrainerEntity> trainers = [];
            List<NpcTrainer> validNpcs = [.. GameplayElementsContainer.GlobalData.AllNpcTrainers.Values.Where(n => ValidLineupGenerator.GetNpcSpeciesSets(n, GameplayElementsContainer.DEFAULT_BATTLE_NMONS, OngoingTournament.Constraints, false).Count > 0)];
            foreach (TrainerEntity trainer in GameplayElementsContainer.GlobalData.Trainers.Values)
            {
                List<List<PokemonSpecies>> possibleBuilds = ValidLineupGenerator.GetTrainersSpeciesSets(trainer, GameplayElementsContainer.DEFAULT_BATTLE_NMONS, OngoingTournament.Constraints, false); // Always 3 mons, these battles are 3v3 for the foreseeable future
                if (possibleBuilds.Count > 0)
                {
                    foreach (List<PokemonSpecies> build in possibleBuilds)
                    {
                        Console.WriteLine($"{trainer.Name} <@{trainer.DiscordId}>: {string.Join(", ", build.Select(p => p.Name))}");
                    }
                    trainers.Add(trainer);
                }
                else
                {
                    Console.WriteLine($"{trainer.Name} <@{trainer.DiscordId}>: Not enough Pokemon that satisfy requirements");
                }
            }
            // Time to add players
            int remainingPlayersNeeded = nPlayers;
            OngoingTournament.ParticipantSeedAndMonSeed.Clear();
            while (OngoingTournament.ParticipantSeedAndMonSeed.Count < nPlayers) // Will do addition loop until all players are selected
            {
                Console.WriteLine($"Choose next trainer(s) to add, or put a trainer rank to fill with a random Npc. No input will fill a random NPC of any rank.\n{string.Join(", ", Enum.GetValues<TrainerRank>())}\n{string.Join(", ", [.. trainers.Select(t => t.Name)])}");
                string trainerString = Console.ReadLine();
                foreach (string trainerChoice in trainerString.Split(","))
                {
                    string nextName = trainerChoice.Trim();
                    if (trainers.Any(t => t.Name == nextName)) // Is trainer a player?
                    {
                        TrainerEntity trainer = trainers.Where(t => t.Name == nextName).First();
                        trainers.Remove(trainer);
                        Console.WriteLine($"{trainerString} added");
                    }
                    else if (Enum.TryParse(nextName, out TrainerRank desiredRank)) // Then, see if filtered by rank
                    {
                        NpcTrainer npc = GeneralUtilities.GetRandomPick([.. validNpcs.Where(n => n.TrainerRank == desiredRank)], rng);
                        validNpcs.Remove(npc);
                        trainerString = npc.Name;
                        Console.WriteLine($"{trainerString} added");
                    }
                    else // Finally, get any random npc then
                    {
                        NpcTrainer npc = GeneralUtilities.GetRandomPick(validNpcs, rng);
                        validNpcs.Remove(npc);
                        trainerString = npc.Name;
                        Console.WriteLine($"{trainerString} added");
                    }
                    OngoingTournament.ParticipantSeedAndMonSeed.Add((trainerString, rng.Next(), rng.Next())); // Add the trainer and its future rng seed
                }
            }
        }
        /// <summary>
        /// Starts the tourn proper, will ask for input of scores
        /// </summary>
        public void ExecuteTournament()
        {
            OngoingTournament.UpdateTeams();
            OngoingTournament.SimulateTournament();
            OngoingTournament.FinaliseTournament(DirectoryPath);
        }
    }
    public class TournamentMatch()
    {
        public int Player1Index;
        public int Player2Index;
        public bool IsBye = false;
        public int Score1 = 0;
        public int Score2 = 0;
        public int Winner;
        public override string ToString()
        {
            return $"#{Player1Index} {Score1}-{Score2} #{Player2Index}";
        }
    }
    public abstract class Tournament
    {
        public int RngSeed { get; set; } = 0;
        protected Random _rng;
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public List<(string, int, int)> ParticipantSeedAndMonSeed { get; set; } = new List<(string, int, int)>(); // Participants, team seed, and pokemon seed base
        readonly List<TrainerEntity> _participants = new List<TrainerEntity>();
        readonly MessageLogger _tournamentMessage = new MessageLogger();
        /// <summary>
        /// Updates all the participants, generates NPC, randomizes teams if needed
        /// </summary>
        public void UpdateTeams()
        {
            _tournamentMessage.Clear(); // This is a new event, so I will clear whatever thet was there before
            // First, shuffle the participants (use seed if needed)
            List<(string, int, int)> seedSlots = new List<(string, int, int)>();
            Console.WriteLine("Want to add specific seeding? y/N");
            string seedInput = Console.ReadLine();
            if (seedInput.Trim().ToLower() == "y") // One last seeding step
            {
                List<(string, int, int)> seedOptions = [.. ParticipantSeedAndMonSeed];
                bool seedingFinished = false;
                while (!seedingFinished && seedSlots.Count < ParticipantSeedAndMonSeed.Count) // Continue seeding until finished or all players seeded
                {
                    Console.WriteLine("Choose next seed, or anything if finished seeding:");
                    for (int i = 0; i < seedOptions.Count; i++)
                    {
                        Console.Write($"{i + 1}: " + seedOptions[i].Item1 + ",");
                    }
                    if (int.TryParse(Console.ReadLine(), out int seedChoice)) // If user chose one...
                    {
                        seedSlots.Add(seedOptions[seedChoice - 1]);
                        seedOptions.RemoveAt(seedChoice - 1);
                    }
                    else // Finish here
                    {
                        seedingFinished = true;
                    }
                }
            }
            ShuffleWithSeeds(seedSlots); // Tournament-specific seed shuffling so that highest seed is at an advantage
            // Next step is to obtain the actual trainer data, generate NPCs and shuffle team if this is needed
            foreach ((string, int, int) participantData in ParticipantSeedAndMonSeed) // First, choose all trainers, obtain their corresponding entity
            {
                string participantName = participantData.Item1;
                int participantSeed = participantData.Item2;
                Random trainerRng = new Random(participantSeed);
                if (GameplayElementsContainer.GlobalData.Trainers.TryGetValue(participantName, out TrainerEntity nextTrainerEntity))
                {
                    // Trainer is a player, need to add, and randomize team shuffle if auto-team (lazy way)
                    if (nextTrainerEntity.AutoTeam)
                    {
                        GeneralUtilities.ShuffleList(nextTrainerEntity.Pokemon, trainerRng);
                    }
                    _participants.Add(nextTrainerEntity); // Regardless, add trainer
                }
                else
                {
                    // Trainer is definitely an NPC then, we'll create a placehodler entity (use all mons here because they're filtered by the constraints after
                    nextTrainerEntity = GameplayElementsContainer.GlobalData.CreateNpcEntity(participantName, int.MaxValue, participantSeed, GameplayElementsContainer.NPC_ITEM_CHANCE, new List<string>()); // The list itself doesnt matter in this case
                    _participants.Add(nextTrainerEntity); // Regardless, add trainer
                }
                // Finally, need to reshuffle trainer's internal team so that the valid N mons are in front
                List<List<PokemonSpecies>> possibleBuilds = ValidLineupGenerator.GetTrainersSpeciesSets(nextTrainerEntity, GameplayElementsContainer.DEFAULT_BATTLE_NMONS, Constraints, false); // Always 3 mons, these battles are 3v3 for the foreseeable future
                List<PokemonSpecies> chosenBuild;
                if (possibleBuilds.Count == 1) // If only one build, no need to go crazy
                {
                    chosenBuild = possibleBuilds[0];
                }
                else if (nextTrainerEntity.AutoTeam)
                {
                    // In this case, the build is chosen at random
                    chosenBuild = GeneralUtilities.GetRandomPick(possibleBuilds, trainerRng);
                }
                else
                {
                    // This is a weird one, because it means we need to manually choose according to what the trainer has chosen
                    // (Fortunately this is only in monotype)
                    Console.WriteLine($"Trainer {nextTrainerEntity.Name} has many valid mon lineups available for this tournament (current mon order is {string.Join(", ", nextTrainerEntity.Pokemon.Select(p => p.Name))})");
                    for (int i = 0; i < possibleBuilds.Count; i++)
                    {
                        Console.WriteLine($"{i}: {string.Join(", ", possibleBuilds[i].Select(p => p.Name))}");
                    }
                    Console.WriteLine("Choose?");
                    chosenBuild = possibleBuilds[int.Parse(Console.ReadLine())];
                }
                // And finally finally, need to place the mons in order from top to bottom until they satisfy the order that first, the mons present in the build, and then the others
                // Bubble up algorithm so that all valids are bubbled up while preserving order
                int arrayBase = 0;// Where to put the valid Pokemon into, this masks all indices < arrayBase from being shuffled again as they'd be valid ones
                for (int i = 0; i < nextTrainerEntity.Pokemon.Count && arrayBase < GameplayElementsContainer.DEFAULT_BATTLE_NMONS; i++)
                {
                    if (chosenBuild.Contains(nextTrainerEntity.Pokemon[i].Species)) // This pokemon is an allowed species for the build
                    {
                        // Swap with next invalid mon
                        (nextTrainerEntity.Pokemon[i], nextTrainerEntity.Pokemon[arrayBase]) = (nextTrainerEntity.Pokemon[arrayBase], nextTrainerEntity.Pokemon[i]);
                        arrayBase++;
                    }
                }
            }
            // This trainer list should be aligned with the seed one and never shuffled again
        }
        /// <summary>
        /// Duting tournament init, asks for extra info if needed
        /// </summary>
        public abstract void RequestAdditionalInfo(int nPlayers);
        /// <summary>
        /// From a tournament, it shuffles players, but also has a list of top seeds (from best to worst) if needed in some tournament
        /// </summary>
        /// <param name="seeds">Seed list to be used in tournament</param>
        public abstract void ShuffleWithSeeds(List<(string, int, int)> seeds);
        /// <summary>
        /// Will play the tournament, uses new game sim and may export video outright
        /// </summary>
        public abstract void SimulateTournament();
        /// <summary>
        /// Will return the position of participant, i.e. if first, second, third, etc
        /// </summary>
        /// <param name="participantId">Which pariticpant id to check</param>
        /// <returns>Participant place</returns>
        public abstract int GetParticipantPlace(int participantId);
        /// <summary>
        /// Deals with tournament post, manages prizes, message, trainer items, etc
        /// </summary>
        public void FinaliseTournament(string directoryBase)
        {
            // Ask for prizes
            static (string, int) GetSplitStrCount()
            {
                string[] split = Console.ReadLine().Split("x");
                if (split.Length == 1 || !int.TryParse(split[1], out int count)) count = 1;
                return (split[0].Trim(), count);
            }
            Console.WriteLine("What's the first prize here?");
            (string, int) firstPrize = GetSplitStrCount();
            string winner = "";
            Console.WriteLine("What's the second prize here?");
            (string, int) secondPrize = GetSplitStrCount();
            string runner = "";
            Console.WriteLine("What's the consolation prize here?");
            (string, int) consolationPrize = GetSplitStrCount();
            // Need to update the items and sheets for participants
            // First, salary
            for (int i = 0; i < _participants.Count; i++)
            {
                TrainerEntity participant = _participants[i];
                // Prize calc
                int place = GetParticipantPlace(i);
                (string, int) prize;
                if (place == 1)
                {
                    prize = firstPrize;
                    winner = participant.Name;
                }
                else if (place == 2)
                {
                    prize = secondPrize;
                    runner = participant.Name;
                }
                else
                {
                    prize = consolationPrize;
                }
                if (!GameplayElementsContainer.GlobalData.Trainers.ContainsKey(participant.Name))
                {
                    continue; // No need to do the other bs if the trainer is an NPC...
                }
                // Ok apply everything item wise
                GameplayElementsContainer.ConsumeTrainersItems(participant, GameplayElementsContainer.DEFAULT_BATTLE_NMONS); // Consume all items for all mons first (may free some space in the bag after all)
                bool prizeSuccesful = GameplayElementsContainer.GlobalData.GivePrizeToTrainer(prize.Item1, participant, prize.Item2);
                GameplayElementsContainer.GlobalData.GivePrizeToTrainer($"1 IMP", participant, GameplayElementsContainer.TRAINER_SALARY);
                string warningString = GameplayElementsContainer.GetTrainerInventoryWarning(participant);
                if (warningString != "")
                {
                    if (!prizeSuccesful)
                    {
                        warningString += $" In this instance, your {prize.Item1} reward has been discarded.";
                    }
                    _tournamentMessage.PostEventText.AppendLine($"- <@{participant.DiscordId}>: ||{warningString}||");
                }
                participant.SaveTrainerCsv(directoryBase);
            }
            // Finally, the tournament string needs to be assembled with the remaining stuff
            _tournamentMessage.EventText.Clear();
            _tournamentMessage.EventText.AppendLine("<Text indicating who organised the tournament and which week>");
            _tournamentMessage.EventText.AppendLine($"A total of {ParticipantSeedAndMonSeed.Count} trainers participated on this event.");
            _tournamentMessage.EventText.AppendLine();
            string prizeString = firstPrize.Item1 + ((firstPrize.Item2 > 1) ? $" x{firstPrize.Item2}" : "");
            _tournamentMessage.EventText.AppendLine($"- Congrats to the winner, ||{winner}||, who has won {prizeString}!");
            prizeString = secondPrize.Item1 + ((secondPrize.Item2 > 1) ? $" x{secondPrize.Item2}" : "");
            _tournamentMessage.EventText.AppendLine($"- Congratulations also to the runner-up, ||{runner}||, who has won {prizeString}");
            prizeString = consolationPrize.Item1 + ((consolationPrize.Item2 > 1) ? $" x{consolationPrize.Item2}" : "");
            _tournamentMessage.EventText.AppendLine($"- All other players have received {prizeString} as a thanks for participating");
            string filePath = Path.Combine(directoryBase, "TOURNAMENT.txt");
            _tournamentMessage.SaveToFile(filePath);
        }
        /// <summary>
        /// Resolves a match using game engine
        /// </summary>
        /// <param name="match">Match to evaluate</param>
        public void ResolveMatch(TournamentMatch match)
        {
            if (match.IsBye)
            {
                match.Winner = match.Player1Index;
                TrainerEntity player1 = _participants[match.Player1Index];
                Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.Write($"{player1.Name} gets a bye"));
            }
            else
            {
                // Create trainer instances, put in there all the data needed to initialize the battle, wait for the output class, which is used to populate fields
                // Usage of new system to create the battle (here there'll be crazier team stuff if it's a more special tourn)
                // Create trainer instances, opposed teams, 3 mons max, apply pokemon seed (may go into method if starts to happen a lot)
                TrainerEntity player1 = _participants[match.Player1Index];
                int monSeed = ParticipantSeedAndMonSeed[match.Player1Index].Item3;
                Random pokemonRng = new Random(monSeed);
                TrainerInstance trainer1 = new TrainerInstance()
                {
                    Name = player1.Name,
                    Team = (Side)1,
                    MaxMonsInField = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                    MaxMonsUsed = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                    ActiveJewelry = player1.EquippedJewelry,
                    PokemonInTeam = [.. player1.Pokemon.Select(p => SimulatorHandler.InstancePokemon(p, pokemonRng))]
                };
                TrainerEntity player2 = _participants[match.Player2Index];
                monSeed = ParticipantSeedAndMonSeed[match.Player2Index].Item3;
                pokemonRng = new Random(monSeed);
                TrainerInstance trainer2 = new TrainerInstance()
                {
                    Name = player2.Name,
                    Team = (Side)2,
                    MaxMonsInField = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                    MaxMonsUsed = GameplayElementsContainer.DEFAULT_BATTLE_NMONS,
                    ActiveJewelry = player2.EquippedJewelry,
                    PokemonInTeam = [.. player2.Pokemon.Select(p => SimulatorHandler.InstancePokemon(p, pokemonRng))]
                };
                // Execute battle now
                Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.Write($"Pre {player1.Name} vs {player2.Name}"));
                GameOutcome outcome = SimulatorHandler.SimulateGame([trainer1, trainer2], _rng);
                Debug.DebugAction(DebugLevel.EVENT_ORGANIZER, () => Console.Write($"Post {player1.Name} vs {player2.Name}")); // TODO: Add a bit more info about scores and stuff once actually applied
                // Determine winner
                if (outcome.WinningTeam == (Side)1)
                {
                    match.Winner = match.Player1Index;
                }
                else
                {
                    match.Winner = match.Player2Index;
                }
            }
        }
        /// <summary>
        /// Checks if certain tournaments are monotype or not
        /// </summary>
        protected void AskSpecialRulesets()
        {
            // Ones that add to the build constraints
            Console.WriteLine("Choose special rules [monotype, lc]");
            string response = Console.ReadLine();
            if (response.Trim().ToLower() == "lc")
            {
                // Ensure mon has evo and hasnt evolved
                Constraint onlyConstraint = new Constraint()
                {
                    AllConstraints = [(ConstraintType.POKEMON_HAS_PREVO, "FALSE"), (ConstraintType.POKEMON_HAS_EVO, "TRUE")],
                    Operation = ConstraintOperation.AND
                };
                Constraints = [onlyConstraint];
            }
            else if (response.Trim().ToLower() == "monotype")
            {
                foreach (PokemonType type in Enum.GetValues<PokemonType>())
                {
                    if (type == PokemonType.NONE) continue; // Skip these
                    Constraint nextTypeConstraint = new Constraint
                    {
                        AllConstraints = [(ConstraintType.POKEMON_TYPE, type.ToString())],
                        Operation = ConstraintOperation.OR
                    };
                    Constraints.Add(nextTypeConstraint);
                }
            }
            else // Normal tournament, empty constraint
            {
                Constraint onlyConstraint = new Constraint()
                {
                    AllConstraints = [],
                };
                Constraints = [onlyConstraint];
            }
        }
    }
    /// <summary>
    /// This class exists because 2 different types of tournament have brackets
    /// </summary>
    public static class PlayoffBracketHelper
    {
        /// <summary>
        /// Gets an internal list of seed order to check which player fights which
        /// </summary>
        /// <param name="nPlayers">How many players total, create the smallest possible seed order list</param>
        /// <returns></returns>
        public static int[] GetSeedOrderForTournament(int nPlayers)
        {
            // First, assemble a list of seeds, for all players get seed order from that to the closest power of 2
            int closestPowerOf2 = 1;
            while (closestPowerOf2 < nPlayers) closestPowerOf2 *= 2;
            int[] seedOrder = new int[closestPowerOf2]; // Create space for all seeds
            int stage = 0;
            while (stage < closestPowerOf2) // Continue until i reach closest power of 2
            {
                if (stage == 0) // First stage there's no logic, just add 1
                {
                    seedOrder[0] = 0;
                    stage = 1; // Start with power of 2
                }
                else // Otherwise need to expand seed list, stage also contains how many players need to be there
                {
                    // First stage is to move all entries and leave one space between
                    for (int i = stage - 1; i >= 0; i--)
                    {
                        seedOrder[2 * i] = seedOrder[i]; // Move to next even
                    }
                    // Then, go on twos, fill the next one with the complement
                    int complement = (2 * stage) - 1;
                    for (int i = 0; i < stage; i++)
                    {
                        int mu = complement - seedOrder[2 * i]; // Calculate MU
                        seedOrder[(2 * i) + 1] = mu; // Put it in the array
                    }
                    // Next stage
                    stage *= 2;
                }
            }
            return seedOrder;
        }
        /// <summary>
        /// Resolves the brackets, to be called as part of SimulateTournament
        /// </summary>
        /// <param name="firstRoundMatches">The matches for the first round</param>
        /// <param name="ongoingTournament">The ongoing tournament (to see the data)</param>
        /// <returns>The list of matches for all rounds and maybe also animation rendering?</returns>>
        public static List<List<TournamentMatch>> ResolveMatches(List<TournamentMatch> firstRoundMatches, Tournament ongoingTournament)
        {
            List<List<TournamentMatch>> roundHistory = new List<List<TournamentMatch>>();
            // Ok will start doing the matchups, first round
            List<TournamentMatch> thisRound = firstRoundMatches;
            roundHistory.Add(thisRound);
            bool finished = false;
            // Begin doing all rounds
            Console.Clear();
            while (!finished)
            {
                List<TournamentMatch> currentRound = roundHistory.Last();
                List<TournamentMatch> nextRound = new List<TournamentMatch>();
                int playerProcessed = 0;
                for (int i = 0; i < currentRound.Count; i++)
                {
                    TournamentMatch match = currentRound[i];
                    ongoingTournament.ResolveMatch(match); // Do the match. It'll contain winner data
                    if ((playerProcessed % 2) == 0) // Even players, first of the next match
                    {
                        TournamentMatch nextMatch = new TournamentMatch
                        {
                            Player1Index = match.Winner,
                        };
                        nextRound.Add(nextMatch);
                    }
                    else // It's the player 2
                    {
                        TournamentMatch nextMatch = nextRound.Last();
                        nextMatch.Player2Index = match.Winner;
                    }
                    playerProcessed++;
                }
                // Finally, now here's the next round, unless tournament was finished
                if (currentRound.Count == 1) // If all's finished (either manually or because this was the last round)
                {
                    finished = true;
                    Console.WriteLine($"Tournament data entry done");
                }
                else
                {
                    roundHistory.Add(nextRound); // Adds the next round to the pile
                }
            }
            return roundHistory;
        }
    }
    public class ElimTournament : Tournament
    {
        public List<List<TournamentMatch>> RoundHistory; // To be used at the end
        // Seed helper
        public override void RequestAdditionalInfo(int nPlayers)
        {
            AskSpecialRulesets();
        }
        public override void ShuffleWithSeeds(List<(string, int, int)> Seeds)
        {
            // Shuffle everything, afterwards if there's some top seeds they'll be put on top after
            _rng = new Random(RngSeed);
            GeneralUtilities.ShuffleList(ParticipantSeedAndMonSeed, _rng);
            // Seeding at this stage will involve putting the best first, bracket helper will put them in place after
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = ParticipantSeedAndMonSeed.IndexOf(Seeds[seed]); // Find where seed is currently
                // Perform switch
                if (currentIndex != seed)
                {
                    (ParticipantSeedAndMonSeed[currentIndex], ParticipantSeedAndMonSeed[seed]) = (ParticipantSeedAndMonSeed[seed], ParticipantSeedAndMonSeed[currentIndex]); // Swap
                }
            }
        }
        public override void SimulateTournament()
        {
            int nPlayers = ParticipantSeedAndMonSeed.Count;
            int[] seedOrder = PlayoffBracketHelper.GetSeedOrderForTournament(nPlayers); // Gets the seed order to use for this bracket
            // Assemble first round
            List<TournamentMatch> firstRound = new List<TournamentMatch>();
            // Need to find seed by seed, and the ones that are higher than the number of player involve a bye
            // Due to how algorithm was created, p1 will always be valid
            for (int i = 0; i < seedOrder.Length; i += 2) // Go in pairs
            {
                TournamentMatch thisMatch = new TournamentMatch();
                // Find both players
                int p1Index = seedOrder[i];
                int p2Index = seedOrder[i + 1];
                thisMatch.Player1Index = p1Index;
                if (p2Index < nPlayers) // Meaning the next seed MU is valid
                {
                    thisMatch.Player2Index = p2Index; // Got the second player
                    thisMatch.IsBye = false;
                }
                else // Otherwise it's a bye
                {
                    thisMatch.IsBye = true;
                }
                firstRound.Add(thisMatch);
            }
            Console.Clear();
            RoundHistory = PlayoffBracketHelper.ResolveMatches(firstRound, this); // Ask the helper to work with this tournament
        }
        public override int GetParticipantPlace(int participantId)
        {
            TournamentMatch final = RoundHistory.Last().Last();
            int result;
            if (final.Winner == participantId) result = 1; // If won final, 1st
            else if (final.Player1Index == participantId || final.Player2Index == participantId) result = 2; // Otherwise but fought final?
            else result = 3; // No additional calc of >2 place until this is needed
            return result;
        }
    }
    public class KingOfTheHillTournament : Tournament
    {
        public List<TournamentMatch> MatchHistory;
        public override void RequestAdditionalInfo(int nPlayers)
        {
            AskSpecialRulesets();
        }
        public override void ShuffleWithSeeds(List<(string, int, int)> Seeds)
        {
            // Shuffle everything, afterwards if there's some top seeds they'll be put on top after
            _rng = new Random(RngSeed);
            GeneralUtilities.ShuffleList(ParticipantSeedAndMonSeed, _rng);
            // Seeding in KOH means putting the best in the bottom
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = ParticipantSeedAndMonSeed.IndexOf(Seeds[seed]); // Find where seed is currently
                int targetIndex = ParticipantSeedAndMonSeed.Count - 1 - seed; // Bottom up
                // Perform switch
                if (currentIndex != targetIndex)
                {
                    (ParticipantSeedAndMonSeed[currentIndex], ParticipantSeedAndMonSeed[targetIndex]) = (ParticipantSeedAndMonSeed[targetIndex], ParticipantSeedAndMonSeed[currentIndex]); // Swap
                }
            }
        }
        public override void SimulateTournament()
        {
            MatchHistory = new List<TournamentMatch>();
            // First vs second player
            TournamentMatch firstMatch = new TournamentMatch
            {
                Player1Index = 0
            };
            if (ParticipantSeedAndMonSeed.Count > 1) // One would assume...
            {
                firstMatch.Player2Index = 1;
            }
            else
            {
                firstMatch.IsBye = true;
            }
            MatchHistory.Add(firstMatch);
            // Begins here
            Console.Clear();
            bool finished = false;
            while (!finished) // In this case, it'll be battle by battle, each decided by the winner of previous
            {
                int matchNumber = MatchHistory.Count;
                TournamentMatch match = MatchHistory.Last(); // Gets last (current) match
                ResolveMatch(match); // Do the match. It'll contain winner data
                // Check end condition
                if (matchNumber >= (ParticipantSeedAndMonSeed.Count - 1)) // This signifies end of tournament
                {
                    finished = true;
                    Console.WriteLine($"Tournament data entry done");
                }
                else // Next match then
                {
                    TournamentMatch nextMatch = new TournamentMatch();
                    MatchHistory.Add(nextMatch); // Add to pile
                    // Winner will advance in the same pos as they were
                    bool winnerWas1 = (match.Winner == match.Player1Index);
                    nextMatch.Player1Index = winnerWas1 ? match.Winner : MatchHistory.Count;
                    nextMatch.Player2Index = winnerWas1 ? MatchHistory.Count : match.Winner;
                }
            }
        }
        public override int GetParticipantPlace(int participantId)
        {
            int i;
            for (i = 0; i <= MatchHistory.Count; i++) // Go matches with 1-index so that i is the prospective place
            {
                if (MatchHistory[MatchHistory.Count - i - 1].Winner == participantId)
                {
                    break;
                }
            }
            return i + 1;
        }
    }
    public class GroupStageTournament : Tournament
    {
        // For the group stage
        public int NGroups { get; set; }
        public int PlayersPerGroup { get; set; }
        public int NWeeks { get; set; }
        public int MatchesPerWeek { get; set; }
        public int NMathesTotalPerGroup { get; set; }
        public int PlayoffPerGroup { get; set; }
        public int[,] Groups { get; set; } = null;
        public List<List<List<TournamentMatch>>> GroupMatches { get; set; } // Will go by weeks (list), each week will contain all groups with all matches due that week for that group
        // For the playoff stage
        public List<List<TournamentMatch>> PlayoffMatches { get; set; }
        public override void RequestAdditionalInfo(int nPlayers)
        {
            AskSpecialRulesets();
            // Then group specific
            Console.WriteLine("How many players each group?");
            PlayersPerGroup = int.Parse(Console.ReadLine());
            if ((nPlayers % PlayersPerGroup) != 0)
            {
                throw new Exception("Can't evenly distribute in groups");
            }
            else
            {
                NGroups = nPlayers / PlayersPerGroup;
            }
            Groups = new int[NGroups, PlayersPerGroup]; // Group contains all participants
            // Calculate the rest of things
            MatchesPerWeek = PlayersPerGroup / 2; // Most that can be played is /2 as all players are playing at the same time
            NMathesTotalPerGroup = ((PlayersPerGroup * PlayersPerGroup) - PlayersPerGroup) / 2; // How many matches total will be played in all groups
            NWeeks = NMathesTotalPerGroup / MatchesPerWeek; // Therefore this is the number of weeks needed
            Console.WriteLine("How many players make playoffs each group?");
            PlayoffPerGroup = int.Parse(Console.ReadLine());
        }
        public override void ShuffleWithSeeds(List<(string, int, int)> Seeds)
        {
            // Shuffle everything, afterwards if there's some top seeds they'll be put on top after
            _rng = new Random(RngSeed);
            // Seeding will involve putting the best first
            for (int seed = 0; seed < Seeds.Count; seed++) // Seed by seed
            {
                int currentIndex = ParticipantSeedAndMonSeed.IndexOf(Seeds[seed]); // Find where seed is currently
                // Perform switch
                if (currentIndex != seed)
                {
                    (ParticipantSeedAndMonSeed[currentIndex], ParticipantSeedAndMonSeed[seed]) = (ParticipantSeedAndMonSeed[seed], ParticipantSeedAndMonSeed[currentIndex]); // Swap
                }
            }
            // Another shuffle to shuffle "pots" of players, players will be segregated by skill tiers and distributed
            for (int tier = 0; tier < PlayersPerGroup; tier++)
            {
                int pot = tier * NGroups; // Next pot is the next n players
                GeneralUtilities.ShuffleList(ParticipantSeedAndMonSeed, pot, NGroups, _rng);
            }
            // Finally, place in each position of group
            int participant = 0;
            for (int level = 0; level < PlayersPerGroup; level++)
            {
                for (int group = 0; group < NGroups; group++)
                {
                    Groups[group, level] = participant;
                    participant++;
                }
            }
        }
        public override void SimulateTournament()
        {
            // Start with group stage
            // Admin, players score involves n million per n matches won, and added to it is the differential
            Dictionary<int, int> playerScores = new Dictionary<int, int>(); // Will save player scores, so that they can be sorted at the end
            for (int i = 0; i < ParticipantSeedAndMonSeed.Count; i++) // All players init with a score of 0
            {
                playerScores[i] = 0;
            }
            // Actual game
            GroupMatches = new List<List<List<TournamentMatch>>>();
            List<List<TournamentMatch>> matchesInGroupsThisWeek = new List<List<TournamentMatch>>();
            for (int group = 0; group < NGroups; group++)
            {
                matchesInGroupsThisWeek.Add(NextRoundRobinMatch(group, 0)); // Add next matches in group here
            }
            GroupMatches.Add(matchesInGroupsThisWeek); // Added week 0
            // This is the part that loads a tournament, visually it prints all matches and prompts user one by one
            Console.Clear();
            bool finished = false;
            while (!finished)
            {
                int week = GroupMatches.Count;
                List<List<TournamentMatch>> currentWeek = GroupMatches.Last(); // Got the current week
                foreach (List<TournamentMatch> matchesThisGroup in currentWeek)
                {
                    foreach (TournamentMatch match in matchesThisGroup)
                    {
                        ResolveMatch(match); // Do the match. It'll contain winner data
                        if (!match.IsBye)
                        {
                            playerScores[match.Player1Index] += match.Score1 - match.Score2;
                            playerScores[match.Player2Index] += match.Score2 - match.Score1;
                            playerScores[match.Winner] += 1000000; // Each win is worth a million so it sorts by win first
                        }
                    }
                }
                // Finally, now here's the next round, unless tournament was finished
                if (week >= NWeeks) // If this was the final week...
                {
                    finished = true;
                    Console.WriteLine($"Group stage done");
                }
                else // Need to add the next week
                {
                    List<List<TournamentMatch>> nextWeek = new List<List<TournamentMatch>>();
                    for (int group = 0; group < NGroups; group++)
                    {
                        nextWeek.Add(NextRoundRobinMatch(group, week)); // Add next matches in group here
                    }
                    GroupMatches.Add(nextWeek); // Added next week
                }
            }
            // Ok, group stage is over, now need to get the first n players of each group!
            List<int> playoffsPlayers = new List<int>();
            for (int i = 0; i < NGroups; i++)
            {
                List<(int, int)> playerAndRank = new List<(int, int)>();
                for (int j = 0; j < PlayersPerGroup; j++)
                {
                    (int, int) playerScore = (Groups[i, j], playerScores[Groups[i, j]]); // Get player and its score
                    playerAndRank.Add(playerScore);
                }
                // Once all players, just sort and pick first n
                playerAndRank = [.. playerAndRank.OrderByDescending(p => p.Item2).ThenByDescending(p => p.Item1)];
                for (int j = 0; j < PlayoffPerGroup; j++)
                {
                    playoffsPlayers.Add(playerAndRank[j].Item1); // Add the players in this order
                }
            }
            // Now there's N players who made playoffs, and they're already ordered/seeded
            List<TournamentMatch> playoffsFirstMatch = new List<TournamentMatch>();
            int[] seedOrder = PlayoffBracketHelper.GetSeedOrderForTournament(playoffsPlayers.Count); // Gets the seed order to use for this bracket
            for (int i = 0; i < seedOrder.Length; i += 2) // Go in pairs
            {
                TournamentMatch thisMatch = new TournamentMatch();
                // Find both players
                int p1Index = seedOrder[i];
                int p2Index = seedOrder[i + 1];
                thisMatch.Player1Index = p1Index;
                if (p2Index < playoffsPlayers.Count) // Meaning the next seed MU is valid
                {
                    thisMatch.Player1Index = p2Index; // Got the second player
                    thisMatch.IsBye = false;
                }
                else // Otherwise it's a bye
                {
                    thisMatch.IsBye = true;
                }
                playoffsFirstMatch.Add(thisMatch);
            }
            // Finally, sim playoff stage now (generic bracker maker)
            PlayoffMatches = PlayoffBracketHelper.ResolveMatches(playoffsFirstMatch, this);
        }
        /// <summary>
        /// Will return the next set of matches for a specific week in a specific group
        /// </summary>
        /// <param name="group">Which group</param>
        /// <param name="week">Which week</param>
        /// <returns>List of matches</returns>
        List<TournamentMatch> NextRoundRobinMatch(int group, int week)
        {
            List<TournamentMatch> schedule = new List<TournamentMatch>();
            int rrPlayersPerGroup = ((PlayersPerGroup % 2) == 0) ? PlayersPerGroup : PlayersPerGroup + 1; // Round up to closest even
            int nPairs = rrPlayersPerGroup / 2; // Number of player pairs
            for (int pair = 0; pair < nPairs; pair++)
            {
                // Related indices of players (initial value before rotating), they're mirrored
                int p1Index = pair;
                int p2Index = rrPlayersPerGroup - 1 - pair;
                // Rotate around round robin, except player 0 which will always be 0 (anchor)
                p1Index = (p1Index == 0) ? 0 : ((p1Index - 1 + week) % (rrPlayersPerGroup - 1)) + 1;
                p2Index = ((p2Index - 1 + week) % (rrPlayersPerGroup - 1)) + 1;
                // Finally, create the match if both ends of pair are valid players (i.e. indices valid)
                if (p1Index < PlayersPerGroup && p2Index < PlayersPerGroup)
                {
                    TournamentMatch match = new TournamentMatch()
                    {
                        IsBye = false, // Never a bye, always pairs
                        Player1Index = Groups[group, p1Index], // And get players
                        Player2Index = Groups[group, p2Index]
                    };
                    schedule.Add(match);
                }
            }
            return schedule;
        }
        public override int GetParticipantPlace(int participantId)
        {
            TournamentMatch final = PlayoffMatches.Last().Last();
            int result;
            if (final.Winner == participantId) result = 1; // If won final, 1st
            else if (final.Player1Index == participantId || final.Player2Index == participantId) result = 2; // Otherwise but fought final?
            else result = 3; // No additional calc of >2 place until this is needed
            return result;
        }
    }
}
