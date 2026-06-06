namespace ParsersAndData
{
    public static class Cleanups
    {
        /// <summary>
        /// Given a dictionary with raw mon data, will create a dictionary where the keys are the pokemon themselves, and the evos/alternate forms absorbe the moves from the base ones
        /// </summary>
        /// <param name="">Dictionary with raw data (e.g. mons, basic moves, but indexed by tag)</param>
        /// <returns>Dictionary indexed by Name and all mons have all moves</returns>
        public static Dictionary<string, Pokemon> NameAndMovesetCleanup(Dictionary<string, Pokemon> monData)
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
        /// <summary>
        /// Simply replaces all move tags with the real move name
        /// </summary>
        /// <param name="monData">Data of all mons</param>
        /// <param name="moveData">Data of all moves (lookup by tag)</param>
        public static void MoveDataCleanup(Dictionary<string, Pokemon> monData, Dictionary<string, Move> moveData)
        {
            foreach (Pokemon mon in monData.Values)
            {
                HashSet<string> newMoves = new HashSet<string>(); // After cleanup
                HashSet<string> newDamagingStabs = new HashSet<string>();
                foreach (string moveTag in mon.Moves)
                {
                    Move move = moveData[moveTag];
                    newMoves.Add(move.Name);
                    if (move.Damaging && mon.Types.Contains(move.Type)) // If damaging, and stab
                    {
                        newDamagingStabs.Add(move.Name);
                    }
                }
                // Finished processign moves, so now I override mon data
                mon.Moves = newMoves;
                mon.DamagingStabs = newDamagingStabs;
            }
        }
        /// <summary>
        /// Cleans move data to reference dict with the actual name
        /// </summary>
        /// <param name="moveData">Move dictionary</param>
        /// <returns>Clean move dictionary</returns>
        public static Dictionary<string, Move> MoveListCleanup(Dictionary<string, Move> moveData)
        {
            Dictionary<string, Move> cleanDictionary = new Dictionary<string, Move>();
            // First step, re-making of dictionary
            foreach (Move move in moveData.Values) // Get all pokemon from the lookup
            {
                cleanDictionary.Add(move.Name, move);
            }
            return cleanDictionary;
        }
    }
}
