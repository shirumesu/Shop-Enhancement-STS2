using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopGiftModeButtonPatches
{
	private sealed class ButtonHolder
	{
		public NButton? GiftButton;

		public MegaLabel? GiftLabel;

		public Panel? GiftPanel;

		public NButton? TargetButton;

		public MegaLabel? TargetLabel;

		public Panel? TargetPanel;
	}

	private static readonly ConditionalWeakTable<NMerchantInventory, ButtonHolder> Buttons = new();

	[HarmonyPatch("_Ready")]
	[HarmonyPostfix]
	public static void Ready_Postfix(NMerchantInventory __instance)
	{
		if (!ShopEnhancementConfig.EnableGiftMode)
		{
			return;
		}
		ButtonHolder holder = Buttons.GetOrCreateValue(__instance);
		if (holder.GiftButton != null && GodotObject.IsInstanceValid(holder.GiftButton))
		{
			return;
		}
		Control? backButton = __instance.GetNodeOrNull<Control>("%BackButton");
		if (backButton == null)
		{
			return;
		}
		NButton giftButton = CreateButton(backButton.GetParent(), "GiftModeButton", 200f, -130f, 360f, -80f);
		Panel giftPanel = CreatePanel(giftButton);
		MegaLabel giftLabel = CreateLabel(giftButton);
		if (__instance.GetThemeFont("font", "Label") != null)
		{
			giftLabel.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
		}
		giftButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
		{
			if (__instance.Inventory != null)
			{
				bool isEnabled = GiftModeState.Toggle(__instance.Inventory);
				if (isEnabled)
				{
					if (GiftModeState.GetTargetPlayerId(__instance.Inventory) == 0L)
					{
						Player player = __instance.Inventory.Player;
						IRunState? runState = player?.RunState;
						if (player != null && runState != null)
						{
							Player? firstTeammate = runState.Players.FirstOrDefault(p => p.NetId != player.NetId);
							if (firstTeammate != null)
							{
								GiftModeState.SetTargetPlayerId(__instance.Inventory, firstTeammate.NetId);
							}
						}
					}
				}
				ApplyGiftButtonVisual(holder, isEnabled);
				UpdateButtonVisibility(__instance);
			}
		}));
		holder.GiftButton = giftButton;
		holder.GiftLabel = giftLabel;
		holder.GiftPanel = giftPanel;
		NButton targetButton = CreateButton(backButton.GetParent(), "GiftTargetButton", 200f, -190f, 360f, -140f);
		Panel targetPanel = CreatePanel(targetButton);
		MegaLabel targetLabel = CreateLabel(targetButton);
		if (__instance.GetThemeFont("font", "Label") != null)
		{
			targetLabel.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
		}
		targetButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
		{
			CycleTargetPlayer(__instance);
			UpdateTargetButtonVisual(holder, __instance);
		}));
		holder.TargetButton = targetButton;
		holder.TargetLabel = targetLabel;
		holder.TargetPanel = targetPanel;
		ApplyGiftButtonVisual(holder, enabled: false);
		UpdateButtonVisibility(__instance);
		UpdateTargetButtonVisual(holder, __instance);
	}

	[HarmonyPatch("Close")]
	[HarmonyPrefix]
	public static void Close_Prefix(NMerchantInventory __instance)
	{
		if (__instance.Inventory != null)
		{
			GiftModeState.Set(__instance.Inventory, isEnabled: false);
		}
		ButtonHolder holder = Buttons.GetOrCreateValue(__instance);
		if (holder.GiftButton != null && GodotObject.IsInstanceValid(holder.GiftButton))
		{
			ApplyGiftButtonVisual(holder, enabled: false);
			UpdateButtonVisibility(__instance);
		}
	}

	[HarmonyPatch("OnActiveScreenUpdated")]
	[HarmonyPostfix]
	public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
	{
		UpdateButtonVisibility(__instance);
	}

	public static void ForceUpdateVisuals(NMerchantInventory instance)
	{
		if (Buttons.TryGetValue(instance, out var holder))
		{
			bool enabled = instance.Inventory != null && GiftModeState.IsEnabled(instance.Inventory);
			ApplyGiftButtonVisual(holder, enabled);
			UpdateButtonVisibility(instance);
		}
	}

	private static void UpdateButtonVisibility(NMerchantInventory instance)
	{
		if (Buttons.TryGetValue(instance, out var holder) && holder.GiftButton != null && GodotObject.IsInstanceValid(holder.GiftButton))
		{
			bool isActiveScreen = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance);
			IRunState? runState = instance.Inventory?.Player?.RunState;
			bool isMultiplayer = runState != null && runState.Players.Count > 1;
			bool showGiftButton = isActiveScreen && isMultiplayer;
			holder.GiftButton.Visible = showGiftButton;
			bool giftModeEnabled = instance.Inventory != null && GiftModeState.IsEnabled(instance.Inventory);
			bool visible = showGiftButton && giftModeEnabled;
			if (holder.TargetButton != null)
			{
				holder.TargetButton.Visible = visible;
			}
		}
	}

	private static NButton CreateButton(Node parent, string name, float left, float top, float right, float bottom)
	{
		NButton button = new NButton();
		button.Name = name;
		parent.CallDeferred(Node.MethodName.AddChild, button);
		button.AnchorLeft = 0f;
		button.AnchorTop = 1f;
		button.AnchorRight = 0f;
		button.AnchorBottom = 1f;
		button.OffsetLeft = left;
		button.OffsetTop = top;
		button.OffsetRight = right;
		button.OffsetBottom = bottom;
		return button;
	}

	private static Panel CreatePanel(NButton parent)
	{
		var panel = new Panel();
		panel.Name = "Background";
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.MouseFilter = Control.MouseFilterEnum.Ignore;
		parent.CallDeferred(Node.MethodName.AddChild, panel);
		var style = new StyleBoxFlat();
		style.BorderWidthBottom = 2;
		style.BorderWidthLeft = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthTop = 2;
		style.CornerRadiusBottomLeft = 6;
		style.CornerRadiusBottomRight = 6;
		style.CornerRadiusTopLeft = 6;
		style.CornerRadiusTopRight = 6;
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	private static MegaLabel CreateLabel(NButton parent)
	{
		var label = new MegaLabel();
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.OffsetLeft = 10f;
		label.OffsetTop = 6f;
		label.OffsetRight = -10f;
		label.OffsetBottom = -6f;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		parent.CallDeferred(Node.MethodName.AddChild, label);
		return label;
	}

	private static void ApplyGiftButtonVisual(ButtonHolder holder, bool enabled)
	{
		if (holder.GiftLabel != null && holder.GiftPanel != null)
		{
			holder.GiftLabel.SetTextAutoSize(new LocString("shop_enhancement", enabled ? "gift.mode_on" : "gift.mode_off").GetFormattedText());
			var style = (StyleBoxFlat)holder.GiftPanel.GetThemeStylebox("panel");
			if (enabled)
			{
				style.BgColor = new Color(0.15f, 0.25f, 0.4f, 0.92f);
				style.BorderColor = new Color(0.5f, 0.7f, 1f);
			}
			else
			{
				style.BgColor = new Color(0.15f, 0.15f, 0.22f, 0.92f);
				style.BorderColor = new Color(0.8f, 0.65f, 0.3f);
			}
			if (holder.TargetLabel != null)
			{
				holder.TargetLabel.Modulate = Colors.Yellow;
			}
			if (holder.TargetButton != null)
			{
				holder.TargetButton.Modulate = Colors.White;
			}
		}
	}

	private static void UpdateTargetButtonVisual(ButtonHolder holder, NMerchantInventory inventory)
	{
		if (holder.TargetLabel == null || holder.TargetPanel == null || inventory.Inventory == null)
		{
			return;
		}
		ulong targetId = GiftModeState.GetTargetPlayerId(inventory.Inventory);
		IRunState? runState = inventory.Inventory?.Player?.RunState;
		string targetText = new LocString("shop_enhancement", "gift.target_none").GetFormattedText();
		if (runState != null && targetId != 0L)
		{
			Player? target = runState.Players.FirstOrDefault(p => p.NetId == targetId);
			if (target != null)
			{
				targetText = $"{target.Character.Title.GetFormattedText()} ({ShopEnhancementNetwork.DescribePlayerPlainText(target)})";
			}
		}
		var label = new LocString("shop_enhancement", "gift.target_button");
		label.Add("0", targetText);
		holder.TargetLabel.SetTextAutoSize(label.GetFormattedText());
		var style = (StyleBoxFlat)holder.TargetPanel.GetThemeStylebox("panel");
		style.BgColor = new Color(0.15f, 0.15f, 0.22f, 0.92f);
		style.BorderColor = new Color(0.8f, 0.65f, 0.3f);
		holder.TargetLabel.Modulate = Colors.Yellow;
		if (holder.TargetButton != null)
		{
			holder.TargetButton.Modulate = Colors.White;
		}
	}

	private static void CycleTargetPlayer(NMerchantInventory inventory)
	{
		if (inventory.Inventory == null)
		{
			return;
		}
		MerchantInventory merchantInventory = inventory.Inventory;
		IRunState? runState = merchantInventory.Player?.RunState;
		if (runState == null)
		{
			return;
		}
		ulong currentTargetId = GiftModeState.GetTargetPlayerId(merchantInventory);
		if (merchantInventory.Player == null)
		{
			return;
		}
		ulong myId = merchantInventory.Player.NetId;
		List<Player> teammates = runState.Players.Where(p => p.NetId != myId).ToList();
		if (teammates.Count != 0)
		{
			int index = (teammates.FindIndex(p => p.NetId == currentTargetId) + 1) % teammates.Count;
			GiftModeState.SetTargetPlayerId(inventory.Inventory, teammates[index].NetId);
		}
	}
}
