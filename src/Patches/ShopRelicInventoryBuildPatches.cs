using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantInventory), "CreateForNormalMerchant")]
internal static class ShopRelicInventoryBuildPatches
{
	[HarmonyPrefix]
	private static void Prefix(Player player, out ShopRelicRefreshState.PullScope? __state)
	{
		__state = ShopRelicRefreshState.BeginInventoryBuild(player);
	}

	[HarmonyPostfix]
	private static void Postfix(MerchantInventory __result, ShopRelicRefreshState.PullScope? __state)
	{
		ShopRelicRefreshState.CompleteInventoryBuild(__result, __state);
	}

	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception, ShopRelicRefreshState.PullScope? __state)
	{
		if (__exception != null)
		{
			ShopRelicRefreshState.CancelPullScope(__state);
		}
		return __exception;
	}
}
