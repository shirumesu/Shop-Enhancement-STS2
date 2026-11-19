using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantRelicEntry))]
internal static class ShopRelicPurchaseLifecyclePatches
{
	[HarmonyPatch("ClearAfterPurchase")]
	[HarmonyPrefix]
	private static void ClearPrefix(MerchantRelicEntry __instance)
	{
		ShopRelicRefreshState.CommitPurchase(__instance);
	}

	[HarmonyPatch("RestockAfterPurchase", new Type[] { typeof(MerchantInventory) })]
	[HarmonyPrefix]
	private static void RestockPrefix(MerchantRelicEntry __instance, MerchantInventory? inventory, out ShopRelicRefreshState.PullScope? __state)
	{
		__state = ShopRelicRefreshState.BeginRestock(__instance, inventory);
	}

	[HarmonyPatch("RestockAfterPurchase", new Type[] { typeof(MerchantInventory) })]
	[HarmonyPostfix]
	private static void RestockPostfix(MerchantRelicEntry __instance, MerchantInventory? inventory, ShopRelicRefreshState.PullScope? __state)
	{
		ShopRelicRefreshState.CompleteRestock(__instance, inventory, __state);
	}

	[HarmonyPatch("RestockAfterPurchase", new Type[] { typeof(MerchantInventory) })]
	[HarmonyFinalizer]
	private static Exception? RestockFinalizer(Exception? __exception, ShopRelicRefreshState.PullScope? __state)
	{
		if (__exception != null)
		{
			ShopRelicRefreshState.CancelPullScope(__state);
		}
		return __exception;
	}
}
