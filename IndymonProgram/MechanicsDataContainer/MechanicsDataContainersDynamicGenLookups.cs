using MechanicsData;

namespace MechanicsDataContainer
{
    public partial class MechanicsDataContainers
    {
        /// <summary>
        /// Gets the move disk, generates and adds into lookup if didnt exist beforehand
        /// </summary>
        /// <param name="diskName">Name of disk to look for</param>
        /// <returns>The desired move disk</returns>
        public MoveDisk GetMoveDisk(string diskName)
        {
            if (!MoveDiskLookup.TryGetValue(diskName, out MoveDisk disk))
            {
                // Disk didn't exist beforehand! Generate and add to lookup
                disk = MoveDisk.Parse(diskName, Moves);
                MoveDiskLookup.Add(diskName, disk);
            }
            return disk;
        }
        /// <summary>
        /// Gets the sandwich, generates and adds into lookup if didnt exist beforehand
        /// </summary>
        /// <param name="sandwichName">Name of sandwich to look for</param>
        /// <returns>The desired sandwich</returns>
        public Sandwich GetSandwich(string sandwichName)
        {
            if (!SandwichLookup.TryGetValue(sandwichName, out Sandwich sando))
            {
                // Sandwich didn't exist beforehand! Generate and add to lookup
                sando = Sandwich.Parse(sandwichName);
                SandwichLookup.Add(sandwichName, sando);
            }
            return sando;
        }
    }
}
