using System;

namespace ShopEnhancement.Config;

internal readonly record struct RelicSellRuleOptions(bool AllowUsedUpRelics, bool AllowUponPickupRelics, bool AllowWaxRelics, bool AllowMeltedRelics, bool AllowDisabledRelics, bool AllowStarterRelics, bool AllowAncientRelics, bool AllowEventRelics)
{
	public static RelicSellRuleOptions ForPreset(RelicSellPreset preset)
	{
		return preset switch
		{
			RelicSellPreset.AllowAll => new RelicSellRuleOptions(AllowUsedUpRelics: true, AllowUponPickupRelics: true, AllowWaxRelics: true, AllowMeltedRelics: true, AllowDisabledRelics: true, AllowStarterRelics: true, AllowAncientRelics: true, AllowEventRelics: true),
			RelicSellPreset.BlockUponPickup => new RelicSellRuleOptions(AllowUsedUpRelics: true, AllowUponPickupRelics: false, AllowWaxRelics: true, AllowMeltedRelics: true, AllowDisabledRelics: true, AllowStarterRelics: true, AllowAncientRelics: true, AllowEventRelics: true),
			RelicSellPreset.BlockTriggered => new RelicSellRuleOptions(AllowUsedUpRelics: false, AllowUponPickupRelics: false, AllowWaxRelics: true, AllowMeltedRelics: false, AllowDisabledRelics: false, AllowStarterRelics: true, AllowAncientRelics: true, AllowEventRelics: true),
			RelicSellPreset.Strict => new RelicSellRuleOptions(AllowUsedUpRelics: false, AllowUponPickupRelics: false, AllowWaxRelics: true, AllowMeltedRelics: false, AllowDisabledRelics: true, AllowStarterRelics: false, AllowAncientRelics: false, AllowEventRelics: false),
			_ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null),
		};
	}
}
