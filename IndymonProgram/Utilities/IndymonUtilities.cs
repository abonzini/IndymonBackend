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
        public static void AddtemToDictionary<T>(Dictionary<T, int> dict, T item, int count = 1, bool eliminateIf0 = false)
        {
            if (!dict.TryAdd(item, count)) // Try to add if not exists already
            {
                dict[item] += count;
                if (eliminateIf0 && dict[item] <= 0) dict.Remove(item);
            }
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
        /// Does a ture-false coin flip with P(true)
        /// </summary>
        /// <param name="chance">P(true)</param>
        /// <returns>Result</returns>
        public static bool RandomSuccess(int chance)
        {
            int roll = GetRandomNumber(100);
            return (chance < roll);
        }
        /// <summary>
        /// Returns a random double [0;1)
        /// </summary>
        /// <returns>A random double with uniform distribution</returns>
        public static double RandomDouble()
        {
            // This is some black magic shit
            _rngSemaphore.Wait();
            byte[] bytes = RandomNumberGenerator.GetBytes(8);
            // bit-shift 11 and 53 based on double's mantissa bits
            _rngSemaphore.Release();
            ulong ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            double d = (double)ul / (double)(1UL << 53);
            return d;
        }
        /// <summary>
        /// Returns an index of a list. The list contains the weights so that chance is weighted towards bigger indices. No need to be normalized
        /// </summary>
        /// <param name="weights">List of weight</param>
        /// <param name="power">Optional power to elevate weights, to skew the decision towards higher/lower weights</param>
        /// <returns>A random index within the list. List is modified by the power</returns>
        public static int RandomIndexOfWeights(List<double> weights, double power = 1.0f)
        {
            double totalSum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                double weight = Math.Pow(weights[i], power);
                weights[i] = weight;
                totalSum += weight;
            }
            // Once processed, I'll get a random number, uniform within sum
            double hit = totalSum * RandomDouble();
            // Finally, search for which element is the winner, one by one
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > hit)
                {
                    return i - 1;
                }
            }
            throw new Exception("Impossible chance reached");
        }
    }
}
