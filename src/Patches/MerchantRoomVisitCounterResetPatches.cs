using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(RunManager), "CleanUp")]
public static class MerchantRoomVisitCounterResetPatches
{
	[HarmonyPrefix]
	public static void Prefix()
	{
		MerchantCardRemovalPatches.ResetVisit();
	}
}
