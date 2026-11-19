using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Network;

[HarmonyPatch(typeof(OneOffSynchronizer), MethodType.Constructor, new Type[]
{
	typeof(RunLocationTargetedMessageBuffer),
	typeof(INetGameService),
	typeof(IPlayerCollection),
	typeof(ulong)
})]
internal static class ShopActionSynchronizerInitializePatch
{
	[HarmonyPostfix]
	private static void Postfix(OneOffSynchronizer __instance, RunLocationTargetedMessageBuffer messageBuffer, INetGameService gameService, IPlayerCollection playerCollection)
	{
		ShopActionSynchronizer.Instances.Add(__instance, new ShopActionSynchronizer(messageBuffer, gameService, playerCollection));
	}
}
