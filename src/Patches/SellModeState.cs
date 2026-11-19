using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

/// <summary>
/// Whether sell mode is on in a shop screen. A shop screen belongs to one shop visit, so sell mode starts on for
/// every visit and afterwards only changes when the player clicks the sell-mode button.
/// </summary>
public static class SellModeState
{
	private sealed class SellState
	{
		public bool IsEnabled = true;
	}

	private static readonly ConditionalWeakTable<NMerchantInventory, SellState> States = new ConditionalWeakTable<NMerchantInventory, SellState>();

	public static bool IsEnabled(NMerchantInventory inventory)
	{
		return States.GetOrCreateValue(inventory).IsEnabled;
	}

	public static bool Toggle(NMerchantInventory inventory)
	{
		SellState state = States.GetOrCreateValue(inventory);
		state.IsEnabled = !state.IsEnabled;
		return state.IsEnabled;
	}
}
