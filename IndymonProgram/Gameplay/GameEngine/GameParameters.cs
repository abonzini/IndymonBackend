using Gameplay.GameplayElements;

namespace Gameplay.GameEngine
{
    public class GameParameters
    {
        public int MaxTurns = 512; /// How many tuens (actions) max this game will do, after this, the game is forced to end to avoid infinite loops
        public bool ForceWin = true; /// If game ended due to max turns, a player win is forced (unless false, in which case it's a tie, mons are added a BUSY status)
        public List<Weather> Weathers = []; /// The weathers in the battlefield (or empty lists)
        public Side FocusedSide = Side.NONE; /// For single-player adventures. Aborts the match if this side loses because no one'll care how it continues
    }
}
