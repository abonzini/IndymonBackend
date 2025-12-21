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
            return ["sand attack", "double team", "minimize", // Evasion
                "hidden power", // Hidden Power
                "flash", "kinesis", "mud-slap", "smokescreen", // Guaranteed Accuracy 
                "fissure", "horn drill", "guillotine", "sheer cold"]; // OHKO
        }
        /// <summary>
        /// Returns a set of abilities considered useless
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetUselessAbilities()
        {
            return ["pickup", "ball fetch", "honey gather", "run away", // Do nothing
                "telepathy", // Doubles
                "frisk", "forewarn"]; // Useless in auto-combat
        }
        /// <summary>
        /// Returns a set of moves considered useless
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetUselessMoves()
        {
            // Removed because useless
            return ["frustration",  // Low damage for no reason
                "splash", "celebrate", "hold hands", // No effect
            "snore", "sleep talk", "dream eater", "swallow", "spit up", // Most of the time is useless
            "ally switch", "follow me", "helping hand", // Doubles
            "natural gift", "fling", "belch", // Too item-dependant
            "tackle", "scratch", "pound"]; // Low level normals
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
