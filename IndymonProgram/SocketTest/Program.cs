using ShowdownBot;
namespace SocketTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BasicShowdownBot bot = new BasicShowdownBot();
            bot.EstablishConnection();
            while (!bot.IsConnected())
            {
                Thread.Sleep(1000);
            }
            Console.WriteLine("Connected detected. try to setup user");
            bot.Login("Indymon_Test", "aaron");
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
