namespace ParsersAndData
{
    public static class DexCleanup
    {
        /// <summary>
        /// Given a dictionary with raw mon data, will create a dictionary where the keys are the pokemon themselves, and the evos/alternate forms absorbe the moves from the base ones
        /// </summary>
        /// <param name="">Dictionary with raw data (e.g. mons, basic moves, but indexed by tag)</param>
        /// <returns>Dictionary indexed by Name and all mons have all moves</returns>
        public static Dictionary<string, Pokemon> Cleanup(Dictionary<string, Pokemon> monData)
        {
            Dictionary<string, Pokemon> cleanDictionary = new Dictionary<string, Pokemon>();
            // First step, re-making of dictionary
            foreach (Pokemon mon in monData.Values) // Get all pokemon from the lookup
            {
                cleanDictionary.Add(mon.Name, mon);
            }
            // Next, move inheritance
            foreach (Pokemon mon in monData.Values)
            {
                // Check prevo
                if (cleanDictionary.TryGetValue(mon.Prevo, out Pokemon prevo))
                {
                    mon.Moves.UnionWith(prevo.Moves); // Add prevo moves
                }
                // Check original form
                if (cleanDictionary.TryGetValue(mon.OriginalForm, out Pokemon originalForm))
                {
                    mon.Moves.UnionWith(originalForm.Moves); // Add original form moves
                }
            }
            // Cleanup complete, return
            return cleanDictionary;
        }
    }
}
