using GameData;
using MechanicsData;
using MechanicsDataContainer;

namespace AutomatedTeamBuilder
{
    public static partial class TeamBuilder
    {
        static double GetItemWeight(Item item, ElementType itemType, TrainerPokemon theMon, PokemonBuildContext monCtx, TeamBuildContext buildCtx)
        {
            if (itemType != ElementType.BATTLE_ITEM && itemType != ElementType.MOD_ITEM) throw new ArgumentException("Not a valid type of item being evaluated");
            // Score battle item according to context. First, check if disableds
            double score = 1;
            double aux;
            (ElementType, string) itemNameTag = (itemType, item.Name);
            if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(itemNameTag)) // If item is disabled but not re-enabled, skip it
            {
                if (monCtx.EnabledOptions.TryGetValue(itemNameTag, out aux))
                {
                    score *= aux;
                }
                else
                {
                    score *= 0;
                }
            }
            foreach (ItemFlag flag in item.Flags)
            {
                (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                if (MechanicsDataContainers.GlobalMechanicsData.ForcedBuilds.ContainsKey(flagTag)) // If item is disabled but not re-enabled, skip it
                {
                    if (monCtx.EnabledOptions.TryGetValue(flagTag, out aux))
                    {
                        score *= aux;
                    }
                    else
                    {
                        score *= 0;
                    }
                }
            }
            // Then weight mods
            if (monCtx.WeightMods.TryGetValue(itemNameTag, out aux))
            {
                score *= aux;
            }
            foreach (ItemFlag flag in item.Flags)
            {
                (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                if (monCtx.WeightMods.TryGetValue(flagTag, out aux))
                {
                    score *= aux;
                }
            }
            // And then additives
            if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(itemNameTag, out aux))
            {
                score += aux;
            }
            foreach (ItemFlag flag in item.Flags)
            {
                (ElementType, string) flagTag = (ElementType.ITEM_FLAGS, flag.ToString());
                if (MechanicsDataContainers.GlobalMechanicsData.FlatIncreaseModifiers.TryGetValue(flagTag, out aux))
                {
                    score += aux;
                }
            }
            // Now the improvement based things
            if (itemType == ElementType.BATTLE_ITEM) theMon.BattleItem = item; // First, equip this item to mon
            if (itemType == ElementType.MOD_ITEM) theMon.ModItem = item; // First, equip this item to mon
            PokemonBuildContext newCtx = ObtainPokemonSetContext(theMon, buildCtx); // Check the new context
            double dmgImprovement = newCtx.DamageScore / monCtx.DamageScore; // Add the corresponding utilities
            double defImprovement = Math.Ceiling(newCtx.Survivability) / Math.Ceiling(monCtx.Survivability); // If this makes you survive approx one more hit, it's worth
            double speedImprovement = newCtx.SpeedScore / monCtx.SpeedScore;
            // If needs an improvement, will be accepted as long as some of the improvements succeeds
            int nImprovChecks = 0;
            int nImproveFails = 0;
            if (item.Flags.Contains(ItemFlag.REQUIRES_OFF_INCREASE))
            {
                nImprovChecks++;
                if (dmgImprovement < 1.1) nImproveFails++;
            }
            if (item.Flags.Contains(ItemFlag.REQUIRES_DEF_INCREASE))
            {
                nImprovChecks++;
                if (dmgImprovement < 1.1) nImproveFails++;
            }
            if (item.Flags.Contains(ItemFlag.REQUIRES_SPEED_INCREASE))
            {
                nImprovChecks++;
                if (dmgImprovement < 1.1) nImproveFails++;
            }
            if (nImprovChecks > 0 && nImproveFails == nImprovChecks)
            {
                score *= 0;
            }
            score *= dmgImprovement * defImprovement * speedImprovement; // Then multiply all utilities gain, give or remove utility from final set!
            if (item.Flags.Contains(ItemFlag.BULKY)) // Healing items are scored on whether they can actually make sense on the mon
            {
                score *= newCtx.Survivability / 3; // If you can take 3 hits or more you're officially a bulky mon (because most recovery is 50% based)
            }
            if (itemType == ElementType.BATTLE_ITEM) theMon.BattleItem = null; // Remove item ofc
            if (itemType == ElementType.MOD_ITEM) theMon.ModItem = null;
            return score;
        }
    }
}
