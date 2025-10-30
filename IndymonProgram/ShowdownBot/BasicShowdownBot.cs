using System.Net.WebSockets;
using System.Text;

namespace ShowdownBot
{
    public class BasicShowdownBot
    {
        bool _verbose;
        bool _connected = false;
        string _challstr;
        ClientWebSocket _socket = null;
        /// <summary>
        /// Tries to establish connection to localhost:8000 server and get all i need to actually log in
        /// </summary>
        public void EstablishConnection(bool verbose = true)
        {
            _verbose = verbose;
            _socket = new ClientWebSocket();
            _socket.ConnectAsync(new Uri("ws://localhost:8000/showdown/websocket"), default).Wait();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task receiveTask = ReceiveFromSocketAsync(_socket, cts.Token);
        }
        public bool IsConnected()
        {
            return _connected;
        }
        public void Login(string name, string avatar)
        {
            if (_connected)
            {
                HttpClient client = new HttpClient(); // One use client to get assertion
                string url = $"https://play.pokemonshowdown.com/~~showdown/action.php?act=getassertion&userid={name}&challstr={_challstr}";
                string assertion = client.GetStringAsync(url).Result;
                string loginCommand = $"|/trn {name},0," + assertion;
                byte[] loginBytes = Encoding.UTF8.GetBytes(loginCommand);
                _socket.SendAsync(new ArraySegment<byte>(loginBytes), WebSocketMessageType.Text, true, default).Wait();
                Thread.Sleep(1000);
                loginCommand = "|/avatar " + avatar;
                loginBytes = Encoding.UTF8.GetBytes(loginCommand);
                _socket.SendAsync(new ArraySegment<byte>(loginBytes), WebSocketMessageType.Text, true, default).Wait();
            }
            else
            {
                throw new Exception("Not connected");
            }
        }
        private async Task ReceiveFromSocketAsync(ClientWebSocket socket, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (_verbose)
                {
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
                string[] chalParts = message.Split('|');
                _challstr = chalParts[2] + "|" + chalParts[3];
                _connected = true;
                if (_verbose)
                {
                    Console.WriteLine("Connected successfuly, ready to work");
                }
            }
        }
    }
}
