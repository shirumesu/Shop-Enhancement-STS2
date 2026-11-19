using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(LocManager), "Initialize")]
public static class LocalizationPatches
{
	[HarmonyPostfix]
	public static void Initialize_Postfix()
	{
		MainFile.Logger.Info("LocManager initialized. Loading ShopEnhancement localization.");
		LocManager.Instance.SubscribeToLocaleChange(MainFile.OnLocaleChanged);
		MainFile.OnLocaleChanged();
	}
}
