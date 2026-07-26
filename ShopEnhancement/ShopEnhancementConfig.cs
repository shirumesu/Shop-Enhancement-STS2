using Godot;
using ShopEnhancement.Config;

namespace ShopEnhancement;

public static class ShopEnhancementConfig
{
    // Requirement 1: Modify card removal cost
    public static int RemoveBaseCost { get; set; } = ConfigSchema.RemoveBaseCost.Default;
    public static int RemoveStepCost { get; set; } = ConfigSchema.RemoveStepCost.Default;

    // Requirement 2: Modify card removal limit
    public static int RemoveLimitPerShop { get; set; } = ConfigSchema.RemoveLimitPerShop.Default;

    // Requirement 3: Refresh shop
    public static int RefreshCost { get; set; } = ConfigSchema.RefreshCost.Default;
    public static int RefreshLimitPerShop { get; set; } = ConfigSchema.RefreshLimitPerShop.Default;
    public static ShopRelicRefreshMode RelicRefreshMode { get; set; } = ConfigSchema.RelicRefreshMode.Default;

    // Requirement 4: No Purchase Reward
    public static bool EnableNoPurchaseReward { get; set; } = ConfigSchema.EnableNoPurchaseReward.Default;
    public static int NoPurchaseRewardGold { get; set; } = ConfigSchema.NoPurchaseRewardGold.Default;

    // Requirement 5: Skip Card Reward Gold
    public static bool EnableSkipCardRewardGold { get; set; } = ConfigSchema.EnableSkipCardRewardGold.Default;
    public static int SkipCardRewardGoldAmount { get; set; } = ConfigSchema.SkipCardRewardGoldAmount.Default;

    // Requirement 6: Cross Class Cards
    public static bool EnableCrossClassCards { get; set; } = ConfigSchema.EnableCrossClassCards.Default;
    public static float CrossClassCardChance { get; set; } = ConfigSchema.CrossClassCardChance.Default;

    public static bool EnableSellMode { get; set; } = ConfigSchema.EnableSellMode.Default;
    public static bool EnableSellPriceVariance { get; set; } = ConfigSchema.EnableSellPriceVariance.Default;
    public static float SellPriceVariance { get; set; } = ConfigSchema.SellPriceVariance.Default;
    public static bool EnableGiftMode { get; set; } = ConfigSchema.EnableGiftMode.Default;

    public static RelicSellPreset RelicSellRulePreset { get; set; } = ConfigSchema.RelicSellRulePreset.Default;
    public static bool AllowUsedUpRelics { get; set; } = ConfigSchema.AllowUsedUpRelics.Default;
    public static bool AllowUponPickupRelics { get; set; } = ConfigSchema.AllowUponPickupRelics.Default;
    public static bool AllowWaxRelics { get; set; } = ConfigSchema.AllowWaxRelics.Default;
    public static bool AllowMeltedRelics { get; set; } = ConfigSchema.AllowMeltedRelics.Default;
    public static bool AllowDisabledRelics { get; set; } = ConfigSchema.AllowDisabledRelics.Default;
    public static bool AllowStarterRelics { get; set; } = ConfigSchema.AllowStarterRelics.Default;
    public static bool AllowAncientRelics { get; set; } = ConfigSchema.AllowAncientRelics.Default;
    public static bool AllowEventRelics { get; set; } = ConfigSchema.AllowEventRelics.Default;
    public static int SellCommonRelicPrice { get; set; } = ConfigSchema.SellCommonRelicPrice.Default;
    public static int SellUncommonRelicPrice { get; set; } = ConfigSchema.SellUncommonRelicPrice.Default;
    public static int SellRareRelicPrice { get; set; } = ConfigSchema.SellRareRelicPrice.Default;
    public static int SellShopRelicPrice { get; set; } = ConfigSchema.SellShopRelicPrice.Default;
    public static int SellAncientRelicPrice { get; set; } = ConfigSchema.SellAncientRelicPrice.Default;
    public static int SellStarterRelicPrice { get; set; } = ConfigSchema.SellStarterRelicPrice.Default;
    public static int SellEventRelicPrice { get; set; } = ConfigSchema.SellEventRelicPrice.Default;
    public static int SellRelicMinGold { get; set; } = ConfigSchema.SellRelicMinGold.Default;
    public static int SellCommonPotionPrice { get; set; } = ConfigSchema.SellCommonPotionPrice.Default;
    public static int SellUncommonPotionPrice { get; set; } = ConfigSchema.SellUncommonPotionPrice.Default;
    public static int SellRarePotionPrice { get; set; } = ConfigSchema.SellRarePotionPrice.Default;
    public static int SellPotionMinGold { get; set; } = ConfigSchema.SellPotionMinGold.Default;

    public static bool EnableRemovalEnchantRandom { get; set; } = ConfigSchema.EnableRemovalEnchantRandom.Default;
    public static bool EnableEnchantService { get; set; } = ConfigSchema.EnableEnchantService.Default;
    public static int EnchantStartShopVisit { get; set; } = ConfigSchema.EnchantStartShopVisit.Default;
    public static float EnchantReplaceChance { get; set; } = ConfigSchema.EnchantReplaceChance.Default;
    public static int EnchantCost { get; set; } = ConfigSchema.EnchantCost.Default;
    public static Vector2I EnchantAmountRange { get; set; } = new(
        ConfigSchema.EnchantAmount.DefaultMin,
        ConfigSchema.EnchantAmount.DefaultMax);
    public static Vector2I EnchantCardCountRange { get; set; } = new(
        ConfigSchema.EnchantCardCount.DefaultMin,
        ConfigSchema.EnchantCardCount.DefaultMax);
    public static bool EnableRandomTeammateGiftService { get; set; } = ConfigSchema.EnableRandomTeammateGiftService.Default;
    public static Vector2I GiftServiceCardCountRange { get; set; } = new(
        ConfigSchema.GiftServiceCardCount.DefaultMin,
        ConfigSchema.GiftServiceCardCount.DefaultMax);
    public static int GiftServiceBaseCost { get; set; } = ConfigSchema.GiftServiceBaseCost.Default;
    public static int GiftServiceStepCost { get; set; } = ConfigSchema.GiftServiceStepCost.Default;
}
