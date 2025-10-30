using ShowdownBot;
using System.Text.Json;
namespace SocketTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string indymonFile = "C:\\Users\\augus\\Documents\\Indymon\\IndymonBackEnd\\indy.mon";
            DataContainers dataCont = JsonSerializer.Deserialize<DataContainers>(File.ReadAllText(indymonFile));
            BotBattle battle = new BotBattle(dataCont);
            Console.WriteLine(battle.SimulateBotBattle("bot", "bot2", 2, 2));
        }
    }
}
