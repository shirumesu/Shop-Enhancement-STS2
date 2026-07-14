using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static partial class SellInteractionsPatches
{
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
    [HarmonyPostfix]
    public static void RelicHolderReady_Postfix(NRelicInventoryHolder __instance)
    {
        __instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } mouse && mouse.IsReleased())
            {
                TaskHelper.RunSafely(TrySellRelic(__instance));
            }
        }));
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder._Ready))]
    [HarmonyPostfix]
    public static void PotionHolderReady_Postfix(NPotionHolder __instance)
    {
        __instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } mouse && mouse.IsReleased())
            {
                TaskHelper.RunSafely(TrySellPotion(__instance));
            }
        }));
    }

    private static async Task TrySellRelic(NRelicInventoryHolder holder)
    {
        if (!ShopEnhancementConfig.EnableSellMode)
            return;
        if (!TryGetCurrentInventory(out var inventory))
            return;
        if (!SellModeState.IsEnabled(inventory))
            return;

        var relic = holder.Relic?.Model;
        if (relic == null)
            return;

        RelicSellEvaluation sellEvaluation = RelicSellRules.Evaluate(relic);
        if (!sellEvaluation.CanSell)
        {
            await ShowRelicSellBlocked(sellEvaluation.Reasons);
            return;
        }

        int price = CalculateRelicPrice(relic);
        string relicName = relic.Title.GetFormattedText();
        
        var msgLoc = new LocString("shop_enhancement", "sell.relic_confirm");
        msgLoc.Add("0", relicName);
        msgLoc.Add("1", price);
        
        if (!await ConfirmSell(msgLoc.GetFormattedText()))
            return;

        SoldToyBoxState.PendingContinuation? toyBoxContinuation = SoldToyBoxState.Prepare(relic);
        await RelicCmd.Remove(relic);
        SoldToyBoxState.Activate(toyBoxContinuation);
        await PlayerCmd.GainGold(price, relic.Owner);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(price);
        ShowSellResult(price);
    }

    private static async Task TrySellPotion(NPotionHolder holder)
    {
        if (!ShopEnhancementConfig.EnableSellMode)
            return;
        if (!TryGetCurrentInventory(out var inventory))
            return;
        if (!SellModeState.IsEnabled(inventory))
            return;

        var potion = holder.Potion?.Model;
        if (potion == null)
            return;

        int price = CalculatePotionPrice(potion);
        string potionName = potion.Title.GetFormattedText();
        
        var msgLoc = new LocString("shop_enhancement", "sell.potion_confirm");
        msgLoc.Add("0", potionName);
        msgLoc.Add("1", price);

        if (!await ConfirmSell(msgLoc.GetFormattedText()))
            return;

        await PotionCmd.Discard(potion);
        await PlayerCmd.GainGold(price, potion.Owner);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(price);
        ShowSellResult(price);
    }

    private static bool TryGetCurrentInventory(out NMerchantInventory inventory)
    {
        inventory = null!;
        if (NRun.Instance?.MerchantRoom?.Inventory == null)
            return false;
        if (ActiveScreenContext.Instance.GetCurrentScreen() is not NMerchantInventory current)
            return false;
        inventory = current;
        return true;
    }

    private static async Task<bool> ConfirmSell(string message)
    {
        if (NModalContainer.Instance == null || NModalContainer.Instance.OpenModal != null)
            return false;

        var popup = NGenericPopup.Create();
        if (popup == null)
            return false;

        NModalContainer.Instance.Add(popup);

        var confirmLoc = new LocString("main_menu_ui", "GENERIC_POPUP.confirm");
        var cancelLoc = new LocString("main_menu_ui", "GENERIC_POPUP.cancel");
        
        // 使用 dummy LocString，稍后通过反射覆盖文本
        var task = popup.WaitForConfirmation(confirmLoc, confirmLoc, cancelLoc, confirmLoc);

        var verticalPopupField = AccessTools.Field(typeof(NGenericPopup), "_verticalPopup");
        if (verticalPopupField?.GetValue(popup) is NVerticalPopup verticalPopup)
        {
             var title = new LocString("shop_enhancement", "sell.confirm_title");
             verticalPopup.SetText(title.GetFormattedText(), message);
        }

        return await task;
    }

    private static async Task ShowRelicSellBlocked(IReadOnlyList<RelicSellBlockReason> reasons)
    {
        if (NModalContainer.Instance == null || NModalContainer.Instance.OpenModal != null)
            return;

        var popup = NGenericPopup.Create();
        if (popup == null)
            return;

        var reasonTexts = reasons
            .Select(reason => new LocString("shop_enhancement", GetBlockedReasonKey(reason)).GetFormattedText())
            .ToList();
        var body = new LocString("shop_enhancement", "sell.blocked_body");
        body.Add("0", string.Join("\n", reasonTexts));
        var title = new LocString("shop_enhancement", "sell.blocked_title");
        var acknowledge = new LocString("shop_enhancement", "sell.blocked_acknowledge");

        NModalContainer.Instance.Add(popup);
        Task<bool> task = popup.WaitForConfirmation(body, title, acknowledge, acknowledge);

        var verticalPopupField = AccessTools.Field(typeof(NGenericPopup), "_verticalPopup");
        if (verticalPopupField?.GetValue(popup) is NVerticalPopup verticalPopup)
        {
            verticalPopup.YesButton.Visible = false;
        }

        await task;
    }

    private static string GetBlockedReasonKey(RelicSellBlockReason reason)
    {
        return reason switch
        {
            RelicSellBlockReason.UsedUp => "sell.blocked_reason.used_up",
            RelicSellBlockReason.UponPickup => "sell.blocked_reason.upon_pickup",
            RelicSellBlockReason.Wax => "sell.blocked_reason.wax",
            RelicSellBlockReason.Melted => "sell.blocked_reason.melted",
            RelicSellBlockReason.Disabled => "sell.blocked_reason.disabled",
            RelicSellBlockReason.Starter => "sell.blocked_reason.starter",
            RelicSellBlockReason.Ancient => "sell.blocked_reason.ancient",
            RelicSellBlockReason.Event => "sell.blocked_reason.event",
            RelicSellBlockReason.StrictPet => "sell.blocked_reason.strict_pet",
            RelicSellBlockReason.StrictUntradable => "sell.blocked_reason.strict_untradable",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }

    private static void ShowSellResult(int price)
    {
        var loc = new LocString("shop_enhancement", "sell.success");
        loc.Add("0", price);
        var tip = NFullscreenTextVfx.Create(loc.GetFormattedText());
        if (tip != null && NGame.Instance != null)
        {
            NGame.Instance.AddChildSafely(tip);
        }
    }

    private static int CalculateRelicPrice(RelicModel relic)
    {
        int configuredPrice = relic.Rarity switch
        {
            RelicRarity.Common => ShopEnhancementConfig.SellCommonRelicPrice,
            RelicRarity.Uncommon => ShopEnhancementConfig.SellUncommonRelicPrice,
            RelicRarity.Rare => ShopEnhancementConfig.SellRareRelicPrice,
            RelicRarity.Shop => ShopEnhancementConfig.SellShopRelicPrice,
            RelicRarity.Ancient => ShopEnhancementConfig.SellAncientRelicPrice,
            RelicRarity.Starter => ShopEnhancementConfig.SellStarterRelicPrice,
            RelicRarity.Event => ShopEnhancementConfig.SellEventRelicPrice,
            _ => ShopEnhancementConfig.SellCommonRelicPrice,
        };

        float variance = ShopEnhancementConfig.EnableSellPriceVariance
            ? ShopEnhancementConfig.SellPriceVariance
            : 0f;
        int? seed = Math.Clamp(variance, 0f, 1f) > 0f ? SellPriceRandomSeeds.GetOrCreateSeed(relic) : null;
        int price = ApplyPriceVariance(configuredPrice, variance, ShopEnhancementConfig.SellRelicMinGold, seed);
        return ApplyMerchantPurchasePriceCap(price, relic);
    }

    private static int CalculatePotionPrice(PotionModel potion)
    {
        int configuredPrice = potion.Rarity switch
        {
            PotionRarity.Rare => ShopEnhancementConfig.SellRarePotionPrice,
            PotionRarity.Uncommon => ShopEnhancementConfig.SellUncommonPotionPrice,
            _ => ShopEnhancementConfig.SellCommonPotionPrice,
        };

        float variance = ShopEnhancementConfig.EnableSellPriceVariance
            ? ShopEnhancementConfig.SellPriceVariance
            : 0f;
        int? seed = Math.Clamp(variance, 0f, 1f) > 0f ? SellPriceRandomSeeds.GetOrCreateSeed(potion) : null;
        int price = ApplyPriceVariance(configuredPrice, variance, ShopEnhancementConfig.SellPotionMinGold, seed);
        return ApplyMerchantPurchasePriceCap(price, potion);
    }

    private static int ApplyMerchantPurchasePriceCap(int price, RelicModel relic)
    {
        return MerchantPurchasePriceState.TryGet(relic, out int purchasePrice)
            ? Math.Min(price, purchasePrice)
            : price;
    }

    private static int ApplyMerchantPurchasePriceCap(int price, PotionModel potion)
    {
        return MerchantPurchasePriceState.TryGet(potion, out int purchasePrice)
            ? Math.Min(price, purchasePrice)
            : price;
    }

    private static int ApplyPriceVariance(int configuredPrice, float variance, int minGold, int? seed)
    {
        int price = Math.Max(0, configuredPrice);
        float clampedVariance = Math.Clamp(variance, 0f, 1f);
        if (clampedVariance > 0f && seed.HasValue)
        {
            int maxDelta = (int)Math.Round(price * clampedVariance, MidpointRounding.AwayFromZero);
            if (maxDelta > 0)
            {
                price += SeedToRange(seed.Value, -maxDelta, maxDelta);
            }
        }

        return Math.Max(price, Math.Max(0, minGold));
    }

    private static int SeedToRange(int seed, int minInclusive, int maxInclusive)
    {
        uint span = (uint)(maxInclusive - minInclusive + 1);
        return minInclusive + (int)(HashSeed(seed) % span);
    }

    private static uint HashSeed(int seed)
    {
        uint hash = unchecked((uint)seed);
        hash ^= hash >> 16;
        hash *= 0x7feb352d;
        hash ^= hash >> 15;
        hash *= 0x846ca68b;
        hash ^= hash >> 16;
        return hash;
    }
}
