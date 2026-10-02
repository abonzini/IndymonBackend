namespace Gameplay.GameEngine
{
    public class GameParameters
    {
        public string Weather { get; set; } = ""; /// Weather of this fight, will compare to gameplay element to fetch proper effects
        public int MaxTurns { get; set; } = 512; /// How many tuens (actions) max this game will do, after this, the game is forced to end to avoid infinite loops
        public bool ForceWin { get; set; } = true; /// If game ended due to max turns, a player win is forced (unless false, in which case it's a tie, mons are added a status later on as they "still fighting" or something)
    }
}
