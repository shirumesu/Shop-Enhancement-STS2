using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static class ShopNoPurchasePatches
{
	private static readonly HashSet<ulong> PlayersPurchasedInCurrentShop = new HashSet<ulong>();

	[HarmonyPatch(typeof(MerchantRoom), "EnterInternal")]
	[HarmonyPostfix]
	public static void EnterInternal_Postfix()
	{
		PlayersPurchasedInCurrentShop.Clear();
	}

	[HarmonyPatch(typeof(Hook), "AfterItemPurchased")]
	[HarmonyPostfix]
	public static void AfterItemPurchased_Postfix(Player player)
	{
		PlayersPurchasedInCurrentShop.Add(player.NetId);
	}

	[HarmonyPatch(typeof(MerchantRoom), "Exit")]
	[HarmonyPrefix]
	public static void Exit_Prefix(IRunState? runState)
	{
		if (ShopEnhancementConfig.EnableNoPurchaseReward && runState != null)
		{
			Player? me = LocalContext.GetMe(runState);
			if (me != null && !PlayersPurchasedInCurrentShop.Contains(me.NetId))
			{
				int rewardGold = ShopEnhancementConfig.NoPurchaseRewardGold;
				TaskHelper.RunSafely(PlayerCmd.GainGold(rewardGold, me));
				// Only the local player's own client grants this, so tell the other clients (same as other gold rewards).
				RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(rewardGold);
				SfxCmd.Play("event:/sfx/ui/rewards/rewards_gold");
			}
		}
	}
}
