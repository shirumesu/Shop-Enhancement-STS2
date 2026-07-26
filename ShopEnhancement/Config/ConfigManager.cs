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
        string tempPath = $"{path}.tmp";

        try
        {
            string json = SerializeConfigData(data, writeIndented: true);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, true);
            return PresetOperationResult.Success;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save preset '{normalizedName}': {ex}");
            return PresetOperationResult.Failed;
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
        ShopEnhancementConfig.RemoveBaseCost = data.RemoveBaseCost;
        ShopEnhancementConfig.RemoveStepCost = data.RemoveStepCost;
        ShopEnhancementConfig.RemoveLimitPerShop = Math.Max(ConfigSchema.RemoveLimitPerShop.Min, data.RemoveLimitPerShop);
        ShopEnhancementConfig.RefreshCost = data.RefreshCost;
        ShopEnhancementConfig.RefreshLimitPerShop = data.RefreshLimitPerShop;
        ShopEnhancementConfig.RelicRefreshMode = Enum.IsDefined(data.RelicRefreshMode)
            ? data.RelicRefreshMode
            : ConfigSchema.RelicRefreshMode.Default;
        ShopEnhancementConfig.EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        ShopEnhancementConfig.NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        ShopEnhancementConfig.EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        ShopEnhancementConfig.SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        ShopEnhancementConfig.EnableCrossClassCards = data.EnableCrossClassCards;
        ShopEnhancementConfig.CrossClassCardChance = data.CrossClassCardChance;
        ShopEnhancementConfig.EnableSellMode = data.EnableSellMode;
        ShopEnhancementConfig.EnableSellPriceVariance = data.EnableSellPriceVariance;
        ShopEnhancementConfig.SellPriceVariance = Math.Clamp(
            data.SellPriceVariance,
            (float)ConfigSchema.SellPriceVariance.Min,
            (float)ConfigSchema.SellPriceVariance.Max);
        ShopEnhancementConfig.EnableGiftMode = data.EnableGiftMode;
        ShopEnhancementConfig.RelicSellRulePreset = Enum.IsDefined(data.RelicSellRulePreset)
            ? data.RelicSellRulePreset
            : ConfigSchema.RelicSellRulePreset.Default;
        ShopEnhancementConfig.AllowUsedUpRelics = data.AllowUsedUpRelics;
        ShopEnhancementConfig.AllowUponPickupRelics = data.AllowUponPickupRelics;
        ShopEnhancementConfig.AllowWaxRelics = data.AllowWaxRelics;
        ShopEnhancementConfig.AllowMeltedRelics = data.AllowMeltedRelics;
        ShopEnhancementConfig.AllowDisabledRelics = data.AllowDisabledRelics;
        ShopEnhancementConfig.AllowStarterRelics = data.AllowStarterRelics;
        ShopEnhancementConfig.AllowAncientRelics = data.AllowAncientRelics;
        ShopEnhancementConfig.AllowEventRelics = data.AllowEventRelics;
        ShopEnhancementConfig.SellCommonRelicPrice = Math.Max(ConfigSchema.SellCommonRelicPrice.Min, data.SellCommonRelicPrice);
        ShopEnhancementConfig.SellUncommonRelicPrice = Math.Max(ConfigSchema.SellUncommonRelicPrice.Min, data.SellUncommonRelicPrice);
        ShopEnhancementConfig.SellRareRelicPrice = Math.Max(ConfigSchema.SellRareRelicPrice.Min, data.SellRareRelicPrice);
        ShopEnhancementConfig.SellShopRelicPrice = Math.Max(ConfigSchema.SellShopRelicPrice.Min, data.SellShopRelicPrice);
        ShopEnhancementConfig.SellAncientRelicPrice = Math.Max(ConfigSchema.SellAncientRelicPrice.Min, data.SellAncientRelicPrice);
        ShopEnhancementConfig.SellStarterRelicPrice = Math.Max(ConfigSchema.SellStarterRelicPrice.Min, data.SellStarterRelicPrice);
        ShopEnhancementConfig.SellEventRelicPrice = Math.Max(ConfigSchema.SellEventRelicPrice.Min, data.SellEventRelicPrice);
        ShopEnhancementConfig.SellRelicMinGold = Math.Max(ConfigSchema.SellRelicMinGold.Min, data.SellRelicMinGold);
        ShopEnhancementConfig.SellCommonPotionPrice = Math.Max(ConfigSchema.SellCommonPotionPrice.Min, data.SellCommonPotionPrice);
        ShopEnhancementConfig.SellUncommonPotionPrice = Math.Max(ConfigSchema.SellUncommonPotionPrice.Min, data.SellUncommonPotionPrice);
        ShopEnhancementConfig.SellRarePotionPrice = Math.Max(ConfigSchema.SellRarePotionPrice.Min, data.SellRarePotionPrice);
        ShopEnhancementConfig.SellPotionMinGold = Math.Max(ConfigSchema.SellPotionMinGold.Min, data.SellPotionMinGold);
        ShopEnhancementConfig.EnableRemovalEnchantRandom = data.EnableRemovalEnchantRandom;
        ShopEnhancementConfig.EnableEnchantService = data.EnableEnchantService;
        ShopEnhancementConfig.EnchantStartShopVisit = data.EnchantStartShopVisit;
        ShopEnhancementConfig.EnchantReplaceChance = data.EnchantReplaceChance;
        ShopEnhancementConfig.EnchantCost = data.EnchantCost;
        ShopEnhancementConfig.EnchantAmountRange = NormalizeRange(
            data.EnchantAmountRange,
            ConfigSchema.EnchantAmount.Min,
            ConfigSchema.EnchantAmount.Max);
        ShopEnhancementConfig.EnchantCardCountRange = NormalizeRange(
            data.EnchantCardCountRange,
            ConfigSchema.EnchantCardCount.Min,
            ConfigSchema.EnchantCardCount.Max);
        ShopEnhancementConfig.EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
        ShopEnhancementConfig.GiftServiceCardCountRange = NormalizeRange(
            data.GiftServiceCardCountRange,
            ConfigSchema.GiftServiceCardCount.Min,
            ConfigSchema.GiftServiceCardCount.Max);
        ShopEnhancementConfig.GiftServiceBaseCost = Math.Max(ConfigSchema.GiftServiceBaseCost.Min, data.GiftServiceBaseCost);
        ShopEnhancementConfig.GiftServiceStepCost = Math.Max(ConfigSchema.GiftServiceStepCost.Min, data.GiftServiceStepCost);
    }

    internal static ConfigData? DeserializeConfigData(string json, out bool migrated)
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
        migrated = migratedLegacySellConfig || migratedSellPriceVariance || migratedBrokenRanges;
        return data;
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

    private static Vector2I NormalizeRange(Vector2I range, int minClamp, int maxClamp)
    {
        int min = Math.Clamp(Math.Min(range.X, range.Y), minClamp, maxClamp);
        int max = Math.Clamp(Math.Max(range.X, range.Y), minClamp, maxClamp);
        return new Vector2I(min, Math.Max(min, max));
    }
}
