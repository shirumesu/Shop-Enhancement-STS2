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
        return V0110Compatibility.ResolveCardRewardButtonCreate();
    }

    [HarmonyPrefix]
    public static void Create_Prefix(ref string optionName, object[] __args)
    {
        if (!ShopEnhancementConfig.EnableSkipCardRewardGold) return;

        if (__args.Length > 1 && V0110Compatibility.IncludesCancelHotkey(__args[1]))
        {
            int gold = ShopEnhancementConfig.SkipCardRewardGoldAmount;
            if (gold > 0)
            {
                var loc = new LocString("shop_enhancement", "reward.skip_gold");
                loc.Add("0", gold);
                optionName += loc.GetFormattedText();
            }
        }
    }
}
