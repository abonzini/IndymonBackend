using GameData;
using GameDataContainer;
using MechanicsData;
using Utilities;

namespace AutomatedTeamBuilder
{
    public class TeamBuildConstraints
    {
        /// Options that could generate a valid team. Some may contian multiple (or) options. E.g. dancer will require one ability or flags. Monotype is multiple separate options each with a possible team comp
        public List<List<(ElementType, string)>> DifferentOptions = new List<List<(ElementType, string)>>();
        /// <summary>
        /// Adds all monotype constraint options (e.g. a team of one type, each with a possible solution
        /// </summary>
        public void AddMonotypeConstraints()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                DifferentOptions.Add([(ElementType.POKEMON_TYPE, type.ToString())]);
            }
        }
    }
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
        /// The set items that can be used to satisfy strict move/ability requirements
        /// </summary>
        public List<string> SetItems = new List<string>();
        /// <summary>
        /// Pokemon that a trainer could lend you as a favor that satisfy req
        /// </summary>
        public Dictionary<string, List<TrainerPokemon>> FavourPokemon = new Dictionary<string, List<TrainerPokemon>>();
    }
    public static partial class TeamBuilder
    {
        /// <summary>
        /// For a given trainer, gives me all the possible teams they could build with the corresponding constraint sets
        /// </summary>
        /// <param name="trainer">Which trainer</param>
        /// <param name="nMons">How many mons need to satisfy the constraints (min)</param>
        /// <param name="constraints">The constraints for teambuild</param>
        /// <returns></returns>
        public static List<PossibleTeamBuild> GetTrainersBuildOptions(Trainer trainer, int nMons, TeamBuildConstraints constraints)
        {
            List<PossibleTeamBuild> resultingBuilds = new List<PossibleTeamBuild>();
            int mostOwnMonsUsed = 0; // Will try to only use the options that use the most own mons to not overuse set items or favors if don't need
            foreach (List<(ElementType, string)> options in constraints.DifferentOptions) // Check each satisfied constraint
            {
                PossibleTeamBuild buildOption = new PossibleTeamBuild(); // The corresponding team options for this case
                // First, check all own mons that satisfy
                foreach (TrainerPokemon mon in trainer.PartyPokemon)
                {
                    bool monValid = false;
                    foreach ((ElementType, string) check in options)
                    {
                        monValid = ValidateMonProperty(mon, check.Item1, check.Item2);
                        if (monValid) // If mon ok, then add to build and ditch
                        {
                            buildOption.TrainerOwnPokemon.Add(mon);
                            break;
                        }
                    }
                }
                // Then, need to check if the set items in inventory also satisfy, there may be lots (but only if trainer can use them
                if (trainer.AutoSetItem)
                {
                    foreach (KeyValuePair<string, int> setItem in trainer.SetItems)
                    {
                        foreach ((ElementType, string) check in options)
                        {
                            Ability setItemAbility = GetSetItemAbility(setItem.Key);
                            if (setItemAbility != null) // Set items that grant ability may grant a satisfying ability
                            {
                                if (ValidateAbilityProperty(setItemAbility, check.Item1, check.Item2))
                                {
                                    buildOption.SetItems.AddRange(Enumerable.Repeat(setItem.Key, setItem.Value));
                                }
                            }
                            Move setItemMove = GetSetItemMove(setItem.Key);
                            if (setItemMove != null) // Set items that grant move may grant a satisfying move 
                            {
                                if (ValidateMoveProperty(setItemMove, check.Item1, check.Item2))
                                {
                                    buildOption.SetItems.AddRange(Enumerable.Repeat(setItem.Key, setItem.Value));
                                }
                            }
                        }
                    }
                }
                // Then, check trainer's favours and add the mons that satisfy, similarly
                if (trainer.AutoFavour)
                {
                    foreach (string friendlyTrainer in trainer.TrainerFavours.Keys)
                    {
                        Trainer theTrainer = GameDataContainers.GlobalGameData.GetTrainer(friendlyTrainer); // Find the trainer whoever it is
                        foreach (TrainerPokemon mon in theTrainer.PartyPokemon)
                        {
                            bool monValid = false;
                            foreach ((ElementType, string) check in options)
                            {
                                monValid = ValidateMonProperty(mon, check.Item1, check.Item2);
                                if (monValid) // If mon ok, then add to build and ditch
                                {
                                    if (!buildOption.FavourPokemon.TryGetValue(friendlyTrainer, out List<TrainerPokemon> value))
                                    {
                                        value = new List<TrainerPokemon>();
                                        buildOption.FavourPokemon.Add(friendlyTrainer, value);
                                    }
                                    value.Add(mon);
                                    break;
                                }
                            }
                        }
                    }
                }
                // Now that all the options are set in place, need to verify if all of these allow to complete a team
                int usableMons = buildOption.TrainerOwnPokemon.Count; // Mons that can be used
                int trainerInvalidMons = trainer.PartyPokemon.Count - usableMons; // These mons want to play but can't!
                usableMons += Math.Min(trainerInvalidMons, buildOption.SetItems.Count); // Can use these to add more mons, but only if i have enough items/mons
                foreach (KeyValuePair<string, List<TrainerPokemon>> favorOption in buildOption.FavourPokemon) // Then, see how much I can borrow from each trainer
                {
                    int numberOfFavors = trainer.TrainerFavours[favorOption.Key];
                    usableMons += Math.Min(favorOption.Value.Count, numberOfFavors); // Can borrow only the valid mon but also limited by number of fav available
                }
                if (usableMons >= nMons) // Need to check now if I have enough options to build a team with these constraints
                {
                    resultingBuilds.Add(buildOption);
                    mostOwnMonsUsed = Math.Max(mostOwnMonsUsed, buildOption.TrainerOwnPokemon.Count); // Also keep this in mind, by the end I'll choose between the options that need the least amount of items
                }
            }
            // Finally, crop to only propose the ones where I use mostly my mons as much as possible
            resultingBuilds = [.. resultingBuilds.Where(b => b.TrainerOwnPokemon.Count == mostOwnMonsUsed)];
            return resultingBuilds;
        }
        /// <summary>
        /// Assembles a trainer's team given all possible builds, randomized if there's any preference
        /// </summary>
        /// <param name="trainer">The trainer whose battle team to define</param>
        /// <param name="teamBuild">All the possible builds for the trainers</param>
        public static void AssembleTrainersBattleTeam(Trainer trainer, int nMons, List<PossibleTeamBuild> teamBuild)
        {
            PossibleTeamBuild usedBuild = IndymonUtilities.GetRandomPick(teamBuild); // TODO: May need to be chosen in some crazy monotypes instead of random
            List<TrainerPokemon> finalBattleTeam = []; // This is the result
            List<TrainerPokemon> allTrainerMons = [.. trainer.PartyPokemon]; // May take from here in order if needed
            if (trainer.AutoTeam) // If shuffling is allowed, all is shuffled then lol
            {
                IndymonUtilities.ShuffleList(allTrainerMons);
                IndymonUtilities.ShuffleList(usedBuild.TrainerOwnPokemon);
            }
            // Now, begin the sequence of try to add trainer mons, in order: Party -> Set Items -> Favours
            for (int i = 0; i < usedBuild.TrainerOwnPokemon.Count && finalBattleTeam.Count < nMons; i++) // Fill as much as possible from here until done or ran out of mons
            {
                finalBattleTeam.Add(usedBuild.TrainerOwnPokemon[i]); // Add to final team
            }
            // Then, if still need to fill, let's go items
            if (finalBattleTeam.Count < nMons)
            {
                allTrainerMons.RemoveAll(p => finalBattleTeam.Contains(p)); // Removes the mons already in the team
                // Add mons + battle item as long as i still have mons (and items!)
                for (int i = 0; usedBuild.SetItems.Count > 0 && i < allTrainerMons.Count && finalBattleTeam.Count < nMons; i++)
                {
                    // Pick the next mon, equip a random (!) set item, cram into party
                    TrainerPokemon chosenMon = allTrainerMons[i];
                    string chosenSetItem = IndymonUtilities.GetRandomPick(usedBuild.SetItems);
                    usedBuild.SetItems.Remove(chosenSetItem); // Need to remove so it's not picked again, mon is ok as it is iterating
                    IndymonUtilities.AddtemToDictionary(trainer.SetItems, chosenSetItem, -1, true); // Also need to remove from the inventory!
                    chosenMon.SetItem = chosenSetItem; // This should be empty as auto-set-item should made sure to take this thing away from mon during parsing
                    finalBattleTeam.Add(chosenMon);
                }
            }
            // Finally finally, same with favor dudes
            while (finalBattleTeam.Count < nMons)
            {
                // First, find which random favor I will cash in
                KeyValuePair<string, List<TrainerPokemon>> nextFavour = IndymonUtilities.GetRandomKvp(usedBuild.FavourPokemon);
                int remainingFavours = trainer.TrainerFavours[nextFavour.Key] - 1;
                IndymonUtilities.AddtemToDictionary(trainer.TrainerFavours, nextFavour.Key, -1, true); // Remove 1 from the remaining favors of trainer
                TrainerPokemon borrowedMon = IndymonUtilities.GetRandomPick(nextFavour.Value);
                if (trainer.TrainerFavours[nextFavour.Key] <= 0)// If i used the last trainer's favour
                {
                    usedBuild.FavourPokemon.Remove(nextFavour.Key); // This trainer can't be used anymore
                }
                else // Trainer still useful, just remove the mon i just borrowed
                {
                    usedBuild.FavourPokemon[nextFavour.Key].Remove(borrowedMon);
                }
                // This will continue until infinite loop or crash or sth because if i reached here everything should work
            }
            // Should be al good here I guess
            trainer.BattleTeam = finalBattleTeam; // Set this team for battle
        }
        /// <summary>
        /// A contaxt of things going on, to be able to build mons sets. Includes opp average profile, typechart, and ongoing archetypes if present
        /// </summary>
        class TeamBuildContext
        {
            public List<TeamArchetype> CurrentTeamArchetypes = new List<TeamArchetype>(); // Contains an ongoing archetype that applies for all team
            public List<List<PokemonType>> OpponentsTypes = new List<List<PokemonType>>(); // Contains a list of all types found in opp teams
            public double[] OpponentsStats = new double[6]; // All opp stats in average
            public double OppSpeedVariance = 0; // Speed is special as I need the variance to calculate speed creep
            public double AverageOpponentWeight = 0; // Average weight of opponents
        }
        /// <summary>
        /// The context of a currently building mon. Also used to store data about current mods and stuff
        /// </summary>
        class PokemonBuildInfo
        {
            // Things that are added on the transcourse of building a set, makes some other stuff more or less desirable
            public TeamBuildConstraints AdditionalConstraints = new TeamBuildConstraints(); /// Teambuild constraint that are added due to required builds (unless unable to complete obviously)
            public Dictionary<(ElementType, string), float> EnabledOptions = new Dictionary<(ElementType, string), float>(); /// Things that normally are disabled but are now enabled, and the weight by where they were just enabled
            public HashSet<(StatModifier, string)> ModifiedTypeEffectiveness = new HashSet<(StatModifier, string)>();
            public Dictionary<(ElementType, string), Dictionary<MoveModifier, string>> MoveMods = new Dictionary<(ElementType, string), Dictionary<MoveModifier, string>>();
            public Dictionary<(ElementType, string), double> WeightMods = new Dictionary<(ElementType, string), double>();
            public List<List<PokemonType>> StillResistedTypes = new List<List<PokemonType>>(); // Types that this mon can't hit very well, to give a bit of a "coverage boost"
            // Then stuff that alters current mon
            public Nature Nature = Nature.SERIOUS;
            public PokemonType TeraType = PokemonType.NONE;
            public PokemonType[] PokemonTypes = [PokemonType.NONE, PokemonType.NONE];
            public int[] Evs = new int[6];
            public int[] StatBoosts = new int[6];
            public double[] StatMultipliers = [1, 1, 1, 1, 1, 1];
            public double PhysicalAccuracyMultiplier = 1;
            public double SpecialAccuracyMultiplier = 1;
            public double WeightMultiplier = 1;
            // Things that alter opp mon
            public int[] OppStatBoosts = new int[6];
            public double[] OppStatMultipliers = [1, 1, 1, 1, 1, 1];
            // Battle sim (how much damage my attacks do, how much damage mon takes from stuff, speed creep)
            public double DamageScore = 0;
            public double DefenseScore = 0;
            public double SpeedScore = 0;
        }
    }
}
