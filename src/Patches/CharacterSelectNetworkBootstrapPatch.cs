using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), "_Process")]
public static class CharacterSelectNetworkBootstrapPatch
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		ShopEnhancementNetwork.EnsureHandlersRegistered();
	}
}
