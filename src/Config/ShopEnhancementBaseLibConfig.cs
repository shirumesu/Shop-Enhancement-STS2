using System;
using System.Collections.Generic;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;

namespace ShopEnhancement.Config;

public class ShopEnhancementBaseLibConfig : SimpleModConfig
{
	private readonly record struct RelicSellRuleSnapshot(RelicSellPreset Preset, RelicSellRuleOptions Options);

	private const string FractionSliderFormat = "{0:0.##}";

	private static readonly Vector2 CompactPresetButtonSize = new Vector2(168f, 48f);

	private static bool _syncing;

	private static RelicSellRuleSnapshot _lastRelicSellRuleSnapshot;

	private static string _shareCode = "";

	private static bool _shareCodeInputChanged;

	private static bool _rangeEndpointAdjusted;

	private static int _enchantAmountMin = 1;

	private static int _enchantAmountMax = 2;

	private static int _enchantCardCountMin = 1;

	private static int _enchantCardCountMax = 2;

	private static int _giftServiceCardCountMin = 1;

	private static int _giftServiceCardCountMax = 1;

	private Control? _optionContainer;

	private VBoxContainer? _presetListContainer;

	private RichTextLabel? _presetStatusLabel;

	[ConfigHideInUI]
	[ConfigIgnoreRestoreDefaults]
	[ConfigTextInput(TextInputPreset.SafeDisplayName, MaxLength = 32)]
	public static string PresetName { get; set; } = "";

	[ConfigIgnore]
	[ConfigTextInput(MaxLength = 8192)]
	public static string ShareCode
	{
		get
		{
			return _shareCode;
		}
		set
		{
			_shareCode = value;
			_shareCodeInputChanged = true;
		}
	}

	[ConfigSection("BasicShop")]
	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 99.0, 1.0)]
	public static int RefreshCost { get; set; } = 40;

	[ConfigSlider(0.0, 20.0, 1.0)]
	public static int RefreshLimitPerShop { get; set; } = 3;

	[ConfigHoverTip(true)]
	public static ShopRelicRefreshMode RelicRefreshMode { get; set; } = ShopRelicRefreshMode.Queue;

	[ConfigSlider(0.0, 200.0, 1.0)]
	[ConfigHoverTip(true)]
	public static int RemoveBaseCost { get; set; } = 50;

	[ConfigSlider(0.0, 100.0, 1.0)]
	public static int RemoveStepCost { get; set; } = 25;

	[ConfigHoverTip(true)]
	[ConfigSlider(1.0, 20.0, 1.0)]
	public static int RemoveLimitPerShop { get; set; } = 3;

	[ConfigSection("ExtraFeatures")]
	[ConfigHoverTip(true)]
	public static bool EnableNoPurchaseReward { get; set; } = true;

	[ConfigSlider(0.0, 100.0, 1.0)]
	public static int NoPurchaseRewardGold { get; set; } = 15;

	[ConfigHoverTip(true)]
	public static bool EnableSkipCardRewardGold { get; set; } = true;

	[ConfigSlider(0.0, 100.0, 1.0)]
	public static int SkipCardRewardGoldAmount { get; set; } = 15;

	[ConfigHoverTip(true)]
	public static bool EnableCrossClassCards { get; set; } = true;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 1.0, 0.01, Format = "{0:0.##}")]
	public static float CrossClassCardChance { get; set; } = 0.2f;

	[ConfigHoverTip(true)]
	public static bool EnableSellMode { get; set; } = true;

	public static bool EnableSellPriceVariance { get; set; } = true;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 1.0, 0.01, Format = "{0:0.##}")]
	public static float SellPriceVariance { get; set; } = 0.2f;

	[ConfigHoverTip(true)]
	public static bool EnableGiftMode { get; set; } = true;

	[ConfigSection("SellRules")]
	[ConfigHoverTip(true)]
	public static RelicSellPreset RelicSellRulePreset { get; set; } = RelicSellPreset.AllowAll;

	public static bool AllowStarterRelics { get; set; } = true;

	public static bool AllowAncientRelics { get; set; } = true;

	public static bool AllowEventRelics { get; set; } = true;

	public static bool AllowWaxRelics { get; set; } = true;

	public static bool AllowMeltedRelics { get; set; } = true;

	public static bool AllowUsedUpRelics { get; set; } = true;

	public static bool AllowUponPickupRelics { get; set; } = true;

	public static bool AllowDisabledRelics { get; set; } = true;

	[ConfigSection("SellPrices")]
	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellCommonRelicPrice { get; set; } = 70;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellUncommonRelicPrice { get; set; } = 90;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellRareRelicPrice { get; set; } = 110;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellShopRelicPrice { get; set; } = 80;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellAncientRelicPrice { get; set; } = 240;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellStarterRelicPrice { get; set; } = 240;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 500.0, 10.0)]
	public static int SellEventRelicPrice { get; set; } = 80;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 100.0, 1.0)]
	public static int SellRelicMinGold { get; set; } = 30;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 300.0, 10.0)]
	public static int SellCommonPotionPrice { get; set; } = 20;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 300.0, 10.0)]
	public static int SellUncommonPotionPrice { get; set; } = 30;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 300.0, 10.0)]
	public static int SellRarePotionPrice { get; set; } = 40;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 50.0, 1.0)]
	public static int SellPotionMinGold { get; set; } = 15;

	[ConfigSection("Enchant")]
	[ConfigHoverTip(true)]
	public static bool EnableRemovalEnchantRandom { get; set; } = true;

	[ConfigHoverTip(true)]
	public static bool EnableEnchantService { get; set; } = true;

	[ConfigHoverTip(true)]
	[ConfigSlider(1.0, 10.0, 1.0)]
	public static int EnchantStartShopVisit { get; set; } = 4;

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 1.0, 0.01, Format = "{0:0.##}")]
	public static float EnchantReplaceChance { get; set; } = 0.3f;

	[ConfigSlider(0.0, 200.0, 10.0)]
	public static int EnchantCost { get; set; } = 100;

	[ConfigSlider(1.0, 10.0, 1.0)]
	public static int EnchantAmountMin
	{
		get
		{
			return _enchantAmountMin;
		}
		set
		{
			SetRangeMinimum(ref _enchantAmountMin, ref _enchantAmountMax, value);
		}
	}

	[ConfigSlider(1.0, 10.0, 1.0)]
	public static int EnchantAmountMax
	{
		get
		{
			return _enchantAmountMax;
		}
		set
		{
			SetRangeMaximum(ref _enchantAmountMin, ref _enchantAmountMax, value);
		}
	}

	[ConfigSlider(1.0, 20.0, 1.0)]
	public static int EnchantCardCountMin
	{
		get
		{
			return _enchantCardCountMin;
		}
		set
		{
			SetRangeMinimum(ref _enchantCardCountMin, ref _enchantCardCountMax, value);
		}
	}

	[ConfigSlider(1.0, 20.0, 1.0)]
	public static int EnchantCardCountMax
	{
		get
		{
			return _enchantCardCountMax;
		}
		set
		{
			SetRangeMaximum(ref _enchantCardCountMin, ref _enchantCardCountMax, value);
		}
	}

	[ConfigHoverTip(true)]
	public static bool EnableRandomTeammateGiftService { get; set; } = true;

	[ConfigSlider(1.0, 20.0, 1.0)]
	public static int GiftServiceCardCountMin
	{
		get
		{
			return _giftServiceCardCountMin;
		}
		set
		{
			SetRangeMinimum(ref _giftServiceCardCountMin, ref _giftServiceCardCountMax, value);
		}
	}

	[ConfigSlider(1.0, 20.0, 1.0)]
	public static int GiftServiceCardCountMax
	{
		get
		{
			return _giftServiceCardCountMax;
		}
		set
		{
			SetRangeMaximum(ref _giftServiceCardCountMin, ref _giftServiceCardCountMax, value);
		}
	}

	[ConfigHoverTip(true)]
	[ConfigSlider(0.0, 300.0, 1.0)]
	public static int GiftServiceBaseCost { get; set; } = 85;

	[ConfigSlider(0.0, 300.0, 1.0)]
	public static int GiftServiceStepCost { get; set; } = 55;

	public override void SetupConfigUI(Control optionContainer)
	{
		base.SetupConfigUI(optionContainer);
		RelicCategoryGallery.Attach(optionContainer);
		_optionContainer = optionContainer;
		var presetSection = CreateCollapsibleSection("Presets", alignToTop: false, collapsedByDefault: true);
		Control contentContainer = presetSection.ContentContainer;
		var presetNameProperty = typeof(ShopEnhancementBaseLibConfig).GetProperty("PresetName");
		if (presetNameProperty != null)
		{
			contentContainer.AddChild(CreateLineEditOption(presetNameProperty));
		}
		contentContainer.AddChild(CreateButton("PresetActions", "SavePreset", SaveCurrentPreset));
		contentContainer.AddChild(CreateSectionHeader("ShareCodeTools", alignToTop: false, centered: false));
		var shareCodeProperty = typeof(ShopEnhancementBaseLibConfig).GetProperty("ShareCode");
		if (shareCodeProperty != null)
		{
			NConfigOptionRow shareCodeRow = CreateLineEditOption(shareCodeProperty);
			shareCodeRow.SettingControl.CustomMinimumSize = new Vector2(720f, 64f);
			contentContainer.AddChild(shareCodeRow);
		}
		contentContainer.AddChild(CreateButton("ExportShareCodeActions", "ExportCurrentShareCode", ExportCurrentConfigFromUi));
		contentContainer.AddChild(CreateButton("ImportShareCodeActions", "ImportShareCode", ImportShareCodeFromUi));
		_presetStatusLabel = ModConfig.CreateRawLabelControl("", 24);
		_presetStatusLabel.CustomMinimumSize = new Vector2(0f, 40f);
		_presetStatusLabel.Visible = false;
		contentContainer.AddChild(_presetStatusLabel);
		contentContainer.AddChild(CreateSectionHeader("SavedPresets", alignToTop: false, centered: false));
		_presetListContainer = new VBoxContainer
		{
			Name = "PresetList"
		};
		contentContainer.AddChild(_presetListContainer);
		optionContainer.AddChild(presetSection);
		optionContainer.MoveChild(presetSection, 0);
		RefreshPresetList();
		var resetDefaultsButtonContainer = optionContainer.GetNodeOrNull<Control>("ResetDefaultsButtonContainer");
		if (resetDefaultsButtonContainer != null)
		{
			optionContainer.MoveChild(resetDefaultsButtonContainer, -1);
		}
		foreach (Node child in optionContainer.GetChildren())
		{
			if (child is NConfigCollapsibleSection section)
			{
				section.IsExpanded = section.Name.ToString() == "CollapsibleSection_BasicShop";
			}
		}
		SimpleModConfig.SetupFocusNeighbors(optionContainer);
	}

	private void SaveCurrentPreset()
	{
		string normalizedName;
		var result = ConfigManager.SavePreset(PresetName, ToConfigData(), out normalizedName);
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
		}
		else
		{
			SetPresetStatus(GetPresetErrorLabel(result));
		}
	}

	private void LoadPresetFromUi(string presetName)
	{
		ConfigData? data;
		var result = ConfigManager.LoadPreset(presetName, out data);
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
		var result = ConfigManager.DeletePreset(presetName);
		if (result != PresetOperationResult.Success)
		{
			SetPresetStatus(GetPresetErrorLabel(result));
			return;
		}
		SetPresetStatus("PresetStatusDeleted");
		RefreshPresetList();
	}

	private void ExportCurrentConfigFromUi()
	{
		ExportConfigToUi(ToConfigData());
	}

	private void ExportPresetFromUi(string presetName)
	{
		ConfigData? data;
		var result = ConfigManager.LoadPreset(presetName, out data);
		if (result != PresetOperationResult.Success || data == null)
		{
			SetPresetStatus(GetPresetErrorLabel(result));
		}
		else
		{
			ExportConfigToUi(data);
		}
	}

	private void ExportConfigToUi(ConfigData data)
	{
		string code;
		var result = ConfigShareCode.Export(data, out code);
		if (result != ShareCodeOperationResult.Success)
		{
			SetPresetStatus(GetShareCodeErrorLabel(result));
			return;
		}
		try
		{
			_syncing = true;
			_shareCode = code;
			_shareCodeInputChanged = false;
			ConfigReloaded();
		}
		finally
		{
			_syncing = false;
		}
		try
		{
			DisplayServer.ClipboardSet(code);
			SetPresetStatus("ShareCodeStatusExported");
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn($"Failed to copy configuration share code to the clipboard: {ex}");
			SetPresetStatus("ShareCodeStatusExportedNoClipboard");
		}
	}

	private void ImportShareCodeFromUi()
	{
		ConfigData? data;
		var result = ConfigShareCode.Import(ShareCode, out data);
		if (result != ShareCodeOperationResult.Success || data == null)
		{
			SetPresetStatus(GetShareCodeErrorLabel(result));
			return;
		}
		try
		{
			_syncing = true;
			CopyFromConfigData(data);
			NormalizeRelicSellRuleSettings();
			ConfigManager.Save(ToConfigData());
			CopyFromRuntimeConfig();
			_lastRelicSellRuleSnapshot = CaptureRelicSellRuleSnapshot();
			SaveDebounced(0);
			ConfigReloaded();
		}
		finally
		{
			_syncing = false;
		}
		SetPresetStatus("ShareCodeStatusImported");
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
		List<string> presets = ConfigManager.GetPresets();
		if (presets.Count == 0)
		{
			RichTextLabel emptyLabel = ModConfig.CreateRawLabelControl(GetLabelText("NoPresets"), 24);
			emptyLabel.CustomMinimumSize = new Vector2(0f, 40f);
			_presetListContainer.AddChild(emptyLabel);
		}
		else
		{
			for (int i = 0; i < presets.Count; i++)
			{
				string presetName = presets[i];
				NConfigButton loadButton = CreateRawButtonControl(GetLabelText("LoadPreset"), () => LoadPresetFromUi(presetName));
				NConfigButton deleteButton = CreateRawButtonControl(GetLabelText("DeletePreset"), () => DeletePresetFromUi(presetName));
				NConfigButton exportButton = CreateRawButtonControl(GetLabelText("ExportPreset"), () => ExportPresetFromUi(presetName));
				MakePresetButtonCompact(loadButton);
				MakePresetButtonCompact(exportButton);
				MakePresetButtonCompact(deleteButton);
				var buttonRow = new HBoxContainer
				{
					SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
				};
				buttonRow.AddThemeConstantOverride("separation", 8);
				buttonRow.AddChild(loadButton);
				buttonRow.AddChild(exportButton);
				buttonRow.AddChild(deleteButton);
				RichTextLabel nameLabel = ModConfig.CreateRawLabelControl(presetName, 28);
				nameLabel.CustomMinimumSize = new Vector2(0f, 64f);
				nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				var entryRow = new HBoxContainer();
				entryRow.AddThemeConstantOverride("separation", 12);
				entryRow.AddChild(nameLabel);
				entryRow.AddChild(buttonRow);
				var entry = new MarginContainer
				{
					Name = $"PresetEntry{i}",
					CustomMinimumSize = new Vector2(0f, 64f)
				};
				entry.AddThemeConstantOverride("margin_left", 12);
				entry.AddThemeConstantOverride("margin_right", 12);
				entry.AddChild(entryRow);
				_presetListContainer.AddChild(entry);
			}
		}
		if (_optionContainer != null)
		{
			SimpleModConfig.SetupFocusNeighbors(_optionContainer);
		}
	}

	private static void MakePresetButtonCompact(NConfigButton button)
	{
		button.CustomMinimumSize = CompactPresetButtonSize;
		button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
		button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		var image = button.GetNodeOrNull<TextureRect>("Image");
		if (image != null)
		{
			image.CustomMinimumSize = new Vector2(48f, 48f);
		}
		var label = button.GetNodeOrNull<Label>("Label");
		if (label?.LabelSettings != null)
		{
			label.LabelSettings.FontSize = 22;
			label.LabelSettings.OutlineSize = 8;
		}
	}

	private void SetPresetStatus(string labelName)
	{
		if (_presetStatusLabel != null)
		{
			_presetStatusLabel.Text = GetLabelText(labelName);
			_presetStatusLabel.Visible = true;
		}
	}

	private static string GetPresetErrorLabel(PresetOperationResult result)
	{
		return result switch
		{
			PresetOperationResult.EmptyName => "PresetStatusEmptyName", 
			PresetOperationResult.InvalidName => "PresetStatusInvalidName", 
			PresetOperationResult.NotFound => "PresetStatusNotFound", 
			PresetOperationResult.InvalidData => "PresetStatusInvalidData", 
			_ => "PresetStatusFailed", 
		};
	}

	private static string GetShareCodeErrorLabel(ShareCodeOperationResult result)
	{
		return result switch
		{
			ShareCodeOperationResult.EmptyCode => "ShareCodeStatusEmpty", 
			ShareCodeOperationResult.InvalidFormat => "ShareCodeStatusInvalidFormat", 
			ShareCodeOperationResult.UnsupportedVersion => "ShareCodeStatusUnsupportedVersion", 
			ShareCodeOperationResult.InvalidData => "ShareCodeStatusInvalidData", 
			_ => "ShareCodeStatusFailed", 
		};
	}

	public static void Register(string modId)
	{
		ConfigSchema.Validate();
		ConfigData currentConfig = ConfigManager.GetCurrentConfig();
		CopyFromConfigData(new ConfigData());
		var config = new ShopEnhancementBaseLibConfig();
		CopyFromConfigData(currentConfig);
		NormalizeRelicSellRuleSettings();
		config.SyncToLegacyConfig();
		config.ConfigChanged += delegate { config.HandleConfigChanged(); };
		config.OnConfigReloaded += config.HandleConfigReloaded;
		ModConfigRegistry.Register(modId, config);
	}

	private void HandleConfigChanged()
	{
		if (_syncing)
		{
			return;
		}
		if (_shareCodeInputChanged)
		{
			_shareCodeInputChanged = false;
			return;
		}
		var snapshot = CaptureRelicSellRuleSnapshot();
		bool presetChanged = snapshot.Preset != _lastRelicSellRuleSnapshot.Preset;
		bool optionsChanged = snapshot.Options != _lastRelicSellRuleSnapshot.Options;
		if (presetChanged)
		{
			if (RelicSellRulePreset != RelicSellPreset.Custom)
			{
				ApplyRelicSellRulePreset(RelicSellRulePreset);
			}
		}
		else if (optionsChanged)
		{
			bool waxChanged = snapshot.Options.AllowWaxRelics != _lastRelicSellRuleSnapshot.Options.AllowWaxRelics;
			bool meltedChanged = snapshot.Options.AllowMeltedRelics != _lastRelicSellRuleSnapshot.Options.AllowMeltedRelics;
			RelicSellRulePreset = RelicSellPreset.Custom;
			// Melted relics are a subset of wax relics, so keep the two toggles consistent.
			if (meltedChanged && AllowMeltedRelics && !AllowWaxRelics)
			{
				AllowWaxRelics = true;
			}
			else if (waxChanged && !AllowWaxRelics && AllowMeltedRelics)
			{
				AllowMeltedRelics = false;
			}
		}
		bool rangeEndpointAdjusted = _rangeEndpointAdjusted;
		_rangeEndpointAdjusted = false;
		SyncToLegacyConfig(presetChanged || optionsChanged || rangeEndpointAdjusted);
	}

	private void HandleConfigReloaded()
	{
		if (!_syncing)
		{
			NormalizeRelicSellRuleSettings();
			SyncToLegacyConfig();
		}
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
		SetEnchantAmountRange(data.EnchantAmountRange);
		SetEnchantCardCountRange(data.EnchantCardCountRange);
		EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
		SetGiftServiceCardCountRange(data.GiftServiceCardCountRange);
		GiftServiceBaseCost = data.GiftServiceBaseCost;
		GiftServiceStepCost = data.GiftServiceStepCost;
		_rangeEndpointAdjusted = false;
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

	private static void SetRangeMinimum(ref int minimum, ref int maximum, int value)
	{
		minimum = value;
		if (minimum > maximum)
		{
			maximum = minimum;
			_rangeEndpointAdjusted = true;
		}
	}

	private static void SetRangeMaximum(ref int minimum, ref int maximum, int value)
	{
		maximum = value;
		if (maximum < minimum)
		{
			minimum = maximum;
			_rangeEndpointAdjusted = true;
		}
	}

	private static void SetEnchantAmountRange(Vector2I range)
	{
		var sorted = SortedRange(range.X, range.Y);
		_enchantAmountMin = sorted.X;
		_enchantAmountMax = sorted.Y;
	}

	private static void SetEnchantCardCountRange(Vector2I range)
	{
		var sorted = SortedRange(range.X, range.Y);
		_enchantCardCountMin = sorted.X;
		_enchantCardCountMax = sorted.Y;
	}

	private static void SetGiftServiceCardCountRange(Vector2I range)
	{
		var sorted = SortedRange(range.X, range.Y);
		_giftServiceCardCountMin = sorted.X;
		_giftServiceCardCountMax = sorted.Y;
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
		var options = RelicSellRuleOptions.ForPreset(preset);
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
		return new RelicSellRuleSnapshot(RelicSellRulePreset, new RelicSellRuleOptions(AllowUsedUpRelics, AllowUponPickupRelics, AllowWaxRelics, AllowMeltedRelics, AllowDisabledRelics, AllowStarterRelics, AllowAncientRelics, AllowEventRelics));
	}
}
