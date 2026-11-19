using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Rewards;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(CardRewardAlternative), "Generate")]
public static class CardRewardAlternativePatches
{
	private static readonly PropertyInfo OnSelectProperty = AccessTools.Property(typeof(CardRewardAlternative), "OnSelect");

	private static readonly PropertyInfo AfterSelectedProperty = AccessTools.Property(typeof(CardRewardAlternative), "AfterSelected");

	[HarmonyPostfix]
	public static void Generate_Postfix(CardReward cardReward, ref IReadOnlyList<CardRewardAlternative> __result)
	{
		if (!ShopEnhancementConfig.EnableSkipCardRewardGold)
		{
			return;
		}
		CardRewardAlternative? skipAlternative = __result.FirstOrDefault(x => x.OptionId == "Skip");
		if (skipAlternative != null)
		{
			Func<Task> onSelect = () => GiveSkipReward(cardReward.Player);
			OnSelectProperty.SetValue(skipAlternative, onSelect);
			AfterSelectedProperty.SetValue(skipAlternative, PostAlternateCardRewardAction.EndSelectionAndCompleteReward);
		}
	}

	private static async Task GiveSkipReward(Player player)
	{
		int goldAmount = ShopEnhancementConfig.SkipCardRewardGoldAmount;
		if (goldAmount > 0)
		{
			// CardReward.OnSelect runs on every client (remote clients replay the owner's choice through
			// PlayerChoiceSynchronizer), so this already happens everywhere for the reward's owner.
			// Do NOT add RewardSynchronizer.SyncLocalObtainedGold here: it would count the gold twice and credit it to
			// whichever player is local on each client.
			await PlayerCmd.GainGold(goldAmount, player);
		}
	}
}
