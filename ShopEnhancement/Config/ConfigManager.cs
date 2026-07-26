using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace ShopEnhancement.Config;

public enum PresetOperationResult
{
    Success,
    EmptyName,
    InvalidName,
    NotFound,
    InvalidData,
    Failed
}

public static class ConfigManager
{
    private const string ConfigDirName = "ShopEnhancement";
    private const string PresetsDirName = "Presets";
    private const string ConfigFileName = "config.json";
    private const int MaxPresetNameLength = 32;
    private const float LegacyRelicPriceRatioDefault = 0.35f;
    private const float LegacyPotionPriceRatioDefault = 0.25f;
    private const int LegacyCommonRelicBasePrice = 175;
    private const int LegacyUncommonRelicBasePrice = 225;
    private const int LegacyRareRelicBasePrice = 275;
    private const int LegacyShopRelicBasePrice = 200;
    private const int LegacyAncientRelicBasePriceDefault = 750;
    private const int LegacyStarterRelicBasePriceDefault = 300;
    private const int LegacyEventRelicBasePriceDefault = 200;
    private const int LegacyCommonPotionBasePrice = 50;
    private const int LegacyUncommonPotionBasePrice = 75;
    private const int LegacyRarePotionBasePrice = 100;
    private static readonly JsonSerializerOptions CompactJsonOptions = new();
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    private static string UserDataDir => "user://";
    private static string ModConfigDir => System.IO.Path.Combine(ProjectSettings.GlobalizePath(UserDataDir), ConfigDirName);
    private static string PresetsDir => System.IO.Path.Combine(ModConfigDir, PresetsDirName);
    private static string ConfigPath => System.IO.Path.Combine(ModConfigDir, ConfigFileName);

    public static void EnsureDirectories()
    {
        if (!Directory.Exists(ModConfigDir))
            Directory.CreateDirectory(ModConfigDir);
        if (!Directory.Exists(PresetsDir))
            Directory.CreateDirectory(PresetsDir);
    }

    public static void Load()
    {
        EnsureDirectories();
        if (!File.Exists(ConfigPath))
        {
            Save(new ConfigData()); // Save default
            return;
        }

        try
        {
            string json = File.ReadAllText(ConfigPath);
            ConfigData? data = DeserializeConfigData(json, out bool migrated);
            if (data != null)
            {
                ApplyConfig(data);
                if (migrated)
                {
                    WriteConfigFile(data);
                    MainFile.Logger.Info("Migrated ShopEnhancement configuration to the current format.");
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to load config: {ex}");
        }
    }

    public static void Save(ConfigData data)
    {
        EnsureDirectories();
        try
        {
            ApplyConfig(data);
            WriteConfigFile(data);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save config: {ex}");
        }
    }

    private static void WriteConfigFile(ConfigData data)
    {
        string json = SerializeConfigData(data, writeIndented: true);
        File.WriteAllText(ConfigPath, json);
    }

    internal static string SerializeConfigData(ConfigData data, bool writeIndented = false)
    {
        return JsonSerializer.Serialize(
            data,
            writeIndented ? IndentedJsonOptions : CompactJsonOptions);
    }

    public static List<string> GetPresets()
    {
        EnsureDirectories();
        List<string> presets = new();
        try
        {
            foreach (string file in Directory.GetFiles(PresetsDir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (TryNormalizePresetName(name, out string normalizedName) && name == normalizedName)
                {
                    presets.Add(name);
                }
            }

            presets.Sort(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to list presets: {ex}");
        }
        return presets;
    }

    public static PresetOperationResult SavePreset(string name, ConfigData data, out string normalizedName)
    {
        normalizedName = "";
        if (!TryNormalizePresetName(name, out normalizedName))
        {
            return string.IsNullOrWhiteSpace(name)
                ? PresetOperationResult.EmptyName
                : PresetOperationResult.InvalidName;
        }

        EnsureDirectories();
        string? existingPath = FindPresetPath(normalizedName);
        string path = existingPath ?? Path.Combine(PresetsDir, $"{normalizedName}.json");
        if (existingPath != null)
        {
            normalizedName = Path.GetFileNameWithoutExtension(existingPath);
        }
        try
        {
            NormalizeConfigData(data);
            WritePresetFile(path, data);
            return PresetOperationResult.Success;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save preset '{normalizedName}': {ex}");
            return PresetOperationResult.Failed;
        }
    }

    public static PresetOperationResult LoadPreset(string name, out ConfigData? data)
    {
        data = null;
        if (!TryNormalizePresetName(name, out string normalizedName))
        {
            return string.IsNullOrWhiteSpace(name)
                ? PresetOperationResult.EmptyName
                : PresetOperationResult.InvalidName;
        }

        EnsureDirectories();
        string? path = FindPresetPath(normalizedName);
        if (path == null)
        {
            return PresetOperationResult.NotFound;
        }

        try
        {
            string json = File.ReadAllText(path);
            data = DeserializeConfigData(json, out bool migrated);
            if (data == null)
            {
                return PresetOperationResult.InvalidData;
            }

            if (migrated)
            {
                WritePresetFile(path, data);
                MainFile.Logger.Info($"Migrated preset '{normalizedName}' to the current configuration format.");
            }

            return PresetOperationResult.Success;
        }
        catch (JsonException ex)
        {
            MainFile.Logger.Error($"Failed to parse preset '{normalizedName}': {ex}");
            return PresetOperationResult.InvalidData;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to load preset '{normalizedName}': {ex}");
            return PresetOperationResult.Failed;
        }
    }

    private static void WritePresetFile(string path, ConfigData data)
    {
        string tempPath = $"{path}.tmp";
        try
        {
            string json = SerializeConfigData(data, writeIndented: true);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }
        }
    }

    public static PresetOperationResult DeletePreset(string name)
    {
        if (!TryNormalizePresetName(name, out string normalizedName))
        {
            return string.IsNullOrWhiteSpace(name)
                ? PresetOperationResult.EmptyName
                : PresetOperationResult.InvalidName;
        }

        EnsureDirectories();
        string? path = FindPresetPath(normalizedName);
        if (path == null)
        {
            return PresetOperationResult.NotFound;
        }

        try
        {
            File.Delete(path);
            return PresetOperationResult.Success;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to delete preset '{normalizedName}': {ex}");
            return PresetOperationResult.Failed;
        }
    }

    private static string? FindPresetPath(string normalizedName)
    {
        foreach (string file in Directory.GetFiles(PresetsDir, "*.json"))
        {
            string existingName = Path.GetFileNameWithoutExtension(file);
            if (string.Equals(existingName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    private static bool TryNormalizePresetName(string? name, out string normalizedName)
    {
        normalizedName = name?.Trim() ?? "";
        if (normalizedName.Length == 0 || normalizedName.Length > MaxPresetNameLength)
        {
            return false;
        }

        foreach (char character in normalizedName)
        {
            if (!char.IsLetterOrDigit(character)
                && character != '_'
                && character != '-'
                && character != ' ')
            {
                return false;
            }
        }

        string upperName = normalizedName.ToUpperInvariant();
        if (upperName is "CON" or "PRN" or "AUX" or "NUL"
            || (upperName.Length == 4
                && (upperName.StartsWith("COM", StringComparison.Ordinal)
                    || upperName.StartsWith("LPT", StringComparison.Ordinal))
                && upperName[3] is >= '1' and <= '9'))
        {
            return false;
        }

        string fullPath = Path.GetFullPath(Path.Combine(PresetsDir, $"{normalizedName}.json"));
        string fullPresetsDir = Path.GetFullPath(PresetsDir) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullPresetsDir, StringComparison.OrdinalIgnoreCase);
    }

    public static ConfigData GetCurrentConfig()
    {
        return new ConfigData
        {
            RemoveBaseCost = ShopEnhancementConfig.RemoveBaseCost,
            RemoveStepCost = ShopEnhancementConfig.RemoveStepCost,
            RemoveLimitPerShop = ShopEnhancementConfig.RemoveLimitPerShop,
            RefreshCost = ShopEnhancementConfig.RefreshCost,
            RefreshLimitPerShop = ShopEnhancementConfig.RefreshLimitPerShop,
            RelicRefreshMode = ShopEnhancementConfig.RelicRefreshMode,
            EnableNoPurchaseReward = ShopEnhancementConfig.EnableNoPurchaseReward,
            NoPurchaseRewardGold = ShopEnhancementConfig.NoPurchaseRewardGold,
            EnableSkipCardRewardGold = ShopEnhancementConfig.EnableSkipCardRewardGold,
            SkipCardRewardGoldAmount = ShopEnhancementConfig.SkipCardRewardGoldAmount,
            EnableCrossClassCards = ShopEnhancementConfig.EnableCrossClassCards,
            CrossClassCardChance = ShopEnhancementConfig.CrossClassCardChance,
            EnableSellMode = ShopEnhancementConfig.EnableSellMode,
            EnableSellPriceVariance = ShopEnhancementConfig.EnableSellPriceVariance,
            SellPriceVariance = ShopEnhancementConfig.SellPriceVariance,
            EnableGiftMode = ShopEnhancementConfig.EnableGiftMode,
            RelicSellRulePreset = ShopEnhancementConfig.RelicSellRulePreset,
            AllowUsedUpRelics = ShopEnhancementConfig.AllowUsedUpRelics,
            AllowUponPickupRelics = ShopEnhancementConfig.AllowUponPickupRelics,
            AllowWaxRelics = ShopEnhancementConfig.AllowWaxRelics,
            AllowMeltedRelics = ShopEnhancementConfig.AllowMeltedRelics,
            AllowDisabledRelics = ShopEnhancementConfig.AllowDisabledRelics,
            AllowStarterRelics = ShopEnhancementConfig.AllowStarterRelics,
            AllowAncientRelics = ShopEnhancementConfig.AllowAncientRelics,
            AllowEventRelics = ShopEnhancementConfig.AllowEventRelics,
            SellCommonRelicPrice = ShopEnhancementConfig.SellCommonRelicPrice,
            SellUncommonRelicPrice = ShopEnhancementConfig.SellUncommonRelicPrice,
            SellRareRelicPrice = ShopEnhancementConfig.SellRareRelicPrice,
            SellShopRelicPrice = ShopEnhancementConfig.SellShopRelicPrice,
            SellAncientRelicPrice = ShopEnhancementConfig.SellAncientRelicPrice,
            SellStarterRelicPrice = ShopEnhancementConfig.SellStarterRelicPrice,
            SellEventRelicPrice = ShopEnhancementConfig.SellEventRelicPrice,
            SellRelicMinGold = ShopEnhancementConfig.SellRelicMinGold,
            SellCommonPotionPrice = ShopEnhancementConfig.SellCommonPotionPrice,
            SellUncommonPotionPrice = ShopEnhancementConfig.SellUncommonPotionPrice,
            SellRarePotionPrice = ShopEnhancementConfig.SellRarePotionPrice,
            SellPotionMinGold = ShopEnhancementConfig.SellPotionMinGold,
            EnableRemovalEnchantRandom = ShopEnhancementConfig.EnableRemovalEnchantRandom,
            EnableEnchantService = ShopEnhancementConfig.EnableEnchantService,
            EnchantStartShopVisit = ShopEnhancementConfig.EnchantStartShopVisit,
            EnchantReplaceChance = ShopEnhancementConfig.EnchantReplaceChance,
            EnchantCost = ShopEnhancementConfig.EnchantCost,
            EnchantAmountRange = ShopEnhancementConfig.EnchantAmountRange,
            EnchantCardCountRange = ShopEnhancementConfig.EnchantCardCountRange,
            EnableRandomTeammateGiftService = ShopEnhancementConfig.EnableRandomTeammateGiftService,
            GiftServiceCardCountRange = ShopEnhancementConfig.GiftServiceCardCountRange,
            GiftServiceBaseCost = ShopEnhancementConfig.GiftServiceBaseCost,
            GiftServiceStepCost = ShopEnhancementConfig.GiftServiceStepCost
        };
    }

    public static void ApplyConfig(ConfigData data)
    {
        NormalizeConfigData(data);

        ShopEnhancementConfig.RemoveBaseCost = data.RemoveBaseCost;
        ShopEnhancementConfig.RemoveStepCost = data.RemoveStepCost;
        ShopEnhancementConfig.RemoveLimitPerShop = data.RemoveLimitPerShop;
        ShopEnhancementConfig.RefreshCost = data.RefreshCost;
        ShopEnhancementConfig.RefreshLimitPerShop = data.RefreshLimitPerShop;
        ShopEnhancementConfig.RelicRefreshMode = data.RelicRefreshMode;
        ShopEnhancementConfig.EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        ShopEnhancementConfig.NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        ShopEnhancementConfig.EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        ShopEnhancementConfig.SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        ShopEnhancementConfig.EnableCrossClassCards = data.EnableCrossClassCards;
        ShopEnhancementConfig.CrossClassCardChance = data.CrossClassCardChance;
        ShopEnhancementConfig.EnableSellMode = data.EnableSellMode;
        ShopEnhancementConfig.EnableSellPriceVariance = data.EnableSellPriceVariance;
        ShopEnhancementConfig.SellPriceVariance = data.SellPriceVariance;
        ShopEnhancementConfig.EnableGiftMode = data.EnableGiftMode;
        ShopEnhancementConfig.RelicSellRulePreset = data.RelicSellRulePreset;
        ShopEnhancementConfig.AllowUsedUpRelics = data.AllowUsedUpRelics;
        ShopEnhancementConfig.AllowUponPickupRelics = data.AllowUponPickupRelics;
        ShopEnhancementConfig.AllowWaxRelics = data.AllowWaxRelics;
        ShopEnhancementConfig.AllowMeltedRelics = data.AllowMeltedRelics;
        ShopEnhancementConfig.AllowDisabledRelics = data.AllowDisabledRelics;
        ShopEnhancementConfig.AllowStarterRelics = data.AllowStarterRelics;
        ShopEnhancementConfig.AllowAncientRelics = data.AllowAncientRelics;
        ShopEnhancementConfig.AllowEventRelics = data.AllowEventRelics;
        ShopEnhancementConfig.SellCommonRelicPrice = data.SellCommonRelicPrice;
        ShopEnhancementConfig.SellUncommonRelicPrice = data.SellUncommonRelicPrice;
        ShopEnhancementConfig.SellRareRelicPrice = data.SellRareRelicPrice;
        ShopEnhancementConfig.SellShopRelicPrice = data.SellShopRelicPrice;
        ShopEnhancementConfig.SellAncientRelicPrice = data.SellAncientRelicPrice;
        ShopEnhancementConfig.SellStarterRelicPrice = data.SellStarterRelicPrice;
        ShopEnhancementConfig.SellEventRelicPrice = data.SellEventRelicPrice;
        ShopEnhancementConfig.SellRelicMinGold = data.SellRelicMinGold;
        ShopEnhancementConfig.SellCommonPotionPrice = data.SellCommonPotionPrice;
        ShopEnhancementConfig.SellUncommonPotionPrice = data.SellUncommonPotionPrice;
        ShopEnhancementConfig.SellRarePotionPrice = data.SellRarePotionPrice;
        ShopEnhancementConfig.SellPotionMinGold = data.SellPotionMinGold;
        ShopEnhancementConfig.EnableRemovalEnchantRandom = data.EnableRemovalEnchantRandom;
        ShopEnhancementConfig.EnableEnchantService = data.EnableEnchantService;
        ShopEnhancementConfig.EnchantStartShopVisit = data.EnchantStartShopVisit;
        ShopEnhancementConfig.EnchantReplaceChance = data.EnchantReplaceChance;
        ShopEnhancementConfig.EnchantCost = data.EnchantCost;
        ShopEnhancementConfig.EnchantAmountRange = data.EnchantAmountRange;
        ShopEnhancementConfig.EnchantCardCountRange = data.EnchantCardCountRange;
        ShopEnhancementConfig.EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
        ShopEnhancementConfig.GiftServiceCardCountRange = data.GiftServiceCardCountRange;
        ShopEnhancementConfig.GiftServiceBaseCost = data.GiftServiceBaseCost;
        ShopEnhancementConfig.GiftServiceStepCost = data.GiftServiceStepCost;
    }

    internal static ConfigData? DeserializeConfigData(
        string json,
        out bool migrated,
        bool rejectInvalidValues = false)
    {
        migrated = false;
        ConfigData? data = JsonSerializer.Deserialize<ConfigData>(json, CompactJsonOptions);
        if (data == null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        bool migratedLegacySellConfig = TryMigrateLegacySellConfig(data, document.RootElement);
        bool migratedSellPriceVariance = !migratedLegacySellConfig
            && TryMigrateLegacySellPriceVariance(data, document.RootElement);
        bool migratedBrokenRanges = TryMigrateBrokenRanges(data, document.RootElement);
        if (rejectInvalidValues && !IsConfigDataValid(data))
        {
            return null;
        }

        bool normalized = NormalizeConfigData(data);
        migrated = migratedLegacySellConfig
            || migratedSellPriceVariance
            || migratedBrokenRanges
            || normalized;
        return data;
    }

    internal static bool IsConfigDataValid(ConfigData data)
    {
        return IsValidInt(data.RemoveBaseCost, ConfigSchema.RemoveBaseCost.Min, ConfigSchema.RemoveBaseCost.Max, ConfigSchema.RemoveBaseCost.Step)
            && IsValidInt(data.RemoveStepCost, ConfigSchema.RemoveStepCost.Min, ConfigSchema.RemoveStepCost.Max, ConfigSchema.RemoveStepCost.Step)
            && IsValidInt(data.RemoveLimitPerShop, ConfigSchema.RemoveLimitPerShop.Min, ConfigSchema.RemoveLimitPerShop.Max, ConfigSchema.RemoveLimitPerShop.Step)
            && IsValidInt(data.RefreshCost, ConfigSchema.RefreshCost.Min, ConfigSchema.RefreshCost.Max, ConfigSchema.RefreshCost.Step)
            && IsValidInt(data.RefreshLimitPerShop, ConfigSchema.RefreshLimitPerShop.Min, ConfigSchema.RefreshLimitPerShop.Max, ConfigSchema.RefreshLimitPerShop.Step)
            && Enum.IsDefined(data.RelicRefreshMode)
            && IsValidInt(data.NoPurchaseRewardGold, ConfigSchema.NoPurchaseRewardGold.Min, ConfigSchema.NoPurchaseRewardGold.Max, ConfigSchema.NoPurchaseRewardGold.Step)
            && IsValidInt(data.SkipCardRewardGoldAmount, ConfigSchema.SkipCardRewardGoldAmount.Min, ConfigSchema.SkipCardRewardGoldAmount.Max, ConfigSchema.SkipCardRewardGoldAmount.Step)
            && IsValidFloat(data.CrossClassCardChance, ConfigSchema.CrossClassCardChance.Min, ConfigSchema.CrossClassCardChance.Max, ConfigSchema.CrossClassCardChance.Step)
            && IsValidFloat(data.SellPriceVariance, ConfigSchema.SellPriceVariance.Min, ConfigSchema.SellPriceVariance.Max, ConfigSchema.SellPriceVariance.Step)
            && Enum.IsDefined(data.RelicSellRulePreset)
            && IsValidInt(data.SellCommonRelicPrice, ConfigSchema.SellCommonRelicPrice.Min, ConfigSchema.SellCommonRelicPrice.Max, ConfigSchema.SellCommonRelicPrice.Step)
            && IsValidInt(data.SellUncommonRelicPrice, ConfigSchema.SellUncommonRelicPrice.Min, ConfigSchema.SellUncommonRelicPrice.Max, ConfigSchema.SellUncommonRelicPrice.Step)
            && IsValidInt(data.SellRareRelicPrice, ConfigSchema.SellRareRelicPrice.Min, ConfigSchema.SellRareRelicPrice.Max, ConfigSchema.SellRareRelicPrice.Step)
            && IsValidInt(data.SellShopRelicPrice, ConfigSchema.SellShopRelicPrice.Min, ConfigSchema.SellShopRelicPrice.Max, ConfigSchema.SellShopRelicPrice.Step)
            && IsValidInt(data.SellAncientRelicPrice, ConfigSchema.SellAncientRelicPrice.Min, ConfigSchema.SellAncientRelicPrice.Max, ConfigSchema.SellAncientRelicPrice.Step)
            && IsValidInt(data.SellStarterRelicPrice, ConfigSchema.SellStarterRelicPrice.Min, ConfigSchema.SellStarterRelicPrice.Max, ConfigSchema.SellStarterRelicPrice.Step)
            && IsValidInt(data.SellEventRelicPrice, ConfigSchema.SellEventRelicPrice.Min, ConfigSchema.SellEventRelicPrice.Max, ConfigSchema.SellEventRelicPrice.Step)
            && IsValidInt(data.SellRelicMinGold, ConfigSchema.SellRelicMinGold.Min, ConfigSchema.SellRelicMinGold.Max, ConfigSchema.SellRelicMinGold.Step)
            && IsValidInt(data.SellCommonPotionPrice, ConfigSchema.SellCommonPotionPrice.Min, ConfigSchema.SellCommonPotionPrice.Max, ConfigSchema.SellCommonPotionPrice.Step)
            && IsValidInt(data.SellUncommonPotionPrice, ConfigSchema.SellUncommonPotionPrice.Min, ConfigSchema.SellUncommonPotionPrice.Max, ConfigSchema.SellUncommonPotionPrice.Step)
            && IsValidInt(data.SellRarePotionPrice, ConfigSchema.SellRarePotionPrice.Min, ConfigSchema.SellRarePotionPrice.Max, ConfigSchema.SellRarePotionPrice.Step)
            && IsValidInt(data.SellPotionMinGold, ConfigSchema.SellPotionMinGold.Min, ConfigSchema.SellPotionMinGold.Max, ConfigSchema.SellPotionMinGold.Step)
            && IsValidInt(data.EnchantStartShopVisit, ConfigSchema.EnchantStartShopVisit.Min, ConfigSchema.EnchantStartShopVisit.Max, ConfigSchema.EnchantStartShopVisit.Step)
            && IsValidFloat(data.EnchantReplaceChance, ConfigSchema.EnchantReplaceChance.Min, ConfigSchema.EnchantReplaceChance.Max, ConfigSchema.EnchantReplaceChance.Step)
            && IsValidInt(data.EnchantCost, ConfigSchema.EnchantCost.Min, ConfigSchema.EnchantCost.Max, ConfigSchema.EnchantCost.Step)
            && IsValidRange(data.EnchantAmountRange, ConfigSchema.EnchantAmount.Min, ConfigSchema.EnchantAmount.Max, ConfigSchema.EnchantAmount.Step)
            && IsValidRange(data.EnchantCardCountRange, ConfigSchema.EnchantCardCount.Min, ConfigSchema.EnchantCardCount.Max, ConfigSchema.EnchantCardCount.Step)
            && IsValidRange(data.GiftServiceCardCountRange, ConfigSchema.GiftServiceCardCount.Min, ConfigSchema.GiftServiceCardCount.Max, ConfigSchema.GiftServiceCardCount.Step)
            && IsValidInt(data.GiftServiceBaseCost, ConfigSchema.GiftServiceBaseCost.Min, ConfigSchema.GiftServiceBaseCost.Max, ConfigSchema.GiftServiceBaseCost.Step)
            && IsValidInt(data.GiftServiceStepCost, ConfigSchema.GiftServiceStepCost.Min, ConfigSchema.GiftServiceStepCost.Max, ConfigSchema.GiftServiceStepCost.Step);
    }

    private static bool NormalizeConfigData(ConfigData data)
    {
        bool changed = false;

        changed |= NormalizeInt(data.RemoveBaseCost, ConfigSchema.RemoveBaseCost.Min, ConfigSchema.RemoveBaseCost.Max, ConfigSchema.RemoveBaseCost.Step, value => data.RemoveBaseCost = value);
        changed |= NormalizeInt(data.RemoveStepCost, ConfigSchema.RemoveStepCost.Min, ConfigSchema.RemoveStepCost.Max, ConfigSchema.RemoveStepCost.Step, value => data.RemoveStepCost = value);
        changed |= NormalizeInt(data.RemoveLimitPerShop, ConfigSchema.RemoveLimitPerShop.Min, ConfigSchema.RemoveLimitPerShop.Max, ConfigSchema.RemoveLimitPerShop.Step, value => data.RemoveLimitPerShop = value);
        changed |= NormalizeInt(data.RefreshCost, ConfigSchema.RefreshCost.Min, ConfigSchema.RefreshCost.Max, ConfigSchema.RefreshCost.Step, value => data.RefreshCost = value);
        changed |= NormalizeInt(data.RefreshLimitPerShop, ConfigSchema.RefreshLimitPerShop.Min, ConfigSchema.RefreshLimitPerShop.Max, ConfigSchema.RefreshLimitPerShop.Step, value => data.RefreshLimitPerShop = value);
        if (!Enum.IsDefined(data.RelicRefreshMode))
        {
            data.RelicRefreshMode = ConfigSchema.RelicRefreshMode.Default;
            changed = true;
        }

        changed |= NormalizeInt(data.NoPurchaseRewardGold, ConfigSchema.NoPurchaseRewardGold.Min, ConfigSchema.NoPurchaseRewardGold.Max, ConfigSchema.NoPurchaseRewardGold.Step, value => data.NoPurchaseRewardGold = value);
        changed |= NormalizeInt(data.SkipCardRewardGoldAmount, ConfigSchema.SkipCardRewardGoldAmount.Min, ConfigSchema.SkipCardRewardGoldAmount.Max, ConfigSchema.SkipCardRewardGoldAmount.Step, value => data.SkipCardRewardGoldAmount = value);
        changed |= NormalizeFloat(data.CrossClassCardChance, ConfigSchema.CrossClassCardChance.Default, ConfigSchema.CrossClassCardChance.Min, ConfigSchema.CrossClassCardChance.Max, ConfigSchema.CrossClassCardChance.Step, value => data.CrossClassCardChance = value);
        changed |= NormalizeFloat(data.SellPriceVariance, ConfigSchema.SellPriceVariance.Default, ConfigSchema.SellPriceVariance.Min, ConfigSchema.SellPriceVariance.Max, ConfigSchema.SellPriceVariance.Step, value => data.SellPriceVariance = value);
        if (!Enum.IsDefined(data.RelicSellRulePreset))
        {
            data.RelicSellRulePreset = ConfigSchema.RelicSellRulePreset.Default;
            changed = true;
        }

        changed |= NormalizeInt(data.SellCommonRelicPrice, ConfigSchema.SellCommonRelicPrice.Min, ConfigSchema.SellCommonRelicPrice.Max, ConfigSchema.SellCommonRelicPrice.Step, value => data.SellCommonRelicPrice = value);
        changed |= NormalizeInt(data.SellUncommonRelicPrice, ConfigSchema.SellUncommonRelicPrice.Min, ConfigSchema.SellUncommonRelicPrice.Max, ConfigSchema.SellUncommonRelicPrice.Step, value => data.SellUncommonRelicPrice = value);
        changed |= NormalizeInt(data.SellRareRelicPrice, ConfigSchema.SellRareRelicPrice.Min, ConfigSchema.SellRareRelicPrice.Max, ConfigSchema.SellRareRelicPrice.Step, value => data.SellRareRelicPrice = value);
        changed |= NormalizeInt(data.SellShopRelicPrice, ConfigSchema.SellShopRelicPrice.Min, ConfigSchema.SellShopRelicPrice.Max, ConfigSchema.SellShopRelicPrice.Step, value => data.SellShopRelicPrice = value);
        changed |= NormalizeInt(data.SellAncientRelicPrice, ConfigSchema.SellAncientRelicPrice.Min, ConfigSchema.SellAncientRelicPrice.Max, ConfigSchema.SellAncientRelicPrice.Step, value => data.SellAncientRelicPrice = value);
        changed |= NormalizeInt(data.SellStarterRelicPrice, ConfigSchema.SellStarterRelicPrice.Min, ConfigSchema.SellStarterRelicPrice.Max, ConfigSchema.SellStarterRelicPrice.Step, value => data.SellStarterRelicPrice = value);
        changed |= NormalizeInt(data.SellEventRelicPrice, ConfigSchema.SellEventRelicPrice.Min, ConfigSchema.SellEventRelicPrice.Max, ConfigSchema.SellEventRelicPrice.Step, value => data.SellEventRelicPrice = value);
        changed |= NormalizeInt(data.SellRelicMinGold, ConfigSchema.SellRelicMinGold.Min, ConfigSchema.SellRelicMinGold.Max, ConfigSchema.SellRelicMinGold.Step, value => data.SellRelicMinGold = value);
        changed |= NormalizeInt(data.SellCommonPotionPrice, ConfigSchema.SellCommonPotionPrice.Min, ConfigSchema.SellCommonPotionPrice.Max, ConfigSchema.SellCommonPotionPrice.Step, value => data.SellCommonPotionPrice = value);
        changed |= NormalizeInt(data.SellUncommonPotionPrice, ConfigSchema.SellUncommonPotionPrice.Min, ConfigSchema.SellUncommonPotionPrice.Max, ConfigSchema.SellUncommonPotionPrice.Step, value => data.SellUncommonPotionPrice = value);
        changed |= NormalizeInt(data.SellRarePotionPrice, ConfigSchema.SellRarePotionPrice.Min, ConfigSchema.SellRarePotionPrice.Max, ConfigSchema.SellRarePotionPrice.Step, value => data.SellRarePotionPrice = value);
        changed |= NormalizeInt(data.SellPotionMinGold, ConfigSchema.SellPotionMinGold.Min, ConfigSchema.SellPotionMinGold.Max, ConfigSchema.SellPotionMinGold.Step, value => data.SellPotionMinGold = value);

        changed |= NormalizeInt(data.EnchantStartShopVisit, ConfigSchema.EnchantStartShopVisit.Min, ConfigSchema.EnchantStartShopVisit.Max, ConfigSchema.EnchantStartShopVisit.Step, value => data.EnchantStartShopVisit = value);
        changed |= NormalizeFloat(data.EnchantReplaceChance, ConfigSchema.EnchantReplaceChance.Default, ConfigSchema.EnchantReplaceChance.Min, ConfigSchema.EnchantReplaceChance.Max, ConfigSchema.EnchantReplaceChance.Step, value => data.EnchantReplaceChance = value);
        changed |= NormalizeInt(data.EnchantCost, ConfigSchema.EnchantCost.Min, ConfigSchema.EnchantCost.Max, ConfigSchema.EnchantCost.Step, value => data.EnchantCost = value);
        changed |= NormalizeRange(data.EnchantAmountRange, ConfigSchema.EnchantAmount.Min, ConfigSchema.EnchantAmount.Max, ConfigSchema.EnchantAmount.Step, value => data.EnchantAmountRange = value);
        changed |= NormalizeRange(data.EnchantCardCountRange, ConfigSchema.EnchantCardCount.Min, ConfigSchema.EnchantCardCount.Max, ConfigSchema.EnchantCardCount.Step, value => data.EnchantCardCountRange = value);
        changed |= NormalizeRange(data.GiftServiceCardCountRange, ConfigSchema.GiftServiceCardCount.Min, ConfigSchema.GiftServiceCardCount.Max, ConfigSchema.GiftServiceCardCount.Step, value => data.GiftServiceCardCountRange = value);
        changed |= NormalizeInt(data.GiftServiceBaseCost, ConfigSchema.GiftServiceBaseCost.Min, ConfigSchema.GiftServiceBaseCost.Max, ConfigSchema.GiftServiceBaseCost.Step, value => data.GiftServiceBaseCost = value);
        changed |= NormalizeInt(data.GiftServiceStepCost, ConfigSchema.GiftServiceStepCost.Min, ConfigSchema.GiftServiceStepCost.Max, ConfigSchema.GiftServiceStepCost.Step, value => data.GiftServiceStepCost = value);

        return changed;
    }

    private static bool TryMigrateBrokenRanges(ConfigData data, JsonElement root)
    {
        bool migrated = false;
        if (HasBrokenRange(root, nameof(ConfigData.EnchantAmountRange)))
        {
            data.EnchantAmountRange = new Vector2I(
                ConfigSchema.EnchantAmount.DefaultMin,
                ConfigSchema.EnchantAmount.DefaultMax);
            migrated = true;
        }

        if (HasBrokenRange(root, nameof(ConfigData.EnchantCardCountRange)))
        {
            data.EnchantCardCountRange = new Vector2I(
                ConfigSchema.EnchantCardCount.DefaultMin,
                ConfigSchema.EnchantCardCount.DefaultMax);
            migrated = true;
        }

        if (HasBrokenRange(root, nameof(ConfigData.GiftServiceCardCountRange)))
        {
            data.GiftServiceCardCountRange = new Vector2I(
                ConfigSchema.GiftServiceCardCount.DefaultMin,
                ConfigSchema.GiftServiceCardCount.DefaultMax);
            migrated = true;
        }

        return migrated;
    }

    private static bool HasBrokenRange(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement range)
            || range.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        bool hasMin = range.TryGetProperty("Min", out _) || range.TryGetProperty("X", out _);
        bool hasMax = range.TryGetProperty("Max", out _) || range.TryGetProperty("Y", out _);
        return !hasMin || !hasMax;
    }

    private static bool TryMigrateLegacySellConfig(ConfigData data, JsonElement root)
    {
        if (HasAnyNewSellPrice(root) || !HasAnyLegacySellConfig(root))
        {
            return false;
        }

        float relicRatio = GetFloat(root, "SellRelicPriceRatio", LegacyRelicPriceRatioDefault);
        float potionRatio = GetFloat(root, "SellPotionPriceRatio", LegacyPotionPriceRatioDefault);
        int ancientBasePrice = GetInt(root, "SellAncientRelicBasePrice", LegacyAncientRelicBasePriceDefault);
        int starterBasePrice = GetInt(root, "SellStarterRelicBasePrice", LegacyStarterRelicBasePriceDefault);
        int eventBasePrice = GetInt(root, "SellEventRelicBasePrice", LegacyEventRelicBasePriceDefault);

        data.SellCommonRelicPrice = LegacySellPrice(LegacyCommonRelicBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellUncommonRelicPrice = LegacySellPrice(LegacyUncommonRelicBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellRareRelicPrice = LegacySellPrice(LegacyRareRelicBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellShopRelicPrice = LegacySellPrice(LegacyShopRelicBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellAncientRelicPrice = LegacySellPrice(ancientBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellStarterRelicPrice = LegacySellPrice(starterBasePrice, relicRatio, data.SellRelicMinGold);
        data.SellEventRelicPrice = LegacySellPrice(eventBasePrice, relicRatio, data.SellRelicMinGold);

        data.SellCommonPotionPrice = LegacySellPrice(LegacyCommonPotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.SellUncommonPotionPrice = LegacySellPrice(LegacyUncommonPotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.SellRarePotionPrice = LegacySellPrice(LegacyRarePotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.EnableSellPriceVariance = false;
        data.SellPriceVariance = ConfigSchema.SellPriceVariance.Default;

        return true;
    }

    private static bool TryMigrateLegacySellPriceVariance(ConfigData data, JsonElement root)
    {
        if (root.TryGetProperty(nameof(ConfigData.EnableSellPriceVariance), out _)
            || root.TryGetProperty(nameof(ConfigData.SellPriceVariance), out _))
        {
            return false;
        }

        bool hasRelicVariance = root.TryGetProperty("SellRelicPriceVariance", out _);
        bool hasPotionVariance = root.TryGetProperty("SellPotionPriceVariance", out _);
        if (!hasRelicVariance && !hasPotionVariance)
        {
            return false;
        }

        float relicVariance = hasRelicVariance ? GetFloat(root, "SellRelicPriceVariance", 0f) : 0f;
        float potionVariance = hasPotionVariance ? GetFloat(root, "SellPotionPriceVariance", 0f) : 0f;
        float variance = Math.Clamp(Math.Max(relicVariance, potionVariance), 0f, 1f);
        data.EnableSellPriceVariance = variance > 0f;
        data.SellPriceVariance = variance > 0f ? variance : ConfigSchema.SellPriceVariance.Default;
        return true;
    }

    private static bool HasAnyNewSellPrice(JsonElement root)
    {
        return root.TryGetProperty(nameof(ConfigData.SellCommonRelicPrice), out _)
            || root.TryGetProperty(nameof(ConfigData.SellUncommonRelicPrice), out _)
            || root.TryGetProperty(nameof(ConfigData.SellRareRelicPrice), out _)
            || root.TryGetProperty(nameof(ConfigData.SellCommonPotionPrice), out _)
            || root.TryGetProperty(nameof(ConfigData.SellUncommonPotionPrice), out _)
            || root.TryGetProperty(nameof(ConfigData.SellRarePotionPrice), out _);
    }

    private static bool HasAnyLegacySellConfig(JsonElement root)
    {
        return root.TryGetProperty("SellRelicPriceRatio", out _)
            || root.TryGetProperty("SellPotionPriceRatio", out _)
            || root.TryGetProperty("SellAncientRelicBasePrice", out _)
            || root.TryGetProperty("SellStarterRelicBasePrice", out _)
            || root.TryGetProperty("SellEventRelicBasePrice", out _);
    }

    private static int LegacySellPrice(int basePrice, float ratio, int minGold)
    {
        int value = (int)Math.Floor(Math.Max(0, basePrice) * Math.Max(0f, ratio));
        return Math.Max(value, Math.Max(0, minGold));
    }

    private static float GetFloat(JsonElement root, string propertyName, float fallback)
    {
        return root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetSingle(out float result)
                ? result
                : fallback;
    }

    private static int GetInt(JsonElement root, string propertyName, int fallback)
    {
        return root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int result)
                ? result
                : fallback;
    }

    private static bool IsValidInt(int value, int min, int max, int step)
    {
        return value >= min && value <= max && (value - min) % step == 0;
    }

    private static bool IsValidFloat(float value, double min, double max, double step)
    {
        if (!float.IsFinite(value) || value < min || value > max)
        {
            return false;
        }

        double stepIndex = (value - min) / step;
        return Math.Abs(stepIndex - Math.Round(stepIndex)) < 0.0001;
    }

    private static bool IsValidRange(Vector2I range, int min, int max, int step)
    {
        return range.X <= range.Y
            && IsValidInt(range.X, min, max, step)
            && IsValidInt(range.Y, min, max, step);
    }

    private static bool NormalizeInt(int value, int min, int max, int step, Action<int> apply)
    {
        int normalized = Math.Clamp(value, min, max);
        normalized = min + ((normalized - min) / step) * step;
        if (normalized == value)
        {
            return false;
        }

        apply(normalized);
        return true;
    }

    private static bool NormalizeFloat(
        float value,
        float fallback,
        double min,
        double max,
        double step,
        Action<float> apply)
    {
        double source = float.IsFinite(value) ? value : fallback;
        double clamped = Math.Clamp(source, min, max);
        double normalized = min + Math.Round((clamped - min) / step) * step;
        float normalizedValue = (float)Math.Clamp(normalized, min, max);
        if (normalizedValue == value)
        {
            return false;
        }

        apply(normalizedValue);
        return true;
    }

    private static bool NormalizeRange(
        Vector2I range,
        int min,
        int max,
        int step,
        Action<Vector2I> apply)
    {
        int normalizedMin = NormalizeIntValue(Math.Min(range.X, range.Y), min, max, step);
        int normalizedMax = NormalizeIntValue(Math.Max(range.X, range.Y), min, max, step);
        Vector2I normalized = new(normalizedMin, Math.Max(normalizedMin, normalizedMax));
        if (normalized == range)
        {
            return false;
        }

        apply(normalized);
        return true;
    }

    private static int NormalizeIntValue(int value, int min, int max, int step)
    {
        int normalized = Math.Clamp(value, min, max);
        return min + ((normalized - min) / step) * step;
    }
}
