namespace MechanicsData
{
    public class PokemonEntity
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
            return (Nickname != "") ? $"{Nickname} ({Species})" : Species.Name;
        }
        public string GetInformalName()
        {
            return (Nickname != "") ? Nickname : Species.Name;
        }
    }
}
