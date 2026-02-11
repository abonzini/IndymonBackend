using MechanicsData;
using MechanicsDataContainer;

namespace GameData
{
    public class SetItem
    {
        public string Name = "";
        public Ability AddedAbility = null;
        public List<Move> AddedMoves = [];
        public bool AlwaysAllowedItem = true;
        public override string ToString()
        {
            return Name;
        }
        public bool CanEquip(TrainerPokemon mon)
        {
            if (mon.SetItem != null) return false; // Mon already has set item so it can't equip more!
            if (AlwaysAllowedItem) return true; // If its always allowed, then it's fine too
            // Otherwise need to make sure mon can learn every single thing
            Pokemon monData = MechanicsDataContainers.GlobalMechanicsData.Dex[mon.Species];
            bool canEquip = true;
            if (AddedAbility != null)
            {
                canEquip &= monData.Abilities.Contains(AddedAbility);
            }
            foreach (Move addedMove in AddedMoves)
            {
                canEquip &= monData.Moveset.Contains(addedMove);
            }
            return canEquip;
        }
    }
}
