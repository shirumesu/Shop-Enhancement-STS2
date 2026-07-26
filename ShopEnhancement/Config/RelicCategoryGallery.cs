using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using System.Reflection;

namespace ShopEnhancement.Config;

internal static class RelicCategoryGallery
{
    private const string CollapsedIndicator = "▶";
    private const string ExpandedIndicator = "▼";
    private static readonly Vector2 RelicHolderMinimumSize = new(80, 80);
    private static readonly MethodInfo? RelicStatusSetter =
        typeof(RelicModel).GetProperty(nameof(RelicModel.Status))?.SetMethod;

    private static readonly GalleryDefinition[] Galleries =
    [
        new(nameof(ShopEnhancementBaseLibConfig.AllowStarterRelics),
            relic => relic.Rarity == RelicRarity.Starter),
        new(nameof(ShopEnhancementBaseLibConfig.AllowAncientRelics),
            relic => relic.Rarity == RelicRarity.Ancient),
        new(nameof(ShopEnhancementBaseLibConfig.AllowEventRelics),
            relic => relic.Rarity == RelicRarity.Event),
        new(nameof(ShopEnhancementBaseLibConfig.AllowWaxRelics),
            IsWaxCandidate,
            GalleryDisplay.Wax),
        new(nameof(ShopEnhancementBaseLibConfig.AllowMeltedRelics),
            IsWaxCandidate,
            GalleryDisplay.Melted),
        new(nameof(ShopEnhancementBaseLibConfig.AllowUsedUpRelics),
            CanBecomeUsedUp,
            GalleryDisplay.UsedUp),
        new(nameof(ShopEnhancementBaseLibConfig.AllowUponPickupRelics),
            HasUponPickupEffect),
        new(nameof(ShopEnhancementBaseLibConfig.AllowDisabledRelics),
            CanBecomeDisabled,
            GalleryDisplay.Disabled)
    ];

    internal static void Attach(Control optionContainer)
    {
        foreach (GalleryDefinition gallery in Galleries)
        {
            AttachGallery(optionContainer, gallery);
        }
    }

    private static void AttachGallery(Control optionContainer, GalleryDefinition definition)
    {
        string propertyName = definition.PropertyName;
        NConfigOptionRow? row = optionContainer.GetNodeOrNull<NConfigOptionRow>($"%{propertyName}");
        if (row == null)
        {
            MainFile.Logger.Warn($"Could not find BaseLib config row '{propertyName}' for its relic gallery.");
            return;
        }

        RichTextLabel? label = row.GetChildren()
            .OfType<RichTextLabel>()
            .FirstOrDefault();
        if (label == null)
        {
            MainFile.Logger.Warn($"Could not find the label for BaseLib config row '{propertyName}'.");
            return;
        }

        Node? parent = row.GetParent();
        if (parent == null)
        {
            MainFile.Logger.Warn($"BaseLib config row '{propertyName}' has no parent container.");
            return;
        }

        HFlowContainer iconFlow = new()
        {
            Name = $"{propertyName}RelicIconFlow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        iconFlow.AddThemeConstantOverride("h_separation", 12);
        iconFlow.AddThemeConstantOverride("v_separation", 12);

        MarginContainer gallery = new()
        {
            Name = $"{propertyName}RelicGallery",
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

        void SetExpanded(bool expanded)
        {
            if (expanded && !isPopulated)
            {
                Populate(iconFlow, definition);
                isPopulated = true;
            }

            isExpanded = expanded;
            gallery.Visible = expanded;
            label.Text = $"{baseLabelText}   {(expanded ? ExpandedIndicator : CollapsedIndicator)}";
        }

        label.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        label.MouseFilter = Control.MouseFilterEnum.Stop;
        label.GuiInput += inputEvent =>
        {
            if (inputEvent is not InputEventMouseButton
                {
                    ButtonIndex: MouseButton.Left,
                    Pressed: true
                })
            {
                return;
            }

            SetExpanded(!isExpanded);
            label.AcceptEvent();
        };

        SetExpanded(expanded: false);
    }

    private static void Populate(HFlowContainer iconFlow, GalleryDefinition definition)
    {
        List<RelicModel> relics;
        try
        {
            relics = ModelDb.AllRelics
                .Where(definition.Includes)
                .DistinctBy(relic => relic.Id)
                .OrderBy(GetLocalizedTitle, LocManager.Instance.StringComparer)
                .ToList();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"Failed to enumerate relics for the '{definition.PropertyName}' config gallery: {ex}");
            return;
        }

        foreach (RelicModel relic in relics)
        {
            try
            {
                RelicModel displayModel = CreateDisplayModel(relic, definition.Display);
                NRelicBasicHolder? holder = NRelicBasicHolder.Create(displayModel);
                if (holder == null)
                {
                    continue;
                }

                if (definition.Display is GalleryDisplay.UsedUp or GalleryDisplay.Disabled)
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

        RelicModel displayModel = relic.ToMutable();
        switch (display)
        {
            case GalleryDisplay.Wax:
                displayModel.IsWax = true;
                break;
            case GalleryDisplay.Melted:
                displayModel.IsWax = true;
                displayModel.IsMelted = true;
                displayModel.Status = RelicStatus.Disabled;
                break;
            case GalleryDisplay.UsedUp:
            case GalleryDisplay.Disabled:
                displayModel.Status = RelicStatus.Disabled;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(display), display, null);
        }

        return displayModel;
    }

    private static void ApplyUsedVisual(NRelicBasicHolder holder)
    {
        try
        {
            if (holder.Relic.Icon.Material is not ShaderMaterial original
                || original.Duplicate() is not ShaderMaterial shader)
            {
                return;
            }

            holder.Relic.Icon.Material = shader;
            shader.SetShaderParameter("is_used", 1);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to apply the used relic visual in the config gallery: {ex}");
        }
    }

    private static bool IsWaxCandidate(RelicModel relic)
    {
        return relic.Rarity is RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare;
    }

    private static bool CanBecomeUsedUp(RelicModel relic)
    {
        try
        {
            MethodInfo? getter = relic.GetType()
                .GetProperty(nameof(RelicModel.IsUsedUp), BindingFlags.Instance | BindingFlags.Public)
                ?.GetMethod;
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

        for (Type? type = relic.GetType();
             type != null && type != typeof(RelicModel);
             type = type.BaseType)
        {
            if (TypeSetsDisabledStatus(type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TypeSetsDisabledStatus(Type type)
    {
        const BindingFlags declaredMethods =
            BindingFlags.DeclaredOnly
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        try
        {
            foreach (MethodBase method in type.GetMethods(declaredMethods).Cast<MethodBase>()
                         .Concat(type.GetConstructors(declaredMethods)))
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

            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (TypeSetsDisabledStatus(nestedType))
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

        for (int offset = 0; offset <= il.Length - 5; offset++)
        {
            if (il[offset] is not (0x28 or 0x6F)
                || !LoadsDisabledValueImmediatelyBefore(il, offset))
            {
                continue;
            }

            try
            {
                int metadataToken = BitConverter.ToInt32(il, offset + 1);
                MethodBase? calledMethod = method.Module.ResolveMethod(
                    metadataToken,
                    method.DeclaringType?.GetGenericArguments(),
                    method.IsGenericMethod ? method.GetGenericArguments() : null);
                if (calledMethod != null
                    && calledMethod.MetadataToken == RelicStatusSetter.MetadataToken
                    && calledMethod.Module == RelicStatusSetter.Module)
                {
                    return true;
                }
            }
            catch
            {
                // The byte may be part of another instruction's operand rather than a call opcode.
            }
        }

        return false;
    }

    private static bool LoadsDisabledValueImmediatelyBefore(byte[] il, int callOffset)
    {
        const byte LdcI4Two = 0x18;
        const byte LdcI4Short = 0x1F;
        const byte LdcI4 = 0x20;

        if (callOffset >= 1 && il[callOffset - 1] == LdcI4Two)
        {
            return true;
        }

        if (callOffset >= 2
            && il[callOffset - 2] == LdcI4Short
            && il[callOffset - 1] == (byte)RelicStatus.Disabled)
        {
            return true;
        }

        return callOffset >= 5
            && il[callOffset - 5] == LdcI4
            && BitConverter.ToInt32(il, callOffset - 4) == (int)RelicStatus.Disabled;
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

    private readonly record struct GalleryDefinition(
        string PropertyName,
        Func<RelicModel, bool> Includes,
        GalleryDisplay Display = GalleryDisplay.Normal);

    private enum GalleryDisplay
    {
        Normal,
        Wax,
        Melted,
        UsedUp,
        Disabled
    }
}
