using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using System.Diagnostics;

namespace ParsersAndData
{
    public enum MoveFlags
    {
        DRAIN,
        HEAL,
        CHANCE,
        PIVOT,
        SOUND,
        SETUP,
        DEFENSE_DAMAGE, // Body press lol
        WEIGHT_DAMAGE,
        ENEMY_ATTACK_DAMAGE, // Foul Play lol
        WEIRD_DAMAGE, // This ones idk just give them an average of 60 whatever
        MULTI_HIT,
        PUNCH,
        BULLET,
        SHARP,
        DANCE,
        SHEER_FORCE,
        CONTACT,
        STRONG_JAW,
        RECOIL,
        LAUNCHER,
        EXPLOSIVE,
        PRIORITY
    }
    public enum MoveCategory
    {
        PHYSICAL,
        SPECIAL,
        STATUS
    }
    public enum Stat
    {
        HP,
        ATTACK,
        DEFENSE,
        SPECIAL_ATTACK,
        SPECIAL_DEFENSE,
        SPEED
    }
    public class Move
    {
        public string Name { get; set; } = "";
        public string TagName = "";
        public string Type { get; set; } = "";
        public bool Damaging { get; set; } = false;
        public int Bp { get; set; } = -1;
        public Stat DamagingStat { get; set; } = Stat.HP;
        public Dictionary<Stat, int> SetupStages { get; set; } = new Dictionary<Stat, int>();

        public override string ToString()
        {
            return Name;
        }
    }
    public static class MoveParser
    {
        /// <summary>
        /// Parses the moves ts into a dex structure with lookup
        /// </summary>
        /// <param name="path">Path to ts file</param>
        /// <returns>The created lookup of move data</returns>
        public static Dictionary<string, Move> ParseMoves(string path)
        {
            Dictionary<string, Move> result = new Dictionary<string, Move>();

            var psi = new ProcessStartInfo
            {
                FileName = "tsc",
                Arguments = $"\"{path}\" --target ES5 --module none --outFile output.js",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p.WaitForExit();
            string js = File.ReadAllText("output.js");

            Engine engine = new Engine();
            engine.Execute(js);
            // Access the Learnsets object
            ObjectInstance dex = engine.GetValue("Moves").AsObject(); // Jint as object not c# object casting...
            Console.WriteLine("Successfully parsed Moves.");
            // Now for each move
            foreach (KeyValuePair<JsValue, PropertyDescriptor> moveData in dex.GetOwnProperties())
            {
            }

            return result;
        }
    }
}