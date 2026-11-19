using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using ShopEnhancement.Compatibility;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static class NCardRewardAlternativeButtonPatches
{
	private static MethodBase TargetMethod()
	{
		return GameCompat.ResolveCardRewardButtonCreate();
	}

	[HarmonyPrefix]
	public static void Create_Prefix(object[] __args)
	{
		if (ShopEnhancementConfig.EnableSkipCardRewardGold && __args.Length > 1 && __args[0] is string label && GameCompat.IncludesCancelHotkey(__args[1]))
		{
			int goldAmount = ShopEnhancementConfig.SkipCardRewardGoldAmount;
			if (goldAmount > 0)
			{
				var goldSuffix = new LocString("shop_enhancement", "reward.skip_gold");
				goldSuffix.Add("0", goldAmount);
				__args[0] = label + goldSuffix.GetFormattedText();
			}
		}
	}
}
