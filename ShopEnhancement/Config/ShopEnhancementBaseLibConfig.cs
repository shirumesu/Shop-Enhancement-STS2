using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;

namespace ShopEnhancement.Config;

public class ShopEnhancementBaseLibConfig : SimpleModConfig
{
    private const string FractionSliderFormat = "{0:0.##}";
    private static bool _syncing;
    private static RelicSellRuleSnapshot _lastRelicSellRuleSnapshot;

    [ConfigSection("BasicShop")]
    [ConfigSlider(0, 99, 1)]
    public static int RefreshCost { get; set; } = 40;

    [ConfigSlider(0, 20, 1)]
    public static int RefreshLimitPerShop { get; set; } = 3;

    [ConfigHoverTip]
    public static ShopRelicRefreshMode RelicRefreshMode { get; set; } = ShopRelicRefreshMode.Queue;

    [ConfigSlider(0, 200, 1)]
    [ConfigHoverTip]
    public static int RemoveBaseCost { get; set; } = 50;

    [ConfigSlider(0, 100, 1)]
    public static int RemoveStepCost { get; set; } = 25;

    [ConfigSlider(1, 20, 1)]
    public static int RemoveLimitPerShop { get; set; } = 3;

    [ConfigSection("ExtraFeatures")]
    public static bool EnableNoPurchaseReward { get; set; } = true;

    [ConfigSlider(0, 999, 1)]
    public static int NoPurchaseRewardGold { get; set; } = 15;

    public static bool EnableSkipCardRewardGold { get; set; } = true;

    [ConfigSlider(0, 999, 1)]
    public static int SkipCardRewardGoldAmount { get; set; } = 15;

    public static bool EnableCrossClassCards { get; set; } = true;

    [ConfigSlider(0, 1, 0.01, Format = FractionSliderFormat)]
    public static float CrossClassCardChance { get; set; } = 0.2f;

    [ConfigHoverTip]
    public static bool EnableSellMode { get; set; } = true;

    public static bool EnableSellPriceVariance { get; set; } = true;

    [ConfigHoverTip]
    [ConfigSlider(0, 1, 0.01, Format = FractionSliderFormat)]
    public static float SellPriceVariance { get; set; } = 0.2f;

    public static bool EnableGiftMode { get; set; } = true;

    [ConfigSection("SellRules")]
    [ConfigHoverTip]
    public static RelicSellPreset RelicSellRulePreset { get; set; } = RelicSellPreset.AllowAll;

    [ConfigHoverTip]
    public static bool AllowStarterRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowAncientRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowEventRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowWaxRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowMeltedRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowUsedUpRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowUponPickupRelics { get; set; } = true;

    [ConfigHoverTip]
    public static bool AllowDisabledRelics { get; set; } = true;

    [ConfigSection("SellPrices")]
    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellCommonRelicPrice { get; set; } = 70;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellUncommonRelicPrice { get; set; } = 90;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellRareRelicPrice { get; set; } = 110;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellShopRelicPrice { get; set; } = 80;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellAncientRelicPrice { get; set; } = 240;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellStarterRelicPrice { get; set; } = 240;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellEventRelicPrice { get; set; } = 80;

    [ConfigSlider(0, 999, 1)]
    public static int SellRelicMinGold { get; set; } = 30;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellCommonPotionPrice { get; set; } = 20;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellUncommonPotionPrice { get; set; } = 30;

    [ConfigHoverTip]
    [ConfigSlider(0, 999, 1)]
    public static int SellRarePotionPrice { get; set; } = 40;

    [ConfigSlider(0, 999, 1)]
    public static int SellPotionMinGold { get; set; } = 15;

    [ConfigSection("Enchant")]
    public static bool EnableRemovalEnchantRandom { get; set; } = true;

    public static bool EnableEnchantService { get; set; } = true;

    [ConfigSlider(1, 10, 1)]
    public static int EnchantStartShopVisit { get; set; } = 4;

    [ConfigSlider(0, 1, 0.01, Format = FractionSliderFormat)]
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

    public override void SetupConfigUI(Control optionContainer)
    {
        base.SetupConfigUI(optionContainer);

        foreach (Node child in optionContainer.GetChildren())
        {
            if (child is NConfigCollapsibleSection section)
            {
                section.IsExpanded = section.Name.ToString() == "CollapsibleSection_BasicShop";
            }
        }
    }

    public static void Register(string modId)
    {
        ConfigData currentConfig = ConfigManager.GetCurrentConfig();
        CopyFromConfigData(new ConfigData());

        ShopEnhancementBaseLibConfig config = new();
        CopyFromConfigData(currentConfig);
        NormalizeRelicSellRuleSettings();
        config.SyncToLegacyConfig();
        config.ConfigChanged += (_, _) => config.HandleConfigChanged();
        config.OnConfigReloaded += config.HandleConfigReloaded;

        ModConfigRegistry.Register(modId, config);
    }

    private void HandleConfigChanged()
    {
        if (_syncing)
        {
            return;
        }

        RelicSellRuleSnapshot current = CaptureRelicSellRuleSnapshot();
        bool presetChanged = current.Preset != _lastRelicSellRuleSnapshot.Preset;
        bool optionsChanged = current.Options != _lastRelicSellRuleSnapshot.Options;

        if (presetChanged)
        {
            if (RelicSellRulePreset != RelicSellPreset.Custom)
            {
                ApplyRelicSellRulePreset(RelicSellRulePreset);
            }
        }
        else if (optionsChanged)
        {
            bool waxChanged = current.Options.AllowWaxRelics != _lastRelicSellRuleSnapshot.Options.AllowWaxRelics;
            bool meltedChanged = current.Options.AllowMeltedRelics != _lastRelicSellRuleSnapshot.Options.AllowMeltedRelics;

            RelicSellRulePreset = RelicSellPreset.Custom;
            if (meltedChanged && AllowMeltedRelics && !AllowWaxRelics)
            {
                AllowWaxRelics = true;
            }
            else if (waxChanged && !AllowWaxRelics && AllowMeltedRelics)
            {
                AllowMeltedRelics = false;
            }
        }

        SyncToLegacyConfig(refreshUi: presetChanged || optionsChanged);
    }

    private void HandleConfigReloaded()
    {
        if (_syncing)
        {
            return;
        }

        NormalizeRelicSellRuleSettings();
        SyncToLegacyConfig();
    }

    private void SyncToLegacyConfig(bool refreshUi = false)
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
            _lastRelicSellRuleSnapshot = CaptureRelicSellRuleSnapshot();
            if (refreshUi)
            {
                ConfigReloaded();
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void CopyFromRuntimeConfig()
    {
        CopyFromConfigData(ConfigManager.GetCurrentConfig());
    }

    private static void CopyFromConfigData(ConfigData data)
    {
        RemoveBaseCost = data.RemoveBaseCost;
        RemoveStepCost = data.RemoveStepCost;
        RemoveLimitPerShop = data.RemoveLimitPerShop;
        RefreshCost = data.RefreshCost;
        RefreshLimitPerShop = data.RefreshLimitPerShop;
        RelicRefreshMode = data.RelicRefreshMode;
        EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        EnableCrossClassCards = data.EnableCrossClassCards;
        CrossClassCardChance = data.CrossClassCardChance;
        EnableSellMode = data.EnableSellMode;
        EnableSellPriceVariance = data.EnableSellPriceVariance;
        SellPriceVariance = data.SellPriceVariance;
        EnableGiftMode = data.EnableGiftMode;
        RelicSellRulePreset = data.RelicSellRulePreset;
        AllowUsedUpRelics = data.AllowUsedUpRelics;
        AllowUponPickupRelics = data.AllowUponPickupRelics;
        AllowWaxRelics = data.AllowWaxRelics;
        AllowMeltedRelics = data.AllowMeltedRelics;
        AllowDisabledRelics = data.AllowDisabledRelics;
        AllowStarterRelics = data.AllowStarterRelics;
        AllowAncientRelics = data.AllowAncientRelics;
        AllowEventRelics = data.AllowEventRelics;
        SellCommonRelicPrice = data.SellCommonRelicPrice;
        SellUncommonRelicPrice = data.SellUncommonRelicPrice;
        SellRareRelicPrice = data.SellRareRelicPrice;
        SellShopRelicPrice = data.SellShopRelicPrice;
        SellAncientRelicPrice = data.SellAncientRelicPrice;
        SellStarterRelicPrice = data.SellStarterRelicPrice;
        SellEventRelicPrice = data.SellEventRelicPrice;
        SellRelicMinGold = data.SellRelicMinGold;
        SellCommonPotionPrice = data.SellCommonPotionPrice;
        SellUncommonPotionPrice = data.SellUncommonPotionPrice;
        SellRarePotionPrice = data.SellRarePotionPrice;
        SellPotionMinGold = data.SellPotionMinGold;
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
            RemoveLimitPerShop = RemoveLimitPerShop,
            RefreshCost = RefreshCost,
            RefreshLimitPerShop = RefreshLimitPerShop,
            RelicRefreshMode = RelicRefreshMode,
            EnableNoPurchaseReward = EnableNoPurchaseReward,
            NoPurchaseRewardGold = NoPurchaseRewardGold,
            EnableSkipCardRewardGold = EnableSkipCardRewardGold,
            SkipCardRewardGoldAmount = SkipCardRewardGoldAmount,
            EnableCrossClassCards = EnableCrossClassCards,
            CrossClassCardChance = CrossClassCardChance,
            EnableSellMode = EnableSellMode,
            EnableSellPriceVariance = EnableSellPriceVariance,
            SellPriceVariance = SellPriceVariance,
            EnableGiftMode = EnableGiftMode,
            RelicSellRulePreset = RelicSellRulePreset,
            AllowUsedUpRelics = AllowUsedUpRelics,
            AllowUponPickupRelics = AllowUponPickupRelics,
            AllowWaxRelics = AllowWaxRelics,
            AllowMeltedRelics = AllowMeltedRelics,
            AllowDisabledRelics = AllowDisabledRelics,
            AllowStarterRelics = AllowStarterRelics,
            AllowAncientRelics = AllowAncientRelics,
            AllowEventRelics = AllowEventRelics,
            SellCommonRelicPrice = SellCommonRelicPrice,
            SellUncommonRelicPrice = SellUncommonRelicPrice,
            SellRareRelicPrice = SellRareRelicPrice,
            SellShopRelicPrice = SellShopRelicPrice,
            SellAncientRelicPrice = SellAncientRelicPrice,
            SellStarterRelicPrice = SellStarterRelicPrice,
            SellEventRelicPrice = SellEventRelicPrice,
            SellRelicMinGold = SellRelicMinGold,
            SellCommonPotionPrice = SellCommonPotionPrice,
            SellUncommonPotionPrice = SellUncommonPotionPrice,
            SellRarePotionPrice = SellRarePotionPrice,
            SellPotionMinGold = SellPotionMinGold,
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

    private static void NormalizeRelicSellRuleSettings()
    {
        if (!Enum.IsDefined(RelicSellRulePreset))
        {
            RelicSellRulePreset = RelicSellPreset.AllowAll;
        }

        if (RelicSellRulePreset != RelicSellPreset.Custom)
        {
            ApplyRelicSellRulePreset(RelicSellRulePreset);
        }
        else if (AllowMeltedRelics && !AllowWaxRelics)
        {
            AllowWaxRelics = true;
        }
    }

    private static void ApplyRelicSellRulePreset(RelicSellPreset preset)
    {
        RelicSellRuleOptions options = RelicSellRuleOptions.ForPreset(preset);
        AllowUsedUpRelics = options.AllowUsedUpRelics;
        AllowUponPickupRelics = options.AllowUponPickupRelics;
        AllowWaxRelics = options.AllowWaxRelics;
        AllowMeltedRelics = options.AllowMeltedRelics;
        AllowDisabledRelics = options.AllowDisabledRelics;
        AllowStarterRelics = options.AllowStarterRelics;
        AllowAncientRelics = options.AllowAncientRelics;
        AllowEventRelics = options.AllowEventRelics;
    }

    private static RelicSellRuleSnapshot CaptureRelicSellRuleSnapshot()
    {
        return new RelicSellRuleSnapshot(
            RelicSellRulePreset,
            new RelicSellRuleOptions(
                AllowUsedUpRelics,
                AllowUponPickupRelics,
                AllowWaxRelics,
                AllowMeltedRelics,
                AllowDisabledRelics,
                AllowStarterRelics,
                AllowAncientRelics,
                AllowEventRelics));
    }

    private readonly record struct RelicSellRuleSnapshot(
        RelicSellPreset Preset,
        RelicSellRuleOptions Options);
}
