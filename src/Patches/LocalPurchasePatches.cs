using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using ShopEnhancement.Compatibility;

namespace ShopEnhancement.Patches;

/// <summary>
/// Entry points for shop purchases started by the local player, where gift mode and the
/// enchant / gift-card services take over the normal purchase. See <see cref="GameCompat"/> for the version split.
/// </summary>
internal static class LocalPurchasePatches
{
	/// <summary>v0.111: every client runs its own purchases through <c>OnTryPurchaseWrapper</c>.</summary>
	[HarmonyPatch]
	private static class LegacyItemPurchase
	{
		private static bool Prepare() => !GameCompat.HasSynchronizedMerchant;

		private static MethodBase TargetMethod()
		{
			return AccessTools.DeclaredMethod(typeof(MerchantEntry), "OnTryPurchaseWrapper", new[] { typeof(MerchantInventory), typeof(bool) })
				?? throw new MissingMethodException(typeof(MerchantEntry).FullName, "OnTryPurchaseWrapper");
		}

		[HarmonyPrefix]
		private static bool Prefix(MerchantEntry __instance, MerchantInventory? inventory, ref Task<bool> __result)
		{
			return GiftPurchasePatches.TryHandleGiftPurchase(__instance, inventory, ref __result);
		}
	}

	/// <summary>v0.111: the card-removal entry has its own wrapper with a <c>cancelable</c> flag.</summary>
	[HarmonyPatch]
	private static class LegacyCardRemovalPurchase
	{
		private static bool Prepare() => !GameCompat.HasSynchronizedMerchant;

		private static MethodBase TargetMethod()
		{
			return AccessTools.DeclaredMethod(typeof(MerchantCardRemovalEntry), "OnTryPurchaseWrapper", new[] { typeof(MerchantInventory), typeof(bool), typeof(bool) })
				?? throw new MissingMethodException(typeof(MerchantCardRemovalEntry).FullName, "OnTryPurchaseWrapper");
		}

		[HarmonyPrefix]
		private static bool Prefix(MerchantCardRemovalEntry __instance, MerchantInventory? inventory, bool ignoreCost, bool cancelable, ref Task<bool> __result)
		{
			return MerchantCardRemovalPatches.TryHandleServicePurchase(__instance, inventory, ignoreCost, cancelable, ref __result);
		}
	}

	/// <summary>
	/// v0.113: the shop UI calls <c>MerchantSynchronizer.PurchaseLocalEntry</c>, which broadcasts the entry id and then
	/// every client replays the purchase. Intercepting here (before the broadcast) lets gift mode and the services
	/// replace the purchase; they replicate themselves via <c>ShopActionSynchronizer</c> instead.
	/// Purchases that the game replays on every client (remote players' purchases, Lord's Parasol) are not touched.
	/// </summary>
	[HarmonyPatch]
	private static class SynchronizedLocalPurchase
	{
		private static bool Prepare() => GameCompat.HasSynchronizedMerchant;

		private static MethodBase TargetMethod()
		{
			Type synchronizer = AccessTools.TypeByName(GameCompat.MerchantSynchronizerTypeName)
				?? throw new TypeLoadException(GameCompat.MerchantSynchronizerTypeName);
			return AccessTools.DeclaredMethod(synchronizer, "PurchaseLocalEntry", new[] { typeof(MerchantEntry) })
				?? throw new MissingMethodException(synchronizer.FullName, "PurchaseLocalEntry");
		}

		[HarmonyPrefix]
		private static bool Prefix(MerchantEntry entry, ref Task<bool> __result)
		{
			MerchantInventory inventory = GameCompat.GetInventoryFor(GameCompat.GetEntryOwner(entry));
			if (entry is MerchantCardRemovalEntry cardRemovalEntry)
			{
				return MerchantCardRemovalPatches.TryHandleServicePurchase(cardRemovalEntry, inventory, ignoreCost: false, GameCompat.IsCardRemovalCancelable(cardRemovalEntry), ref __result);
			}
			return GiftPurchasePatches.TryHandleGiftPurchase(entry, inventory, ref __result);
		}
	}
}
