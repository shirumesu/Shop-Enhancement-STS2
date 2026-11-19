using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopSellModeButtonPatches
{
	private sealed class ButtonHolder
	{
		public NButton? Button;

		public MegaLabel? Label;

		public Panel? Panel;
	}

	private static readonly ConditionalWeakTable<NMerchantInventory, ButtonHolder> Buttons = new ConditionalWeakTable<NMerchantInventory, ButtonHolder>();

	[HarmonyPatch("_Ready")]
	[HarmonyPostfix]
	public static void Ready_Postfix(NMerchantInventory __instance)
	{
		if (!ShopEnhancementConfig.EnableSellMode)
		{
			return;
		}
		ButtonHolder holder = Buttons.GetOrCreateValue(__instance);
		if (holder.Button != null && GodotObject.IsInstanceValid(holder.Button))
		{
			return;
		}
		Control? backButton = __instance.GetNodeOrNull<Control>("%BackButton");
		if (backButton != null)
		{
			var button = new NButton();
			button.Name = "SellModeButton";
			backButton.GetParent().CallDeferred(Node.MethodName.AddChild, button);
			button.AnchorLeft = 0f;
			button.AnchorTop = 1f;
			button.AnchorRight = 0f;
			button.AnchorBottom = 1f;
			button.OffsetLeft = 20f;
			button.OffsetTop = -130f;
			button.OffsetRight = 180f;
			button.OffsetBottom = -80f;
			var panel = new Panel();
			panel.Name = "Background";
			panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			panel.MouseFilter = Control.MouseFilterEnum.Ignore;
			button.CallDeferred(Node.MethodName.AddChild, panel);
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
			label.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
			button.CallDeferred(Node.MethodName.AddChild, label);
			button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
			{
				bool enabled = SellModeState.Toggle(__instance);
				ApplyButtonVisual(holder, enabled);
			}));
			holder.Button = button;
			holder.Label = label;
			holder.Panel = panel;
			ApplyButtonVisual(holder, SellModeState.IsEnabled(__instance));
			UpdateButtonVisibility(__instance);
		}
	}

	[HarmonyPatch("Open")]
	[HarmonyPostfix]
	public static void Open_Postfix(NMerchantInventory __instance)
	{
		ForceUpdateVisuals(__instance);
	}

	[HarmonyPatch("Close")]
	[HarmonyPrefix]
	public static void Close_Prefix(NMerchantInventory __instance)
	{
		UpdateButtonVisibility(__instance);
	}

	[HarmonyPatch("OnActiveScreenUpdated")]
	[HarmonyPostfix]
	public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
	{
		UpdateButtonVisibility(__instance);
	}

	public static void ForceUpdateVisuals(NMerchantInventory instance)
	{
		if (Buttons.TryGetValue(instance, out ButtonHolder? holder))
		{
			ApplyButtonVisual(holder, SellModeState.IsEnabled(instance));
			UpdateButtonVisibility(instance);
		}
	}

	private static void UpdateButtonVisibility(NMerchantInventory instance)
	{
		if (Buttons.TryGetValue(instance, out ButtonHolder? holder) && holder.Button != null && GodotObject.IsInstanceValid(holder.Button))
		{
			bool visible = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance);
			holder.Button.Visible = visible;
		}
	}

	private static void ApplyButtonVisual(ButtonHolder holder, bool enabled)
	{
		if (holder.Button != null && holder.Label != null && holder.Panel != null)
		{
			NButton button = holder.Button;
			MegaLabel label = holder.Label;
			Panel panel = holder.Panel;
			label.SetTextAutoSize(new LocString("shop_enhancement", enabled ? "sell.mode_on" : "sell.mode_off").GetFormattedText());
			panel.AddThemeStyleboxOverride(stylebox: new StyleBoxFlat
			{
				BorderWidthBottom = 2,
				BorderWidthLeft = 2,
				BorderWidthRight = 2,
				BorderWidthTop = 2,
				CornerRadiusBottomLeft = 6,
				CornerRadiusBottomRight = 6,
				CornerRadiusTopLeft = 6,
				CornerRadiusTopRight = 6,
				BgColor = enabled ? new Color(0.15f, 0.35f, 0.2f, 0.92f) : new Color(0.15f, 0.15f, 0.22f, 0.92f),
				BorderColor = enabled ? new Color(0.6f, 0.85f, 0.55f) : new Color(0.8f, 0.65f, 0.3f)
			}, name: "panel");
			button.Modulate = Colors.White;
		}
	}
}
