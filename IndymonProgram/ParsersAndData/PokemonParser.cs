using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace ParsersAndData
{
    public class Pokemon
    {
        public string Name { get; set; } = "";
        public string TagName = "";
        public HashSet<string> Types { get; set; } = new HashSet<string>();
        public HashSet<string> Abilities { get; set; } = new HashSet<string>();
        public string Prevo = "";
        public string OriginalForm = "";
        public HashSet<string> Evos { get; set; } = new HashSet<string>();
        public HashSet<string> Moves { get; set; } = new HashSet<string>();
        public HashSet<string> DamagingStabs { get; set; } = new HashSet<string>();
        public HashSet<string> AiMoveBanlist { get; set; } = new HashSet<string>();
        public HashSet<string> AiAbilityBanlist { get; set; } = new HashSet<string>();
        public override string ToString()
        {
            return $"{Name} ({TagName})";
        }
    }
    public static class DexParser
    {
        /// <summary>
        /// Parses the dex ts into a dex structure with lookup
        /// </summary>
        /// <param name="path">Path to ts file with the first line renamed</param>
        /// <returns>The created lookup of pokemon data</returns>
        public static Dictionary<string, Pokemon> ParseDexFile(string path)
        {
            Dictionary<string, Pokemon> result = new Dictionary<string, Pokemon>();
            string script = File.ReadAllText(path);
            Engine engine = new Engine();
            engine.Execute(script);
            // Access the Learnsets object
            ObjectInstance dex = engine.GetValue("Dex").AsObject(); // Jint as object not c# object casting...
            Console.WriteLine("Successfully parsed Dex.");
            // Now for each mon
            foreach (KeyValuePair<JsValue, PropertyDescriptor> monData in dex.GetOwnProperties())
            {
                Pokemon nextPokemon = new Pokemon();
                nextPokemon.TagName = monData.Key.ToString(); // Extract the tag as the name tag
                // Get the data for this mon
                ObjectInstance monValues = monData.Value.Value.AsObject();
                if (monValues.HasProperty("name"))
                {
                    nextPokemon.Name = monValues.Get("name").AsString().ToLower();
                }
                if (monValues.HasProperty("types"))
                {
                    JsArray typesArray = monValues.Get("types").AsArray();
                    for (int i = 0; i < typesArray.Length; i++)
                    {
                        nextPokemon.Types.Add(typesArray.Get(i).AsString().ToLower());
                    }
                }
                if (monValues.HasProperty("abilities"))
                {
                    ObjectInstance abilitiesObj = monValues.Get("abilities").AsObject();
                    foreach (KeyValuePair<JsValue, PropertyDescriptor> ability in abilitiesObj.GetOwnProperties())
                    {
                        JsValue abilityValue = ability.Value.Value;
                        nextPokemon.Abilities.Add(abilityValue.AsString().ToLower());
                    }
                }
                if (monValues.HasProperty("prevo"))
                {
                    nextPokemon.Prevo = monValues.Get("prevo").AsString().ToLower();
                }
                if (monValues.HasProperty("baseSpecies"))
                {
                    nextPokemon.OriginalForm = monValues.Get("baseSpecies").AsString().ToLower();
                }
                if (monValues.HasProperty("evos"))
                {
                    JsArray typesArray = monValues.Get("evos").AsArray();
                    for (int i = 0; i < typesArray.Length; i++)
                    {
                        nextPokemon.Evos.Add(typesArray.Get(i).AsString().ToLower());
                    }
                }
                // Finally add to result
                result.Add(nextPokemon.TagName, nextPokemon);
            }
            return result;
        }
    }
}
