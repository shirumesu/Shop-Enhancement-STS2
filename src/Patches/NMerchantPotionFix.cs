using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantPotion), "UpdateVisual")]
public static class NMerchantPotionFix
{
	[HarmonyPrefix]
	[HarmonyPriority(600)]
	public static bool Prefix(NMerchantPotion __instance)
	{
		if (GodotObject.IsInstanceValid(__instance))
		{
			return __instance.IsInsideTree();
		}
		return false;
	}

	[HarmonyFinalizer]
	public static Exception? Finalizer(Exception? __exception)
	{
		return MerchantVisualFinalizer.Handle(__exception);
	}
}
