namespace Gameplay.GameplayElements
{
    public class PokemonEntity : GameplayElement
    {
        public PokemonSpecies Species = null;
        public string Nickname = "";
        public bool IsShiny = false;
        public PokeBall PokeBall = null;
        public Nature Nature = null;
        public Move[] Moves = [null, null]; // The intrinsic moves of the mon
        public MoveDisk[] MoveDisks = [null, null]; // Same with move disks
        public bool[] MoveDisksChosen = [false, false];
        public Ability[] Abilities = [null, null, null]; // Abilities is always a 3-element list
        public bool[] AbilityActive = [true, false, false];
        public bool[] GummyChosen = [false, false, false];
        public HeldItem HeldItem = null;
        public bool HeldItemChosen = false;

        public bool Borrowed = false;
        public override string ToString()
        {
            return Nickname != "" ? $"{Nickname} ({Name})" : Name;
        }
        public override string GetDescription()
        {
            // Glossary for pokemon will be used only for boxed mons so it'll need to explain more or less what the mon has it going for it.
            string description = ToString() + " ";
            description += $"{Nature.Name} Nature in a {PokeBall.Name}. ";
            List<string> moves = [.. Moves.Where(m => m != null).Select(m => m.Name)];
            if (moves.Count > 0)
            {
                description += $"MOVES: {string.Join(" + ", moves)}. ";
            }
            List<string> abilities = [.. Abilities.Where(a => a != null).Select(a => a.Name)];
            if (abilities.Count > 0)
            {
                description += $"ABILITIES: {string.Join(" + ", abilities)}. ";
            }
            return description.Trim();
        }
    }
}
