namespace Gameplay.GameEngine
{
    /// <summary>
    /// For returning a game outcome + a rendering summary of the game
    /// </summary>
    public class GameOutcome
    {
        public int WinningTeam = 0; /// Which team won
        public List<TrainerInstance> WinningTrainers = new List<TrainerInstance>(); // All the trainers who won this game
        // TODO also work on redering event queue
    }
}
