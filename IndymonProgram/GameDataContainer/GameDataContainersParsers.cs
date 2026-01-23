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
            const int TRAINER_CARD_COLS = 21; // Number of columns per trainer card
            string csv = IndymonUtilities.GetCsvFromGoogleSheets(sheetId, sheetTab);
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
                    nextTrainer.AutoTeam = bool.Parse(nextLine[j + 4]);
                    nextTrainer.AutoSetItem = bool.Parse(nextLine[j + 6]);
                    nextTrainer.Avatar = nextLine[j + 7];
                    nextTrainer.AutoModItem = bool.Parse(nextLine[j + 10]);
                    nextTrainer.AutoBattleItem = bool.Parse(nextLine[j + 12]);
                    nextTrainer.AutoFavour = bool.Parse(nextLine[j + 14]);
                    // Second line is skipped, just headers for me to manually access
                    // Then, relative rows 2->21 have all the data always in order
                    for (int remainingRows = 2; (remainingRows < TRAINER_CARD_ROWS && remainingRows + i < rows.Length); remainingRows++)
                    {
                        nextLine = rows[remainingRows + i].Trim().Split(",");
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
                            // Set item, and verify if goes to trainer or mon
                            string setItemName = nextLine[j + 3];
                            if (setItemName != "")
                            {
                                if (nextTrainer.AutoSetItem) IndymonUtilities.AddtemToDictionary(nextTrainer.SetItems, setItemName);
                                else newPokemon.SetItem = setItemName;
                            }
                            // Mod item, and verify if goes to trainer or mon
                            string modItemName = nextLine[j + 4];
                            if (modItemName != "")
                            {
                                ModItem modItem = MechanicsDataContainers.GlobalMechanicsData.ModItems[modItemName];
                                if (nextTrainer.AutoModItem)
                                {
                                    IndymonUtilities.AddtemToDictionary(nextTrainer.ModItems, modItem);
                                }
                                else
                                {
                                    newPokemon.ModItem = modItem;
                                }
                            }
                            // Battle item, and verify if goes to trainer or mon
                            string battleItemName = nextLine[j + 5];
                            if (battleItemName != "")
                            {
                                BattleItem battleItem = MechanicsDataContainers.GlobalMechanicsData.BattleItems[battleItemName];
                                if (nextTrainer.AutoBattleItem)
                                {
                                    IndymonUtilities.AddtemToDictionary(nextTrainer.BattleItems, battleItem);
                                }
                                else
                                {
                                    newPokemon.BattleItem = battleItem;
                                }
                            }
                            // Finally, add Pokemon to team (or nowhere if team full)
                            if (nextTrainer.PartyPokemon.Count < Trainer.MAX_MONS_IN_TEAM) // Add to team
                            {
                                nextTrainer.PartyPokemon.Add(newPokemon);
                            }
                            else
                            {
                                nextTrainer.BoxedPokemon.Add(newPokemon);
                            }
                        }
                        // Next is Set items in bag so
                        string itemName = nextLine[j + 6];
                        if (itemName != "") // A set item here
                        {
                            int itemCount = int.Parse(nextLine[j + 7]);
                            IndymonUtilities.AddtemToDictionary(nextTrainer.SetItems, itemName, itemCount);
                        }
                        // Next, mod items
                        itemName = nextLine[j + 9];
                        if (itemName != "") // A set item here
                        {
                            ModItem modItem = MechanicsDataContainers.GlobalMechanicsData.ModItems[itemName];
                            int itemCount = int.Parse(nextLine[j + 10]);
                            IndymonUtilities.AddtemToDictionary(nextTrainer.ModItems, modItem, itemCount);
                        }
                        // Next, battle items
                        itemName = nextLine[j + 12];
                        if (itemName != "") // A set item here
                        {
                            BattleItem battleItem = MechanicsDataContainers.GlobalMechanicsData.BattleItems[itemName];
                            int itemCount = int.Parse(nextLine[j + 13]);
                            IndymonUtilities.AddtemToDictionary(nextTrainer.BattleItems, battleItem, itemCount);
                        }
                        // Finally for now, favours
                        itemName = nextLine[j + 15];
                        if (itemName != "")
                        {
                            int itemCount = int.Parse(nextLine[j + 16]);
                            if (itemName.Contains("Favour")) // A key item here, is it favour?
                            {
                                string trainerFavour = itemName.Split("'")[0].Trim(); // Got trainer who owns the favour
                                GetTrainer(trainerFavour);
                                IndymonUtilities.AddtemToDictionary(nextTrainer.TrainerFavours, trainerFavour, itemCount);
                            }
                            else // Regular key item
                            {
                                IndymonUtilities.AddtemToDictionary(nextTrainer.KeyItems, itemName, itemCount);
                            }
                        }
                        // Finally, Ballz
                        itemName = nextLine[j + 18];
                        if (itemName != "")
                        {
                            int itemCount = int.Parse(nextLine[j + 19]);
                            IndymonUtilities.AddtemToDictionary(nextTrainer.PokeBalls, itemName, itemCount);
                        }
                    }
                    trainerContainer.Add(nextTrainer.Name, nextTrainer);
                }
            }
        }
    }
}
