namespace Utilities
{
    public static class IndymonUtilities
    {
        /// <summary>
        /// Gets a csv from a google sheets id+tab combo
        /// </summary>
        /// <param name="sheetId">Id</param>
        /// <param name="sheetTab">Tab</param>
        /// <returns>The csv</returns>
        public static string GetCsvFromGoogleSheets(string sheetId, string sheetTab)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={sheetTab}";
            using HttpClient client = new HttpClient();
            return client.GetStringAsync(url).GetAwaiter().GetResult();
        }
        /// <summary>
        /// General method to add an item to a dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        public static void AddtemToDictionary<T>(Dictionary<T, int> dict, T item, int count = 1)
        {
            if (!dict.TryAdd(item, count)) // Try to add if not exists already
            {
                dict[item] += count;
            }
        }
    }
}
