using System.Text.Json.Serialization;
using Godot;

namespace ShopEnhancement.Config;

public class ConfigData
{
    public int RemoveBaseCost { get; set; } = 50;
    public int RemoveStepCost { get; set; } = 25;
    public int RemoveLimitPerShop { get; set; } = 3;

    public int RefreshCost { get; set; } = 40;
    public int RefreshLimitPerShop { get; set; } = 3;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShopRelicRefreshMode RelicRefreshMode { get; set; } = ShopRelicRefreshMode.Queue;

    public bool EnableNoPurchaseReward { get; set; } = true;
    public int NoPurchaseRewardGold { get; set; } = 15;

    public bool EnableSkipCardRewardGold { get; set; } = true;
    public int SkipCardRewardGoldAmount { get; set; } = 15;

    public bool EnableCrossClassCards { get; set; } = true;
    public float CrossClassCardChance { get; set; } = 0.2f;

    public bool EnableSellMode { get; set; } = true;
    public int SellCommonRelicPrice { get; set; } = 70;
    public int SellUncommonRelicPrice { get; set; } = 90;
    public int SellRareRelicPrice { get; set; } = 110;
    public int SellShopRelicPrice { get; set; } = 80;
    public int SellAncientRelicPrice { get; set; } = 240;
    public int SellStarterRelicPrice { get; set; } = 240;
    public int SellEventRelicPrice { get; set; } = 80;
    public float SellRelicPriceVariance { get; set; } = 0.2f;
    public int SellRelicMinGold { get; set; } = 30;
    public int SellCommonPotionPrice { get; set; } = 20;
    public int SellUncommonPotionPrice { get; set; } = 30;
    public int SellRarePotionPrice { get; set; } = 40;
    public float SellPotionPriceVariance { get; set; } = 0.2f;
    public int SellPotionMinGold { get; set; } = 15;

    public bool EnableGiftMode { get; set; } = true;
    public bool EnableRemovalEnchantRandom { get; set; } = true;
    public bool EnableEnchantService { get; set; } = true;
    public int EnchantStartShopVisit { get; set; } = 4;
    public float EnchantReplaceChance { get; set; } = 0.3f;
    public int EnchantCost { get; set; } = 105;
    public Vector2I EnchantAmountRange { get; set; } = new Vector2I(1, 2);
    public Vector2I EnchantCardCountRange { get; set; } = new Vector2I(1, 2);
    public bool EnableRandomTeammateGiftService { get; set; } = true;
    public Vector2I GiftServiceCardCountRange { get; set; } = new Vector2I(1, 1);
    public int GiftServiceBaseCost { get; set; } = 85;
    public int GiftServiceStepCost { get; set; } = 55;
}
