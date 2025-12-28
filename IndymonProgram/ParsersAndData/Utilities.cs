using System.Security.Cryptography;

namespace ParsersAndData
{
    public static class Utilities
    {
        /// <summary>
        /// Performs a Fischer Yates shuffling of an array
        /// </summary>
        /// <param name="list">List to shuffle</param>
        /// <param name="offset">Which index to start the shuffle</param>
        /// <param name="number">How many elements will be shuffled starting from the index</param>
        public static void ShuffleList<T>(List<T> list, int offset, int number)
        {
            int n = number;
            while (n > 1) // Fischer yates
            {
                n--;
                int k = RandomNumberGenerator.GetInt32(n + 1);
                (list[offset + k], list[offset + n]) = (list[offset + n], list[offset + k]); // Swap
            }
        }
        /// <summary>
        /// Fetches a trainer by name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <param name="backendData">Backend data where trainers are stored</param>
        /// <returns>The trainer</returns>
        public static TrainerData GetTrainerByName(string name, DataContainers backendData)
        {
            if (backendData.TrainerData.TryGetValue(name, out TrainerData result)) { }
            else if (backendData.NpcData.TryGetValue(name, out result)) { }
            else if (backendData.NamedNpcData.TryGetValue(name, out result)) { }
            else throw new Exception("Trainer not found!?");
            return result;
        }
        /// <summary>
        /// Creates the dialog box to ask the organiser to choose one specific trainer
        /// </summary>
        /// <param name="settings">Settigns to filter valid trainers</param>
        /// <param name="backendData">Data to find trainers and stuff</param>
        /// <returns></returns>
        public static TrainerData ChooseOneTrainerDialog(TeambuildSettings settings, DataContainers backendData)
        {
            Console.WriteLine("Which trainer group? 1 Players, 2 NPCs, 3 Famous NPCs");
            int choice = int.Parse(Console.ReadLine());
            List<TrainerData> trainers = choice switch
            {
                1 => [.. backendData.TrainerData.Values.Where(t => t.GetValidTeamComps(backendData, 1, int.MaxValue, settings).Count > 0)],
                2 => [.. backendData.NpcData.Values.Where(t => t.GetValidTeamComps(backendData, 1, int.MaxValue, settings).Count > 0)],
                3 => [.. backendData.NamedNpcData.Values.Where(t => t.GetValidTeamComps(backendData, 1, int.MaxValue, settings).Count > 0)],
                _ => throw new Exception("Invalid trainer group")
            };
            Console.WriteLine("Which trainer?");
            for (int i = 0; i < trainers.Count; i++)
            {
                Console.Write($"{i + 1}: {trainers[i].Name}, ");
            }
            // Finally, try to define teamsheet
            return trainers[int.Parse(Console.ReadLine()) - 1];
        }
    }
}
