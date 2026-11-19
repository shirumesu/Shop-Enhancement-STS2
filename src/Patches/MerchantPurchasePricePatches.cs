using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;

namespace ShopEnhancement.Patches;

/// <summary>Remembers what a relic / potion was bought for, so selling it later can use that price.</summary>
[HarmonyPatch]
internal static class MerchantPurchasePricePatches
{
	// The per-entry purchase step: "OnTryPurchase" on v0.111, renamed "OnTryPurchaseInternal" on v0.113.
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return ResolvePurchaseStep(typeof(MerchantRelicEntry));
		yield return ResolvePurchaseStep(typeof(MerchantPotionEntry));
	}

	private static MethodBase ResolvePurchaseStep(Type entryType)
	{
		Type[] parameters = { typeof(MerchantInventory), typeof(bool) };
		return AccessTools.DeclaredMethod(entryType, "OnTryPurchaseInternal", parameters)
			?? AccessTools.DeclaredMethod(entryType, "OnTryPurchase", parameters)
			?? throw new MissingMethodException(entryType.FullName, "OnTryPurchaseInternal / OnTryPurchase");
	}

	[HarmonyPostfix]
	private static void TrackPurchase(MerchantEntry __instance, ref Task<(bool success, int goldSpent)> __result)
	{
		switch (__instance)
		{
		case MerchantRelicEntry { Model: { } relic }:
			__result = RecordPurchase(__result, goldSpent => MerchantPurchasePriceState.Set(relic, goldSpent));
			break;
		case MerchantPotionEntry { Model: { } potion }:
			__result = RecordPurchase(__result, goldSpent => MerchantPurchasePriceState.Set(potion, goldSpent));
			break;
		}
	}

	private static async Task<(bool success, int goldSpent)> RecordPurchase(Task<(bool success, int goldSpent)> purchaseTask, Action<int> record)
	{
		var result = await purchaseTask;
		if (result.success)
		{
			record(result.goldSpent);
		}
		return result;
	}
}
