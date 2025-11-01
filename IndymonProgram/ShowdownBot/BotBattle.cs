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
            BasicShowdownBot acceptBot = new BasicShowdownBot();
            acceptBot.Verbose = false;
            BasicShowdownBot challengeBot = new BasicShowdownBot();
            challengeBot.Verbose = false;
            acceptBot.EstablishConnection();
            challengeBot.EstablishConnection();
            while ((acceptBot.GetState() != BotState.CONNECTED) || (challengeBot.GetState() != BotState.CONNECTED))
            {
                Thread.Sleep(5); // Wait until connected
            }
            acceptBot.Login(player1, _backend);
            challengeBot.Login(player2, _backend);
            // Wait until ok
            while ((acceptBot.GetState() != BotState.PROFILE_INITIALISED) || (challengeBot.GetState() != BotState.PROFILE_INITIALISED))
            {
                Thread.Sleep(5);
            }
            // Now I challenge
            Thread.Sleep(10);
            challengeBot.Challenge(acceptBot.BotName, "Custom Game", nMons2);
            while ((acceptBot.GetState() != BotState.GAME_DONE) || (challengeBot.GetState() != BotState.GAME_DONE))
            {
                if (acceptBot.GetState() == BotState.BEING_CHALLENGED)
                {
                    acceptBot.AcceptChallenge(acceptBot.Challenger, nMons1);
                    // And that's it, they'll playe
                }
                Thread.Sleep(5);
            }
            // Game's done
            return (acceptBot.BotRemainingMons, challengeBot.BotRemainingMons);
        }
        /// <summary>
        /// Acceptbattle bot that battles against me
        /// </summary>
        /// <param name="player1">Bot player</param>
        /// <param name="nMons1">Number of mons</param>
        /// <returns></returns>
        public int SimulateBotBattle(string player1, int nMons1)
        {
            BasicShowdownBot acceptBot = new BasicShowdownBot();
            acceptBot.Verbose = false;
            acceptBot.EstablishConnection();
            while ((acceptBot.GetState() != BotState.CONNECTED))
            {
                Thread.Sleep(5); // Wait until connected
            }
            acceptBot.Login(player1, _backend);
            // Wait until ok
            while ((acceptBot.GetState() != BotState.PROFILE_INITIALISED))
            {
                Thread.Sleep(5);
            }
            // Now I wait for challenge
            while ((acceptBot.GetState() != BotState.GAME_DONE))
            {
                if (acceptBot.GetState() == BotState.BEING_CHALLENGED)
                {
                    acceptBot.AcceptChallenge(acceptBot.Challenger, nMons1);
                    // And that's it, they'll playe
                }
                Thread.Sleep(5);
            }
            // Game's done
            return acceptBot.BotRemainingMons;
        }
    }
}
