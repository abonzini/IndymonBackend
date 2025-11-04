using ParsersAndData;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ShowdownBot
{
    public enum BotState
    {
        BLANK,
        CONNECTED,
        PROFILE_INITIALISED,
        CHALLENGING,
        BEING_CHALLENGED,
        IN_FIGHT,
        GAME_DONE
    }

    public class BasicShowdownBot
    {
        public bool Verbose = true;
        BotState CurrentState = BotState.BLANK;
        string _challstr;
        ClientWebSocket _socket = null;
        public ConsoleColor Color = ConsoleColor.White;
        TrainerData _botTrainer;
        DataContainers _backend;
        public string BotName;
        Random _rng = new Random();
        string BattleName;
        public string Winner;
        public int BotRemainingMons;
        GameState _currentGameState;
        public string Challenger;
        /// <summary>
        /// Tries to establish connection to localhost:8000 server and get all i need to actually log in
        /// </summary>
        public void EstablishConnection()
        {
            _socket = new ClientWebSocket();
            _socket.ConnectAsync(new Uri("ws://localhost:8000/showdown/websocket"), default).Wait();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task receiveTask = ReceiveFromSocketAsync(_socket, cts.Token);
        }
        public BotState GetState()
        {
            return CurrentState;
        }
        /// <summary>
        /// Log in as specific trainer
        /// </summary>
        /// <param name="trainer">Trainer class</param>
        public void Login(string trainerName, DataContainers backendData)
        {
            if (CurrentState == BotState.CONNECTED)
            {
                // Fetch trainer and stuff
                _backend = backendData;
                if (_backend.TrainerData.ContainsKey(trainerName)) _botTrainer = _backend.TrainerData[trainerName];
                else if (_backend.NpcData.ContainsKey(trainerName)) _botTrainer = _backend.NpcData[trainerName];
                else if (_backend.NamedNpcData.ContainsKey(trainerName)) _botTrainer = _backend.NamedNpcData[trainerName];
                else throw new Exception("Trainer not found!?");
                BotName = $"Indy_{_botTrainer.Name}";
                // Ok try and connect
                HttpClient client = new HttpClient(); // One use client to get assertion
                string url = $"https://play.pokemonshowdown.com/~~showdown/action.php?act=getassertion&userid={BotName}&challstr={_challstr}";
                string assertion = client.GetStringAsync(url).Result;
                string loginCommand = $"|/trn {BotName},0," + assertion;
                byte[] loginBytes = Encoding.UTF8.GetBytes(loginCommand);
                _socket.SendAsync(new ArraySegment<byte>(loginBytes), WebSocketMessageType.Text, true, default).Wait();
                loginCommand = "|/avatar " + _botTrainer.Avatar;
                loginBytes = Encoding.UTF8.GetBytes(loginCommand);
                _socket.SendAsync(new ArraySegment<byte>(loginBytes), WebSocketMessageType.Text, true, default).Wait();
            }
            else
            {
                throw new Exception("Can only be done the first time after being connected");
            }
        }
        /// <summary>
        /// Challenge a user to a fight
        /// </summary>
        /// <param name="opponentName">Who</param>
        /// <param name="packedTeamData">Team</param>
        /// <param name="format">What format</param>
        public void Challenge(string opponentName, string format, int nMons)
        {
            if (CurrentState == BotState.PROFILE_INITIALISED) // Can only start challenge from idle
            {
                string packedTeamData = _botTrainer.GetPacked(_backend, nMons); // Get packed string of N mons
                string command = "|/utm " + packedTeamData;
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
                command = $"|/challenge {opponentName}, {format}";
                commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
                CurrentState = BotState.IN_FIGHT; // Opp wont reject
            }
        }
        /// <summary>
        /// Accepts challenge from person
        /// </summary>
        /// <param name="opponentName">From who</param>
        /// <param name="packedTeamData">TeamData</param>
        public void AcceptChallenge(string opponentName, int nMons)
        {
            if (CurrentState == BotState.BEING_CHALLENGED) // Can only accept challenge from here
            {
                string packedTeamData = _botTrainer.GetPacked(_backend, nMons); // Get packed string of N mons
                string command = "|/utm " + packedTeamData;
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
                command = $"|/accept {opponentName}";
                commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
                CurrentState = BotState.IN_FIGHT;
            }
        }
        private async Task ReceiveFromSocketAsync(ClientWebSocket socket, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (Verbose)
                {
                    Console.ForegroundColor = Color;
                    Console.WriteLine("Socket received:");
                    Console.WriteLine("\t" + message); // Replace with your handler
                }
                ParseResponse(message);
            }
        }
        private void ParseResponse(string message)
        {
            if (message.Contains("|challstr|"))
            {
                if (CurrentState == BotState.BLANK)
                {
                    string[] chalParts = message.Split('|');
                    _challstr = chalParts[2] + "|" + chalParts[3];
                    CurrentState = BotState.CONNECTED;
                    if (Verbose)
                    {
                        Console.ForegroundColor = Color;
                        Console.WriteLine("Connected successfuly, ready to work");
                    }
                }
                else
                {
                    Console.ForegroundColor = Color;
                    Console.WriteLine("Received a challstr but I don't need it?");
                }
            }
            else if (message.Contains("|updateuser|"))
            {
                if (CurrentState == BotState.CONNECTED)
                {
                    CurrentState = BotState.PROFILE_INITIALISED; // Means profile init was ok??
                }
            }
            else if (message.Contains($"{BotName}|/challenge")) // Someone is challenging me (they won't reject anyway)
            {
                if (CurrentState == BotState.PROFILE_INITIALISED) // Can only accept from here (1 fight at a time)
                {
                    string[] challengeData = message.Split("|");
                    Challenger = challengeData[2]; // Obtain challenger
                    Challenger = Challenger.Trim('~').Trim().ToLower(); // Clean challenger
                    CurrentState = BotState.BEING_CHALLENGED;
                }
            }
            else if (message.Contains("|request|")) // Battle request, bot needs to do a decision
            {
                if (CurrentState == BotState.IN_FIGHT)
                {
                    string[] battleData = message.Split('|');
                    BattleName = battleData[0].Trim('>').Trim();
                    string state = battleData[2]; // This should be the json
                    BotDecision(BattleName, state);
                }
            }
            else if (message.Contains("|win|")) // Battle ended, no matter who won
            {
                Winner = message.Split("|win|")[1].Trim().ToLower();
                BotRemainingMons = _currentGameState.side.GetAliveMons();
                if (BotName.ToLower() != Winner)
                {
                    BotRemainingMons = 0; // Loser got 0 mon
                }
                // Send showteam
                if (Verbose) Console.WriteLine($"{Winner} won, this bot had {BotRemainingMons} mons remaining");
                CurrentState = BotState.GAME_DONE;
                _socket.Dispose();
            }
            else
            {
                // Skip message I guess
            }
        }
        private void BotDecision(string battle, string state)
        {
            _currentGameState = JsonSerializer.Deserialize<GameState>(state);
            PokemonSet currentPokemon = null;
            foreach (SidePokemon pokemon in _currentGameState.side.pokemon) // First, need to parse the current mon state and update
            {
                // Get the first mon of that species (may create trouble if there's duplicates, deal with that later)
                string species = pokemon.details.Split(',')[0].Trim().ToLower(); // Extract name from details
                PokemonSet pokemonInTeam = _botTrainer.Teamsheet.Where(p => p.Species == species).First();
                if (pokemonInTeam.ExplorationStatus != null)
                {
                    pokemonInTeam.ExplorationStatus.SetStatus(pokemon.condition);
                }
                if (pokemon.active) // This is the current mon, definitely
                {
                    currentPokemon = pokemonInTeam;
                }
            }
            string command = "";
            bool forcedSwitch = false;
            if (_currentGameState.forceSwitch != null)
            {
                foreach (bool forceSwitch in _currentGameState.forceSwitch)
                {
                    forcedSwitch |= forceSwitch;
                }
            }
            if (_currentGameState.wait)
            {
                // Dont do anything, just need to wait and dont mess up
            }
            else if (_currentGameState.teamPreview) // We're in team preview stage, nothing to do but to choose the next mon
            {
                command = $"{battle}|/choose default"; // Choose first legal option
            }
            else if (forcedSwitch) // Mon needs to switch no matter what
            {
                command = $"{battle}|/choose default"; // Choose first legal option
            }
            else // Then its probably a move?
            {
                bool invalidChoice;
                do
                {
                    int moveChoice = _rng.Next(0, 4); // 0 -> 3 can be the choice
                    ActiveOptions playOptions = _currentGameState.active.FirstOrDefault();
                    // Move is valid as long its in a valid slot and usable (not disabled, pp)
                    invalidChoice = moveChoice >= playOptions.moves.Count || playOptions.moves[moveChoice].disabled || (playOptions.moves[moveChoice].pp == 0);
                    if (invalidChoice) // If choice invalid, may need to switch randomly
                    {
                        if (!playOptions.trapped) // But only can if not trapped
                        {
                            List<string> switchIns = _currentGameState.side.GetValidSwitchIns();
                            if (switchIns.Count > 0) // Can switch then, so theres a valid move
                            {
                                int switchChoice = _rng.Next(0, switchIns.Count);
                                string switchInMon = switchIns[switchChoice].Split(",")[0];
                                command = $"{battle}|/choose switch {switchInMon}"; // Switch to random mon
                                invalidChoice = false; // Move valid after all
                            }
                        }
                    }
                    else // try the move then
                    {
                        command = $"{battle}|/choose move {moveChoice + 1}"; // Choose move (slot 1-4)
                        string possibleTera = _currentGameState.active.First().canTerastallize.Trim().ToLower();
                        if (possibleTera != "" && (possibleTera == currentPokemon.GetTera(_backend))) // Means the current mon can tera and it's consistent
                        {
                            command += " terastallize";
                        }
                    }
                } while (invalidChoice); // Try again until I get a valid option
            }
            // Whatever the choice, send command unless waiting
            if (!_currentGameState.wait)
            {
                if (Verbose) Console.WriteLine("Sent " + command);
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
            }
        }
    }
}
