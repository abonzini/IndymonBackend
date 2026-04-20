using GameData;
using GameDataContainer;
using MechanicsData;
using Utilities;

namespace AutomatedTeamBuilder
{
    /// <summary>
    /// When attempting to check a valid team build, this structure contains all possible options that fulfill the desired constraints
    /// </summary>
    public class PossibleTeamBuild
    {
        /// <summary>
        /// The Trainer's mons that can be used for this
        /// </summary>
        public List<TrainerPokemon> TrainerOwnPokemon = new List<TrainerPokemon>();
        /// <summary>
        /// The set items that can be used to satisfy strict move/ability requirements and which mons to apply to
        /// </summary>
        public Dictionary<SetItem, List<TrainerPokemon>> TrainerOwnPokemonUsingSetItem = new Dictionary<SetItem, List<TrainerPokemon>>();
        /// <summary>
        /// Pokemon that a trainer could lend you as a favor that satisfy req
        /// </summary>
        public Dictionary<Trainer, List<TrainerPokemon>> FavourPokemon = new Dictionary<Trainer, List<TrainerPokemon>>();
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// Gets a lis tof all the possible good teams given a list of one or many team build constraints.
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="nMons">Number of mons desired in the team</param>
        /// <param name="constraintSets">All the different valid constraints that apply separately, only one needs to succeed</param>
        /// <param name="acceptLessMons">Whether to accept less pokemon if not enough</param>
        /// <returns></returns>
        public static List<PossibleTeamBuild> GetTrainersPossibleBuilds(Trainer trainer, int nMons, List<Constraint> constraintSets, bool acceptLessMons)
        {
            List<PossibleTeamBuild> resultingBuilds = new List<PossibleTeamBuild>();
            foreach (Constraint constraint in constraintSets)
            {
                // Get the possible lineup for this constraint
                PossibleTeamBuild thisTeamBuild = GetTrainersBuildOptions(trainer, constraint);
                int usableMons = thisTeamBuild.TrainerOwnPokemon.Count; // Mons that can be used
                // The next foreach is not correct because it would potentially add mons multiple times but this may be a non issue unless thigns get really heated up
                foreach (KeyValuePair<SetItem, List<TrainerPokemon>> setItemOptions in thisTeamBuild.TrainerOwnPokemonUsingSetItem) // Then check how many equippable items options
                {
                    int setItemCount = trainer.SetItems[setItemOptions.Key]; // How many of these items I have?
                    int potentialMonCount = setItemOptions.Value.Count;
                    usableMons += Math.Min(setItemCount, potentialMonCount); // Can use these to add more mons, but only if i have enough items/mons
                }
                foreach (KeyValuePair<Trainer, List<TrainerPokemon>> favorOption in thisTeamBuild.FavourPokemon) // Then, see how much I can borrow from each trainer
                {
                    int numberOfFavors = trainer.Favours[favorOption.Key];
                    usableMons += Math.Min(favorOption.Value.Count, numberOfFavors); // Can borrow only the valid mon but also limited by number of fav available
                }
                if (usableMons >= nMons || acceptLessMons) // Need to check now if I have enough options to build a team with these constraints
                {
                    resultingBuilds.Add(thisTeamBuild);
                }
            }
            return resultingBuilds;
        }
        /// <summary>
        /// For a given trainer, gives me all the possible teams they could build with the corresponding constraint sets
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="constraint">The teambuild constraints</param>
        /// <returns>The teambuild that could satisfy all these</returns>
        public static PossibleTeamBuild GetTrainersBuildOptions(Trainer trainer, Constraint constraint)
        {
            PossibleTeamBuild resultingBuild = new PossibleTeamBuild();
            // First, check all own mons that satisfy by themselves
            List<TrainerPokemon> invalidPokemon = new List<TrainerPokemon>(); // Will contain pokemon that can't fill the case naturally
            foreach (TrainerPokemon mon in trainer.PartyPokemon)
            {
                if (!constraint.SatisfiedByMon(mon, true)) // Check if mon would potentially satisfy constraint
                {
                    if (mon.PokeBall != "Heavy Ball") // Consider only if no heavy ball
                    {
                        invalidPokemon.Add(mon);
                    }
                }
                else
                {
                    resultingBuild.TrainerOwnPokemon.Add(mon);
                }
            }
            // Then, need to check if mon could satisfy by just having a set item equipped
            foreach (SetItem setItem in trainer.SetItems.Keys)
            {
                // If set item satisfies, then check which mons can use it and add them to result
                if (constraint.SatisfiedBySetItem(setItem))
                {
                    List<TrainerPokemon> validMons = new List<TrainerPokemon>();
                    foreach (TrainerPokemon mon in invalidPokemon)
                    {
                        if (setItem.CanEquip(mon))
                        {
                            validMons.Add(mon);
                        }
                    }
                    if (validMons.Count > 0)
                    {
                        resultingBuild.TrainerOwnPokemonUsingSetItem.Add(setItem, validMons);
                    }
                }
            }
            // Then, check trainer's favours and add the mons that satisfy, similarly
            foreach (Trainer friendlyTrainer in trainer.Favours.Keys)
            {
                foreach (TrainerPokemon mon in friendlyTrainer.PartyPokemon)
                {
                    if (constraint.SatisfiedByMon(mon, true)) // If mon could potentially fit
                    {
                        if (!resultingBuild.FavourPokemon.TryGetValue(friendlyTrainer, out List<TrainerPokemon> value))
                        {
                            value = new List<TrainerPokemon>();
                            resultingBuild.FavourPokemon.Add(friendlyTrainer, value);
                        }
                        value.Add(mon);
                    }
                }
            }
            return resultingBuild;
        }
        /// <summary>
        /// Assembles a trainer's team given all possible builds, randomized if there's any preference
        /// </summary>
        /// <param name="trainer">The trainer whose battle team to define</param>
        /// <param name="possibleTeamBuilds">All the possible builds for the trainers</param>
        /// <param name="acceptLessMons">If we accept less mons than the exace value of nMons</param>
        /// <param name="seed">Seed used to select trainer mons</param>
        public static void AssembleTrainersBattleTeam(Trainer trainer, int nMons, List<PossibleTeamBuild> possibleTeamBuilds, bool acceptLessMons, int seed = 0)
        {
            Console.WriteLine($"Assembling {trainer.Name}'s team");
            Random rng;
            if (seed == 0) // Add a new or existing seed
            {
                rng = new Random();
            }
            else
            {
                rng = new Random(seed);
            }
            int monsInTeam = 0;
            List<TrainerPokemon> finalBattleTeam = [.. Enumerable.Repeat<TrainerPokemon>(null, nMons)]; // This will be the result, will contain mons in the right slots hopefully
            // Choose which team build option will be used
            PossibleTeamBuild usedBuild;
            if (possibleTeamBuilds.Count > 1)
            {
                Console.WriteLine("There's many potential builds, which one will be chosen? 0-N or -1 for random");
                for (int i = 0; i < possibleTeamBuilds.Count; i++)
                {
                    List<string> monNames = [.. possibleTeamBuilds[i].TrainerOwnPokemon.Select(m => m.Species)]; // Get all names
                    Console.WriteLine($"{i} - {string.Join(',', monNames)}");
                }
                int selection = int.Parse(Console.ReadLine());
                if (selection == -1) // If chose random option, then...
                {
                    selection = rng.Next(0, possibleTeamBuilds.Count);
                }
                usedBuild = possibleTeamBuilds[selection];
            }
            else
            {
                usedBuild = possibleTeamBuilds[0];
            }
            // Now, begin the sequence of try to add trainer mons, but first, need to ask if the trainer wants to cash in favours
            void BorrowFavour(Trainer favourTrainer, TrainerPokemon favourMon) /// Function to borrow mon from trainer, trainer and build are "global" variabels here
            {
                // Trainer used favour, housekeeping
                int remainingFavours = GeneralUtilities.AddtemToCountDictionary(trainer.Favours, favourTrainer, -1, true); // Remove 1 from the remaining favors of trainer
                GameDataContainers.GlobalGameData.CurrentEventMessage.PreEventText.AppendLine(
                    $"- <@{trainer.DiscordNumber}> asked their friend {favourTrainer.Name} to lend them a Pokemon for the tournament. {favourTrainer.Name} has lent them their {favourMon.Species}."
                    );
                if (remainingFavours > 0)// If still have favours
                {
                    usedBuild.FavourPokemon[favourTrainer].Remove(favourMon);
                }
                else // Trainer still useful, just remove the mon i just borrowed
                {
                    usedBuild.FavourPokemon.Remove(favourTrainer); // This trainer can't be used anymore
                }
            }
            void AddNextMonToFinalTeam(TrainerPokemon mon, int place = -1) // Adds the next mon, either in a place or to the next possible location
            {
                if (place != -1)
                {
                    if (finalBattleTeam[place] != null) throw new Exception("There's a Pokemon in this slot already");
                    finalBattleTeam[place] = mon;
                    monsInTeam++;
                }
                else
                {
                    for (int i = 0; i < finalBattleTeam.Count; i++)
                    {
                        if (finalBattleTeam[i] == null)
                        {
                            finalBattleTeam[i] = mon;
                            monsInTeam++;
                            return;
                        }
                    }
                    throw new Exception("Mon list already full");
                }
            }
            while (monsInTeam < nMons && usedBuild.FavourPokemon.Count > 0) // While need mons and there's favour to use
            {
                Console.WriteLine("Will favour be used? y/N");
                string input = Console.ReadLine();
                if (input.ToLower() == "y") // Favour to be used
                {
                    // Get trainer
                    List<string> nameList = [.. usedBuild.FavourPokemon.Keys.Select(f => f.Name)];
                    Console.WriteLine($"Which favour to use? {string.Join(",", nameList)}");
                    input = Console.ReadLine();
                    // Get mon
                    Trainer borrowedTrainer = usedBuild.FavourPokemon.Keys.Where(f => f.Name == input).First();
                    nameList = [.. usedBuild.FavourPokemon[borrowedTrainer].Select(m => m.Species)];
                    Console.WriteLine($"Which mon to borrow? 0 if random. {string.Join(",", nameList)}");
                    input = Console.ReadLine();
                    TrainerPokemon borrowedMon;
                    if (input == "0")
                    {
                        borrowedMon = GeneralUtilities.GetRandomPick(usedBuild.FavourPokemon[borrowedTrainer]);
                    }
                    else
                    {
                        borrowedMon = usedBuild.FavourPokemon[borrowedTrainer].Where(m => m.Species == input).First();
                    }
                    borrowedMon.Borrowed = true;
                    // Then, item building
                    if (trainer.SetItems.Count > 0)
                    {
                        Console.WriteLine("Use set item? y/N");
                        input = Console.ReadLine();
                        if (input.ToLower() == "y")
                        {
                            nameList = [.. trainer.SetItems.Keys.Select(i => i.Name)];
                            Console.WriteLine($"Which set item? {string.Join(",", nameList)}");
                            input = Console.ReadLine();
                            SetItem item = trainer.SetItems.Keys.Where(i => i.Name == input).First();
                            borrowedMon.SetItem = item;
                            borrowedMon.SetItemChosen = true;
                            GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, item, -1, true);
                        }
                    }
                    if (trainer.ModItems.Count > 0)
                    {
                        Console.WriteLine("Use mod item? y/N");
                        input = Console.ReadLine();
                        if (input.ToLower() == "y")
                        {
                            nameList = [.. trainer.ModItems.Keys.Select(i => i.Name)];
                            Console.WriteLine($"Which mod item? {string.Join(",", nameList)}");
                            input = Console.ReadLine();
                            Item item = trainer.ModItems.Keys.Where(i => i.Name == input).First();
                            borrowedMon.ModItem = item;
                            borrowedMon.ModItemChosen = true;
                            GeneralUtilities.AddtemToCountDictionary(trainer.ModItems, item, -1, true);
                        }
                    }
                    if (trainer.BattleItems.Count > 0)
                    {
                        Console.WriteLine("Use battle item? y/N");
                        input = Console.ReadLine();
                        if (input.ToLower() == "y")
                        {
                            nameList = [.. trainer.BattleItems.Keys.Select(i => i.Name)];
                            Console.WriteLine($"Which battle item? {string.Join(",", nameList)}");
                            input = Console.ReadLine();
                            Item item = trainer.BattleItems.Keys.Where(i => i.Name == input).First();
                            borrowedMon.BattleItem = item;
                            borrowedMon.BattleItemChosen = true;
                            GeneralUtilities.AddtemToCountDictionary(trainer.BattleItems, item, -1, true);
                        }
                    }
                    // Finally, where to put the mon
                    List<int> validSelections = [];
                    for (int i = 0; i < finalBattleTeam.Count; i++) // Check the still valid places
                    {
                        if (finalBattleTeam[i] == null)
                        {
                            validSelections.Add(i);
                        }
                    }
                    Console.WriteLine($"Where to place the mon? {string.Join(",", validSelections)}, -1 if random");
                    int selection = int.Parse(Console.ReadLine());
                    if (selection == -1)
                    {
                        selection = rng.Next(0, validSelections.Count);
                    }
                    BorrowFavour(borrowedTrainer, borrowedMon); // Housekeeping regarding the favour in question
                    AddNextMonToFinalTeam(borrowedMon, selection);
                }
                else
                {
                    break;
                }
            }
            // If trainer defined a strict order, will add them in the order of team as stated, otherwise do mon>set item>favor
            if (trainer.AutoTeam) // If shuffling is allowed, all is shuffled then and picks prioritising item efficiency
            {
                GeneralUtilities.ShuffleListDeterministic(usedBuild.TrainerOwnPokemon, rng);
                for (int i = 0; i < usedBuild.TrainerOwnPokemon.Count && monsInTeam < nMons; i++) // Fill as much as possible from here until done or ran out of mons
                {
                    TrainerPokemon chosenMon = usedBuild.TrainerOwnPokemon[i];
                    AddNextMonToFinalTeam(chosenMon);
                }
                // Then, if still need to fill, let's go items
                if (monsInTeam < nMons && !acceptLessMons && trainer.AutoSetItem)
                {
                    // Add mons + battle item as long as i still have mons (and items!) and need to get more
                    while (usedBuild.TrainerOwnPokemonUsingSetItem.Count > 0 && monsInTeam < nMons)
                    {
                        // Pick a random set item, a random mon from the list, decrement uses of set item
                        SetItem chosenSetItem = usedBuild.TrainerOwnPokemonUsingSetItem.Keys.ToList()[rng.Next(usedBuild.TrainerOwnPokemonUsingSetItem.Count)]; // Choose an item
                        List<TrainerPokemon> potentialMons = [.. usedBuild.TrainerOwnPokemonUsingSetItem[chosenSetItem].Where(p => !finalBattleTeam.Contains(p))];
                        if (potentialMons.Count > 0) // ok theres something to work with
                        {
                            TrainerPokemon chosenMon = potentialMons[rng.Next(potentialMons.Count)];
                            chosenMon.SetItem = chosenSetItem; // Equip the thing
                            int finalSetItemAmount = GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, chosenSetItem, -1, true);
                            if (finalSetItemAmount <= 0) // If this was the last use of this item, then remove it from options from now on
                            {
                                usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                            }
                            AddNextMonToFinalTeam(chosenMon);
                        }
                        else
                        {
                            usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                        }
                    }
                }
            }
            else // Respect the strict order, mons are retrieved from their corresponding places
            {
                // Choose mons until i ran out of need or ran out of mon, but do so in the desired order
                for (int i = 0; i < trainer.PartyPokemon.Count && monsInTeam < nMons; i++)
                {
                    TrainerPokemon chosenMon = trainer.PartyPokemon[i];
                    if (usedBuild.TrainerOwnPokemon.Contains(chosenMon)) // This was a valid mon, just add as is!
                    {
                        AddNextMonToFinalTeam(chosenMon);
                    }
                    else // Then, may still be able to add it with a set item
                    {
                        List<SetItem> potentialSetItems = new List<SetItem>();
                        foreach (KeyValuePair<SetItem, List<TrainerPokemon>> monsWSetItems in usedBuild.TrainerOwnPokemonUsingSetItem)
                        {
                            if (monsWSetItems.Value.Contains(chosenMon)) // Mon can use this
                            {
                                potentialSetItems.Add(monsWSetItems.Key);
                            }
                        }
                        if (potentialSetItems.Count > 0 && trainer.AutoSetItem) // Ok this mon can have the set item...
                        {
                            SetItem chosenSetItem = potentialSetItems[rng.Next(potentialSetItems.Count)]; // Choose one at random
                            chosenMon.SetItem = chosenSetItem; // Equip the thing
                            int finalSetItemAmount = GeneralUtilities.AddtemToCountDictionary(trainer.SetItems, chosenSetItem, -1, true);
                            if (finalSetItemAmount <= 0) // If this was the last use of this item, then remove it from options from now on
                            {
                                usedBuild.TrainerOwnPokemonUsingSetItem.Remove(chosenSetItem); // This set item no longer valid
                            }
                            AddNextMonToFinalTeam(chosenMon);
                        }
                    }
                }
            }
            // Finally finally, same with favor dudes, this will be random either way
            while (monsInTeam < nMons && !acceptLessMons && trainer.AutoFavour) // Will only cash in favors if I absolutely need to (and can!)
            {
                // First, find which random favor I will cash in
                Trainer nextFavourTrainer = usedBuild.FavourPokemon.Keys.ToList()[rng.Next(usedBuild.FavourPokemon.Count)]; // Get a random trainer
                List<TrainerPokemon> possibleFavourPokemon = usedBuild.FavourPokemon[nextFavourTrainer];
                TrainerPokemon borrowedMon = possibleFavourPokemon[rng.Next(possibleFavourPokemon.Count)]; // Get a random mon
                BorrowFavour(nextFavourTrainer, borrowedMon);
                AddNextMonToFinalTeam(borrowedMon); // Add mon to team
            }
            // Should be al good here I guess, validate (no null spaces and allowed mon number) and reshuffle if auto team so the favour mon can be anywhere too
            finalBattleTeam = [.. finalBattleTeam.Where(m => m != null)];
            if (!acceptLessMons && finalBattleTeam.Count < nMons) throw new Exception("For some reason the final battle team didn't have enough mons!");
            if (trainer.AutoTeam) GeneralUtilities.ShuffleListDeterministic(finalBattleTeam, rng); // One last shuffle to allow any mon in any position
            trainer.BattleTeam = finalBattleTeam; // Set this team for battle
            Console.WriteLine();//Empty line break
        }
    }
}
