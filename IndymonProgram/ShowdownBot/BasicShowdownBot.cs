using Newtonsoft.Json;
using ParsersAndData;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
        readonly DataContainers _backend;
        public string BotName;
        string _selfId;
        string _battleName;
        public string Winner;
        public int BotRemainingMons;
        GameState _currentGameState;
        public string Challenger;
        readonly Dictionary<string, PokemonSet> _monsById = new Dictionary<string, PokemonSet>();
        public BasicShowdownBot(DataContainers backend)
        {
            _backend = backend;
        }
        /// <summary>
        /// Tries to establish connection to localhost:8000 server and get all i need to actually log in
        /// </summary>
        public void EstablishConnection()
        {
            _socket = new ClientWebSocket();
            _socket.ConnectAsync(new Uri("ws://localhost:8000/showdown/websocket"), default).Wait();
            CancellationTokenSource cts = new CancellationTokenSource();
            _ = ReceiveFromSocketAsync(_socket, cts.Token);
        }
        public BotState GetState()
        {
            return CurrentState;
        }
        /// <summary>
        /// Log in as specific trainer
        /// </summary>
        /// <param name="trainer">All data for the trainer who will fight</param>
        public void Login(TrainerData trainer)
        {
            if (CurrentState == BotState.CONNECTED)
            {
                _botTrainer = trainer;

                BotName = $"Indy{_botTrainer.Name}".Replace(" ", "").Replace("?", ""); // Fuck you psy
                if (BotName.Length > 18) // Sanitize, name has to be shorter than 19 and no spaces
                {
                    BotName = BotName[..18].Trim();
                }
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
                command = $"|/challenge {opponentName},{format}";
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
                try
                {
                    ParseResponse(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bot {BotName}, processing message {message} threw an exception: {ex}");
                }
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
            else if (message.Contains("|nametaken|")) // Name registration error
            {
                Console.WriteLine($"NAME BOT REGISTRATION ERROR! {message}");
            }
            else if (message.Contains("|request|")) // Battle request, bot needs to do a decision
            {
                if (CurrentState == BotState.IN_FIGHT)
                {
                    string[] battleData = message.Split('|');
                    _battleName = battleData[0].Trim('>').Trim();
                    string state = battleData[2]; // This should be the json
                    BotDecision(_battleName, state);
                }
            }
            else
            {
                // May be actually battle message, so I'll explore further
                // Will tell me which player ID i am... |player|p1|Indytiago|spark-casual|
                MatchCollection matches = Regex.Matches(message, @"\|player\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[2].Value == BotName) // Found myself
                    {
                        _selfId = m.Groups[1].Value;
                        //Console.WriteLine($"Player debug: {BotName} recognized itself as {_selfId}");
                    }
                }
                // Mon switches in with extra data, capture all types of switching in
                matches = Regex.Matches(message, @"\|(?:switch|drag|detailschange|replace)\|([^|]+)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        string monSpecies = m.Groups[2].Value.Trim().ToLower(); // Species of the mon
                        string status = m.Groups[3].Value.Trim().ToLower(); // Hp status
                        //Console.WriteLine($"Switch debug: {monId}->{monSpecies}->{status}");
                        if (!_monsById.TryGetValue(monId, out PokemonSet pokemonInTeam)) // Id not known yet
                        {
                            // Try all I can to identify my mon
                            pokemonInTeam = _botTrainer.Teamsheet.FirstOrDefault(p => p.NickName == monId) // Ideally nickname
                                ?? _botTrainer.Teamsheet.FirstOrDefault(p => p.Species == monId || p.Species == monSpecies); // Otherwise scramble towards any identifier I can get
                            //Console.WriteLine($"Switch debug added {monId} key");
                            _monsById.Add(monId, pokemonInTeam);
                        }
                        pokemonInTeam.ExplorationStatus?.SetStatus(status);
                    }
                }
                // Mon receives direct damage or hp change
                matches = Regex.Matches(message, @"\|(?:-damage|-heal)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        string status = m.Groups[2].Value.Trim().ToLower(); // Hp status
                        //Console.WriteLine($"Damage debug: {monId}->{status}");
                        PokemonSet pokemonInTeam = _monsById[monId];
                        pokemonInTeam.ExplorationStatus?.SetStatus(status);
                    }
                }
                // Mon's HP is changed for some reason (i honestly don't believe this is used)
                matches = Regex.Matches(message, @"\|(?:-sethp)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        string status = m.Groups[2].Value.Trim().ToLower(); // Hp (status blank i guess?)
                        PokemonSet pokemonInTeam = _monsById[monId];
                        pokemonInTeam.ExplorationStatus?.SetStatus(status);
                    }
                }
                // Mon's nonvolatile status is changed (mon gained status)
                matches = Regex.Matches(message, @"\|(?:-status)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        string status = m.Groups[2].Value.Trim().ToLower(); // Non-volatile status
                        PokemonSet pokemonInTeam = _monsById[monId];
                        if (pokemonInTeam.ExplorationStatus != null)
                        {
                            pokemonInTeam.ExplorationStatus.NonVolatileStatus = status;
                        }
                    }
                }
                // Mon's nonvolatile status is cured
                matches = Regex.Matches(message, @"\|(?:-curestatus)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        string status = m.Groups[2].Value.Trim().ToLower(); // Non-volatile status cured
                        PokemonSet pokemonInTeam = _monsById[monId];
                        if (pokemonInTeam.ExplorationStatus != null && pokemonInTeam.ExplorationStatus.NonVolatileStatus == status)
                        {
                            pokemonInTeam.ExplorationStatus.NonVolatileStatus = "";
                        }
                    }
                }
                // Mon's fainted
                matches = Regex.Matches(message, @"\|(?:faint)\|([^|]+)\|([^|]+)");
                foreach (Match m in matches)
                {
                    if (m.Groups[1].Value.Contains(_selfId)) // If this switch corresponds to one of my guys, may contain HP info too
                    {
                        string monId = m.Groups[1].Value.Split(':')[1].Trim().ToLower(); // Id of the mon in question
                        PokemonSet pokemonInTeam = _monsById[monId];
                        //Console.WriteLine($"Faint debug: {monId}");
                        if (pokemonInTeam.ExplorationStatus != null)
                        {
                            pokemonInTeam.ExplorationStatus.HealthPercentage = 1;
                            pokemonInTeam.ExplorationStatus.NonVolatileStatus = "";
                        }
                    }
                }
                // If a player won game, this is relevant to end the simulation
                if (message.Contains("|win|")) // Battle ended, no matter who won
                {
                    Winner = message.Split("|win|")[1].Trim().ToLower();
                    BotRemainingMons = _currentGameState.Side.GetAliveMons();
                    if (BotName.ToLower() != Winner)
                    {
                        BotRemainingMons = 0; // Loser got 0 mon
                    }
                    // Send showteam
                    //Console.WriteLine($"{Winner} won, this bot had {BotRemainingMons} mons remaining");
                    CurrentState = BotState.GAME_DONE;
                    _socket.Dispose();
                }
            }
        }
        private void BotDecision(string battle, string state)
        {
            _currentGameState = JsonConvert.DeserializeObject<GameState>(state);
            PokemonSet currentPokemon = null;
            foreach (SidePokemon pokemon in _currentGameState.Side.Pokemon) // First, need to parse the current mon state and update
            {
                // Get the first mon of that species (may create trouble if there's duplicates, deal with that later)
                string species = pokemon.Details.Split(',')[0].Trim().ToLower(); // Extract name from details
                PokemonSet pokemonInTeam = _botTrainer.Teamsheet.Where(p => p.Species == species).First();
                if (pokemon.Active) // This is the current mon, definitely
                {
                    currentPokemon = pokemonInTeam;
                    if (currentPokemon.ExplorationStatus != null)
                    {
                        // Means the active field also has data of the moves current PP, load here
                        foreach (AvailableMove move in _currentGameState.Active[0].Moves)
                        {
                            int moveIndex = Array.IndexOf(currentPokemon.Moves, move.Move.Trim().ToLower());
                            currentPokemon.ExplorationStatus.MovePp[moveIndex] = move.Pp;
                        }
                    }
                }
            }
            string command = "";
            bool forcedSwitch = false;
            if (_currentGameState.ForceSwitch != null)
            {
                foreach (bool forceSwitch in _currentGameState.ForceSwitch)
                {
                    forcedSwitch |= forceSwitch;
                }
            }
            if (_currentGameState.Wait)
            {
                // Dont do anything, just need to wait and dont mess up
            }
            else if (_currentGameState.TeamPreview) // We're in team preview stage, nothing to do but to choose the next mon
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
                    int moveChoice = RandomNumberGenerator.GetInt32(0, 4); // 0 -> 3 can be the choice
                    ActiveOptions playOptions = _currentGameState.Active.FirstOrDefault();
                    // Move is valid as long its in a valid slot and usable (not disabled, pp)
                    invalidChoice = moveChoice >= playOptions.Moves.Count || playOptions.Moves[moveChoice].Disabled || (playOptions.Moves[moveChoice].Pp == 0);
                    if (invalidChoice) // If choice invalid, may need to switch randomly
                    {
                        if (!playOptions.Trapped) // But only can if not trapped
                        {
                            List<int> switchIns = _currentGameState.Side.GetValidSwitchIns();
                            if (switchIns.Count > 0) // Can switch then, so theres a valid move
                            {
                                int switchChoice = RandomNumberGenerator.GetInt32(0, switchIns.Count);
                                int switchedInMon = switchIns[switchChoice];
                                command = $"{battle}|/choose switch {switchedInMon}"; // Switch to random mon
                                invalidChoice = false; // Move valid after all
                            }
                        }
                    }
                    else // try the move then
                    {
                        command = $"{battle}|/choose move {moveChoice + 1}"; // Choose move (slot 1-4)
                        string possibleTera = _currentGameState.Active.First().CanTerastallize.Trim().ToLower();
                        if (possibleTera != "" && (possibleTera == currentPokemon.GetTera(_backend))) // Means the current mon can tera and it's consistent
                        {
                            command += " terastallize";
                        }
                    }
                } while (invalidChoice); // Try again until I get a valid option
            }
            // Whatever the choice, send command unless waiting
            if (!_currentGameState.Wait)
            {
                if (Verbose) Console.WriteLine("Sent " + command);
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                _socket.SendAsync(new ArraySegment<byte>(commandBytes), WebSocketMessageType.Text, true, default).Wait();
            }
        }
    }
}
