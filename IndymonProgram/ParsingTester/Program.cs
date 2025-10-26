using ParsersAndData;

namespace ParsingTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Folder where learnsets.ts and pokedex.ts are located?");
            string path = Console.ReadLine();
            string learnsetPath = Path.Combine(path, "learnsets.ts");
            string dexPath = Path.Combine(path, "pokedex.ts");
            string movesPath = Path.Combine(path, "moves.csv");
            string typeChartFile = Path.Combine(path, "typechart.ts");
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
            if (!File.Exists(movesPath))
            {
                Console.WriteLine("Moves file not found.");
                return;
            }
            if (!File.Exists(typeChartFile))
            {
                Console.WriteLine("Typechart file not found.");
                return;
            }
            // Get mons first
            Dictionary<string, Pokemon> monData = DexParser.ParseDexFile(dexPath);
            // Update the moves
            MovesetParser.ParseMoves(learnsetPath, monData);
            // Cleanup from name and inherited moves
            monData = Cleanups.NameAndMovesetCleanup(monData);
            // Parse moves
            Dictionary<string, Move> moveData = MoveParser.ParseMoves(movesPath);
            Cleanups.MoveDataCleanup(monData, moveData);
            // Parse typechart
            Dictionary<string, Dictionary<string, float>> typechart = TypeChartParser.ParseTypechartFile(typeChartFile);
        }
    }
}
