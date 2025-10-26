using ParsersAndData;

namespace MoveParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Folder where learnsets.ts and pokedex.ts are located?");
            string path = Console.ReadLine();
            string learnsetPath = Path.Combine(path, "learnsets.ts");
            string dexPath = Path.Combine(path, "pokedex.ts");
            string learnsetCsvPath = Path.Combine(path, "learnsets.csv");
            string abilityCsvPath = Path.Combine(path, "abilities.csv");
            if (!File.Exists(learnsetPath))
            {
                Console.WriteLine("Learnset file not found.");
                return;
            }
            if (!File.Exists(dexPath))
            {
                Console.WriteLine("Dex file not found.");
                return;
            }
            // Get mons first
            Dictionary<string, Pokemon> monData = DexParser.ParseDexFile(dexPath);
            // Update the moves
            MovesetParser.ParseMoves(learnsetPath, monData);
            // Cleanup
            monData = Cleanups.NameAndMovesetCleanup(monData);
            // Finally, write csv
            string resultingCsv = "";
            foreach (Pokemon mon in monData.Values)
            {
                resultingCsv += mon.Name;
                foreach (string move in mon.Moves)
                {
                    resultingCsv += "," + move;
                }
                resultingCsv += "\n";
            }
            File.WriteAllText(learnsetCsvPath, resultingCsv);
            resultingCsv = "";
            foreach (Pokemon mon in monData.Values)
            {
                resultingCsv += mon.Name;
                foreach (string ability in mon.Abilities)
                {
                    resultingCsv += "," + ability;
                }
                resultingCsv += "\n";
            }
            File.WriteAllText(abilityCsvPath, resultingCsv);
        }
    }
}
