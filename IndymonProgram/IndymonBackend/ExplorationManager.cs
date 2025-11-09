using ParsersAndData;

namespace IndymonBackend
{
    public class ExplorationManager
    {
        DataContainers _backEndData = null;
        public string Dungeon { get; set; }
        public string Trainer { get; set; }
        public ExplorationManager(DataContainers backEndData)
        {
            _backEndData = backEndData;
        }
        public void InitializeExploration()
        {
            // First ask organizer to choose dungeon
            List<string> options = _backEndData.Dungeons.Keys.ToList();
            Console.WriteLine("Creating a brand new exploration, which dungeon?");
            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"{i + 1}: {options[i]}, ");
            }
            Console.WriteLine("");
            Dungeon = options[int.Parse(Console.ReadLine()) - 1];
            // Then which player
            options = _backEndData.TrainerData.Keys.ToList();
            Console.WriteLine("Which trainer?");
            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"{i + 1}: {options[i]}, ");
            }
            Trainer = options[int.Parse(Console.ReadLine()) - 1];
            // Finally, try to define teamsheet
            TrainerData trainerData = _backEndData.TrainerData[Trainer];
            trainerData.DefineSets(_backEndData, int.MaxValue, true, true); // Gets the team for everyone, this time it has no mon limit, and mons initialised in exploration mode (with HP and status)
        }
    }
}
