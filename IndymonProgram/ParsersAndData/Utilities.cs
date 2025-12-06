namespace ParsersAndData
{
    public static class Utilities
    {
        /// <summary>
        /// Performs a Fischer Yates shuffling of an array
        /// </summary>
        /// <param name="list">List to shuffle</param>
        /// <param name="offset">Which index to start the shuffle</param>
        /// <param name="number">How many elements will be shuffled starting from the index</param>
        /// <param name="rng">Random number generator for shuffling</param>
        public static void ShuffleList<T>(List<T> list, int offset, int number, Random rng)
        {
            int n = number;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = rng.Next(n + 1);
                (list[offset + k], list[offset + n]) = (list[offset + n], list[offset + k]); // Swap
            }
        }
        /// <summary>
        /// Fetches a trainer by name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <param name="backendData">Backend data where trainers are stored</param>
        /// <returns>The trainer</returns>
        public static TrainerData GetTrainerByName(string name, DataContainers backendData)
        {
            if (backendData.TrainerData.TryGetValue(name, out TrainerData result)) { }
            else if (backendData.NpcData.TryGetValue(name, out result)) { }
            else if (backendData.NamedNpcData.TryGetValue(name, out result)) { }
            else throw new Exception("Trainer not found!?");
            return result;
        }
        static readonly Random rng = new Random();
        /// <summary>
        /// Returns the singleton RNG to avoid repeated (fast!) calls to new Random()
        /// </summary>
        /// <returns>The RNG</returns>
        public static Random GetRng()
        {
            return rng;
        }
    }
}
