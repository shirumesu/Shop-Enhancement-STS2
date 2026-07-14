namespace ShopEnhancement.Config;

public enum RelicSellPreset
{
    AllowAll,
    BlockUponPickup,
    BlockTriggered,
    Strict,
    Custom
}

internal readonly record struct RelicSellRuleOptions(
    bool AllowUsedUpRelics,
    bool AllowUponPickupRelics,
    bool AllowWaxRelics,
    bool AllowMeltedRelics,
    bool AllowDisabledRelics,
    bool AllowStarterRelics,
    bool AllowAncientRelics,
    bool AllowEventRelics)
{
    public static RelicSellRuleOptions ForPreset(RelicSellPreset preset)
    {
        return preset switch
        {
            RelicSellPreset.AllowAll => new(true, true, true, true, true, true, true, true),
            RelicSellPreset.BlockUponPickup => new(true, false, true, true, true, true, true, true),
            RelicSellPreset.BlockTriggered => new(false, false, true, false, false, true, true, true),
            RelicSellPreset.Strict => new(false, false, true, false, true, false, false, false),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }
}
