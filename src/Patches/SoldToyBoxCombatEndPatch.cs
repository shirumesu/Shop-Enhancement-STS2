using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(Hook), "AfterCombatEnd", new Type[]
{
	typeof(IRunState),
	typeof(ICombatState),
	typeof(CombatRoom)
})]
internal static class SoldToyBoxCombatEndPatch
{
	[HarmonyPostfix]
	private static void Postfix(IRunState runState, ref Task __result)
	{
		__result = SoldToyBoxState.ContinueAfterCombatEnd(__result, runState);
	}
}
