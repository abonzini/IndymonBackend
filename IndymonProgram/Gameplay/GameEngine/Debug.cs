namespace Gameplay.GameEngine
{
    /// <summary>
    /// Manages the debug level of the system, so that it pauses but only for events of debug lvl or lower
    /// </summary>
    public enum DebugLevel
    {
        NONE = 0,
        EVENT_ORGANIZER = 1,
    }
    public static class Debug
    {
        public static void DebugAction(DebugLevel lvl, Action action)
        {
            if (lvl <= GameplayElementsContainer.GameplayElementsContainer.GlobalData.CurrentDebugLevel)
            {
                action();
                Console.ReadLine();
            }
        }
    }
}
