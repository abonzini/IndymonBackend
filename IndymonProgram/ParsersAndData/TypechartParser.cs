using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace ParsersAndData
{
    public static class TypeChartParser
    {
        /// <summary>
        /// Parses the typechart ts into a float dictionary with damagemultipliers
        /// </summary>
        /// <param name="path">Path to ts file with the first line renamed</param>
        /// <returns>Defensive typechart data, for each type, how much damage receives from another</returns>
        public static Dictionary<string, Dictionary<string, float>> ParseTypechartFile(string path)
        {
            Dictionary<string, Dictionary<string, float>> result = new Dictionary<string, Dictionary<string, float>>();
            string script = File.ReadAllText(path);
            Engine engine = new Engine();
            engine.Execute(script);
            // Access the Learnsets object
            ObjectInstance dex = engine.GetValue("Typechart").AsObject(); // Jint as object not c# object casting...
            Console.WriteLine("Successfully parsed Typechart data.");
            // Now for each mon
            foreach (KeyValuePair<JsValue, PropertyDescriptor> typeData in dex.GetOwnProperties())
            {
                Dictionary<string, float> thisTypeEffectiveness = new Dictionary<string, float>();
                string type = typeData.Key.ToString(); // Extract the tag as the name tag
                ObjectInstance typeObject = typeData.Value.Value.AsObject(); // Jint as object not c# object casting...
                Console.WriteLine("Successfully parsed type defensive chart.");
                if (typeObject.HasProperty("damageTaken"))
                {
                    ObjectInstance defTypes = typeObject.Get("damageTaken").AsObject();
                    foreach (KeyValuePair<JsValue, PropertyDescriptor> defData in defTypes.GetOwnProperties())
                    {
                        string defMoveType = defData.Key.ToString().ToLower();
                        float moveMultiplier = defData.Value.Value.AsNumber() switch
                        {
                            0 => 1.0f,
                            1 => 2.0f,
                            2 => 0.5f,
                            3 => 0f,
                            _ => 1.0f
                        };
                        thisTypeEffectiveness.Add(defMoveType, moveMultiplier);
                    }
                }
                result.Add(type, thisTypeEffectiveness);
            }
            return result;
        }
    }
}
