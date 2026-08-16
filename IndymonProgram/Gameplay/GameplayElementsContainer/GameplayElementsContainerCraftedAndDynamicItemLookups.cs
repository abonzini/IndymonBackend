using Gameplay.GameplayElements;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Gets the move disk, tries to generate and add into lookup if didnt exist beforehand
        /// </summary>
        /// <param name="diskName">Name of disk to look for</param>
        /// <returns>The desired move disk, null if input string invalid</returns>
        public MoveDisk GetMoveDisk(string diskName)
        {
            if (!MoveDiskLookup.TryGetValue(diskName, out MoveDisk disk))
            {
                // Disk didn't exist beforehand! Try to generate and add to lookup
                try
                {
                    disk = MoveDisk.Parse(diskName, Moves);
                    MoveDiskLookup.Add(diskName, disk);
                }
                catch
                {
                    disk = null;
                }
            }
            return disk;
        }
        /// <summary>
        /// Gets the sandwich, tries to generate and add into lookup if didnt exist beforehand
        /// </summary>
        /// <param name="sandwichName">Name of sandwich to look for</param>
        /// <returns>The desired sandwich, null if input string invalid</returns>
        public Sandwich GetSandwich(string sandwichName)
        {
            if (!SandwichLookup.TryGetValue(sandwichName, out Sandwich sando))
            {
                // Sandwich didn't exist beforehand! Try to generate and add to lookup
                try
                {
                    sando = Sandwich.Parse(sandwichName);
                    SandwichLookup.Add(sandwichName, sando);
                }
                catch
                {
                    sando = null;
                }
            }
            return sando;
        }
    }
}
