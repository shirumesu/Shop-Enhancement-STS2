using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

using ShopEnhancement.Compatibility;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantRoom))]
internal static class ShopRelicMerchantRoomPatches
{
	[HarmonyPatch("EnterInternal", new Type[]
	{
		typeof(IRunState),
		typeof(bool)
	})]
	[HarmonyPrefix]
	private static void EnterPrefix(IRunState? runState, bool isRestoringRoomStackBase)
	{
		if (runState != null && !isRestoringRoomStackBase)
		{
			ShopRelicRefreshState.BeginMerchantSession(runState);
		}
	}

	[HarmonyPatch("Exit", new Type[] { typeof(IRunState) })]
	[HarmonyPrefix]
	private static void ExitPrefix(MerchantRoom __instance)
	{
		ShopRelicRefreshState.ReleaseInventories(GameCompat.GetMerchantInventories(__instance));
	}

	[HarmonyPatch("Exit", new Type[] { typeof(IRunState) })]
	[HarmonyPostfix]
	private static void ExitPostfix(IRunState? runState)
	{
		ShopRelicRefreshState.EndMerchantSession(runState);
	}
}
