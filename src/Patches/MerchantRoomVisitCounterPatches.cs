using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantRoom), "EnterInternal")]
public static class MerchantRoomVisitCounterPatches
{
	[HarmonyPostfix]
	public static void EnterInternal_Postfix()
	{
		MerchantCardRemovalPatches.IncrementVisit();
	}
}
