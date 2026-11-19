using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

using ShopEnhancement.Compatibility;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantCardRemoval), "CreateHoverTip")]
public static class MerchantCardRemovalHoverTipPatches
{
	[HarmonyPrefix]
	public static bool CreateHoverTip_Prefix(NMerchantCardRemoval __instance)
	{
		if (!GameCompat.TryGetLocalInventory(out MerchantInventory? localInventory))
		{
			return true;
		}
		var state = MerchantCardRemovalPatches.EnsureServiceState(localInventory);
		if (!state.IsEnchantShop && !state.IsGiftCardShop)
		{
			return true;
		}
		LocString title;
		LocString description;
		if (state.IsGiftCardShop)
		{
			title = new LocString("shop_enhancement", "gift.service_title");
			description = new LocString("shop_enhancement", "gift.service_desc");
			description.Add("0", MerchantCardRemovalPatches.GetCurrentGiftTargetName(localInventory));
			description.Add("1", state.GiftCardCount);
			description.Add("2", state.GiftCost);
		}
		else
		{
			string currentEnchantName = MerchantCardRemovalPatches.GetCurrentEnchantName(localInventory);
			title = new LocString("shop_enhancement", "enchant.service_title");
			description = new LocString("shop_enhancement", "enchant.service_desc");
			description.Add("0", currentEnchantName);
			description.Add("1", state.EnchantmentAmount);
			description.Add("2", state.EnchantUsesRemaining);
			description.Add("3", state.EnchantCardCount);
			description.Add("4", state.EnchantCost);
		}
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(__instance, new HoverTip(title, description));
		if (hoverTipSet == null)
		{
			return false;
		}
		hoverTipSet.GlobalPosition = __instance.GlobalPosition;
		if (__instance.GlobalPosition.X > __instance.GetViewport().GetVisibleRect().Size.X * 0.5f)
		{
			hoverTipSet.SetAlignment(__instance, HoverTipAlignment.Left);
			hoverTipSet.GlobalPosition -= __instance.Size * 0.5f * __instance.Scale;
		}
		else
		{
			hoverTipSet.GlobalPosition += Vector2.Right * __instance.Size.X * 0.5f * __instance.Scale + Vector2.Up * __instance.Size.Y * 0.5f * __instance.Scale;
		}
		return false;
	}
}
