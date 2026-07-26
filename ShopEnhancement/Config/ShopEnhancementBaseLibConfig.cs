using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;

namespace ShopEnhancement.Config;

public class ShopEnhancementBaseLibConfig : SimpleModConfig
{
    private const string FractionSliderFormat = "{0:0.##}";
    private static readonly Vector2 CompactPresetButtonSize = new(168, 48);
    private static bool _syncing;
    private static RelicSellRuleSnapshot _lastRelicSellRuleSnapshot;
    private Control? _optionContainer;
    private VBoxContainer? _presetListContainer;
    private RichTextLabel? _presetStatusLabel;

    [ConfigHideInUI]
    [ConfigIgnoreRestoreDefaults]
    [ConfigTextInput(TextInputPreset.SafeDisplayName, MaxLength = 32)]
    public static string PresetName { get; set; } = "";

    [ConfigSection("BasicShop")]
    [ConfigSlider(ConfigSchema.RefreshCost.Min, ConfigSchema.RefreshCost.Max, ConfigSchema.RefreshCost.Step)]
    public static int RefreshCost { get; set; } = ConfigSchema.RefreshCost.Default;

    [ConfigSlider(ConfigSchema.RefreshLimitPerShop.Min, ConfigSchema.RefreshLimitPerShop.Max, ConfigSchema.RefreshLimitPerShop.Step)]
    public static int RefreshLimitPerShop { get; set; } = ConfigSchema.RefreshLimitPerShop.Default;

    [ConfigHoverTip]
    public static ShopRelicRefreshMode RelicRefreshMode { get; set; } = ConfigSchema.RelicRefreshMode.Default;

    [ConfigSlider(ConfigSchema.RemoveBaseCost.Min, ConfigSchema.RemoveBaseCost.Max, ConfigSchema.RemoveBaseCost.Step)]
    [ConfigHoverTip]
    public static int RemoveBaseCost { get; set; } = ConfigSchema.RemoveBaseCost.Default;

    [ConfigSlider(ConfigSchema.RemoveStepCost.Min, ConfigSchema.RemoveStepCost.Max, ConfigSchema.RemoveStepCost.Step)]
    public static int RemoveStepCost { get; set; } = ConfigSchema.RemoveStepCost.Default;

    [ConfigSlider(ConfigSchema.RemoveLimitPerShop.Min, ConfigSchema.RemoveLimitPerShop.Max, ConfigSchema.RemoveLimitPerShop.Step)]
    public static int RemoveLimitPerShop { get; set; } = ConfigSchema.RemoveLimitPerShop.Default;

    [ConfigSection("ExtraFeatures")]
    public static bool EnableNoPurchaseReward { get; set; } = ConfigSchema.EnableNoPurchaseReward.Default;

    [ConfigSlider(ConfigSchema.NoPurchaseRewardGold.Min, ConfigSchema.NoPurchaseRewardGold.Max, ConfigSchema.NoPurchaseRewardGold.Step)]
    public static int NoPurchaseRewardGold { get; set; } = ConfigSchema.NoPurchaseRewardGold.Default;

    public static bool EnableSkipCardRewardGold { get; set; } = ConfigSchema.EnableSkipCardRewardGold.Default;

    [ConfigSlider(ConfigSchema.SkipCardRewardGoldAmount.Min, ConfigSchema.SkipCardRewardGoldAmount.Max, ConfigSchema.SkipCardRewardGoldAmount.Step)]
    public static int SkipCardRewardGoldAmount { get; set; } = ConfigSchema.SkipCardRewardGoldAmount.Default;

    public static bool EnableCrossClassCards { get; set; } = ConfigSchema.EnableCrossClassCards.Default;

    [ConfigSlider(ConfigSchema.CrossClassCardChance.Min, ConfigSchema.CrossClassCardChance.Max, ConfigSchema.CrossClassCardChance.Step, Format = FractionSliderFormat)]
    public static float CrossClassCardChance { get; set; } = ConfigSchema.CrossClassCardChance.Default;

    [ConfigHoverTip]
    public static bool EnableSellMode { get; set; } = ConfigSchema.EnableSellMode.Default;

    public static bool EnableSellPriceVariance { get; set; } = ConfigSchema.EnableSellPriceVariance.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellPriceVariance.Min, ConfigSchema.SellPriceVariance.Max, ConfigSchema.SellPriceVariance.Step, Format = FractionSliderFormat)]
    public static float SellPriceVariance { get; set; } = ConfigSchema.SellPriceVariance.Default;

    public static bool EnableGiftMode { get; set; } = ConfigSchema.EnableGiftMode.Default;

    [ConfigSection("SellRules")]
    [ConfigHoverTip]
    public static RelicSellPreset RelicSellRulePreset { get; set; } = ConfigSchema.RelicSellRulePreset.Default;

    public static bool AllowStarterRelics { get; set; } = ConfigSchema.AllowStarterRelics.Default;

    public static bool AllowAncientRelics { get; set; } = ConfigSchema.AllowAncientRelics.Default;

    public static bool AllowEventRelics { get; set; } = ConfigSchema.AllowEventRelics.Default;

    public static bool AllowWaxRelics { get; set; } = ConfigSchema.AllowWaxRelics.Default;

    public static bool AllowMeltedRelics { get; set; } = ConfigSchema.AllowMeltedRelics.Default;

    public static bool AllowUsedUpRelics { get; set; } = ConfigSchema.AllowUsedUpRelics.Default;

    public static bool AllowUponPickupRelics { get; set; } = ConfigSchema.AllowUponPickupRelics.Default;

    public static bool AllowDisabledRelics { get; set; } = ConfigSchema.AllowDisabledRelics.Default;

    [ConfigSection("SellPrices")]
    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellCommonRelicPrice.Min, ConfigSchema.SellCommonRelicPrice.Max, ConfigSchema.SellCommonRelicPrice.Step)]
    public static int SellCommonRelicPrice { get; set; } = ConfigSchema.SellCommonRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellUncommonRelicPrice.Min, ConfigSchema.SellUncommonRelicPrice.Max, ConfigSchema.SellUncommonRelicPrice.Step)]
    public static int SellUncommonRelicPrice { get; set; } = ConfigSchema.SellUncommonRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellRareRelicPrice.Min, ConfigSchema.SellRareRelicPrice.Max, ConfigSchema.SellRareRelicPrice.Step)]
    public static int SellRareRelicPrice { get; set; } = ConfigSchema.SellRareRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellShopRelicPrice.Min, ConfigSchema.SellShopRelicPrice.Max, ConfigSchema.SellShopRelicPrice.Step)]
    public static int SellShopRelicPrice { get; set; } = ConfigSchema.SellShopRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellAncientRelicPrice.Min, ConfigSchema.SellAncientRelicPrice.Max, ConfigSchema.SellAncientRelicPrice.Step)]
    public static int SellAncientRelicPrice { get; set; } = ConfigSchema.SellAncientRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellStarterRelicPrice.Min, ConfigSchema.SellStarterRelicPrice.Max, ConfigSchema.SellStarterRelicPrice.Step)]
    public static int SellStarterRelicPrice { get; set; } = ConfigSchema.SellStarterRelicPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellEventRelicPrice.Min, ConfigSchema.SellEventRelicPrice.Max, ConfigSchema.SellEventRelicPrice.Step)]
    public static int SellEventRelicPrice { get; set; } = ConfigSchema.SellEventRelicPrice.Default;

    [ConfigSlider(ConfigSchema.SellRelicMinGold.Min, ConfigSchema.SellRelicMinGold.Max, ConfigSchema.SellRelicMinGold.Step)]
    public static int SellRelicMinGold { get; set; } = ConfigSchema.SellRelicMinGold.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellCommonPotionPrice.Min, ConfigSchema.SellCommonPotionPrice.Max, ConfigSchema.SellCommonPotionPrice.Step)]
    public static int SellCommonPotionPrice { get; set; } = ConfigSchema.SellCommonPotionPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellUncommonPotionPrice.Min, ConfigSchema.SellUncommonPotionPrice.Max, ConfigSchema.SellUncommonPotionPrice.Step)]
    public static int SellUncommonPotionPrice { get; set; } = ConfigSchema.SellUncommonPotionPrice.Default;

    [ConfigHoverTip]
    [ConfigSlider(ConfigSchema.SellRarePotionPrice.Min, ConfigSchema.SellRarePotionPrice.Max, ConfigSchema.SellRarePotionPrice.Step)]
    public static int SellRarePotionPrice { get; set; } = ConfigSchema.SellRarePotionPrice.Default;

    [ConfigSlider(ConfigSchema.SellPotionMinGold.Min, ConfigSchema.SellPotionMinGold.Max, ConfigSchema.SellPotionMinGold.Step)]
    public static int SellPotionMinGold { get; set; } = ConfigSchema.SellPotionMinGold.Default;

    [ConfigSection("Enchant")]
    public static bool EnableRemovalEnchantRandom { get; set; } = ConfigSchema.EnableRemovalEnchantRandom.Default;

    public static bool EnableEnchantService { get; set; } = ConfigSchema.EnableEnchantService.Default;

    [ConfigSlider(ConfigSchema.EnchantStartShopVisit.Min, ConfigSchema.EnchantStartShopVisit.Max, ConfigSchema.EnchantStartShopVisit.Step)]
    public static int EnchantStartShopVisit { get; set; } = ConfigSchema.EnchantStartShopVisit.Default;

    [ConfigSlider(ConfigSchema.EnchantReplaceChance.Min, ConfigSchema.EnchantReplaceChance.Max, ConfigSchema.EnchantReplaceChance.Step, Format = FractionSliderFormat)]
    public static float EnchantReplaceChance { get; set; } = ConfigSchema.EnchantReplaceChance.Default;

    [ConfigSlider(ConfigSchema.EnchantCost.Min, ConfigSchema.EnchantCost.Max, ConfigSchema.EnchantCost.Step)]
    public static int EnchantCost { get; set; } = ConfigSchema.EnchantCost.Default;

    [ConfigSlider(ConfigSchema.EnchantAmount.Min, ConfigSchema.EnchantAmount.Max, ConfigSchema.EnchantAmount.Step)]
    public static int EnchantAmountMin { get; set; } = ConfigSchema.EnchantAmount.DefaultMin;

    [ConfigSlider(ConfigSchema.EnchantAmount.Min, ConfigSchema.EnchantAmount.Max, ConfigSchema.EnchantAmount.Step)]
    public static int EnchantAmountMax { get; set; } = ConfigSchema.EnchantAmount.DefaultMax;

    [ConfigSlider(ConfigSchema.EnchantCardCount.Min, ConfigSchema.EnchantCardCount.Max, ConfigSchema.EnchantCardCount.Step)]
    public static int EnchantCardCountMin { get; set; } = ConfigSchema.EnchantCardCount.DefaultMin;

    [ConfigSlider(ConfigSchema.EnchantCardCount.Min, ConfigSchema.EnchantCardCount.Max, ConfigSchema.EnchantCardCount.Step)]
    public static int EnchantCardCountMax { get; set; } = ConfigSchema.EnchantCardCount.DefaultMax;

    public static bool EnableRandomTeammateGiftService { get; set; } = ConfigSchema.EnableRandomTeammateGiftService.Default;

    [ConfigSlider(ConfigSchema.GiftServiceCardCount.Min, ConfigSchema.GiftServiceCardCount.Max, ConfigSchema.GiftServiceCardCount.Step)]
    public static int GiftServiceCardCountMin { get; set; } = ConfigSchema.GiftServiceCardCount.DefaultMin;

    [ConfigSlider(ConfigSchema.GiftServiceCardCount.Min, ConfigSchema.GiftServiceCardCount.Max, ConfigSchema.GiftServiceCardCount.Step)]
    public static int GiftServiceCardCountMax { get; set; } = ConfigSchema.GiftServiceCardCount.DefaultMax;

    [ConfigSlider(ConfigSchema.GiftServiceBaseCost.Min, ConfigSchema.GiftServiceBaseCost.Max, ConfigSchema.GiftServiceBaseCost.Step)]
    public static int GiftServiceBaseCost { get; set; } = ConfigSchema.GiftServiceBaseCost.Default;

    [ConfigSlider(ConfigSchema.GiftServiceStepCost.Min, ConfigSchema.GiftServiceStepCost.Max, ConfigSchema.GiftServiceStepCost.Step)]
    public static int GiftServiceStepCost { get; set; } = ConfigSchema.GiftServiceStepCost.Default;

    public override void SetupConfigUI(Control optionContainer)
    {
        base.SetupConfigUI(optionContainer);
        RelicCategoryGallery.Attach(optionContainer);

        _optionContainer = optionContainer;
        NConfigCollapsibleSection presetSection = CreateCollapsibleSection(
            "Presets",
            collapsedByDefault: true);
        Control presetContent = presetSection.ContentContainer;

        var presetNameProperty = typeof(ShopEnhancementBaseLibConfig).GetProperty(nameof(PresetName));
        if (presetNameProperty != null)
        {
            presetContent.AddChild(CreateLineEditOption(presetNameProperty));
        }

        presetContent.AddChild(CreateButton(
            "PresetActions",
            "SavePreset",
            SaveCurrentPreset));

        _presetStatusLabel = CreateRawLabelControl("", 24);
        _presetStatusLabel.CustomMinimumSize = new Vector2(0, 40);
        _presetStatusLabel.Visible = false;
        presetContent.AddChild(_presetStatusLabel);

        presetContent.AddChild(CreateSectionHeader(
            "SavedPresets",
            alignToTop: false,
            centered: false));

        _presetListContainer = new VBoxContainer
        {
            Name = "PresetList"
        };
        presetContent.AddChild(_presetListContainer);

        optionContainer.AddChild(presetSection);
        optionContainer.MoveChild(presetSection, 0);
        RefreshPresetList();

        Control? resetContainer = optionContainer.GetNodeOrNull<Control>("ResetDefaultsButtonContainer");
        if (resetContainer != null)
        {
            optionContainer.MoveChild(resetContainer, -1);
        }

        foreach (Node child in optionContainer.GetChildren())
        {
            if (child is NConfigCollapsibleSection section)
            {
                section.IsExpanded = section.Name.ToString() == "CollapsibleSection_BasicShop";
            }
        }

        SetupFocusNeighbors(optionContainer);
    }

    private void SaveCurrentPreset()
    {
        PresetOperationResult result = ConfigManager.SavePreset(
            PresetName,
            ToConfigData(),
            out string normalizedName);

        if (result == PresetOperationResult.Success)
        {
            try
            {
                _syncing = true;
                PresetName = normalizedName;
                SaveDebounced(0);
                ConfigReloaded();
            }
            finally
            {
                _syncing = false;
            }

            SetPresetStatus("PresetStatusSaved");
            RefreshPresetList();
            return;
        }

        SetPresetStatus(GetPresetErrorLabel(result));
    }

    private void LoadPresetFromUi(string presetName)
    {
        PresetOperationResult result = ConfigManager.LoadPreset(presetName, out ConfigData? data);
        if (result != PresetOperationResult.Success || data == null)
        {
            SetPresetStatus(GetPresetErrorLabel(result));
            return;
        }

        try
        {
            _syncing = true;
            CopyFromConfigData(data);
            NormalizeRelicSellRuleSettings();
            ConfigManager.Save(ToConfigData());
            CopyFromRuntimeConfig();
            PresetName = presetName;
            _lastRelicSellRuleSnapshot = CaptureRelicSellRuleSnapshot();
            SaveDebounced(0);
            ConfigReloaded();
        }
        finally
        {
            _syncing = false;
        }

        SetPresetStatus("PresetStatusLoaded");
    }

    private void DeletePresetFromUi(string presetName)
    {
        PresetOperationResult result = ConfigManager.DeletePreset(presetName);
        if (result != PresetOperationResult.Success)
        {
            SetPresetStatus(GetPresetErrorLabel(result));
            return;
        }

        SetPresetStatus("PresetStatusDeleted");
        RefreshPresetList();
    }

    private void RefreshPresetList()
    {
        if (_presetListContainer == null)
        {
            return;
        }

        foreach (Node child in _presetListContainer.GetChildren())
        {
            _presetListContainer.RemoveChild(child);
            child.QueueFree();
        }

        var presets = ConfigManager.GetPresets();
        if (presets.Count == 0)
        {
            RichTextLabel emptyLabel = CreateRawLabelControl(GetLabelText("NoPresets"), 24);
            emptyLabel.CustomMinimumSize = new Vector2(0, 40);
            _presetListContainer.AddChild(emptyLabel);
        }
        else
        {
            for (int index = 0; index < presets.Count; index++)
            {
                string presetName = presets[index];
                NConfigButton loadButton = CreateRawButtonControl(
                    GetLabelText("LoadPreset"),
                    () => LoadPresetFromUi(presetName));
                NConfigButton deleteButton = CreateRawButtonControl(
                    GetLabelText("DeletePreset"),
                    () => DeletePresetFromUi(presetName));
                MakePresetButtonCompact(loadButton);
                MakePresetButtonCompact(deleteButton);

                HBoxContainer actions = new()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
                };
                actions.AddThemeConstantOverride("separation", 8);
                actions.AddChild(loadButton);
                actions.AddChild(deleteButton);

                RichTextLabel nameLabel = CreateRawLabelControl(presetName, 28);
                nameLabel.CustomMinimumSize = new Vector2(0, 64);
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

                HBoxContainer rowContent = new();
                rowContent.AddThemeConstantOverride("separation", 12);
                rowContent.AddChild(nameLabel);
                rowContent.AddChild(actions);

                MarginContainer row = new()
                {
                    Name = $"PresetEntry{index}",
                    CustomMinimumSize = new Vector2(0, 64)
                };
                row.AddThemeConstantOverride("margin_left", 12);
                row.AddThemeConstantOverride("margin_right", 12);
                row.AddChild(rowContent);
                _presetListContainer.AddChild(row);
            }
        }

        if (_optionContainer != null)
        {
            SetupFocusNeighbors(_optionContainer);
        }
    }

    private static void MakePresetButtonCompact(NConfigButton button)
    {
        button.CustomMinimumSize = CompactPresetButtonSize;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        TextureRect? image = button.GetNodeOrNull<TextureRect>("Image");
        if (image != null)
        {
            image.CustomMinimumSize = new Vector2(48, 48);
        }

        Label? label = button.GetNodeOrNull<Label>("Label");
        if (label?.LabelSettings != null)
        {
            label.LabelSettings.FontSize = 22;
            label.LabelSettings.OutlineSize = 8;
        }
    }

    private void SetPresetStatus(string labelName)
    {
        if (_presetStatusLabel == null)
        {
            return;
        }

        _presetStatusLabel.Text = GetLabelText(labelName);
        _presetStatusLabel.Visible = true;
    }

    private static string GetPresetErrorLabel(PresetOperationResult result)
    {
        return result switch
        {
            PresetOperationResult.EmptyName => "PresetStatusEmptyName",
            PresetOperationResult.InvalidName => "PresetStatusInvalidName",
            PresetOperationResult.NotFound => "PresetStatusNotFound",
            PresetOperationResult.InvalidData => "PresetStatusInvalidData",
            _ => "PresetStatusFailed"
        };
    }

    public static void Register(string modId)
    {
        ConfigSchema.Validate();
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
