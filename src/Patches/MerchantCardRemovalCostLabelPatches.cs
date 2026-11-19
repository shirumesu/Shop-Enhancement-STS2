using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;

using ShopEnhancement.Compatibility;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantCardRemoval), "UpdateVisual")]
public static class MerchantCardRemovalCostLabelPatches
{
	[HarmonyPostfix]
	public static void UpdateVisual_Postfix(NMerchantCardRemoval __instance)
	{
		if (!GameCompat.TryGetLocalInventory(out MerchantInventory? localInventory))
		{
			return;
		}
		var state = MerchantCardRemovalPatches.EnsureServiceState(localInventory);
		if (!state.IsEnchantShop && !state.IsGiftCardShop)
		{
			return;
		}
		MerchantCardRemovalEntry? cardRemovalEntry = localInventory.CardRemovalEntry;
		if (cardRemovalEntry != null && !cardRemovalEntry.Used)
		{
			MegaLabel? costLabel = __instance.GetNodeOrNull<MegaLabel>("%CostLabel");
			Control? costContainer = __instance.GetNodeOrNull<Control>("Cost");
			if (costLabel != null && costContainer != null)
			{
				int cost = state.IsGiftCardShop ? state.GiftCost : state.EnchantCost;
				string pricePrefix = new LocString("shop_enhancement", state.IsGiftCardShop ? "gift.price_prefix" : "enchant.price_prefix").GetFormattedText();
				costLabel.Visible = true;
				costLabel.SetTextAutoSize($"{pricePrefix}{cost}");
				costLabel.Modulate = localInventory.Player.Gold >= cost ? new Color(0.55f, 0.84f, 1f) : StsColors.red;
				costContainer.Visible = true;
			}
		}
	}
}
