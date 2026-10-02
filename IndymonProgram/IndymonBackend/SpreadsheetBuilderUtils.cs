using Gameplay.GameplayElements;
using Gameplay.GameplayElementsContainer;
using System.Text;
using Utilities;

namespace IndymonBackendProgram
{
    public static class SpreadsheetBuilderUtils
    {
        /// <summary>
        /// Exports glossary corresponding to all items and boxed mons
        /// </summary>
        /// <param name="directoryPath">Path where saved</param>
        public static void ExportGlossary(string directoryPath)
        {
            Dictionary<string, string> glossary = new Dictionary<string, string>();
            // Add all elements that need a glossary
            GameplayElementsContainer.GlobalData.Abilities.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Essences.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.EvoPlates.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Gummies.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.HeldItems.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Jewelries.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.KeyItems.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Mints.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Moves.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.MoveDiskLookup.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.Natures.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.PokeBalls.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            GameplayElementsContainer.GlobalData.SandwichLookup.ToList().ForEach(x => glossary[x.Key] = x.Value.GetDescription());
            // Only put the box mons that are still boxed
            foreach (TrainerEntity trainer in GameplayElementsContainer.GlobalData.Trainers.Values)
            {
                foreach (string boxedMonName in trainer.BoxedMons)
                {
                    PokemonEntity boxedMon = GameplayElementsContainer.GlobalData.BoxedMons[boxedMonName];
                    glossary[boxedMonName] = boxedMon.GetDescription();
                }
            }
            // Put current dungeon weathers
            foreach (Dungeon dung in GameplayElementsContainer.GlobalData.Dungeons.Values)
            {
                glossary[dung.CurrentWeather.Name] = dung.CurrentWeather.Description;
            }
            // File complete, save
            List<string> glossarySorted = [.. glossary.Select(x => $"{x.Key},{x.Value}").Order()];
            File.WriteAllLines(Path.Combine(directoryPath, $".gloss"), glossarySorted);
        }
        /// <summary>
        /// Exports all boxed mons but iterates through trainers to remove abandoned/unboxed mons
        /// </summary>
        /// <param name="directoryPath">Path where saved</param>
        public static void ExportAllBoxedMons(string directoryPath)
        {
            StringBuilder fileBuilder = new StringBuilder();
            foreach (TrainerEntity trainer in GameplayElementsContainer.GlobalData.Trainers.Values)
            {
                foreach (string boxedMonName in trainer.BoxedMons)
                {
                    PokemonEntity boxedMon = GameplayElementsContainer.GlobalData.BoxedMons[boxedMonName];
                    fileBuilder.Append($"{boxedMonName},");
                    fileBuilder.Append($"{boxedMon.Name},");
                    fileBuilder.Append($"{boxedMon.Nickname},");
                    fileBuilder.Append($"{boxedMon.PokeBall},");
                    fileBuilder.Append($"{(string.Join(',', boxedMon.Moves.Select(m => m != null ? m.Name : "")))},");
                    fileBuilder.Append($"{boxedMon.Nature},");
                    fileBuilder.Append($"{(string.Join(',', boxedMon.Abilities.Select(a => a != null ? a.Name : "")))}");
                    fileBuilder.AppendLine("");
                }
            }
            // File complete, save
            File.WriteAllText(Path.Combine(directoryPath, $".box"), fileBuilder.ToString());
        }
        /// <summary>
        /// Exports cram type corresponding to all items
        /// </summary>
        /// <param name="directoryPath">Path where saved</param>
        public static void ExportCramTypes(string directoryPath)
        {
            Dictionary<string, PokemonType> cramDict = new Dictionary<string, PokemonType>();
            // Add all elements that need a glossary
            GameplayElementsContainer.GlobalData.Essences.ToList().ForEach(x => cramDict[x.Key] = x.Value.Type);
            GameplayElementsContainer.GlobalData.EvoPlates.ToList().ForEach(x => cramDict[x.Key] = x.Value.Type);
            GameplayElementsContainer.GlobalData.Gummies.ToList().ForEach(x => cramDict[x.Key] = x.Value.Type);
            GameplayElementsContainer.GlobalData.HeldItems.ToList().ForEach(x => cramDict[x.Key] = x.Value.CramType);
            GameplayElementsContainer.GlobalData.KeyItems.ToList().ForEach(x => cramDict[x.Key] = x.Value.CramType);
            GameplayElementsContainer.GlobalData.Mints.ToList().ForEach(x => cramDict[x.Key] = PokemonType.GRASS); // All mints are grass idk
            GameplayElementsContainer.GlobalData.MoveDiskLookup.ToList().ForEach(x => cramDict[x.Key] = x.Value.GetCramType()); // Type of move
            GameplayElementsContainer.GlobalData.PokeBalls.ToList().ForEach(x => cramDict[x.Key] = x.Value.CramType);
            GameplayElementsContainer.GlobalData.SandwichLookup.ToList().ForEach(x => cramDict[x.Key] = x.Value.CramType);
            // File complete, save
            List<string> dictSorted = [.. cramDict.Select(x => $"{x.Key},{x.Value}").Order()];
            File.WriteAllLines(Path.Combine(directoryPath, $".cram"), dictSorted);
        }
        /// <summary>
        /// Rerolls all dungeons diff and weather, and then exports them in alphabetical order to the spreadsheet with all the data
        /// </summary>
        /// <param name="directoryPath"></param>
        public static void RollAndExportDungeons(string directoryPath)
        {
            // Dungeon is a bit harder and more complex, need to reroll difficulties, always allow one dungeon of each diff, and then also roll the weathers
            List<Dungeon> orderedDungeons = [.. GameplayElementsContainer.GlobalData.Dungeons.Values.OrderBy(d => d.Name)]; // Sort them by name!
            List<int> difficulties = [];
            while (difficulties.Count < orderedDungeons.Count) // Fill to at least the number of dungeons
            {
                List<int> nextDiffSet = [1, 2, 3, 4, 5]; // 5 Difficulties total
                GeneralUtilities.ShuffleList(nextDiffSet, GameplayElementsContainer.GlobalData.CommonRng); // Shuffle it
                difficulties.AddRange(nextDiffSet); // Append to diff, this way the diff is random but each level is guaranteed
            }
            StringBuilder fileBuilder = new StringBuilder();
            for (int i = 0; i < orderedDungeons.Count; i++)
            {
                Dungeon nextDungeon = orderedDungeons[i];
                nextDungeon.Difficulty = difficulties[i];
                // Roll the weather by chance
                int weatherIdx = GeneralUtilities.GetRandomWeightedIndex([.. nextDungeon.PossibleWeathers.Select(w => w.Chance)], GameplayElementsContainer.GlobalData.CommonRng);
                nextDungeon.CurrentWeather = nextDungeon.PossibleWeathers[weatherIdx];
                // Now create + append the csv string
                string dangerLevelString = new string('★', nextDungeon.Difficulty);
                fileBuilder.AppendLine($"{nextDungeon.Name},,,,,Danger Level,{dangerLevelString},Weather Report,{nextDungeon.CurrentWeather.Name}");
                fileBuilder.AppendLine(nextDungeon.Description1);
                fileBuilder.AppendLine(nextDungeon.Description2);
                HashSet<string> nextElements = [.. nextDungeon.CommonDrops.Select(c => c.Name)];
                fileBuilder.AppendLine($"Common Items,{string.Join(",", nextElements.ToList().Order())}");
                nextElements = [.. nextDungeon.RareDrops.Select(c => c.Name)];
                fileBuilder.AppendLine($"Rare Items,{string.Join(",", nextElements.ToList().Order())}");
                // Then check pokemon, considering weather is important
                for (int f = 0; f < nextDungeon.Floors.Count; f++)
                {
                    nextElements = [.. nextDungeon.Floors[f].WeatherMons["ALL"]];
                    nextElements.UnionWith([.. nextDungeon.Floors[f].WeatherMons[nextDungeon.CurrentWeather.Name]]);
                    fileBuilder.AppendLine($"Floor {i + 1},{string.Join(",", nextElements.ToList().Order())}");
                }
                // Boss, this has the potential of breaking HARD if params 0/1 are not the boss anymore
                fileBuilder.AppendLine($"Floor 3 Boss,{nextDungeon.FinalBossEncounter.Params[0]},Reward,{nextDungeon.FinalBossEncounter.Params[1]}");
                fileBuilder.AppendLine();
            }
            File.WriteAllText(Path.Combine(directoryPath, $".dung"), fileBuilder.ToString());
        }
    }
}
