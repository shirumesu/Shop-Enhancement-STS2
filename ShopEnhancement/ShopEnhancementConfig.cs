using Godot;
using ShopEnhancement.Config;

namespace ShopEnhancement;

public static class ShopEnhancementConfig
{
    // Requirement 1: Modify card removal cost
    public static int RemoveBaseCost { get; set; } = 50; // Base cost for the first removal. 50 is cheaper than vanilla (75) to encourage deck thinning, but not free.
    public static int RemoveStepCost { get; set; } = 25; // Increase per removal. Standard scaling.

    // Requirement 2: Modify card removal limit
    public static int RemoveLimitPerShop { get; set; } = 3; // 3 removals allow for aggressive thinning if you have the gold (50+75+100=225g).

    // Requirement 3: Refresh shop
    public static int RefreshCost { get; set; } = 40; // 10 was too cheap. 40 makes it a tactical decision.
    public static int RefreshLimitPerShop { get; set; } = 3; // Limit to 3 to prevent infinite digging/breaking the game loop.
    public static ShopRelicRefreshMode RelicRefreshMode { get; set; } = ShopRelicRefreshMode.Queue;

    // Requirement 4: No Purchase Reward
    public static bool EnableNoPurchaseReward { get; set; } = true;
    public static int NoPurchaseRewardGold { get; set; } = 15; // 25 was a bit high. 15 is a nice consolation for a bad shop.

    // Requirement 5: Skip Card Reward Gold
    public static bool EnableSkipCardRewardGold { get; set; } = true;
    public static int SkipCardRewardGoldAmount { get; set; } = 15; // 15g is a fair trade for skipping a card power spike.

    // Requirement 6: Cross Class Cards
    public static bool EnableCrossClassCards { get; set; } = true;
    public static float CrossClassCardChance { get; set; } = 0.2f; // 20% chance per card. 100% (1f) is too chaotic. 20% adds spice without diluting class identity.

    public static bool EnableSellMode { get; set; } = true;
    public static bool EnableSellPriceVariance { get; set; } = true;
    public static float SellPriceVariance { get; set; } = 0.2f;
    public static bool EnableGiftMode { get; set; } = true;

    public static RelicSellPreset RelicSellRulePreset { get; set; } = RelicSellPreset.AllowAll;
    public static bool AllowUsedUpRelics { get; set; } = true;
    public static bool AllowUponPickupRelics { get; set; } = true;
    public static bool AllowWaxRelics { get; set; } = true;
    public static bool AllowMeltedRelics { get; set; } = true;
    public static bool AllowDisabledRelics { get; set; } = true;
    public static bool AllowStarterRelics { get; set; } = true;
    public static bool AllowAncientRelics { get; set; } = true;
    public static bool AllowEventRelics { get; set; } = true;
    public static int SellCommonRelicPrice { get; set; } = 70;
    public static int SellUncommonRelicPrice { get; set; } = 90;
    public static int SellRareRelicPrice { get; set; } = 110;
    public static int SellShopRelicPrice { get; set; } = 80;
    public static int SellAncientRelicPrice { get; set; } = 240;
    public static int SellStarterRelicPrice { get; set; } = 240;
    public static int SellEventRelicPrice { get; set; } = 80;
    public static int SellRelicMinGold { get; set; } = 30;
    public static int SellCommonPotionPrice { get; set; } = 20;
    public static int SellUncommonPotionPrice { get; set; } = 30;
    public static int SellRarePotionPrice { get; set; } = 40;
    public static int SellPotionMinGold { get; set; } = 15;

    public static bool EnableRemovalEnchantRandom { get; set; } = true;
    public static bool EnableEnchantService { get; set; } = true;
    public static int EnchantStartShopVisit { get; set; } = 4;
    public static float EnchantReplaceChance { get; set; } = 0.3f;
    public static int EnchantCost { get; set; } = 105;
    public static Vector2I EnchantAmountRange { get; set; } = new Vector2I(1, 2);
    public static Vector2I EnchantCardCountRange { get; set; } = new Vector2I(1, 2);
    public static bool EnableRandomTeammateGiftService { get; set; } = true;
    public static Vector2I GiftServiceCardCountRange { get; set; } = new Vector2I(1, 1);
    public static int GiftServiceBaseCost { get; set; } = 85;
    public static int GiftServiceStepCost { get; set; } = 55;
}
