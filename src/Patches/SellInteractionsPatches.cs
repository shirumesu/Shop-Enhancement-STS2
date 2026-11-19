using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static class SellInteractionsPatches
{
	[HarmonyPatch(typeof(NRelicInventoryHolder), "_Ready")]
	[HarmonyPostfix]
	public static void RelicHolderReady_Postfix(NRelicInventoryHolder __instance)
	{
		__instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From((InputEvent inputEvent) =>
		{
			if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Right && mouseButton.IsReleased())
			{
				TaskHelper.RunSafely(TrySellRelic(__instance));
			}
		}));
	}

	[HarmonyPatch(typeof(NPotionHolder), "_Ready")]
	[HarmonyPostfix]
	public static void PotionHolderReady_Postfix(NPotionHolder __instance)
	{
		__instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From((InputEvent inputEvent) =>
		{
			if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Right && mouseButton.IsReleased())
			{
				TaskHelper.RunSafely(TrySellPotion(__instance));
			}
		}));
	}

	private static async Task TrySellRelic(NRelicInventoryHolder holder)
	{
		if (!ShopEnhancementConfig.EnableSellMode || !TryGetCurrentInventory(out NMerchantInventory? inventory) || !SellModeState.IsEnabled(inventory))
		{
			return;
		}
		RelicModel? relic = holder.Relic?.Model;
		if (relic == null)
		{
			return;
		}
		RelicSellEvaluation evaluation = RelicSellRules.Evaluate(relic);
		if (!evaluation.CanSell)
		{
			await ShowRelicSellBlocked(evaluation.Reasons);
			return;
		}
		int price = CalculateRelicPrice(relic);
		string relicName = relic.Title.GetFormattedText();
		var confirmMessage = new LocString("shop_enhancement", "sell.relic_confirm");
		confirmMessage.Add("0", relicName);
		confirmMessage.Add("1", price);
		if (await ConfirmSell(confirmMessage.GetFormattedText()))
		{
			await ShopActionSynchronizer.Current.DoLocalSell(new ShopSellMessage
			{
				IsRelic = true,
				Index = relic.Owner.Relics.ToList().IndexOf(relic),
				ModelId = relic.Id.ToString(),
				Price = price
			});
			ShowSellResult(price);
		}
	}

	private static async Task TrySellPotion(NPotionHolder holder)
	{
		if (!ShopEnhancementConfig.EnableSellMode || !TryGetCurrentInventory(out NMerchantInventory? inventory) || !SellModeState.IsEnabled(inventory))
		{
			return;
		}
		PotionModel? potion = holder.Potion?.Model;
		if (potion != null)
		{
			int price = CalculatePotionPrice(potion);
			string potionName = potion.Title.GetFormattedText();
			var confirmMessage = new LocString("shop_enhancement", "sell.potion_confirm");
			confirmMessage.Add("0", potionName);
			confirmMessage.Add("1", price);
			if (await ConfirmSell(confirmMessage.GetFormattedText()))
			{
				await ShopActionSynchronizer.Current.DoLocalSell(new ShopSellMessage
				{
					IsRelic = false,
					Index = potion.Owner.GetPotionSlotIndex(potion),
					ModelId = potion.Id.ToString(),
					Price = price
				});
				ShowSellResult(price);
			}
		}
	}

	private static bool TryGetCurrentInventory([NotNullWhen(true)] out NMerchantInventory? inventory)
	{
		inventory = null;
		if (NRun.Instance?.MerchantRoom?.Inventory == null)
		{
			return false;
		}
		if (!(ActiveScreenContext.Instance.GetCurrentScreen() is NMerchantInventory currentInventory))
		{
			return false;
		}
		inventory = currentInventory;
		return true;
	}

	private static async Task<bool> ConfirmSell(string message)
	{
		if (NModalContainer.Instance == null || NModalContainer.Instance.OpenModal != null)
		{
			return false;
		}
		NGenericPopup? popup = NGenericPopup.Create();
		if (popup == null)
		{
			return false;
		}
		NModalContainer.Instance.Add(popup);
		// Body and header are placeholders; the vertical popup's text is replaced right below.
		var confirmText = new LocString("main_menu_ui", "GENERIC_POPUP.confirm");
		var noButton = new LocString("main_menu_ui", "GENERIC_POPUP.cancel");
		Task<bool> confirmation = popup.WaitForConfirmation(confirmText, confirmText, noButton, confirmText);
		if (AccessTools.Field(typeof(NGenericPopup), "_verticalPopup")?.GetValue(popup) is NVerticalPopup verticalPopup)
		{
			var title = new LocString("shop_enhancement", "sell.confirm_title");
			verticalPopup.SetText(title.GetFormattedText(), message);
		}
		return await confirmation;
	}

	private static async Task ShowRelicSellBlocked(IReadOnlyList<RelicSellBlockReason> reasons)
	{
		if (NModalContainer.Instance == null || NModalContainer.Instance.OpenModal != null)
		{
			return;
		}
		NGenericPopup? popup = NGenericPopup.Create();
		if (popup != null)
		{
			List<string> reasonTexts = reasons.Select(reason => new LocString("shop_enhancement", GetBlockedReasonKey(reason)).GetFormattedText()).ToList();
			var body = new LocString("shop_enhancement", "sell.blocked_body");
			body.Add("0", string.Join("\n", reasonTexts));
			var header = new LocString("shop_enhancement", "sell.blocked_title");
			var acknowledge = new LocString("shop_enhancement", "sell.blocked_acknowledge");
			NModalContainer.Instance.Add(popup);
			Task<bool> acknowledged = popup.WaitForConfirmation(body, header, acknowledge, acknowledge);
			// Single-button notice: only the "no" button (acknowledge) stays visible.
			if (AccessTools.Field(typeof(NGenericPopup), "_verticalPopup")?.GetValue(popup) is NVerticalPopup verticalPopup)
			{
				verticalPopup.YesButton.Visible = false;
			}
			await acknowledged;
		}
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
			_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
		};
	}

	private static void ShowSellResult(int price)
	{
		var message = new LocString("shop_enhancement", "sell.success");
		message.Add("0", price);
		NFullscreenTextVfx? vfx = NFullscreenTextVfx.Create(message.GetFormattedText());
		if (vfx != null && NGame.Instance != null)
		{
			NGame.Instance.AddChildSafely(vfx);
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
		float variance = ShopEnhancementConfig.EnableSellPriceVariance ? ShopEnhancementConfig.SellPriceVariance : 0f;
		return ApplyMerchantPurchasePriceCap(
			ApplyPriceVariance(
				seed: Math.Clamp(variance, 0f, 1f) > 0f ? SellPriceRandomSeeds.GetOrCreateSeed(relic) : null,
				configuredPrice: configuredPrice,
				variance: variance,
				minGold: ShopEnhancementConfig.SellRelicMinGold),
			relic);
	}

	private static int CalculatePotionPrice(PotionModel potion)
	{
		int configuredPrice = potion.Rarity switch
		{
			PotionRarity.Rare => ShopEnhancementConfig.SellRarePotionPrice,
			PotionRarity.Uncommon => ShopEnhancementConfig.SellUncommonPotionPrice,
			_ => ShopEnhancementConfig.SellCommonPotionPrice,
		};
		float variance = ShopEnhancementConfig.EnableSellPriceVariance ? ShopEnhancementConfig.SellPriceVariance : 0f;
		return ApplyMerchantPurchasePriceCap(
			ApplyPriceVariance(
				seed: Math.Clamp(variance, 0f, 1f) > 0f ? SellPriceRandomSeeds.GetOrCreateSeed(potion) : null,
				configuredPrice: configuredPrice,
				variance: variance,
				minGold: ShopEnhancementConfig.SellPotionMinGold),
			potion);
	}

	private static int ApplyMerchantPurchasePriceCap(int price, RelicModel relic)
	{
		if (!MerchantPurchasePriceState.TryGet(relic, out var purchasePrice))
		{
			return price;
		}
		return Math.Min(price, purchasePrice);
	}

	private static int ApplyMerchantPurchasePriceCap(int price, PotionModel potion)
	{
		if (!MerchantPurchasePriceState.TryGet(potion, out var purchasePrice))
		{
			return price;
		}
		return Math.Min(price, purchasePrice);
	}

	private static int ApplyPriceVariance(int configuredPrice, float variance, int minGold, int? seed)
	{
		int price = Math.Max(0, configuredPrice);
		float clampedVariance = Math.Clamp(variance, 0f, 1f);
		if (clampedVariance > 0f && seed.HasValue)
		{
			int maxDelta = (int)Math.Round((float)price * clampedVariance, MidpointRounding.AwayFromZero);
			if (maxDelta > 0)
			{
				price += SeedToRange(seed.Value, -maxDelta, maxDelta);
			}
		}
		return Math.Max(price, Math.Max(0, minGold));
	}

	private static int SeedToRange(int seed, int minInclusive, int maxInclusive)
	{
		uint rangeSize = (uint)(maxInclusive - minInclusive + 1);
		return minInclusive + (int)(HashSeed(seed) % rangeSize);
	}

	private static uint HashSeed(int seed)
	{
		int mixed = (seed ^ (seed >>> 16)) * 2146121005;
		int remixed = (mixed ^ (mixed >>> 15)) * -2073254261;
		return (uint)(remixed ^ (remixed >>> 16));
	}
}
