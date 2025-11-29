namespace ParsersAndData
{
    public static class SpecificSets
    {
        /// <summary>
        /// Returns a set of abilities that are not allowed to be used
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetBannedAbilities()
        {
            return ["moody"];
        }
        /// <summary>
        /// Returns a set of moves that are not allowed to be used
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetBannedMoves()
        {
            return ["sand attack", "double team", "minimize", "hidden power", "flash", "kinesis", "mud-slap", "smokescreen", "fissure", "horn drill", "guillotine", "sheer cold"];
        }
        /// <summary>
        /// Returns a set of abilities considered useless
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetUselessAbilities()
        {
            return ["pickup", "ball fetch", "honey gather", "run away", "telepathy", "frisk"];
        }
        /// <summary>
        /// Returns a set of moves considered useless
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetUselessMoves()
        {
            // Removed because useless
            return ["frustration", "splash", "celebrate", "hold hands"];
        }
        /// <summary>
        /// Returns a set of dancing abilities for checking dance off battles
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetDancingAbilities()
        {
            return ["dancer"];
        }
        /// <summary>
        /// Returns a set of dancing moves for checking dance off battles
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetDancingMoves()
        {
            return ["aqua step", "clangorous soul", "victory dance", "revelation dance", "swords dance", "petal dance", "rain dance", "feather dance", "teeter dance", "dragon dance", "lunar dance", "quiver dance", "fiery dance"];
        }
    }
}
