using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

/// <summary>
/// Gift mode: buying a card / relic / potion sends it to the selected teammate instead.
/// Called from <see cref="LocalPurchasePatches"/> for purchases the local player starts; the purchase itself is
/// replicated to every client by <see cref="ShopActionSynchronizer"/>.
/// </summary>
public static class GiftPurchasePatches
{
	/// <summary>Returns false when the purchase was taken over (Harmony prefix convention).</summary>
	internal static bool TryHandleGiftPurchase(MerchantEntry __instance, MerchantInventory? inventory, ref Task<bool> __result)
	{
		if (__instance is not (MerchantCardEntry or MerchantRelicEntry or MerchantPotionEntry))
		{
			return true;
		}
		if (inventory == null)
		{
			return true;
		}
		if (!ShopEnhancementConfig.EnableGiftMode)
		{
			return true;
		}
		if (!GiftModeState.IsEnabled(inventory))
		{
			return true;
		}
		ulong targetPlayerId = GiftModeState.GetTargetPlayerId(inventory);
		if (targetPlayerId == 0L || targetPlayerId == inventory.Player.NetId)
		{
			return true;
		}
		__result = HandleGiftPurchase(__instance, inventory, targetPlayerId);
		return false;
	}

	private static async Task<bool> HandleGiftPurchase(MerchantEntry entry, MerchantInventory inventory, ulong targetId)
	{
		Player player = inventory.Player;
		int cost = entry.Cost;
		ShopGiftMessage? giftMessage = CreateGiftMessage(entry, inventory, targetId, cost);
		if (giftMessage == null)
		{
			// Nothing giftable in this slot (e.g. an empty card entry): refuse instead of charging for nothing.
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
			return false;
		}
		if (player.Gold < cost)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
			return false;
		}
		ShopActionSynchronizer synchronizer = ShopActionSynchronizer.Current;
		if (entry is MerchantPotionEntry { Model: { } potion })
		{
			// A potion the target cannot hold would be paid for and then silently dropped, so refuse up front.
			Player target = synchronizer.GetGiftTarget(targetId);
			PurchaseStatus status = ShopActionSynchronizer.CheckCanReceivePotion(target, potion);
			if (status != PurchaseStatus.Success)
			{
				entry.InvokePurchaseFailed(status);
				ShopEnhancementNetwork.ShowGiftRefused(target, status);
				return false;
			}
		}
		await synchronizer.DoLocalGift(giftMessage);
		return true;
	}

	private static ShopGiftMessage? CreateGiftMessage(MerchantEntry entry, MerchantInventory inventory, ulong targetId, int cost)
	{
		var message = new ShopGiftMessage
		{
			TargetId = targetId,
			EntryIndex = ShopActionSynchronizer.GetGiftableEntries(inventory).IndexOf(entry),
			GoldCost = cost
		};
		switch (entry)
		{
		case MerchantCardEntry { CreationResult: { } creationResult }:
			message.ItemType = GiftItemType.Card;
			message.ItemId = creationResult.Card.Id.ToString();
			message.UpgradeCount = creationResult.Card.CurrentUpgradeLevel;
			break;
		case MerchantRelicEntry { Model: { } relic }:
			message.ItemType = GiftItemType.Relic;
			message.ItemId = relic.Id.ToString();
			break;
		case MerchantPotionEntry { Model: { } potion }:
			message.ItemType = GiftItemType.Potion;
			message.ItemId = potion.Id.ToString();
			break;
		default:
			return null;
		}
		return message.EntryIndex >= 0 ? message : throw new System.InvalidOperationException("Gift entry is not part of the buyer's shop inventory.");
	}
}
