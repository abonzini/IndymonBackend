using MechanicsData;
using System.Text;

namespace GameData
{
    public class Trainer
    {
        public const int MAX_MONS_IN_TEAM = 12; /// How many mons top can the team have (rest goes to box)
        public string Name = "";
        public string DungeonIdentifier = "?";
        public int Imp = 0;
        public string Avatar = "";
        public string AvatarUrl = "";
        public string DiscordNumber = "";
        public TrainerRank TrainerRank = TrainerRank.UNRANKED;
        public bool AutoTeam = true;
        public bool AutoFavour = true;
        public bool AutoSetItem = true;
        public bool AutoModItem = true;
        public bool AutoBattleItem = true;
        public List<TrainerPokemon> PartyPokemon = new List<TrainerPokemon>();
        public List<TrainerPokemon> BoxedPokemon = new List<TrainerPokemon>();
        public Dictionary<SetItem, int> SetItems = new Dictionary<SetItem, int>();
        public Dictionary<Item, int> ModItems = new Dictionary<Item, int>();
        public Dictionary<Item, int> BattleItems = new Dictionary<Item, int>();
        public Dictionary<string, int> KeyItems = new Dictionary<string, int>();
        public Dictionary<Trainer, int> Favours = new Dictionary<Trainer, int>();
        public Dictionary<string, int> PokeBalls = new Dictionary<string, int>();
        public Dictionary<Sandwich, int> Sandwiches = new Dictionary<Sandwich, int>();
        public override string ToString()
        {
            return Name;
        }
        // Things related to an assembled team ready for battle
        public List<TrainerPokemon> BattleTeam = new List<TrainerPokemon>(); // A subset of team but can also have borrowed mons ready to battle
        /// <summary>
        /// Reset the state of all mons pre-battle
        /// </summary>
        public void RestoreAll()
        {
            foreach (TrainerPokemon mon in BattleTeam)
            {
                mon.HealFull();
            }
        }
        /// <summary>
        /// Gets trainer data as part of a packed string as is received by (my modified version of) showdown
        /// </summary>
        /// <returns>Packed string</returns>
        public string GetShowdownPackedString()
        {
            List<string> eachMonPacked = new List<string>();
            foreach (TrainerPokemon mon in BattleTeam)
            {
                eachMonPacked.Add(mon.GetShowdownPackedString());
            }
            return string.Join("]", eachMonPacked); // Returns the packed data joined with ]
        }
        /// <summary>
        /// Saves the CSV of this trainer per indymon S2 spreadsheet standard
        /// </summary>
        /// <param name="filePath">Path to save to</param>
        public void SaveTrainerCsv(string filePath)
        {
            const int TRAINER_CARD_ROWS = 22;
            // Unlike mons, every other thing needs to be sorted in alphabetical order
            List<(string, int)> setItemList = [];
            foreach (KeyValuePair<SetItem, int> setItemData in SetItems) setItemList.Add((setItemData.Key.Name, setItemData.Value));
            setItemList = [.. setItemList.OrderBy(s => s.Item1)];
            List<(string, int)> modItemList = [];
            foreach (KeyValuePair<Item, int> modItemData in ModItems) modItemList.Add((modItemData.Key.Name, modItemData.Value));
            modItemList = [.. modItemList.OrderBy(s => s.Item1)];
            List<(string, int)> battleItemList = [];
            foreach (KeyValuePair<Item, int> battleItemData in BattleItems) battleItemList.Add((battleItemData.Key.Name, battleItemData.Value));
            battleItemList = [.. battleItemList.OrderBy(s => s.Item1)];
            List<(string, int)> keyItemList = [];
            foreach (KeyValuePair<string, int> keyItemData in KeyItems) keyItemList.Add((keyItemData.Key, keyItemData.Value));
            keyItemList = [.. keyItemList.OrderBy(s => s.Item1)];
            List<(string, int)> favourList = [];
            foreach (KeyValuePair<Trainer, int> favourData in Favours) favourList.Add((favourData.Key.Name, favourData.Value));
            favourList = [.. favourList.OrderBy(s => s.Item1)];
            List<(string, int)> ballList = [];
            foreach (KeyValuePair<string, int> pokeBallData in PokeBalls) ballList.Add((pokeBallData.Key, pokeBallData.Value));
            ballList = [.. ballList.OrderBy(s => s.Item1)];
            List<(string, int)> sandwichList = [];
            foreach (KeyValuePair<Sandwich, int> sandwichData in Sandwiches) sandwichList.Add((sandwichData.Key.Name, sandwichData.Value));
            sandwichList = [.. sandwichList.OrderBy(s => s.Item1)];
            BoxedPokemon = [.. BoxedPokemon.OrderBy(p => p.Species)]; // Also sort the boxed mons in place whatever
            // Ok now ready to go
            StringBuilder fileBuilder = new StringBuilder();
            StringBuilder lineBuilder = new StringBuilder();
            // Line 1, just pure trainer data
            lineBuilder.Append($"{Name},{DungeonIdentifier},{Imp},,");
            lineBuilder.Append($"Shuffle:,{AutoTeam.ToString().ToUpper()},");
            lineBuilder.Append($"Auto Set Item:,{AutoSetItem.ToString().ToUpper()},");
            lineBuilder.Append($"{Avatar},{AvatarUrl},");
            lineBuilder.Append($"Auto Mod Item:,{AutoModItem.ToString().ToUpper()},");
            lineBuilder.Append($"Auto Held Item:,{AutoBattleItem.ToString().ToUpper()},");
            lineBuilder.Append($"Auto Favour:,{AutoFavour.ToString().ToUpper()},");
            lineBuilder.Append($"{DiscordNumber},{TrainerRank.ToString()},,,-");
            fileBuilder.AppendLine(lineBuilder.ToString());
            // Line 2 is purely text but does contain actual assembled string
            lineBuilder.Clear();
            lineBuilder.Append($"Mons,,{String.Join("; ", BoxedPokemon.Select(m => m.IsShiny ? $"{m.Species}" : $"{m.Species}✦"))},,Set,Mod,Battle,");
            lineBuilder.Append($"Set Items:,{String.Join("; ", setItemList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"Mod Items:,{String.Join("; ", modItemList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"Held Items:,{String.Join("; ", battleItemList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"Key Items:,{String.Join("; ", keyItemList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"Favours:,{String.Join("; ", favourList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"PokeBalls:,{String.Join("; ", ballList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))},");
            lineBuilder.Append($"Sandwiches:,{String.Join("; ", sandwichList.Select(i => (i.Item2 > 1) ? $"{i.Item1} x{i.Item2}" : i.Item1))}");
            fileBuilder.AppendLine(lineBuilder.ToString());
            // Finally, remaining lines are listing stuff in order
            for (int i = 0; i < TRAINER_CARD_ROWS - 2; i++) // Starting from line 2 until end of trainer card
            {
                lineBuilder.Clear();
                if (i < MAX_MONS_IN_TEAM) // If I'm dealing with party or boxed mons
                {
                    if (PartyPokemon.Count > i) // There's a mon to add
                    {
                        TrainerPokemon nextMon = PartyPokemon[i];
                        lineBuilder.Append($"{nextMon.Species},");
                        lineBuilder.Append((nextMon.Nickname != "") ? $"{nextMon.Nickname}," : ",");
                        lineBuilder.Append($"{nextMon.IsShiny.ToString().ToUpper()},");
                        lineBuilder.Append($"{nextMon.PokeBall},");
                        lineBuilder.Append((nextMon.SetItem != null) ? $"{nextMon.SetItem.Name}," : ",");
                        lineBuilder.Append((nextMon.ModItem != null) ? $"{nextMon.ModItem.Name}," : ",");
                        lineBuilder.Append((nextMon.BattleItem != null) ? $"{nextMon.BattleItem.Name}," : ",");
                    }
                    else
                    {
                        lineBuilder.Append(",,,,,,,"); // No mon here
                    }
                }
                else // Otherwise I look for boxed mons
                {
                    // Left boxed mon
                    if (BoxedPokemon.Count > 2 * i)
                    {
                        TrainerPokemon nextMon = PartyPokemon[2 * i];
                        lineBuilder.Append($"{nextMon.Species},");
                        lineBuilder.Append($"{nextMon.PokeBall}");
                        lineBuilder.Append($"{nextMon.IsShiny.ToString().ToUpper()},");
                    }
                    else
                    {
                        lineBuilder.Append(",,,"); // No mon here
                    }
                    lineBuilder.Append(','); // The space between boxed stuff
                    // Right boxed mon
                    if (BoxedPokemon.Count > ((2 * i) + 1))
                    {
                        TrainerPokemon nextMon = PartyPokemon[(2 * i) + 1];
                        lineBuilder.Append($"{nextMon.Species},");
                        lineBuilder.Append($"{nextMon.PokeBall}");
                        lineBuilder.Append($"{nextMon.IsShiny.ToString().ToUpper()},");
                    }
                    else
                    {
                        lineBuilder.Append(",,,"); // No mon here
                    }
                }
                // Set items
                if (setItemList.Count > i)
                {
                    lineBuilder.Append($"{setItemList[i].Item1},{setItemList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Mod items
                if (modItemList.Count > i)
                {
                    lineBuilder.Append($"{modItemList[i].Item1},{modItemList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Battle items
                if (battleItemList.Count > i)
                {
                    lineBuilder.Append($"{battleItemList[i].Item1},{battleItemList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Key items
                if (keyItemList.Count > i)
                {
                    lineBuilder.Append($"{keyItemList[i].Item1},{keyItemList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Set items
                if (favourList.Count > i)
                {
                    lineBuilder.Append($"{favourList[i].Item1},{favourList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Balls
                if (ballList.Count > i)
                {
                    lineBuilder.Append($"{ballList[i].Item1},{ballList[i].Item2},");
                }
                else
                {
                    lineBuilder.Append(",,");
                }
                // Sandwiches
                if (sandwichList.Count > i)
                {
                    lineBuilder.Append($"{sandwichList[i].Item1},{sandwichList[i].Item2}");
                }
                else
                {
                    lineBuilder.Append(',');
                }
                // Finished row
                fileBuilder.AppendLine(lineBuilder.ToString());
            }
            // File complete, save
            File.WriteAllText(filePath, fileBuilder.ToString());
        }
    }
}
