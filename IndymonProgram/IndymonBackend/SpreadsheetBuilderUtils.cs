using MechanicsData;
using MechanicsDataContainer;
using System.Text;

namespace IndymonBackendProgram
{
    public static class SpreadsheetBuilderUtils
    {
        /// <summary>
        /// Exports glossary corresponding to all items and boxed mons
        /// </summary>
        /// <param name="directoryPath">Path where glossary will be saved</param>
        public static void ExportGlossary(string directoryPath)
        {
            StringBuilder fileBuilder = new StringBuilder();
            void appendAll(IEnumerable<string> list)
            {
                foreach (string str in list)
                {
                    fileBuilder.AppendLine(str);
                }
            }
            // Add all elements that need a glossary
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Abilities.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Essences.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.EvoPlates.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Gummies.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.HeldItems.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.KeyItems.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Mints.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Moves.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.MoveDiskLookup.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Natures.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.PokeBalls.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.SandwichLookup.Select(kvp => $"{kvp.Key},{kvp.Value.GetDescription()}"));
            // Only put the box mons that are still boxed
            foreach (Trainer trainer in MechanicsDataContainers.GlobalMechanicsData.Trainers.Values)
            {
                foreach (string boxedMonName in trainer.BoxedMons)
                {
                    PokemonEntity boxedMon = MechanicsDataContainers.GlobalMechanicsData.BoxedMons[boxedMonName];
                    fileBuilder.AppendLine($"{boxedMonName},{boxedMon.GetDescription()}");
                }
            }
            // File complete, save
            File.WriteAllText(Path.Combine(directoryPath, $".gloss"), fileBuilder.ToString());
        }
        /// <summary>
        /// Exports all boxed mons but iterates through trainers to remove abandoned/unboxed mons
        /// </summary>
        /// <param name="directoryPath">Path where box will be saved</param>
        public static void ExportAllBoxedMons(string directoryPath)
        {
            StringBuilder fileBuilder = new StringBuilder();
            foreach (Trainer trainer in MechanicsDataContainers.GlobalMechanicsData.Trainers.Values)
            {
                foreach (string boxedMonName in trainer.BoxedMons)
                {
                    PokemonEntity boxedMon = MechanicsDataContainers.GlobalMechanicsData.BoxedMons[boxedMonName];
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
        /// <param name="directoryPath">Path where glossary will be saved</param>
        public static void ExportCramTypes(string directoryPath)
        {
            StringBuilder fileBuilder = new StringBuilder();
            void appendAll(IEnumerable<string> list)
            {
                foreach (string str in list)
                {
                    fileBuilder.AppendLine(str);
                }
            }
            // Add all elements that need a glossary
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Essences.Select(kvp => $"{kvp.Key},{kvp.Value.Type}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.EvoPlates.Select(kvp => $"{kvp.Key},{kvp.Value.Type}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Gummies.Select(kvp => $"{kvp.Key},{kvp.Value.Type}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.HeldItems.Select(kvp => $"{kvp.Key},{kvp.Value.CramType}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.KeyItems.Select(kvp => $"{kvp.Key},{kvp.Value.CramType}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.Mints.Select(kvp => $"{kvp.Key},{PokemonType.GRASS}")); // All mints are grass idk
            appendAll(MechanicsDataContainers.GlobalMechanicsData.MoveDiskLookup.Select(kvp => $"{kvp.Key},{(kvp.Value.IsRandomMove ? PokemonType.NORMAL : kvp.Value.AddedMove.Type)}")); // Type of move
            appendAll(MechanicsDataContainers.GlobalMechanicsData.PokeBalls.Select(kvp => $"{kvp.Key},{kvp.Value.CramType}"));
            appendAll(MechanicsDataContainers.GlobalMechanicsData.SandwichLookup.Select(kvp => $"{kvp.Key},{kvp.Value.CramType}"));
            // File complete, save
            File.WriteAllText(Path.Combine(directoryPath, $".cram"), fileBuilder.ToString());
        }
    }
}
