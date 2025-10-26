namespace ParsersAndData
{
    public class Move
    {
        public string Name = "";
        public string TagName = "";
        public string Type = "";
        public bool Damaging = false;

        public override string ToString()
        {
            return Name;
        }
    }
    public static class MoveParser
    {
        /// <summary>
        /// Parses the moves csv (shivzy) into a dex structure with lookup
        /// </summary>
        /// <param name="path">Path to csv file</param>
        /// <returns>The created lookup of move data</returns>
        public static Dictionary<string, Move> ParseMoves(string path)
        {
            Dictionary<string, Move> result = new Dictionary<string, Move>();
            string[] script = File.ReadAllLines(path);
            List<string> firstLine = script[0].Split(',').ToList();
            for (int field = 0; field < firstLine.Count; field++) // Cleanup
            {
                firstLine[field] = firstLine[field].Trim().ToLower();
            }
            int rawNameIndex = firstLine.IndexOf("raw name");
            int nameIndex = firstLine.IndexOf("name");
            int typeIndex = firstLine.IndexOf("type");
            int catIndex = firstLine.IndexOf("category");

            for (int moveIndex = 1; moveIndex < script.Length; moveIndex++)
            {
                Move nextMove = new Move();
                string[] line = script[moveIndex].Split(",");
                nextMove.TagName = line[rawNameIndex].ToLower();
                nextMove.Name = line[nameIndex].ToLower();
                nextMove.Type = line[typeIndex].ToLower();
                string cat = line[catIndex].ToLower();
                if (cat == "physical" || cat == "special")
                {
                    nextMove.Damaging = true;
                }
                if (!result.ContainsKey(nextMove.TagName))
                {
                    result.Add(nextMove.TagName, nextMove);
                }
            }
            return result;
        }
    }
}