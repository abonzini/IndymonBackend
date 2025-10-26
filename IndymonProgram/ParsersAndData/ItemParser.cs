namespace ParsersAndData
{
    public static class ItemParser
    {
        public static Dictionary<string, string> ParseItemAndTypes(string path)
        {
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                string[] script = File.ReadAllLines(path);
                foreach (string line in script)
                {
                    string[] itemData = line.Split(',');
                    result.Add(itemData[0].ToLower(), itemData[1].ToLower());
                }
                return result;
            }
        }
    }
}
