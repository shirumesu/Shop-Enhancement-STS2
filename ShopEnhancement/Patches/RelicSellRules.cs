using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using ShopEnhancement.Config;

namespace ShopEnhancement.Patches;

internal enum RelicSellBlockReason
{
    UsedUp,
    UponPickup,
    Wax,
    Melted,
    Disabled,
    Starter,
    Ancient,
    Event,
    StrictPet,
    StrictUntradable
}

internal sealed record RelicSellEvaluation(
    bool CanSell,
    IReadOnlyList<RelicSellBlockReason> Reasons)
{
    public static RelicSellEvaluation Allowed { get; } = new(true, Array.Empty<RelicSellBlockReason>());
}

internal static class RelicSellRules
{
    public static RelicSellEvaluation Evaluate(RelicModel relic)
    {
        if (ShopEnhancementConfig.RelicSellRulePreset == RelicSellPreset.Strict)
        {
            return EvaluateStrict(relic);
        }

        List<RelicSellBlockReason> reasons = [];
        if (relic.IsUsedUp && !ShopEnhancementConfig.AllowUsedUpRelics)
        {
            reasons.Add(RelicSellBlockReason.UsedUp);
        }
        if (relic.HasUponPickupEffect && !ShopEnhancementConfig.AllowUponPickupRelics)
        {
            reasons.Add(RelicSellBlockReason.UponPickup);
        }
        if (relic.IsWax && !ShopEnhancementConfig.AllowWaxRelics)
        {
            reasons.Add(RelicSellBlockReason.Wax);
        }
        if (relic.IsMelted && !ShopEnhancementConfig.AllowMeltedRelics)
        {
            reasons.Add(RelicSellBlockReason.Melted);
        }
        if (relic.Status == RelicStatus.Disabled && !ShopEnhancementConfig.AllowDisabledRelics)
        {
            reasons.Add(RelicSellBlockReason.Disabled);
        }
        if (relic.Rarity == RelicRarity.Starter && !ShopEnhancementConfig.AllowStarterRelics)
        {
            reasons.Add(RelicSellBlockReason.Starter);
        }
        if (relic.Rarity == RelicRarity.Ancient && !ShopEnhancementConfig.AllowAncientRelics)
        {
            reasons.Add(RelicSellBlockReason.Ancient);
        }
        if (relic.Rarity == RelicRarity.Event && !ShopEnhancementConfig.AllowEventRelics)
        {
            reasons.Add(RelicSellBlockReason.Event);
        }
        return CreateEvaluation(reasons);
    }

    private static RelicSellEvaluation EvaluateStrict(RelicModel relic)
    {
        List<RelicSellBlockReason> reasons = [];
        if (relic.IsUsedUp)
        {
            reasons.Add(RelicSellBlockReason.UsedUp);
        }
        if (relic.HasUponPickupEffect)
        {
            reasons.Add(RelicSellBlockReason.UponPickup);
        }
        if (relic.IsMelted)
        {
            reasons.Add(RelicSellBlockReason.Melted);
        }
        RelicSellBlockReason? rarityReason = relic.Rarity switch
        {
            RelicRarity.Starter => RelicSellBlockReason.Starter,
            RelicRarity.Ancient => RelicSellBlockReason.Ancient,
            RelicRarity.Event => RelicSellBlockReason.Event,
            _ => null
        };
        if (rarityReason.HasValue)
        {
            reasons.Add(rarityReason.Value);
        }
        if (relic.SpawnsPets)
        {
            reasons.Add(RelicSellBlockReason.StrictPet);
        }
        if (!relic.IsTradable && reasons.Count == 0)
        {
            reasons.Add(RelicSellBlockReason.StrictUntradable);
        }

        return CreateEvaluation(reasons);
    }

    private static RelicSellEvaluation CreateEvaluation(List<RelicSellBlockReason> reasons)
    {
        return reasons.Count == 0
            ? RelicSellEvaluation.Allowed
            : new RelicSellEvaluation(false, reasons);
    }
}
