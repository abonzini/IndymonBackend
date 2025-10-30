using ParsersAndData;

namespace ShowdownBot
{
    public class BotBattle
    {
        DataContainers _backend;
        public BotBattle(DataContainers backend)
        {
            _backend = backend;
        }

        /// <summary>
        /// Runs a bot battle
        /// </summary>
        /// <param name="player1">Who's the player 1?</param>
        /// <param name="player2">Who's player 2?</param>
        /// <param name="nMons">How many mons per side?</param>
        /// <returns>The score</returns>
        public (int, int) SimulateBotBattle(string player1, string player2, int nMons1, int nMons2)
        {
            BasicShowdownBot challengerBot = new BasicShowdownBot();
            challengerBot.Verbose = false;
            BasicShowdownBot receiverBot = new BasicShowdownBot();
            receiverBot.Verbose = false;
            challengerBot.EstablishConnection();
            receiverBot.EstablishConnection();
            while ((challengerBot.GetState() != BotState.CONNECTED) || (challengerBot.GetState() != BotState.CONNECTED))
            {
                Thread.Sleep(5); // Wait until connected
            }
            challengerBot.Login(player1, _backend);
            receiverBot.Login(player2, _backend);
            // Wait until ok
            while ((challengerBot.GetState() != BotState.PROFILE_INITIALISED) || (receiverBot.GetState() != BotState.PROFILE_INITIALISED))
            {
                Thread.Sleep(5);
            }
            // Now I challenge
            Thread.Sleep(10);
            receiverBot.Challenge(challengerBot.BotName, "Custom Game", nMons2);
            while ((challengerBot.GetState() != BotState.GAME_DONE) || (receiverBot.GetState() != BotState.GAME_DONE))
            {
                if (challengerBot.GetState() == BotState.BEING_CHALLENGED)
                {
                    challengerBot.AcceptChallenge(receiverBot.BotName, nMons1);
                    // And that's it, they'll playe
                }
                Thread.Sleep(5);
            }
            // Game's done
            return (challengerBot.BotRemainingMons, receiverBot.BotRemainingMons);
        }
    }
}
