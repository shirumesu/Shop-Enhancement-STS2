using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace ShopEnhancement.Network;

[HarmonyPatch(typeof(OneOffSynchronizer), "Dispose")]
internal static class ShopActionSynchronizerDisposePatch
{
	[HarmonyPostfix]
	private static void Postfix(OneOffSynchronizer __instance)
	{
		if (ShopActionSynchronizer.Instances.TryGetValue(__instance, out var synchronizer))
		{
			synchronizer.Dispose();
			ShopActionSynchronizer.Instances.Remove(__instance);
		}
	}
}
