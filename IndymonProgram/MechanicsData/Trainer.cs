using System.Text;

namespace MechanicsData
{
    public class Trainer
    {
        public const int MAX_NUMBER_POKEMON = 10;
        public const int MAX_NUMBER_BAG = 19; // Any bag/box/ has 20 items but the first is the label
        public string Name = "";
        public string PictureUrl = "";
        public string DiscordId = "";
        public int Imp = 0;
        public bool AutoTeam = false;
        public bool AutoMoveDisk = false;
        public bool AutoHeldItem = false;
        public bool AutoFavour = false;
        public bool AutoGummy = false;
        public Jewelry EquippedJewelry = null;
        public int EquippedJewelryUses = 0;
        public List<PokemonEntity> Pokemon = new List<PokemonEntity>();
        public Dictionary<Gummy, int> Gummies = new Dictionary<Gummy, int>();
        public Dictionary<MoveDisk, int> MoveDisks = new Dictionary<MoveDisk, int>();
        public Dictionary<HeldItem, int> HeldItems = new Dictionary<HeldItem, int>();
        public Dictionary<Mint, int> Mints = new Dictionary<Mint, int>();
        public Dictionary<EvoPlate, int> EvoPlates = new Dictionary<EvoPlate, int>();
        public Dictionary<Essence, int> Essences = new Dictionary<Essence, int>();
        public Dictionary<KeyItem, int> KeyItems = new Dictionary<KeyItem, int>();
        public Dictionary<PokeBall, int> PokeBalls = new Dictionary<PokeBall, int>();
        public List<Sandwich> Sandwiches = new List<Sandwich>();
        public Dictionary<NpcTrainer, int> Favours = new Dictionary<NpcTrainer, int>();
        public HashSet<string> BoxedMons = new HashSet<string>(); // Boxed mons are just the id as the boxed mon data is global for all trainers
        public override string ToString()
        {
            return Name;
        }
        /// <summary>
        /// Saves the CSV of this trainer per indymon S2 spreadsheet standard
        /// </summary>
        /// <param name="directoryPath">Directory where the csv will be saved</param>
        public void SaveTrainerCsv(string directoryPath)
        {
            StringBuilder fileBuilder = new StringBuilder();
            // Line 1, trainer meta data and one toggle
            fileBuilder.AppendLine($",,{Name},,{PictureUrl},,{DiscordId},,,,,,{Imp},,,,Auto Team,,,{AutoTeam.ToString().ToUpper()},");
            // Line 2, quite empty, just some toggles
            fileBuilder.AppendLine($",,,,,,,,,,,,Auto Move Disks,,,{AutoMoveDisk.ToString().ToUpper()},Auto Held Item,,,{AutoHeldItem.ToString().ToUpper()},");
            // Line 3 is similar to 2 with jewelry too
            fileBuilder.AppendLine($",,{(EquippedJewelry != null ? EquippedJewelry.Name : "Empty Jewelry Slot")},,,,,,,,,{(EquippedJewelry != null ? EquippedJewelryUses : "-")},Auto Gummy,,,{AutoGummy.ToString().ToUpper()},Auto Favour,,,{AutoFavour.ToString().ToUpper()},");
            for (int pokemonRow = 0; pokemonRow < 2; pokemonRow++) // 2 Rows of pokemon
            {
                int pokemonSlotBase = pokemonRow * 5; // Calculate the base of the pokemon that is first in the row
                for (int pokemonLine = 0; pokemonLine < 10; pokemonLine++) // Will do line by line, pokemon have 10 lines each
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    for (int pokemonColumn = 0; pokemonColumn < 5; pokemonColumn++) // 5 columns of pokemon
                    {
                        if (Pokemon.Count <= pokemonSlotBase + pokemonColumn)
                        {
                            // If there's no pokemon, no data to fill, then leave all fields blank
                            lineBuilder.Append(",,,,");
                        }
                        else
                        {
                            PokemonEntity thePokemon = Pokemon[pokemonSlotBase + pokemonColumn];
                            string nextLinePortion = pokemonLine switch
                            {
                                // First, the types for each Pokemon, type is mentioned twice if monotype
                                0 => $"{(thePokemon.Species.Types.Item1 != PokemonType.NONE ? thePokemon.Species.Types.Item1 : thePokemon.Species.Types.Item2)},,{(thePokemon.Species.Types.Item2 != PokemonType.NONE ? thePokemon.Species.Types.Item2 : thePokemon.Species.Types.Item1)},,",
                                // url + nickname
                                1 => $"{thePokemon.Species.ImageUrl},,{((thePokemon.Nickname == "") ? "Nickname" : thePokemon.Nickname)},,",
                                // Species descriptor
                                2 => $",,{thePokemon.Species.Name}{(thePokemon.IsShiny ? "★" : "")},,",
                                // Pokeball and nature
                                3 => $"{thePokemon.PokeBall.Name},,{thePokemon.Nature.Name},,",
                                // Moves
                                4 => $"{(thePokemon.Moves[0] != null ? thePokemon.Moves[0].Name : "No Move")},,{(thePokemon.Moves[1] != null ? thePokemon.Moves[1].Name : "No Move")},,",
                                // Move Disks
                                5 => $"{(thePokemon.MoveDisks[0] != null ? thePokemon.MoveDisks[0].Name : "No Move Disk")},,{(thePokemon.MoveDisks[0] != null ? thePokemon.MoveDisks[1].Name : "No Move Disk")},,",
                                // Abilities (3 of them)
                                6 or 7 or 8 => $"{(thePokemon.Abilities[pokemonLine - 6] != null ? thePokemon.Abilities[pokemonLine - 6].Name : "No Ability")},,,{thePokemon.AbilityActive[pokemonLine - 6].ToString().ToUpper()},",
                                // Item Slot
                                9 => $"{(thePokemon.HeldItem != null ? thePokemon.HeldItem.Name : "Empty Item Slot")},,,,",
                                _ => throw new Exception("Invalid case reached when building trainer card")
                            };
                            lineBuilder.Append(nextLinePortion);
                        }
                    }
                    // Line done, append to csv
                    fileBuilder.AppendLine(lineBuilder.ToString());
                }
            }
            // Finally, items and boxes
            void fillBagLines(string label, List<string> itemWithCountList)
            {
                int nextItemIndex = 0;
                for (int row = 0; row < 2; row++)
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    for (int column = 0; column < 10; column++)
                    {
                        if (column == 0 && row == 0)
                        {
                            lineBuilder.Append($"{label},,"); // If very first, append label
                        }
                        else // Then it's an item slot
                        {
                            if (itemWithCountList.Count <= nextItemIndex) // No item to put, just leave empty
                            {
                                lineBuilder.Append($",,");
                            }
                            else
                            {
                                lineBuilder.Append(itemWithCountList[nextItemIndex]);
                            }
                            nextItemIndex++;
                        }
                    }
                    fileBuilder.AppendLine(lineBuilder.ToString()); // Line finished, add to file
                }
            }
            // Go bag by bag, need to assemble, create array of string + count, order by name
            fillBagLines("GUMMIES:", [.. Gummies.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("MOVE DISKS:", [.. MoveDisks.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("HELD ITEMS:", [.. HeldItems.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("MINTS:", [.. Mints.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("EVO PLATES:", [.. EvoPlates.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("ESSENCES:", [.. Essences.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("KEY/CRAFTING:", [.. KeyItems.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("POKE BALLS:", [.. PokeBalls.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("SANDWICHES:", [.. Sandwiches.Select(k => $"{k.Name},,")]); // Sandwich is instead one by one, no count and order is retained
            fillBagLines("FAVOURS:", [.. Favours.Select(k => $"{k.Key.Name},{k.Value},").Order()]);
            fillBagLines("BOX:", [.. BoxedMons.Select(k => $"{k},,").Order()]); // No value, just string but do order
            // Finally, margins
            fileBuilder.AppendLine(",,,,,,,,,,,,,,,,,,,,.");
            // File complete, save
            File.WriteAllText(Path.Combine(directoryPath, $"{Name.ToUpper()}.trainer"), fileBuilder.ToString());
        }
    }
}
