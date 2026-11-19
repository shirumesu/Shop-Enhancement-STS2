using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory), "Initialize")]
public static class MerchantInventoryServiceStateInitPatches
{
	[HarmonyPostfix]
	public static void Initialize_Postfix(MerchantInventory inventory)
	{
		MerchantCardRemovalPatches.EnsureServiceState(inventory);
		inventory.CardRemovalEntry?.CalcCost();
	}
}
