using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rewards;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate))]
public static class CardRewardAlternativePatches
{
    private static readonly PropertyInfo OnSelectProperty =
        AccessTools.Property(typeof(CardRewardAlternative), nameof(CardRewardAlternative.OnSelect));

    private static readonly PropertyInfo AfterSelectedProperty =
        AccessTools.Property(typeof(CardRewardAlternative), nameof(CardRewardAlternative.AfterSelected));

    [HarmonyPostfix]
    public static void Generate_Postfix(CardReward cardReward, ref IReadOnlyList<CardRewardAlternative> __result)
    {
        if (!ShopEnhancementConfig.EnableSkipCardRewardGold) return;

        CardRewardAlternative? skipOption = __result.FirstOrDefault(x => x.OptionId == "Skip");
        if (skipOption == null)
        {
            return;
        }

        Func<Task> onSelect = () => TaskHelper.RunSafely(GiveSkipReward(cardReward.Player));
        OnSelectProperty.SetValue(skipOption, onSelect);
        AfterSelectedProperty.SetValue(skipOption, PostAlternateCardRewardAction.EndSelectionAndCompleteReward);
    }

    private static async Task GiveSkipReward(Player player)
    {
        int gold = ShopEnhancementConfig.SkipCardRewardGoldAmount;
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, player);
        }
    }
}
