using System.Globalization;

namespace Utilities
{
    public static class GeneralUtilities
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
        /// <param name="dict">Dictionary to add to</param>
        /// <param name="item">Item to add</param>
        /// <param name="count">How many to add</param>
        /// <param name="maxKeys">How many uniquekeys this dict can have</param>
        /// <returns>The final count of the item</returns>
        public static int AddtemToCountDictionary<T>(Dictionary<T, int> dict, T item, int count, int maxKeys = int.MaxValue)
        {
            int itemCount = count;
            if (!dict.TryAdd(item, count)) // Try to add if not exists already
            {
                itemCount = dict[item];
                itemCount += count;
                dict[item] = itemCount;
                if (itemCount <= 0) // Remove the item if negative
                {
                    dict.Remove(item);
                    itemCount = 0;
                }
            }
            else
            {
                if (dict.Count > maxKeys) // The addition of this item caused a key overflow
                {
                    dict.Remove(item);
                    itemCount = 0;
                }
            }
            return itemCount;
        }
        /// <summary>
        /// Shuffle a list with F-Y but with a deterministic (i.e. repeatable) rng
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">List to shuffle</param>
        /// <param name="offset">Where to start shuffle</param>
        /// <param name="number">How many elements to shuffle</param>
        /// <param name="rng">The rng</param>
        public static void ShuffleList<T>(List<T> list, int offset, int number, Random rng)
        {
            rng ??= new Random(); // New rng if passed a null one
            int n = number;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = rng.Next(n + 1);
                (list[offset + k], list[offset + n]) = (list[offset + n], list[offset + k]); // Swap
            }
        }
        /// <summary>
        /// Shuffle a list with F-Y but with a deterministic (i..e repeatable) rng
        /// </summary>
        /// <param name="list">List to shuffle</param>
        /// <param name="rng">The rng</param>
        public static void ShuffleList<T>(List<T> list, Random rng)
        {
            ShuffleList(list, 0, list.Count, rng);
        }
        /// <summary>
        /// Gets random [min-max)
        /// </summary>
        /// <param name="minInclusive">[min</param>
        /// <param name="maxExclusive">max)</param>
        /// <param name="rng">The rng</param>
        /// <returns>Random int</returns>
        public static int GetRandomNumber(int minInclusive, int maxExclusive, Random rng)
        {
            rng ??= new Random(); // New rng if passed a null one
            int result = rng.Next(minInclusive, maxExclusive);
            return result;
        }
        /// <summary>
        /// Same but [0,max)
        /// </summary>
        /// <param name="maxExclusive">max)</param>
        /// <param name="rng">The rng</param>
        /// <returns>Random int</returns>
        public static int GetRandomNumber(int maxExclusive, Random rng)
        {
            return GetRandomNumber(0, maxExclusive, rng);
        }
        /// <summary>
        /// Gets random pick of an element from a list
        /// </summary>
        /// <param name="list">List where to choose from</param>
        /// <param name="rng">The rng</param>
        /// <returns>Element, not removed from list</returns>
        public static T GetRandomPick<T>(List<T> list, Random rng)
        {
            return list[GetRandomNumber(list.Count, rng)];
        }
        /// <summary>
        /// Gets a random element from a dictionary
        /// </summary>
        /// <param name="dict">The dictionary</param>
        /// <param name="rng">The rng</param>
        /// <returns>A random key value pick from dictionary</returns>
        public static KeyValuePair<T, U> GetRandomKvp<T, U>(Dictionary<T, U> dict, Random rng)
        {
            T key = GetRandomPick(dict.Keys.ToList(), rng);
            return new KeyValuePair<T, U>(key, dict[key]);
        }
        /// <summary>
        /// Creates a string that is the APA capitalization of an input string
        /// </summary>
        /// <param name="str">String to capitalize</param>
        /// <returns>A new APA capitalized string</returns>
        public static string ApaCapitalize(string str)
        {
            string[] strings = str.Split(' ');
            for (int i = 0; i < strings.Length; i++)
            {
                string newString = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strings[i].ToLower());
                strings[i] = newString;
            }
            return string.Join(" ", strings);
        }
    }
}
