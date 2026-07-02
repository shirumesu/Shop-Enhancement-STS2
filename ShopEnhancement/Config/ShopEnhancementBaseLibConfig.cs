using BaseLib.Config;
using Godot;

namespace ShopEnhancement.Config;

public class ShopEnhancementBaseLibConfig : SimpleModConfig
{
    private static bool _syncing;

    [ConfigSection("CardRemoval")]
    [ConfigHoverTip]
    public static bool UseLinearCost { get; set; } = true;

    [ConfigSlider(0, 200, 1)]
    public static int RemoveBaseCost { get; set; } = 50;

    [ConfigSlider(0, 100, 1)]
    public static int RemoveStepCost { get; set; } = 25;

    [ConfigSlider(0, 20, 1)]
    public static int RemoveLimitPerShop { get; set; } = 3;

    [ConfigSection("ShopRefresh")]
    [ConfigSlider(0, 99, 1)]
    public static int RefreshCost { get; set; } = 40;

    [ConfigSlider(0, 20, 1)]
    public static int RefreshLimitPerShop { get; set; } = 3;

    [ConfigSection("Rewards")]
    public static bool EnableNoPurchaseReward { get; set; } = true;

    [ConfigSlider(0, 999, 1)]
    public static int NoPurchaseRewardGold { get; set; } = 15;

    public static bool EnableSkipCardRewardGold { get; set; } = true;

    [ConfigSlider(0, 999, 1)]
    public static int SkipCardRewardGoldAmount { get; set; } = 15;

    [ConfigSection("Cards")]
    public static bool EnableCrossClassCards { get; set; } = true;

    [ConfigSlider(0, 1, 0.01)]
    public static float CrossClassCardChance { get; set; } = 0.2f;

    [ConfigButton("UnlockAllRunNow")]
    public static void RunUnlockAllOnce()
    {
        ConfigData data = ToConfigData();
        data.EnableUnlockAll = true;
        ConfigManager.Save(data);
        CopyFromRuntimeConfig();
    }

    [ConfigSection("SellMode")]
    public static bool EnableSellMode { get; set; } = true;

    [ConfigSlider(0, 5, 0.01)]
    public static float SellRelicPriceRatio { get; set; } = 0.35f;

    [ConfigSlider(0, 5, 0.01)]
    public static float SellPotionPriceRatio { get; set; } = 0.25f;

    [ConfigSlider(0, 999, 1)]
    public static int SellRelicMinGold { get; set; } = 30;

    [ConfigSlider(0, 999, 1)]
    public static int SellPotionMinGold { get; set; } = 15;

    [ConfigSection("RelicBasePrices")]
    [ConfigSlider(0, 999, 1)]
    public static int SellAncientRelicBasePrice { get; set; } = 750;

    [ConfigSlider(0, 999, 1)]
    public static int SellStarterRelicBasePrice { get; set; } = 300;

    [ConfigSlider(0, 999, 1)]
    public static int SellEventRelicBasePrice { get; set; } = 200;

    [ConfigSection("Other")]
    public static bool EnableGiftMode { get; set; } = true;

    [ConfigSection("Enchant")]
    public static bool EnableRemovalEnchantRandom { get; set; } = true;

    public static bool EnableEnchantService { get; set; } = true;

    [ConfigSlider(1, 10, 1)]
    public static int EnchantStartShopVisit { get; set; } = 4;

    [ConfigSlider(0, 1, 0.01)]
    public static float EnchantReplaceChance { get; set; } = 0.3f;

    [ConfigSlider(0, 999, 1)]
    public static int EnchantCost { get; set; } = 105;

    [ConfigSlider(1, 999, 1)]
    public static int EnchantAmountMin { get; set; } = 1;

    [ConfigSlider(1, 999, 1)]
    public static int EnchantAmountMax { get; set; } = 2;

    [ConfigSlider(1, 20, 1)]
    public static int EnchantCardCountMin { get; set; } = 1;

    [ConfigSlider(1, 20, 1)]
    public static int EnchantCardCountMax { get; set; } = 2;

    public static bool EnableRandomTeammateGiftService { get; set; } = true;

    [ConfigSlider(1, 20, 1)]
    public static int GiftServiceCardCountMin { get; set; } = 1;

    [ConfigSlider(1, 20, 1)]
    public static int GiftServiceCardCountMax { get; set; } = 1;

    [ConfigSlider(0, 999, 1)]
    public static int GiftServiceBaseCost { get; set; } = 85;

    [ConfigSlider(0, 999, 1)]
    public static int GiftServiceStepCost { get; set; } = 55;

    public static void Register(string modId)
    {
        CopyFromRuntimeConfig();

        ShopEnhancementBaseLibConfig config = new();
        config.SyncToLegacyConfig();
        config.ConfigChanged += (_, _) => config.SyncToLegacyConfig();
        config.OnConfigReloaded += config.SyncToLegacyConfig;

        ModConfigRegistry.Register(modId, config);
    }

    private void SyncToLegacyConfig()
    {
        if (_syncing)
        {
            return;
        }

        try
        {
            _syncing = true;
            ConfigManager.Save(ToConfigData());
            CopyFromRuntimeConfig();
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void CopyFromRuntimeConfig()
    {
        ConfigData data = ConfigManager.GetCurrentConfig();

        RemoveBaseCost = data.RemoveBaseCost;
        RemoveStepCost = data.RemoveStepCost;
        UseLinearCost = data.UseLinearCost;
        RemoveLimitPerShop = data.RemoveLimitPerShop;
        RefreshCost = data.RefreshCost;
        RefreshLimitPerShop = data.RefreshLimitPerShop;
        EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        EnableCrossClassCards = data.EnableCrossClassCards;
        CrossClassCardChance = data.CrossClassCardChance;
        EnableSellMode = data.EnableSellMode;
        SellRelicPriceRatio = data.SellRelicPriceRatio;
        SellPotionPriceRatio = data.SellPotionPriceRatio;
        SellRelicMinGold = data.SellRelicMinGold;
        SellPotionMinGold = data.SellPotionMinGold;
        SellAncientRelicBasePrice = data.SellAncientRelicBasePrice;
        SellStarterRelicBasePrice = data.SellStarterRelicBasePrice;
        SellEventRelicBasePrice = data.SellEventRelicBasePrice;
        EnableGiftMode = data.EnableGiftMode;
        EnableRemovalEnchantRandom = data.EnableRemovalEnchantRandom;
        EnableEnchantService = data.EnableEnchantService;
        EnchantStartShopVisit = data.EnchantStartShopVisit;
        EnchantReplaceChance = data.EnchantReplaceChance;
        EnchantCost = data.EnchantCost;
        EnchantAmountMin = data.EnchantAmountRange.X;
        EnchantAmountMax = data.EnchantAmountRange.Y;
        EnchantCardCountMin = data.EnchantCardCountRange.X;
        EnchantCardCountMax = data.EnchantCardCountRange.Y;
        EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
        GiftServiceCardCountMin = data.GiftServiceCardCountRange.X;
        GiftServiceCardCountMax = data.GiftServiceCardCountRange.Y;
        GiftServiceBaseCost = data.GiftServiceBaseCost;
        GiftServiceStepCost = data.GiftServiceStepCost;
    }

    private static ConfigData ToConfigData()
    {
        return new ConfigData
        {
            RemoveBaseCost = RemoveBaseCost,
            RemoveStepCost = RemoveStepCost,
            UseLinearCost = UseLinearCost,
            RemoveLimitPerShop = RemoveLimitPerShop,
            RefreshCost = RefreshCost,
            RefreshLimitPerShop = RefreshLimitPerShop,
            EnableNoPurchaseReward = EnableNoPurchaseReward,
            NoPurchaseRewardGold = NoPurchaseRewardGold,
            EnableSkipCardRewardGold = EnableSkipCardRewardGold,
            SkipCardRewardGoldAmount = SkipCardRewardGoldAmount,
            EnableCrossClassCards = EnableCrossClassCards,
            CrossClassCardChance = CrossClassCardChance,
            EnableUnlockAll = false,
            EnableSellMode = EnableSellMode,
            SellRelicPriceRatio = SellRelicPriceRatio,
            SellPotionPriceRatio = SellPotionPriceRatio,
            SellRelicMinGold = SellRelicMinGold,
            SellPotionMinGold = SellPotionMinGold,
            SellAncientRelicBasePrice = SellAncientRelicBasePrice,
            SellStarterRelicBasePrice = SellStarterRelicBasePrice,
            SellEventRelicBasePrice = SellEventRelicBasePrice,
            EnableGiftMode = EnableGiftMode,
            EnableRemovalEnchantRandom = EnableRemovalEnchantRandom,
            EnableEnchantService = EnableEnchantService,
            EnchantStartShopVisit = EnchantStartShopVisit,
            EnchantReplaceChance = EnchantReplaceChance,
            EnchantCost = EnchantCost,
            EnchantAmountRange = SortedRange(EnchantAmountMin, EnchantAmountMax),
            EnchantCardCountRange = SortedRange(EnchantCardCountMin, EnchantCardCountMax),
            EnableRandomTeammateGiftService = EnableRandomTeammateGiftService,
            GiftServiceCardCountRange = SortedRange(GiftServiceCardCountMin, GiftServiceCardCountMax),
            GiftServiceBaseCost = GiftServiceBaseCost,
            GiftServiceStepCost = GiftServiceStepCost
        };
    }

    private static Vector2I SortedRange(int first, int second)
    {
        return new Vector2I(Math.Min(first, second), Math.Max(first, second));
    }
}
