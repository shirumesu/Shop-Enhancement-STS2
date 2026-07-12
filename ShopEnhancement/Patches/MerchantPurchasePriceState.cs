using System.Runtime.CompilerServices;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ShopEnhancement.Patches;

internal static class MerchantPurchasePriceState
{
    private const string RelicPurchasePriceSaveId = "ShopEnhancement.relic_merchant_purchase_price";
    private const string PotionPurchasePriceSaveId = "ShopEnhancement.potion_merchant_purchase_price";

    private static readonly ConditionalWeakTable<RelicModel, PriceBox> RelicPrices = new();
    private static readonly ConditionalWeakTable<PotionModel, PriceBox> PotionPrices = new();

    private static bool _registered;

    public static void RegisterSaves()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ExtendedSaveTypes.RegisterObjectSaveType<PurchasePriceSave>(
            ExtendedSaveTypes.PropertyFunc<PurchasePriceSave, int>(nameof(PurchasePriceSave.Price)));
        ExtendedSaveTypes.RegisterSavedValue<RelicModel, PurchasePriceSave>(
            RelicPurchasePriceSaveId,
            GetRelicPurchasePriceSave,
            SetRelicPurchasePriceSave,
            WritePurchasePriceSave,
            ReadPurchasePriceSave);
        ExtendedSaveTypes.RegisterSavedValue<PotionModel, PurchasePriceSave>(
            PotionPurchasePriceSaveId,
            GetPotionPurchasePriceSave,
            SetPotionPurchasePriceSave,
            WritePurchasePriceSave,
            ReadPurchasePriceSave);
    }

    public static void Set(RelicModel relic, int price)
    {
        RelicPrices.GetOrCreateValue(relic).Value = Math.Max(0, price);
    }

    public static void Set(PotionModel potion, int price)
    {
        PotionPrices.GetOrCreateValue(potion).Value = Math.Max(0, price);
    }

    public static bool TryGet(RelicModel relic, out int price)
    {
        return TryGet(RelicPrices, relic, out price);
    }

    public static bool TryGet(PotionModel potion, out int price)
    {
        return TryGet(PotionPrices, potion, out price);
    }

    private static bool TryGet<T>(ConditionalWeakTable<T, PriceBox> prices, T item, out int price)
        where T : class
    {
        if (prices.TryGetValue(item, out PriceBox? box) && box.Value.HasValue)
        {
            price = box.Value.Value;
            return true;
        }

        price = 0;
        return false;
    }

    private static PurchasePriceSave? GetRelicPurchasePriceSave(RelicModel relic)
    {
        return TryGet(relic, out int price) ? new PurchasePriceSave { Price = price } : null;
    }

    private static void SetRelicPurchasePriceSave(RelicModel relic, PurchasePriceSave? save)
    {
        if (save != null)
        {
            Set(relic, save.Price);
        }
    }

    private static PurchasePriceSave? GetPotionPurchasePriceSave(PotionModel potion)
    {
        return TryGet(potion, out int price) ? new PurchasePriceSave { Price = price } : null;
    }

    private static void SetPotionPurchasePriceSave(PotionModel potion, PurchasePriceSave? save)
    {
        if (save != null)
        {
            Set(potion, save.Price);
        }
    }

    private static void WritePurchasePriceSave(PurchasePriceSave save, PacketWriter writer)
    {
        writer.WriteInt(save.Price, 32);
    }

    private static PurchasePriceSave ReadPurchasePriceSave(PacketReader reader)
    {
        return new PurchasePriceSave { Price = reader.ReadInt(32) };
    }

    private sealed class PriceBox
    {
        public int? Value { get; set; }
    }

    private sealed class PurchasePriceSave
    {
        public int Price { get; set; }
    }
}

[HarmonyPatch]
internal static class MerchantPurchasePricePatches
{
    [HarmonyPatch(typeof(MerchantRelicEntry), "OnTryPurchase", typeof(MerchantInventory), typeof(bool))]
    [HarmonyPostfix]
    private static void TrackRelicPurchase(MerchantRelicEntry __instance, ref Task<(bool success, int goldSpent)> __result)
    {
        RelicModel? relic = __instance.Model;
        if (relic != null)
        {
            __result = RecordRelicPurchase(__result, relic);
        }
    }

    [HarmonyPatch(typeof(MerchantPotionEntry), "OnTryPurchase", typeof(MerchantInventory), typeof(bool))]
    [HarmonyPostfix]
    private static void TrackPotionPurchase(MerchantPotionEntry __instance, ref Task<(bool success, int goldSpent)> __result)
    {
        PotionModel? potion = __instance.Model;
        if (potion != null)
        {
            __result = RecordPotionPurchase(__result, potion);
        }
    }

    private static async Task<(bool success, int goldSpent)> RecordRelicPurchase(
        Task<(bool success, int goldSpent)> purchaseTask,
        RelicModel relic)
    {
        (bool success, int goldSpent) result = await purchaseTask;
        if (result.success)
        {
            MerchantPurchasePriceState.Set(relic, result.goldSpent);
        }

        return result;
    }

    private static async Task<(bool success, int goldSpent)> RecordPotionPurchase(
        Task<(bool success, int goldSpent)> purchaseTask,
        PotionModel potion)
    {
        (bool success, int goldSpent) result = await purchaseTask;
        if (result.success)
        {
            MerchantPurchasePriceState.Set(potion, result.goldSpent);
        }

        return result;
    }
}
