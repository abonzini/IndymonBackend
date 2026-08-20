using Gameplay.GameplayElements;
using Utilities;

namespace Gameplay.GameplayElementsContainer
{
    public partial class GameplayElementsContainer
    {
        /// <summary>
        /// Full randomization of a Pokemon's battle stats and the sort, for the beginning of new games or when catching/fighting npcs
        /// </summary>
        /// <param name="mon">Mon to fully randomize (only battle stuff!)</param>
        /// <param name="rng">Rng to use</param>
        public void RandomizePokemon(PokemonEntity mon, Random rng)
        {
            // Naturally, some aspects like speices, nickname, pokeball are not changed here
            mon.Nature = GeneralUtilities.GetRandomPick([.. Natures.Values], rng);
            List<Move> availableMoves = [.. mon.Species.Moveset];
            List<Move> availableStabs = [.. availableMoves]; // TODO Can only find stabs once moves have more info ofc
            // Move 1 is always a stab (if available)
            if (availableStabs.Count > 0) mon.Moves[0] = GeneralUtilities.GetRandomPick(availableStabs, rng);
            if (mon.Moves[0] != null) availableMoves.Remove(mon.Moves[0]); // Remove from list, to reroll move 2
            // Reroll move 2
            if (availableMoves.Count > 0) mon.Moves[1] = GeneralUtilities.GetRandomPick(availableMoves, rng);
            // Then abilities, simpler
            List<Ability> availableAbilities = [.. mon.Species.Abilities];
            GeneralUtilities.ShuffleList(availableAbilities, rng); // Shuffle it
            mon.Abilities = [null, null, null]; // Blank list
            for (int i = 0; i < mon.Abilities.Length && i < availableAbilities.Count; i++) // Add as many as possible
            {
                mon.Abilities[i] = availableAbilities[i];
            }
        }
        /// <summary>
        /// Equips a gummy to the desired pokemon. Slot will be used for a tiebreaker, otherwise ignored
        /// </summary>
        /// <param name="mon">Which mon to give gummy</param>
        /// <param name="gummy">Which gummy to give</param>
        /// <param name="slot">In case of monotype, which slot (1 or 2) to give, unused otherwise</param>
        public static void EquipGummyToPokemon(PokemonEntity mon, Gummy gummy, int slot)
        {
            if (mon.Species.Types.Item1 == mon.Species.Types.Item2) // Monotype pokemon detected, then use the slot
            {
                if (slot == 0) throw new Exception("Cant equip a gummy in slot 0, it's always active!");
                if (mon.Species.Types.Item1 == gummy.Type) // If gummy is of correct type, activate the desired ability
                {
                    mon.AbilityActive[slot] = true;
                }
            }
            else // Then just check which slot the activation goes
            {
                if (mon.Species.Types.Item1 == gummy.Type)
                {
                    mon.AbilityActive[1] = true;
                }
                if (mon.Species.Types.Item2 == gummy.Type)
                {
                    mon.AbilityActive[2] = true;
                }
            }
        }
    }
}

