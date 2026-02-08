using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace ParsersAndData
{
    public static class MovesetParser
    {
        /// <summary>
        /// Parses the move ts
        /// </summary>
        /// <param name="path">Path to ts</param>
        /// <param name="pokemonLookup">Pokemon list where the moves will be added</param>
        public static void ParseMovests(string path, Dictionary<string, Pokemon> pokemonLookup)
        {
            string script = File.ReadAllText(path);
            Engine engine = new Engine();
            engine.Execute(script);
            // Access the Learnsets object
            ObjectInstance learnsets = engine.GetValue("Learnsets").AsObject(); // Jint as object not c# object casting...
            Console.WriteLine("Successfully parsed Learnsets.");
            // Now for each mon
            foreach (KeyValuePair<JsValue, PropertyDescriptor> monData in learnsets.GetOwnProperties())
            {
                string monName = monData.Key.ToString().ToLower();
                if (pokemonLookup.TryGetValue(monName, out Pokemon pokemon)) // See if this pokemon exists...
                {
                    ObjectInstance monObject = monData.Value.Value.AsObject();
                    if (monObject.HasProperty("learnset"))
                    {
                        // Add learnset of this mon
                        ObjectInstance monLearnset = monObject.Get("learnset").AsObject();
                        foreach (KeyValuePair<JsValue, PropertyDescriptor> moveData in monLearnset.GetOwnProperties())
                        {
                            if (true/*moveData.Value.Value.AsArray().GetOwnProperties().First().Value.Value.ToString().StartsWith("9")*/)
                            {
                                string move = moveData.Key.ToString().ToLower();
                                pokemon.Moves.Add(move);
                            }
                        }
                    }
                }
            }
        }
    }
}
