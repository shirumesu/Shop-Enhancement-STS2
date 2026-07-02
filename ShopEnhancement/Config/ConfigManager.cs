using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace ShopEnhancement.Config;

public static class ConfigManager
{
    private const string ConfigDirName = "ShopEnhancement";
    private const string PresetsDirName = "Presets";
    private const string ConfigFileName = "config.json";
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
                    MainFile.Logger.Info("Migrated legacy sell-mode config to rarity-based sell prices.");
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
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public static List<string> GetPresets()
    {
        EnsureDirectories();
        List<string> presets = new();
        try
        {
            foreach (string file in Directory.GetFiles(PresetsDir, "*.json"))
            {
                presets.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to list presets: {ex}");
        }
        return presets;
    }

    public static void SavePreset(string name, ConfigData data)
    {
        EnsureDirectories();
        try
        {
            string path = Path.Combine(PresetsDir, $"{name}.json");
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save preset '{name}': {ex}");
        }
    }

    public static void LoadPreset(string name)
    {
        EnsureDirectories();
        try
        {
            string path = Path.Combine(PresetsDir, $"{name}.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            ConfigData? data = DeserializeConfigData(json, out bool migrated);
            if (data != null)
            {
                if (migrated)
                {
                    MainFile.Logger.Info($"Migrated legacy sell-mode config from preset '{name}'.");
                }

                Save(data); // Save as current config
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to load preset '{name}': {ex}");
        }
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
            EnableNoPurchaseReward = ShopEnhancementConfig.EnableNoPurchaseReward,
            NoPurchaseRewardGold = ShopEnhancementConfig.NoPurchaseRewardGold,
            EnableSkipCardRewardGold = ShopEnhancementConfig.EnableSkipCardRewardGold,
            SkipCardRewardGoldAmount = ShopEnhancementConfig.SkipCardRewardGoldAmount,
            EnableCrossClassCards = ShopEnhancementConfig.EnableCrossClassCards,
            CrossClassCardChance = ShopEnhancementConfig.CrossClassCardChance,
            EnableSellMode = ShopEnhancementConfig.EnableSellMode,
            SellCommonRelicPrice = ShopEnhancementConfig.SellCommonRelicPrice,
            SellUncommonRelicPrice = ShopEnhancementConfig.SellUncommonRelicPrice,
            SellRareRelicPrice = ShopEnhancementConfig.SellRareRelicPrice,
            SellShopRelicPrice = ShopEnhancementConfig.SellShopRelicPrice,
            SellAncientRelicPrice = ShopEnhancementConfig.SellAncientRelicPrice,
            SellStarterRelicPrice = ShopEnhancementConfig.SellStarterRelicPrice,
            SellEventRelicPrice = ShopEnhancementConfig.SellEventRelicPrice,
            SellRelicPriceVariance = ShopEnhancementConfig.SellRelicPriceVariance,
            SellRelicMinGold = ShopEnhancementConfig.SellRelicMinGold,
            SellCommonPotionPrice = ShopEnhancementConfig.SellCommonPotionPrice,
            SellUncommonPotionPrice = ShopEnhancementConfig.SellUncommonPotionPrice,
            SellRarePotionPrice = ShopEnhancementConfig.SellRarePotionPrice,
            SellPotionPriceVariance = ShopEnhancementConfig.SellPotionPriceVariance,
            SellPotionMinGold = ShopEnhancementConfig.SellPotionMinGold,
            EnableGiftMode = ShopEnhancementConfig.EnableGiftMode,
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
        ShopEnhancementConfig.RemoveLimitPerShop = data.RemoveLimitPerShop;
        ShopEnhancementConfig.RefreshCost = data.RefreshCost;
        ShopEnhancementConfig.RefreshLimitPerShop = data.RefreshLimitPerShop;
        ShopEnhancementConfig.EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        ShopEnhancementConfig.NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        ShopEnhancementConfig.EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        ShopEnhancementConfig.SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        ShopEnhancementConfig.EnableCrossClassCards = data.EnableCrossClassCards;
        ShopEnhancementConfig.CrossClassCardChance = data.CrossClassCardChance;
        ShopEnhancementConfig.EnableSellMode = data.EnableSellMode;
        ShopEnhancementConfig.SellCommonRelicPrice = Math.Max(0, data.SellCommonRelicPrice);
        ShopEnhancementConfig.SellUncommonRelicPrice = Math.Max(0, data.SellUncommonRelicPrice);
        ShopEnhancementConfig.SellRareRelicPrice = Math.Max(0, data.SellRareRelicPrice);
        ShopEnhancementConfig.SellShopRelicPrice = Math.Max(0, data.SellShopRelicPrice);
        ShopEnhancementConfig.SellAncientRelicPrice = Math.Max(0, data.SellAncientRelicPrice);
        ShopEnhancementConfig.SellStarterRelicPrice = Math.Max(0, data.SellStarterRelicPrice);
        ShopEnhancementConfig.SellEventRelicPrice = Math.Max(0, data.SellEventRelicPrice);
        ShopEnhancementConfig.SellRelicPriceVariance = Math.Clamp(data.SellRelicPriceVariance, 0f, 1f);
        ShopEnhancementConfig.SellRelicMinGold = Math.Max(0, data.SellRelicMinGold);
        ShopEnhancementConfig.SellCommonPotionPrice = Math.Max(0, data.SellCommonPotionPrice);
        ShopEnhancementConfig.SellUncommonPotionPrice = Math.Max(0, data.SellUncommonPotionPrice);
        ShopEnhancementConfig.SellRarePotionPrice = Math.Max(0, data.SellRarePotionPrice);
        ShopEnhancementConfig.SellPotionPriceVariance = Math.Clamp(data.SellPotionPriceVariance, 0f, 1f);
        ShopEnhancementConfig.SellPotionMinGold = Math.Max(0, data.SellPotionMinGold);
        ShopEnhancementConfig.EnableGiftMode = data.EnableGiftMode;
        ShopEnhancementConfig.EnableRemovalEnchantRandom = data.EnableRemovalEnchantRandom;
        ShopEnhancementConfig.EnableEnchantService = data.EnableEnchantService;
        ShopEnhancementConfig.EnchantStartShopVisit = data.EnchantStartShopVisit;
        ShopEnhancementConfig.EnchantReplaceChance = data.EnchantReplaceChance;
        ShopEnhancementConfig.EnchantCost = data.EnchantCost;
        ShopEnhancementConfig.EnchantAmountRange = NormalizeRange(data.EnchantAmountRange, 1, 999);
        ShopEnhancementConfig.EnchantCardCountRange = NormalizeRange(data.EnchantCardCountRange, 1, 20);
        ShopEnhancementConfig.EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
        ShopEnhancementConfig.GiftServiceCardCountRange = NormalizeRange(data.GiftServiceCardCountRange, 1, 20);
        ShopEnhancementConfig.GiftServiceBaseCost = Math.Max(0, data.GiftServiceBaseCost);
        ShopEnhancementConfig.GiftServiceStepCost = Math.Max(0, data.GiftServiceStepCost);
    }

    private static ConfigData? DeserializeConfigData(string json, out bool migrated)
    {
        migrated = false;
        ConfigData? data = JsonSerializer.Deserialize<ConfigData>(json);
        if (data == null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        migrated = TryMigrateLegacySellConfig(data, document.RootElement);
        return data;
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
        data.SellRelicPriceVariance = 0f;

        data.SellCommonPotionPrice = LegacySellPrice(LegacyCommonPotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.SellUncommonPotionPrice = LegacySellPrice(LegacyUncommonPotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.SellRarePotionPrice = LegacySellPrice(LegacyRarePotionBasePrice, potionRatio, data.SellPotionMinGold);
        data.SellPotionPriceVariance = 0f;

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
