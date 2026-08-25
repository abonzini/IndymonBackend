namespace Gameplay.GameEngine
{
    /// <summary>
    /// For returning a game outcome + a rendering summary of the game
    /// </summary>
    public class GameOutcome
    {
        public Side WinningTeam = 0; /// Which side won
        public List<TrainerInstance> WinningTrainers = new List<TrainerInstance>(); // All the trainers who won this game
        // TODO also work on redering event queue
    }
}
