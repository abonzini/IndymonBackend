namespace Gameplay.GameEngine
{
    /// <summary>
    /// For returning a game outcome + a rendering summary of the game
    /// </summary>
    public class GameOutcome
    {
        public Side WinningTeam = Side.FIELD; /// Which side won
        // TODO also work on redering event queue
    }
}
