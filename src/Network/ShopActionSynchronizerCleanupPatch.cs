using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

[HarmonyPatch(typeof(RunManager), "CleanUp")]
internal static class ShopActionSynchronizerCleanupPatch
{
	[HarmonyPrefix]
	private static void Prefix(RunManager __instance)
	{
		OneOffSynchronizer? oneOffSynchronizer = __instance.OneOffSynchronizer;
		if (oneOffSynchronizer != null && ShopActionSynchronizer.Instances.TryGetValue(oneOffSynchronizer, out var synchronizer))
		{
			synchronizer.Dispose();
			ShopActionSynchronizer.Instances.Remove(oneOffSynchronizer);
		}
	}
}
