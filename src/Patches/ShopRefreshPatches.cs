using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.addons.mega_text;

using ShopEnhancement.Compatibility;
using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopRefreshPatches
{
	private static NButton? _refreshButton;

	private static int _refreshCount;

	[HarmonyPatch("_Ready")]
	[HarmonyPostfix]
	public static void Ready_Postfix(NMerchantInventory __instance)
	{
		_refreshCount = 0;
		CreateRefreshButton(__instance);
	}

	private static void CreateRefreshButton(NMerchantInventory instance)
	{
		NBackButton backButton = instance.GetNode<NBackButton>("%BackButton");
		if (backButton != null)
		{
			_refreshButton = new NButton();
			_refreshButton.Name = "RefreshButton";
			backButton.GetParent().CallDeferred(Node.MethodName.AddChild, _refreshButton);
			// Bottom-left corner, 160x50, 20px from the left and bottom edges (all set before the node enters the tree).
			_refreshButton.Scale = Vector2.One;
			_refreshButton.AnchorLeft = 0f;
			_refreshButton.AnchorTop = 1f;
			_refreshButton.AnchorRight = 0f;
			_refreshButton.AnchorBottom = 1f;
			_refreshButton.GrowVertical = Control.GrowDirection.Begin;
			_refreshButton.GrowHorizontal = Control.GrowDirection.End;
			_refreshButton.OffsetLeft = 20f;
			_refreshButton.OffsetTop = -70f;
			_refreshButton.OffsetRight = 180f;
			_refreshButton.OffsetBottom = -20f;
			Panel panel = new Panel();
			panel.Name = "Background";
			panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			panel.MouseFilter = Control.MouseFilterEnum.Ignore;
			StyleBoxFlat style = new StyleBoxFlat();
			style.BorderWidthBottom = 2;
			style.BorderWidthLeft = 2;
			style.BorderWidthRight = 2;
			style.BorderWidthTop = 2;
			style.CornerRadiusBottomLeft = 6;
			style.CornerRadiusBottomRight = 6;
			style.CornerRadiusTopLeft = 6;
			style.CornerRadiusTopRight = 6;
			style.BgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
			style.BorderColor = new Color(0.8f, 0.6f, 0.2f);
			panel.AddThemeStyleboxOverride("panel", style);
			_refreshButton.CallDeferred(Node.MethodName.AddChild, panel);
			_refreshButton.MouseEntered += () =>
			{
				style.BgColor = new Color(0.2f, 0.2f, 0.35f, 0.9f);
			};
			_refreshButton.MouseExited += () =>
			{
				style.BgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
			};
			_refreshButton.Released += _ =>
			{
				TaskHelper.RunSafely(OnRefreshClicked(instance));
			};
			UpdateRefreshButtonState(instance);
		}
	}

	private static void SetButtonText(NButton button, string text, Control context)
	{
		MegaLabel megaLabel = button.GetNodeOrNull<MegaLabel>("Label") ?? button.GetNodeOrNull<MegaLabel>("Text");
		if (megaLabel == null)
		{
			megaLabel = new MegaLabel();
			megaLabel.Name = "Label";
			if (context != null)
			{
				megaLabel.AddThemeFontOverride("font", context.GetThemeFont("font", "Label"));
			}
			button.CallDeferred(Node.MethodName.AddChild, megaLabel);
			megaLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			megaLabel.HorizontalAlignment = HorizontalAlignment.Center;
			megaLabel.VerticalAlignment = VerticalAlignment.Center;
			megaLabel.AutowrapMode = TextServer.AutowrapMode.Off;
			megaLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		}
		megaLabel.OffsetLeft = 10f;
		megaLabel.OffsetTop = 6f;
		megaLabel.OffsetRight = -10f;
		megaLabel.OffsetBottom = -6f;
		megaLabel.SetTextAutoSize(text);
	}

	private static async Task OnRefreshClicked(NMerchantInventory instance)
	{
		if (instance.Inventory != null)
		{
			Player player = instance.Inventory.Player;
			int refreshCost = ShopEnhancementConfig.RefreshCost;
			if (!GameCompat.IsBlitzShop(instance.Inventory) && player.Gold >= refreshCost && (ShopEnhancementConfig.RefreshLimitPerShop == -1 || _refreshCount < ShopEnhancementConfig.RefreshLimitPerShop))
			{
				_refreshCount++;
				MerchantInventory newInventory = await ShopActionSynchronizer.Current.DoLocalRefresh(refreshCost);
				ShowRefreshedInventory(instance, newInventory);
				UpdateRefreshButtonState(instance);
			}
		}
	}

	/// <summary>
	/// Replaces the player's shop inventory with a freshly rolled one. Runs on every client (see
	/// <see cref="ShopActionSynchronizer"/>), so it must only use synchronized state and RNG.
	/// </summary>
	internal static MerchantInventory RebuildInventory(Player player)
	{
		MerchantInventory previousInventory = GameCompat.GetInventoryFor(player);
		var transaction = ShopRelicRefreshState.BeginRefresh(previousInventory);
		try
		{
			var newInventory = MerchantInventory.CreateForNormalMerchant(player);
			ReplaceWithCrossClassCards(newInventory, player);
			GameCompat.ReplaceInventory(player, newInventory);
			return newInventory;
		}
		catch
		{
			ShopRelicRefreshState.RollbackRefresh(transaction);
			GameCompat.ReplaceInventory(player, previousInventory);
			throw;
		}
	}

	/// <summary>Local client only: rebinds the shop screen to the refreshed inventory.</summary>
	private static void ShowRefreshedInventory(NMerchantInventory instance, MerchantInventory newInventory)
	{
		AccessTools.Property(typeof(NMerchantInventory), "Inventory").SetValue(instance, null);
		DisconnectSlots(instance);
		instance.Initialize(newInventory, MerchantRoom.Dialogue);
		if (GiftModeState.IsEnabled(newInventory))
		{
			GiftModeState.Set(newInventory, isEnabled: false);
		}
		ShopGiftModeButtonPatches.ForceUpdateVisuals(instance);
	}

	private static void DisconnectSlots(Node node)
	{
		string name = node.GetType().Name;
		if (name.Contains("MerchantCard") || name.Contains("MerchantRelic") || name.Contains("MerchantPotion") || name.Contains("MerchantSlot"))
		{
			DisconnectSignal(node, "Hovered");
			DisconnectSignal(node, "Unhovered");
			DisconnectSignal(node, "Clicked");
			DisconnectSignal(node, "Released");
		}
		foreach (Node child in node.GetChildren())
		{
			DisconnectSlots(child);
		}
	}

	private static void DisconnectSignal(Node node, string signalName)
	{
		if (!node.HasSignal(signalName))
		{
			return;
		}
		foreach (Dictionary signalConnection in node.GetSignalConnectionList(signalName))
		{
			Callable callable = signalConnection["callable"].AsCallable();
			if (node.IsConnected(signalName, callable))
			{
				node.Disconnect(signalName, callable);
			}
		}
	}

	private static void UpdateRefreshButtonState(NMerchantInventory instance)
	{
		if (_refreshButton == null || instance.Inventory == null)
		{
			return;
		}
		// Refreshing rebuilds a normal merchant inventory, which would replace a Blitz shop's offers.
		bool isVisible = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance) && !GameCompat.IsBlitzShop(instance.Inventory);
		_refreshButton.Visible = isVisible;
		if (isVisible)
		{
			Player player = instance.Inventory.Player;
			int refreshCost = ShopEnhancementConfig.RefreshCost;
			if (ShopEnhancementConfig.RefreshLimitPerShop != -1 && _refreshCount >= ShopEnhancementConfig.RefreshLimitPerShop)
			{
				_refreshButton.Disable();
				_refreshButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.8f);
			}
			else if (player.Gold >= refreshCost)
			{
				_refreshButton.Enable();
				_refreshButton.Modulate = Colors.White;
			}
			else
			{
				_refreshButton.Disable();
				_refreshButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.8f);
			}
			string label = new LocString("shop_enhancement", "refresh.button_text").GetFormattedText();
			SetButtonText(_refreshButton, $"{label} ({refreshCost}g)", instance);
		}
	}

	[HarmonyPatch("OnActiveScreenUpdated")]
	[HarmonyPostfix]
	public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
	{
		UpdateRefreshButtonState(__instance);
	}

	private static readonly FieldInfo CreationResultField = AccessTools.Field(typeof(MerchantCardEntry), "<CreationResult>k__BackingField")
		?? throw new MissingFieldException(typeof(MerchantCardEntry).FullName, "<CreationResult>k__BackingField");

	private static readonly FieldInfo CardTypeField = AccessTools.Field(typeof(MerchantCardEntry), "_cardType")
		?? throw new MissingFieldException(typeof(MerchantCardEntry).FullName, "_cardType");

	/// <summary>
	/// With the configured chance, swaps each character card for a card of the same type from another character.
	/// Uses the player's shop RNG (not Godot's global RNG) so every client rolls the same shop when replaying a refresh.
	/// </summary>
	private static void ReplaceWithCrossClassCards(MerchantInventory inventory, Player player)
	{
		if (!ShopEnhancementConfig.EnableCrossClassCards || ShopEnhancementConfig.CrossClassCardChance <= 0f)
		{
			return;
		}
		List<CharacterModel> otherCharacters = ModelDb.AllCharacters.Where(c => c.Id != player.Character.Id).ToList();
		if (otherCharacters.Count == 0)
		{
			return;
		}
		Rng rng = player.PlayerRng.Shops;
		HashSet<CardModel> offeredCards = inventory.CardEntries.Select(e => e.CreationResult?.Card.CanonicalInstance).OfType<CardModel>().ToHashSet();
		foreach (MerchantCardEntry cardEntry in inventory.CharacterCardEntries)
		{
			if (rng.NextFloat() >= ShopEnhancementConfig.CrossClassCardChance)
			{
				continue;
			}
			// Character card entries are always created for a card type (colorless ones use a rarity instead).
			var cardType = (CardType?)CardTypeField.GetValue(cardEntry) ?? throw new InvalidOperationException("Character card entry has no card type.");
			CardModel? originalCard = cardEntry.CreationResult?.Card.CanonicalInstance;
			if (originalCard != null)
			{
				offeredCards.Remove(originalCard);
			}
			bool replaced = false;
			for (int attempt = 0; attempt < 6 && !replaced; attempt++)
			{
				List<CardModel> unlockedCards = otherCharacters[rng.NextInt(otherCharacters.Count)].CardPool
					.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).ToList();
				// Same pre-filter as CardFactory.CreateForMerchant, which throws when nothing of this type is left.
				if (!unlockedCards.Any(c => c.Type == cardType && c.Rarity != CardRarity.Basic && !offeredCards.Contains(c)))
				{
					continue;
				}
				CardCreationResult creationResult = GameCompat.CreateMerchantCard(player, unlockedCards, offeredCards, cardType);
				CreationResultField.SetValue(cardEntry, creationResult);
				cardEntry.CalcCost();
				offeredCards.Add(creationResult.Card.CanonicalInstance);
				replaced = true;
			}
			if (!replaced && originalCard != null)
			{
				offeredCards.Add(originalCard);
			}
		}
	}
}
