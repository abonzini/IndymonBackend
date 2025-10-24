using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using System.Text.Json.Nodes;

namespace MoveParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Path of file to analyse?");
            Console.WriteLine("Replace the whole first line with var Learnsets = {");
            string path = Console.ReadLine();
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found.");
                return;
            }
            string script = File.ReadAllText(path);
            Engine engine = new Engine();
            string resultingCsv = "";
            engine.Execute(script);
            // Access the Learnsets object
            ObjectInstance learnsets = engine.GetValue("Learnsets").AsObject(); // Jint as object not c# object casting...
            Console.WriteLine("Successfully parsed Learnsets.");
            // Now for each mon
            foreach (KeyValuePair<JsValue, PropertyDescriptor> monData in learnsets.GetOwnProperties())
            {
                string moves = monData.Key.ToString();
                // Mon has many weird data but i only care about "learnset" inside
                ObjectInstance monObject = monData.Value.Value.AsObject();
                if (!monObject.HasProperty("learnset"))
                {
                    continue; // Skip mon without learnset
                }
                ObjectInstance monLearnset = monObject.Get("learnset").AsObject();
                Console.WriteLine($"Learnset data for {monData.Key.ToString()}:");
                Console.Write("\t");
                foreach (KeyValuePair<JsValue, PropertyDescriptor> moveData in monLearnset.GetOwnProperties())
                {
                    Console.Write($"{moveData.Key.ToString()} ");
                    moves += "," + moveData.Key.ToString();
                }
                Console.WriteLine(""); // New line
                resultingCsv += moves + "\n"; // Put in csv
            }
            // ok got all, now store the csv
            string csvPath = Directory.GetParent(path).FullName;
            File.WriteAllText(System.IO.Path.Combine(csvPath, "learnsets.csv"), resultingCsv);
        }
    }
}
