namespace ParsersAndData
{
    public static class ItemParser
    {
        public static Dictionary<string, HashSet<string>> ParseItemAndEffects(string path)
        {
            {
                Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();
                string[] script = File.ReadAllLines(path);
                foreach (string line in script)
                {
                    HashSet<string> allProperties = new HashSet<string>();
                    string[] itemData = line.Split(',');
                    for (int i = 1; i < itemData.Length; i++)
                    {
                        string nextProp = itemData[i].Trim().ToLower();
                        if (nextProp != "")
                        {
                            allProperties.Add(itemData[i].Trim().ToLower());
                        }
                    }
                    result.Add(itemData[0].Trim().ToLower(), allProperties);
                }
                return result;
            }
        }
    }
}
