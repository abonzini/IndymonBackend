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
        /// <param name="rng">(Optional) Random number generator for shuffling</param>
        public static void ShuffleList<T>(List<T> list, int offset, int number, Random rng = null)
        {
            if (rng == null)
            {
                rng = new Random();
            }
            int n = number;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = rng.Next(n + 1);
                (list[offset + k], list[offset + n]) = (list[offset + n], list[offset + k]); // Swap
            }
        }
    }
}
