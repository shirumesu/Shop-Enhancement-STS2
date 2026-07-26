using System.Text.Json.Serialization;
using Godot;

namespace ShopEnhancement.Config;

public class ConfigData
{
    public int RemoveBaseCost { get; set; } = ConfigSchema.RemoveBaseCost.Default;
    public int RemoveStepCost { get; set; } = ConfigSchema.RemoveStepCost.Default;
    public int RemoveLimitPerShop { get; set; } = ConfigSchema.RemoveLimitPerShop.Default;

    public int RefreshCost { get; set; } = ConfigSchema.RefreshCost.Default;
    public int RefreshLimitPerShop { get; set; } = ConfigSchema.RefreshLimitPerShop.Default;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShopRelicRefreshMode RelicRefreshMode { get; set; } = ConfigSchema.RelicRefreshMode.Default;

    public bool EnableNoPurchaseReward { get; set; } = ConfigSchema.EnableNoPurchaseReward.Default;
    public int NoPurchaseRewardGold { get; set; } = ConfigSchema.NoPurchaseRewardGold.Default;

    public bool EnableSkipCardRewardGold { get; set; } = ConfigSchema.EnableSkipCardRewardGold.Default;
    public int SkipCardRewardGoldAmount { get; set; } = ConfigSchema.SkipCardRewardGoldAmount.Default;

    public bool EnableCrossClassCards { get; set; } = ConfigSchema.EnableCrossClassCards.Default;
    public float CrossClassCardChance { get; set; } = ConfigSchema.CrossClassCardChance.Default;

    public bool EnableSellMode { get; set; } = ConfigSchema.EnableSellMode.Default;
    public bool EnableSellPriceVariance { get; set; } = ConfigSchema.EnableSellPriceVariance.Default;
    public float SellPriceVariance { get; set; } = ConfigSchema.SellPriceVariance.Default;
    public bool EnableGiftMode { get; set; } = ConfigSchema.EnableGiftMode.Default;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RelicSellPreset RelicSellRulePreset { get; set; } = ConfigSchema.RelicSellRulePreset.Default;
    public bool AllowUsedUpRelics { get; set; } = ConfigSchema.AllowUsedUpRelics.Default;
    public bool AllowUponPickupRelics { get; set; } = ConfigSchema.AllowUponPickupRelics.Default;
    public bool AllowWaxRelics { get; set; } = ConfigSchema.AllowWaxRelics.Default;
    public bool AllowMeltedRelics { get; set; } = ConfigSchema.AllowMeltedRelics.Default;
    public bool AllowDisabledRelics { get; set; } = ConfigSchema.AllowDisabledRelics.Default;
    public bool AllowStarterRelics { get; set; } = ConfigSchema.AllowStarterRelics.Default;
    public bool AllowAncientRelics { get; set; } = ConfigSchema.AllowAncientRelics.Default;
    public bool AllowEventRelics { get; set; } = ConfigSchema.AllowEventRelics.Default;
    public int SellCommonRelicPrice { get; set; } = ConfigSchema.SellCommonRelicPrice.Default;
    public int SellUncommonRelicPrice { get; set; } = ConfigSchema.SellUncommonRelicPrice.Default;
    public int SellRareRelicPrice { get; set; } = ConfigSchema.SellRareRelicPrice.Default;
    public int SellShopRelicPrice { get; set; } = ConfigSchema.SellShopRelicPrice.Default;
    public int SellAncientRelicPrice { get; set; } = ConfigSchema.SellAncientRelicPrice.Default;
    public int SellStarterRelicPrice { get; set; } = ConfigSchema.SellStarterRelicPrice.Default;
    public int SellEventRelicPrice { get; set; } = ConfigSchema.SellEventRelicPrice.Default;
    public int SellRelicMinGold { get; set; } = ConfigSchema.SellRelicMinGold.Default;
    public int SellCommonPotionPrice { get; set; } = ConfigSchema.SellCommonPotionPrice.Default;
    public int SellUncommonPotionPrice { get; set; } = ConfigSchema.SellUncommonPotionPrice.Default;
    public int SellRarePotionPrice { get; set; } = ConfigSchema.SellRarePotionPrice.Default;
    public int SellPotionMinGold { get; set; } = ConfigSchema.SellPotionMinGold.Default;

    public bool EnableRemovalEnchantRandom { get; set; } = ConfigSchema.EnableRemovalEnchantRandom.Default;
    public bool EnableEnchantService { get; set; } = ConfigSchema.EnableEnchantService.Default;
    public int EnchantStartShopVisit { get; set; } = ConfigSchema.EnchantStartShopVisit.Default;
    public float EnchantReplaceChance { get; set; } = ConfigSchema.EnchantReplaceChance.Default;
    public int EnchantCost { get; set; } = ConfigSchema.EnchantCost.Default;
    [JsonConverter(typeof(Vector2IJsonConverter))]
    public Vector2I EnchantAmountRange { get; set; } = new(
        ConfigSchema.EnchantAmount.DefaultMin,
        ConfigSchema.EnchantAmount.DefaultMax);
    [JsonConverter(typeof(Vector2IJsonConverter))]
    public Vector2I EnchantCardCountRange { get; set; } = new(
        ConfigSchema.EnchantCardCount.DefaultMin,
        ConfigSchema.EnchantCardCount.DefaultMax);
    public bool EnableRandomTeammateGiftService { get; set; } = ConfigSchema.EnableRandomTeammateGiftService.Default;
    [JsonConverter(typeof(Vector2IJsonConverter))]
    public Vector2I GiftServiceCardCountRange { get; set; } = new(
        ConfigSchema.GiftServiceCardCount.DefaultMin,
        ConfigSchema.GiftServiceCardCount.DefaultMax);
    public int GiftServiceBaseCost { get; set; } = ConfigSchema.GiftServiceBaseCost.Default;
    public int GiftServiceStepCost { get; set; } = ConfigSchema.GiftServiceStepCost.Default;
}
