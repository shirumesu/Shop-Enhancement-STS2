using System.Globalization;
using System.Reflection;

namespace ShopEnhancement.Config;

internal static class ConfigSchema
{
    public static void Validate()
    {
        const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Static;

        foreach (Type settingType in typeof(ConfigSchema).GetNestedTypes(BindingFlags.NonPublic))
        {
            FieldInfo? minField = settingType.GetField("Min", fieldFlags);
            FieldInfo? maxField = settingType.GetField("Max", fieldFlags);
            FieldInfo? stepField = settingType.GetField("Step", fieldFlags);
            if (minField == null || maxField == null || stepField == null)
            {
                continue;
            }

            double min = ConstantAsDouble(minField);
            double max = ConstantAsDouble(maxField);
            double step = ConstantAsDouble(stepField);
            if (min >= max || step <= 0 || step > max - min)
            {
                throw new InvalidOperationException(
                    $"Invalid config slider schema for {settingType.Name}: range={min}..{max}, step={step}.");
            }

            foreach (FieldInfo defaultField in settingType
                         .GetFields(fieldFlags)
                         .Where(field => field.Name.StartsWith("Default", StringComparison.Ordinal)))
            {
                double defaultValue = ConstantAsDouble(defaultField);
                double stepIndex = (defaultValue - min) / step;
                bool alignedToStep = Math.Abs(stepIndex - Math.Round(stepIndex)) < 0.0001;
                if (defaultValue < min || defaultValue > max || !alignedToStep)
                {
                    throw new InvalidOperationException(
                        $"Invalid config default for {settingType.Name}.{defaultField.Name}: " +
                        $"value={defaultValue}, range={min}..{max}, step={step}.");
                }
            }
        }
    }

    private static double ConstantAsDouble(FieldInfo field)
    {
        return Convert.ToDouble(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
    }

    internal static class RemoveBaseCost
    {
        public const int Default = 50;
        public const int Min = 0;
        public const int Max = 200;
        public const int Step = 1;
    }

    internal static class RemoveStepCost
    {
        public const int Default = 25;
        public const int Min = 0;
        public const int Max = 100;
        public const int Step = 1;
    }

    internal static class RemoveLimitPerShop
    {
        public const int Default = 3;
        public const int Min = 1;
        public const int Max = 20;
        public const int Step = 1;
    }

    internal static class RefreshCost
    {
        public const int Default = 40;
        public const int Min = 0;
        public const int Max = 99;
        public const int Step = 1;
    }

    internal static class RefreshLimitPerShop
    {
        public const int Default = 3;
        public const int Min = 0;
        public const int Max = 20;
        public const int Step = 1;
    }

    internal static class RelicRefreshMode
    {
        public const ShopRelicRefreshMode Default = ShopRelicRefreshMode.Queue;
    }

    internal static class EnableNoPurchaseReward
    {
        public const bool Default = true;
    }

    internal static class NoPurchaseRewardGold
    {
        public const int Default = 15;
        public const int Min = 0;
        public const int Max = 100;
        public const int Step = 1;
    }

    internal static class EnableSkipCardRewardGold
    {
        public const bool Default = true;
    }

    internal static class SkipCardRewardGoldAmount
    {
        public const int Default = 15;
        public const int Min = 0;
        public const int Max = 100;
        public const int Step = 1;
    }

    internal static class EnableCrossClassCards
    {
        public const bool Default = true;
    }

    internal static class CrossClassCardChance
    {
        public const float Default = 0.2f;
        public const double Min = 0;
        public const double Max = 1;
        public const double Step = 0.01;
    }

    internal static class EnableSellMode
    {
        public const bool Default = true;
    }

    internal static class EnableSellPriceVariance
    {
        public const bool Default = true;
    }

    internal static class SellPriceVariance
    {
        public const float Default = 0.2f;
        public const double Min = 0;
        public const double Max = 1;
        public const double Step = 0.01;
    }

    internal static class EnableGiftMode
    {
        public const bool Default = true;
    }

    internal static class RelicSellRulePreset
    {
        public const RelicSellPreset Default = RelicSellPreset.AllowAll;
    }

    internal static class AllowStarterRelics
    {
        public const bool Default = true;
    }

    internal static class AllowAncientRelics
    {
        public const bool Default = true;
    }

    internal static class AllowEventRelics
    {
        public const bool Default = true;
    }

    internal static class AllowWaxRelics
    {
        public const bool Default = true;
    }

    internal static class AllowMeltedRelics
    {
        public const bool Default = true;
    }

    internal static class AllowUsedUpRelics
    {
        public const bool Default = true;
    }

    internal static class AllowUponPickupRelics
    {
        public const bool Default = true;
    }

    internal static class AllowDisabledRelics
    {
        public const bool Default = true;
    }

    internal static class SellCommonRelicPrice
    {
        public const int Default = 70;
        public const int Min = 0;
        public const int Max = 500;
        public const int Step = 10;
    }

    internal static class SellUncommonRelicPrice
    {
        public const int Default = 90;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellRareRelicPrice
    {
        public const int Default = 110;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellShopRelicPrice
    {
        public const int Default = 80;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellAncientRelicPrice
    {
        public const int Default = 240;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellStarterRelicPrice
    {
        public const int Default = 240;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellEventRelicPrice
    {
        public const int Default = 80;
        public const int Min = SellCommonRelicPrice.Min;
        public const int Max = SellCommonRelicPrice.Max;
        public const int Step = SellCommonRelicPrice.Step;
    }

    internal static class SellRelicMinGold
    {
        public const int Default = 30;
        public const int Min = 0;
        public const int Max = 100;
        public const int Step = 1;
    }

    internal static class SellCommonPotionPrice
    {
        public const int Default = 20;
        public const int Min = 0;
        public const int Max = 300;
        public const int Step = 10;
    }

    internal static class SellUncommonPotionPrice
    {
        public const int Default = 30;
        public const int Min = SellCommonPotionPrice.Min;
        public const int Max = SellCommonPotionPrice.Max;
        public const int Step = SellCommonPotionPrice.Step;
    }

    internal static class SellRarePotionPrice
    {
        public const int Default = 40;
        public const int Min = SellCommonPotionPrice.Min;
        public const int Max = SellCommonPotionPrice.Max;
        public const int Step = SellCommonPotionPrice.Step;
    }

    internal static class SellPotionMinGold
    {
        public const int Default = 15;
        public const int Min = 0;
        public const int Max = 50;
        public const int Step = 1;
    }

    internal static class EnableRemovalEnchantRandom
    {
        public const bool Default = true;
    }

    internal static class EnableEnchantService
    {
        public const bool Default = true;
    }

    internal static class EnchantStartShopVisit
    {
        public const int Default = 4;
        public const int Min = 1;
        public const int Max = 10;
        public const int Step = 1;
    }

    internal static class EnchantReplaceChance
    {
        public const float Default = 0.3f;
        public const double Min = 0;
        public const double Max = 1;
        public const double Step = 0.01;
    }

    internal static class EnchantCost
    {
        public const int Default = 100;
        public const int Min = 0;
        public const int Max = 200;
        public const int Step = 10;
    }

    internal static class EnchantAmount
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 2;
        public const int Min = 1;
        public const int Max = 10;
        public const int Step = 1;
    }

    internal static class EnchantCardCount
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 2;
        public const int Min = 1;
        public const int Max = 20;
        public const int Step = 1;
    }

    internal static class EnableRandomTeammateGiftService
    {
        public const bool Default = true;
    }

    internal static class GiftServiceCardCount
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 1;
        public const int Min = 1;
        public const int Max = 20;
        public const int Step = 1;
    }

    internal static class GiftServiceBaseCost
    {
        public const int Default = 85;
        public const int Min = 0;
        public const int Max = 300;
        public const int Step = 1;
    }

    internal static class GiftServiceStepCost
    {
        public const int Default = 55;
        public const int Min = GiftServiceBaseCost.Min;
        public const int Max = GiftServiceBaseCost.Max;
        public const int Step = GiftServiceBaseCost.Step;
    }
}
