using GameData;
using GameDataContainer;

namespace IndymonBackendProgram
{
    public static class IndymonUtilities
    {
        /// <summary>
        /// Returns a trainer from a string containing the trainer's name
        /// </summary>
        /// <param name="name">Name of trainer</param>
        /// <returns>The trainer instance</returns>
        public static Trainer GetTrainerByName(string name)
        {
            if (GameDataContainers.GlobalGameData.TrainerData.TryGetValue(name, out Trainer trainer)) { }
            else if (GameDataContainers.GlobalGameData.NpcData.TryGetValue(name, out trainer)) { }
            else if (GameDataContainers.GlobalGameData.FamousNpcData.TryGetValue(name, out trainer)) { }
            else throw new Exception("Trainer not found!?");
            return trainer;
        }
    }
}
