using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace ShopEnhancement.Config;

/// <summary>
/// Display-only relic galleries in the config screen ("these relics count as used-up / disabled / ...").
/// It inspects every relic in ModelDb, including other mods' relics, via reflection and IL scanning, so the
/// try/catch blocks here deliberately treat a relic that cannot be inspected as "not in this category" instead of
/// breaking the settings page. Nothing here affects gameplay.
/// </summary>
internal static class RelicCategoryGallery
{
	private readonly record struct GalleryDefinition(string PropertyName, Func<RelicModel, bool> Includes, GalleryDisplay Display = GalleryDisplay.Normal);

	private enum GalleryDisplay
	{
		Normal,
		Wax,
		Melted,
		UsedUp,
		Disabled
	}

	private const string CollapsedIndicator = "▶";

	private const string ExpandedIndicator = "▼";

	private static readonly Vector2 RelicHolderMinimumSize = new Vector2(80f, 80f);

	private static readonly MethodInfo? RelicStatusSetter = typeof(RelicModel).GetProperty("Status")?.SetMethod;

	private static readonly GalleryDefinition[] Galleries =
	{
		new GalleryDefinition("AllowStarterRelics", relic => relic.Rarity == RelicRarity.Starter),
		new GalleryDefinition("AllowAncientRelics", relic => relic.Rarity == RelicRarity.Ancient),
		new GalleryDefinition("AllowEventRelics", relic => relic.Rarity == RelicRarity.Event),
		new GalleryDefinition("AllowWaxRelics", IsWaxCandidate, GalleryDisplay.Wax),
		new GalleryDefinition("AllowMeltedRelics", IsWaxCandidate, GalleryDisplay.Melted),
		new GalleryDefinition("AllowUsedUpRelics", CanBecomeUsedUp, GalleryDisplay.UsedUp),
		new GalleryDefinition("AllowUponPickupRelics", HasUponPickupEffect),
		new GalleryDefinition("AllowDisabledRelics", CanBecomeDisabled, GalleryDisplay.Disabled)
	};

	internal static void Attach(Control optionContainer)
	{
		foreach (GalleryDefinition definition in Galleries)
		{
			AttachGallery(optionContainer, definition);
		}
	}

	private static void AttachGallery(Control optionContainer, GalleryDefinition definition)
	{
		string propertyName = definition.PropertyName;
		var row = optionContainer.GetNodeOrNull<NConfigOptionRow>("%" + propertyName);
		if (row == null)
		{
			MainFile.Logger.Warn("Could not find BaseLib config row '" + propertyName + "' for its relic gallery.");
			return;
		}
		RichTextLabel? label = row.GetChildren().OfType<RichTextLabel>().FirstOrDefault();
		if (label == null)
		{
			MainFile.Logger.Warn("Could not find the label for BaseLib config row '" + propertyName + "'.");
			return;
		}
		Node parent = row.GetParent();
		if (parent == null)
		{
			MainFile.Logger.Warn("BaseLib config row '" + propertyName + "' has no parent container.");
			return;
		}
		HFlowContainer iconFlow = new HFlowContainer
		{
			Name = propertyName + "RelicIconFlow",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		iconFlow.AddThemeConstantOverride("h_separation", 12);
		iconFlow.AddThemeConstantOverride("v_separation", 12);
		MarginContainer gallery = new MarginContainer
		{
			Name = propertyName + "RelicGallery",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Visible = false
		};
		gallery.AddThemeConstantOverride("margin_left", 24);
		gallery.AddThemeConstantOverride("margin_right", 24);
		gallery.AddThemeConstantOverride("margin_top", 8);
		gallery.AddThemeConstantOverride("margin_bottom", 16);
		gallery.AddChild(iconFlow);
		parent.AddChild(gallery);
		parent.MoveChild(gallery, row.GetIndex() + 1);
		string baseLabelText = label.Text;
		bool isExpanded = false;
		bool isPopulated = false;
		label.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		label.MouseFilter = Control.MouseFilterEnum.Stop;
		label.GuiInput += inputEvent =>
		{
			if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				SetExpanded(!isExpanded);
				label.AcceptEvent();
			}
		};
		SetExpanded(expanded: false);
		void SetExpanded(bool expanded)
		{
			if (expanded && !isPopulated)
			{
				Populate(iconFlow, definition);
				isPopulated = true;
			}
			isExpanded = expanded;
			gallery.Visible = expanded;
			label.Text = baseLabelText + "   " + (expanded ? ExpandedIndicator : CollapsedIndicator);
		}
	}

	private static void Populate(HFlowContainer iconFlow, GalleryDefinition definition)
	{
		List<RelicModel> relics;
		try
		{
			relics = ModelDb.AllRelics.Where(definition.Includes).DistinctBy(relicModel => relicModel.Id).OrderBy(GetLocalizedTitle, LocManager.Instance.StringComparer)
				.ToList();
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn($"Failed to enumerate relics for the '{definition.PropertyName}' config gallery: {ex}");
			return;
		}
		foreach (RelicModel relic in relics)
		{
			try
			{
				RelicModel displayRelic = CreateDisplayModel(relic, definition.Display);
				NRelicBasicHolder? holder = NRelicBasicHolder.Create(displayRelic);
				if (holder == null)
				{
					continue;
				}
				// UsedUp or Disabled
				if ((uint)(definition.Display - GalleryDisplay.UsedUp) <= 1u)
				{
					holder.Ready += () => ApplyUsedVisual(holder);
				}
				holder.CustomMinimumSize = RelicHolderMinimumSize;
				iconFlow.AddChild(holder);
			}
			catch (Exception ex)
			{
				MainFile.Logger.Warn($"Failed to create config gallery icon for relic '{relic.Id}': {ex}");
			}
		}
	}

	private static RelicModel CreateDisplayModel(RelicModel relic, GalleryDisplay display)
	{
		if (display == GalleryDisplay.Normal)
		{
			return relic;
		}
		RelicModel mutable = relic.ToMutable();
		switch (display)
		{
		case GalleryDisplay.Wax:
			mutable.IsWax = true;
			break;
		case GalleryDisplay.Melted:
			mutable.IsWax = true;
			mutable.IsMelted = true;
			mutable.Status = RelicStatus.Disabled;
			break;
		case GalleryDisplay.UsedUp:
		case GalleryDisplay.Disabled:
			mutable.Status = RelicStatus.Disabled;
			break;
		default:
			throw new ArgumentOutOfRangeException(nameof(display), display, null);
		}
		return mutable;
	}

	private static void ApplyUsedVisual(NRelicBasicHolder holder)
	{
		try
		{
			if (holder.Relic.Icon.Material is ShaderMaterial material && material.Duplicate() is ShaderMaterial materialCopy)
			{
				holder.Relic.Icon.Material = materialCopy;
				materialCopy.SetShaderParameter("is_used", 1);
			}
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn($"Failed to apply the used relic visual in the config gallery: {ex}");
		}
	}

	private static bool IsWaxCandidate(RelicModel relic)
	{
		// Common, Uncommon or Rare
		if ((uint)(relic.Rarity - RelicRarity.Common) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static bool CanBecomeUsedUp(RelicModel relic)
	{
		try
		{
			MethodInfo? getter = relic.GetType().GetProperty("IsUsedUp", BindingFlags.Instance | BindingFlags.Public)?.GetMethod;
			return getter != null && getter.DeclaringType != typeof(RelicModel);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasUponPickupEffect(RelicModel relic)
	{
		try
		{
			return relic.HasUponPickupEffect;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanBecomeDisabled(RelicModel relic)
	{
		if (CanBecomeUsedUp(relic))
		{
			return true;
		}
		Type? type = relic.GetType();
		while (type != null && type != typeof(RelicModel))
		{
			if (TypeSetsDisabledStatus(type))
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	private static bool TypeSetsDisabledStatus(Type type)
	{
		try
		{
			foreach (MethodBase method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
			{
				if (MethodSetsDisabledStatus(method))
				{
					return true;
				}
			}
			if (type.TypeInitializer != null && MethodSetsDisabledStatus(type.TypeInitializer))
			{
				return true;
			}
			Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < nestedTypes.Length; i++)
			{
				if (TypeSetsDisabledStatus(nestedTypes[i]))
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool MethodSetsDisabledStatus(MethodBase method)
	{
		if (RelicStatusSetter == null)
		{
			return false;
		}
		byte[]? il;
		try
		{
			il = method.GetMethodBody()?.GetILAsByteArray();
		}
		catch
		{
			return false;
		}
		if (il == null || il.Length < 6)
		{
			return false;
		}
		for (int i = 0; i <= il.Length - 5; i++)
		{
			// 40 = call, 111 = callvirt; both are followed by a 4-byte method token.
			byte opcode = il[i];
			bool isCall = ((opcode == 40 || opcode == 111) ? true : false);
			if (!isCall || !LoadsDisabledValueImmediatelyBefore(il, i))
			{
				continue;
			}
			try
			{
				int metadataToken = BitConverter.ToInt32(il, i + 1);
				MethodBase? callee = method.Module.ResolveMethod(metadataToken, method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
				if (callee != null && callee.MetadataToken == RelicStatusSetter.MetadataToken && callee.Module == RelicStatusSetter.Module)
				{
					return true;
				}
			}
			catch
			{
				// The scan walks raw IL bytes, so this "call" may be a byte inside another instruction's operand and
				// its token garbage; such matches are simply not real calls.
			}
		}
		return false;
	}

	// Matches "ldc.i4.2" (24), "ldc.i4.s 2" (31) or "ldc.i4 2" (32) right before the call; 2 is RelicStatus.Disabled.
	private static bool LoadsDisabledValueImmediatelyBefore(byte[] il, int callOffset)
	{
		if (callOffset >= 1 && il[callOffset - 1] == 24)
		{
			return true;
		}
		if (callOffset >= 2 && il[callOffset - 2] == 31 && il[callOffset - 1] == 2)
		{
			return true;
		}
		if (callOffset >= 5 && il[callOffset - 5] == 32)
		{
			return BitConverter.ToInt32(il, callOffset - 4) == 2;
		}
		return false;
	}

	private static string GetLocalizedTitle(RelicModel relic)
	{
		try
		{
			return relic.Title.GetFormattedText();
		}
		catch
		{
			return relic.Id.Entry;
		}
	}
}
