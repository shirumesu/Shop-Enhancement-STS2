using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
public static class NMerchantCardFix
{
	[HarmonyPrefix]
	[HarmonyPriority(600)]
	public static bool Prefix(NMerchantCard __instance)
	{
		if (!GodotObject.IsInstanceValid(__instance))
		{
			return false;
		}
		return true;
	}

	[HarmonyFinalizer]
	public static Exception? Finalizer(Exception? __exception)
	{
		return MerchantVisualFinalizer.Handle(__exception);
	}
}
