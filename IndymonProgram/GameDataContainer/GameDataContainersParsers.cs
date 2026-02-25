using GameData;
using MechanicsData;
using MechanicsDataContainer;
using Utilities;

namespace GameDataContainer
{
    public partial class GameDataContainers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheetId"></param>
        /// <param name="sheetTab"></param>
        /// <param name="trainerContainer"></param>
        void ParseTrainerCards(string sheetId, string sheetTab, Dictionary<string, Trainer> trainerContainer)
        {
            trainerContainer.Clear();
            // Parse csv
            const int TRAINER_CARD_ROWS = 22; // Number of lines per trainer card
            const int TRAINER_CARD_COLS = 18; // Number of columns per trainer card
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            string[] rows = csv.Split("\n");
            string[] cols = rows[0].Trim().Split(',');
            for (int i = 0; i < rows.Length; i += TRAINER_CARD_ROWS) // Parse all trainer rows, one by one, each i will be a row of trainers
            {
                for (int j = 0; j < cols.Length; j += TRAINER_CARD_COLS)
                {
                    string[] nextLine = rows[i].Trim().Split(','); // Trim to avoid weird carriage return shenanigans
                    // Can parse trainer i,j no problem!
                    Trainer nextTrainer = new Trainer();
                    // Row 0, main data
                    string name = nextLine[j + 0];
                    if (name == "") continue; // No trainer here, move on
                    nextTrainer.Name = name;
                    nextTrainer.DungeonIdentifier = nextLine[j + 1];
                    nextTrainer.IMP = int.Parse(nextLine[j + 2]);
                    nextTrainer.AutoTeam = bool.Parse(nextLine[j + 4]);
                    nextTrainer.AutoSetItem = bool.Parse(nextLine[j + 6]);
                    nextTrainer.Avatar = nextLine[j + 7];
                    nextTrainer.AvatarUrl = nextLine[j + 8];
                    nextTrainer.AutoModItem = bool.Parse(nextLine[j + 10]);
                    nextTrainer.AutoBattleItem = bool.Parse(nextLine[j + 12]);
                    nextTrainer.AutoFavour = bool.Parse(nextLine[j + 14]);
                    nextTrainer.DiscordNumber = nextLine[j + 15].Trim();
                    nextTrainer.TrainerRank = Enum.Parse<TrainerRank>(nextLine[j + 16]);
                    // Second line is skipped, just headers for me to manually access
                    // Then, relative rows 2->21 have all the data always in order
                    for (int remainingRows = 2; (remainingRows < TRAINER_CARD_ROWS && remainingRows + i < rows.Length); remainingRows++)
                    {
                        // Get next line w data
                        nextLine = rows[remainingRows + i].Trim().Split(",");
                        // For mons
                        bool boxArea = (remainingRows >= (2 + Trainer.MAX_MONS_IN_TEAM)); // At some point it's only boxed mons
                        if (boxArea)
                        {
                            for (int boxIndex = 0; boxIndex < 6; boxIndex += 3) // There's 2 col of boxed mons with 3 things (no item)
                            {
                                string pokemonName = nextLine[j + boxIndex]; // Get next mon name
                                if (pokemonName != "") // Valid pokemon, parse
                                {
                                    TrainerPokemon newPokemon = new TrainerPokemon()
                                    {
                                        Species = pokemonName,
                                        Nickname = nextLine[j + boxIndex + 1],
                                        IsShiny = bool.Parse(nextLine[j + boxIndex + 2]),
                                    };
                                    nextTrainer.BoxedPokemon.Add(newPokemon);
                                }
                            }
                        }
                        else // Party mon
                        {
                            string pokemonName = nextLine[j + 0];
                            if (pokemonName != "") // Valid pokemon, parse
                            {
                                MechanicsDataContainers.GlobalMechanicsData.AssertElementExistance(ElementType.POKEMON, pokemonName); // Make sure pokemon exists, no typo
                                TrainerPokemon newPokemon = new TrainerPokemon()
                                {
                                    Species = pokemonName,
                                    Nickname = nextLine[j + 1],
                                    IsShiny = bool.Parse(nextLine[j + 2]),
                                };
                                // Set item
                                string setItemName = nextLine[j + 3];
                                if (setItemName != "")
                                {
                                    if (!SetItems.TryGetValue(setItemName, out SetItem item)) // Creates it if doesn't exist
                                    {
                                        item = SetItem.Parse(setItemName);
                                        SetItems.Add(setItemName, item);
                                    }
                                    newPokemon.SetItem = item;
                                    if (!item.CanEquip(newPokemon)) throw new Exception("Pokemon has an invalid set item");
                                }
                                // Mod item
                                string modItemName = nextLine[j + 4];
                                if (modItemName != "")
                                {
                                    newPokemon.ModItem = MechanicsDataContainers.GlobalMechanicsData.ModItems[modItemName];
                                }
                                // Battle item
                                string battleItemName = nextLine[j + 5];
                                if (battleItemName != "")
                                {
                                    newPokemon.BattleItem = MechanicsDataContainers.GlobalMechanicsData.BattleItems[battleItemName];
                                }
                                // Finally, add Pokemon to team
                                nextTrainer.PartyPokemon.Add(newPokemon);
                            }
                        }
                        // Next is Set items in bag so
                        string itemName = nextLine[j + 6];
                        if (itemName != "") // A set item here
                        {
                            if (!SetItems.TryGetValue(itemName, out SetItem item)) // Creates it if doesn't exist
                            {
                                item = SetItem.Parse(itemName);
                                SetItems.Add(itemName, item);
                            }
                            int itemCount = int.Parse(nextLine[j + 7]);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.SetItems, item, itemCount);
                        }
                        // Next, mod items
                        itemName = nextLine[j + 8];
                        if (itemName != "") // A set item here
                        {
                            Item modItem = MechanicsDataContainers.GlobalMechanicsData.ModItems[itemName];
                            int itemCount = int.Parse(nextLine[j + 9]);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.ModItems, modItem, itemCount);
                        }
                        // Next, battle items
                        itemName = nextLine[j + 10];
                        if (itemName != "") // A set item here
                        {
                            Item battleItem = MechanicsDataContainers.GlobalMechanicsData.BattleItems[itemName];
                            int itemCount = int.Parse(nextLine[j + 11]);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.BattleItems, battleItem, itemCount);
                        }
                        // Then, Key Items
                        itemName = nextLine[j + 12];
                        if (itemName != "")
                        {
                            int itemCount = int.Parse(nextLine[j + 13]);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.KeyItems, itemName, itemCount);
                        }
                        itemName = nextLine[j + 14];
                        if (itemName != "")
                        {
                            int itemCount = int.Parse(nextLine[j + 15]);
                            Trainer trainer = GetTrainer(itemName);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.TrainerFavours, trainer, itemCount);
                        }
                        // Finally, Ballz
                        itemName = nextLine[j + 16];
                        if (itemName != "")
                        {
                            int itemCount = int.Parse(nextLine[j + 17]);
                            GeneralUtilities.AddtemToCountDictionary(nextTrainer.PokeBalls, itemName, itemCount);
                        }
                    }
                    trainerContainer.Add(nextTrainer.Name, nextTrainer);
                }
            }
        }
        /// <summary>
        /// Finds battle stats and parses it into place
        /// </summary>
        /// <param name="sheetId">Google sheet ID</param>
        /// <param name="sheetTab">Google Sheet tab</param>
        void ParseBattleStats(string sheetId, string sheetTab)
        {
            BattleStats = new BattleStats();
            // Parse csv
            string csv = GeneralUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
            // Obtained tournament data csv
            // Assumes the row order is same as column order!
            string[] rows = csv.Split("\n");
            // First pass is to obtain list of players in the order given no matter what
            for (int row = 2; row < rows.Length; row++)
            {
                string[] cols = rows[row].Split(',');
                string playerName = cols[0].Trim().ToLower(); // Contains player name
                PlayerAndStats nextPlayer = new PlayerAndStats
                {
                    Name = playerName,
                    // Statistics...
                    TournamentWins = int.Parse(cols[1]),
                    TournamentsPlayed = int.Parse(cols[2]),
                    GamesWon = int.Parse(cols[4]),
                    GamesPlayed = int.Parse(cols[5]),
                    Kills = int.Parse(cols[7]),
                    Deaths = int.Parse(cols[8])
                };
                // Finally add to the right place
                if (TrainerData.ContainsKey(playerName)) // This was an actual player, add to correct array
                {
                    BattleStats.PlayerStats.Add(nextPlayer);
                }
                else if (NpcData.ContainsKey(playerName)) // Otherwise it's NPC data
                {
                    BattleStats.NpcStats.Add(nextPlayer);
                }
                else
                {
                    throw new Exception("Found a non-npc and non-player in tournament data!");
                }
            }
            // Once the players are in the correct order, we begin the parsing
            for (int row = 0; row < BattleStats.PlayerStats.Count; row++) // Next part is to examine each PLAYER CHARACTER ONLY FOR STATS
            {
                int yOffset = 2; // 2nd row begins
                string[] cols = rows[yOffset + row].Split(',');
                int xOffset = 10; // Beginning of "vs trainer" data
                PlayerAndStats thisPlayer = BattleStats.PlayerStats[row]; // Get player owner of this data
                thisPlayer.EachMuWr = new Dictionary<string, IndividualMu>();
                for (int col = 0; col < BattleStats.PlayerStats.Count; col++) // Check all players score first
                {
                    if (row == col) continue; // No MU agains oneself
                    // Get data for this opp
                    string oppName = BattleStats.PlayerStats[col].Name;
                    int wins = int.Parse(cols[xOffset + (3 * col)]); // Data has 3 columns per player
                    int losses = int.Parse(cols[xOffset + (3 * col) + 1]);
                    thisPlayer.EachMuWr.Add(oppName, new IndividualMu { Wins = wins, Losses = losses }); // Add this data to the stats
                }
                xOffset = 10 + (3 * BattleStats.PlayerStats.Count); // Offset to NPC data
                for (int col = 0; col < BattleStats.NpcStats.Count; col++) // Check NPC score now
                {
                    // Get data for this opp
                    string oppName = BattleStats.NpcStats[col].Name;
                    int wins = int.Parse(cols[xOffset + (3 * col)]);
                    int losses = int.Parse(cols[xOffset + (3 * col) + 1]);
                    thisPlayer.EachMuWr.Add(oppName, new IndividualMu { Wins = wins, Losses = losses });
                }
            }
            // And thats it, tourn data has been found
        }
    }
}
