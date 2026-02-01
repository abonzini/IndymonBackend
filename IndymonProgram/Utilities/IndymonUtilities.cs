using System.Security.Cryptography;

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
        /// <param name="dict">Dictionary to add to</param>
        /// <param name="item">Item to add</param>
        /// <param name="count">How many to add</param>
        /// <param name="eliminateIf0">If subtracting, and the result is less than 0, remove the thing</param>
        /// <returns>The final count of the item</returns>
        public static int AddtemToCountDictionary<T>(Dictionary<T, int> dict, T item, int count = 1, bool eliminateIf0 = false)
        {
            int itemCount = count;
            if (!dict.TryAdd(item, count)) // Try to add if not exists already
            {
                itemCount = dict[item];
                itemCount += count;
                dict[item] = itemCount;
                if (eliminateIf0 && count <= 0) dict.Remove(item);
            }
            return itemCount;
        }
        /// <summary>
        /// Performs a Fischer Yates shuffling of an array
        /// </summary>
        /// <param name="list">List to shuffle</param>
        /// <param name="offset">Which index to start the shuffle</param>
        /// <param name="number">How many elements will be shuffled starting from the index</param>
        public static void ShuffleList<T>(List<T> list, int offset, int number)
        {
            int n = number;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = GetRandomNumber(n + 1);
                (list[offset + k], list[offset + n]) = (list[offset + n], list[offset + k]); // Swap
            }
        }
        /// <summary>
        /// Performs a Fischer Yates shuffling of an array
        /// </summary>
        /// <param name="list">List to shuffle</param>
        public static void ShuffleList<T>(List<T> list)
        {
            ShuffleList(list, 0, list.Count);
        }
        // For the complex RNG
        static readonly List<int> _rngNumbers = [];
        static int _currentRngIndex = 0;
        static readonly SemaphoreSlim _rngSemaphore = new SemaphoreSlim(1, 1);
        const int RNG_LIST_SIZE = 1000;
        const int MAX_INT = 1000000; // Idk
        /// <summary>
        /// Gets random [min-max)
        /// </summary>
        /// <param name="minInclusive">[min</param>
        /// <param name="maxExclusive">max)</param>
        /// <returns></returns>
        public static int GetRandomNumber(int minInclusive, int maxExclusive)
        {
            _rngSemaphore.Wait();
            if (_currentRngIndex >= _rngNumbers.Count)
            {
                _currentRngIndex = 0;
                _rngNumbers.Clear();
                for (int i = 0; i < RNG_LIST_SIZE; i++)
                {
                    _rngNumbers.Add(RandomNumberGenerator.GetInt32(MAX_INT));
                }
            }
            int result = _rngNumbers[_currentRngIndex] % (maxExclusive - minInclusive); // Trim to range
            _currentRngIndex++; // Will check next index later
            result += minInclusive;
            _rngSemaphore.Release();
            return result;
        }
        /// <summary>
        /// Same but [0,max)
        /// </summary>
        /// <param name="maxExclusive">max)</param>
        /// <returns></returns>
        public static int GetRandomNumber(int maxExclusive)
        {
            return GetRandomNumber(0, maxExclusive);
        }
        /// <summary>
        /// Gets random pick of an element from a list
        /// </summary>
        /// <param name="list">List where to choose from</param>
        /// <returns>Element, not removed from list</returns>
        public static T GetRandomPick<T>(List<T> list)
        {
            return list[GetRandomNumber(list.Count)];
        }
        /// <summary>
        /// Gets a random element from a dictionary
        /// </summary>
        /// <param name="dict">The dictionary</param>
        /// <returns>A random key value pick from dictionary</returns>
        public static KeyValuePair<T, U> GetRandomKvp<T, U>(Dictionary<T, U> dict)
        {
            T key = GetRandomPick(dict.Keys.ToList());
            return new KeyValuePair<T, U>(key, dict[key]);
        }
        /// <summary>
        /// Returns an array where every element is the max of all elements of array. Assumes all sub-arrays to be same size
        /// </summary>
        /// <param name="arrays">Array of arrays that contain numbers</param>
        /// <returns>A single array with the max value for each index</returns>
        public static List<double> ArrayMax(List<List<double>> arrays)
        {
            double[] result = new double[arrays[0].Count]; // Prepares an array with the size pre-determined and all arrays need to be this size
            for (int i = 0; i < result.Length; i++)
            {
                double maxValue = double.NegativeInfinity;
                foreach (List<double> array in arrays)
                {
                    maxValue = Math.Max(maxValue, array[i]);
                }
                result[i] = maxValue;
            }
            return [.. result];
        }
        /// <summary>
        /// Returns the average of elements of array
        /// </summary>
        /// <param name="array">Array that contains numbers</param>
        /// <returns>The average value of elements in array</returns>
        public static double ArrayAverage(List<double> array)
        {
            double result = 0;
            foreach (double element in array)
            {
                result += element;
            }
            return result / array.Count;
        }
    }
}
