using IndymonBackend;
using ShowdownBot;
using System.Text.Json;
namespace SocketTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string indymonFile = "C:\\Users\\augus\\Documents\\Indymon\\IndymonBackEnd\\indy.mon";
            IndymonData dataCont = JsonSerializer.Deserialize<IndymonData>(File.ReadAllText(indymonFile));
            BotBattle battle = new BotBattle(dataCont.DataContainer);
            //Console.WriteLine(battle.SimulateBotBattle("bot", "bot2", 2, 2));
            battle.SimulateBotBattle("bot", 3);
        }
    }
}
