using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(RelicFactory), "PullNextRelicFromBack", new Type[]
{
	typeof(Player),
	typeof(RelicRarity),
	typeof(Func<RelicModel, bool>)
})]
internal static class ShopRelicFactoryPatches
{
	[HarmonyPrefix]
	private static void Prefix(Player player, RelicRarity rarity, ref Func<RelicModel, bool> filter, out ShopRelicRefreshState.PullState? __state)
	{
		ShopRelicRefreshState.PreparePull(player, rarity, ref filter, out __state);
	}

	[HarmonyPostfix]
	private static void Postfix(RelicModel __result, ShopRelicRefreshState.PullState? __state)
	{
		ShopRelicRefreshState.CompletePull(__result, __state);
	}
}
