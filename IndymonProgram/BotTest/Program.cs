using Newtonsoft.Json;
using ShowdownBot;
namespace BotTest
{
    internal class Program
    {
        static void Main()
        {
            string indymonFile = "C:\\Users\\augus\\Documents\\Indymon\\IndymonBackEnd\\indy.mon";
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            };
            SessionData dataCont = JsonConvert.DeserializeObject<SessionData>(File.ReadAllText(indymonFile), settings);
            BotBattle battle = new BotBattle(dataCont.DataContainer);
            battle.SimulateBotBattle(dataCont.DataContainer.TrainerData["tiago"], 3);
        }
    }
}
