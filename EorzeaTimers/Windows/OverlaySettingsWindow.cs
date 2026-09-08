using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using EorzeaTimers.Models;

namespace EorzeaTimers.Windows;

public sealed class OverlaySettingsWindow : Window
{
    private const float DefaultScale = 1f;
    private const float DefaultWidth = 280f;
    private const float DefaultOpacity = 0.9f;

    private readonly Plugin plugin;
    private readonly TimerOverlayWindow overlayWindow;

    public OverlaySettingsWindow(Plugin plugin, TimerOverlayWindow overlayWindow)
        : base("Eorzea Timers - Overlay Settings###EorzeaTimersOverlaySettings")
    {
        this.plugin = plugin;
        this.overlayWindow = overlayWindow;

        Size = new Vector2(560, 720);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 640),
            MaximumSize = new Vector2(760, 900),
        };
    }

    public override void Draw()
    {
        var configuration = plugin.Configuration;
        var changed = false;

        ImGui.TextColored(new Vector4(0.92f, 0.75f, 0.39f, 1f), "Overlay mode");
        ImGui.Separator();
        ImGui.Spacing();

        changed |= DrawModeRadio(
            "Overlay Off",
            OverlayDisplayMode.Off,
            "The overlay is always hidden.");
        changed |= DrawModeRadio(
            "Overlay Persistent",
            OverlayDisplayMode.Persistent,
            "The overlay remains visible according to the visibility settings below.");
        changed |= DrawModeRadio(
            "Overlay Key Bound",
            OverlayDisplayMode.KeyBound,
            "The overlay is visible only while the selected key is held.");

        if (configuration.OverlayMode == OverlayDisplayMode.KeyBound)
        {
            ImGui.Indent(24f * ImGuiHelpers.GlobalScale);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Hold key");
            ImGui.SameLine(130f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);

            if (ImGui.BeginCombo(
                    "##OverlayHoldKey",
                    OverlayKeys.GetName(configuration.OverlayHoldKey)))
            {
                foreach (var option in OverlayKeys.Options)
                {
                    var selected = configuration.OverlayHoldKey == option.Key;
                    if (ImGui.Selectable(option.Name, selected))
                    {
                        configuration.OverlayHoldKey = option.Key;
                        changed = true;
                    }

                    if (selected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TextDisabled(
                $"Hold {OverlayKeys.GetName(configuration.OverlayHoldKey)} to show the overlay.");
            ImGui.Unindent(24f * ImGuiHelpers.GlobalScale);
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.92f, 0.75f, 0.39f, 1f), "Position and interaction");
        ImGui.Separator();

        var locked = configuration.OverlayLocked;
        if (ImGui.Checkbox("Lock overlay position and size", ref locked))
        {
            configuration.OverlayLocked = locked;
            changed = true;
        }

        var clickThrough = configuration.OverlayClickThrough;
        if (ImGui.Checkbox("Click through overlay", ref clickThrough))
        {
            configuration.OverlayClickThrough = clickThrough;
            changed = true;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.76f, 0.28f, 1f));
        ImGui.TextWrapped(
            "While enabled, the overlay ignores all mouse input. Disable it from this settings window or use /etimers clickthrough off.");
        ImGui.PopStyleColor();

        ImGui.TextDisabled("Click a timer to open it. Drag a timer row to move the overlay.");
        ImGui.TextDisabled("Drag the bottom-right grip to change the overlay width.");

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.92f, 0.75f, 0.39f, 1f), "Size and style");
        ImGui.Separator();

        var scale = configuration.OverlayScale;
        ImGui.SetNextItemWidth(280f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Scale", ref scale, 0.75f, 2f, "%.2fx"))
        {
            configuration.OverlayScale = Math.Clamp(scale, 0.75f, 2f);
            changed = true;
        }

        var width = configuration.OverlayWidth;
        ImGui.SetNextItemWidth(280f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Width", ref width, 200f, 500f, "%.0f px"))
        {
            configuration.OverlayWidth = Math.Clamp(width, 200f, 500f);
            changed = true;
        }

        ImGui.TextUnformatted("Width presets");
        ImGui.SameLine(130f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button("Small##OverlayWidth", new Vector2(82f * ImGuiHelpers.GlobalScale, 0)))
        {
            configuration.OverlayWidth = 240f;
            changed = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("Medium##OverlayWidth", new Vector2(82f * ImGuiHelpers.GlobalScale, 0)))
        {
            configuration.OverlayWidth = 320f;
            changed = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("Large##OverlayWidth", new Vector2(82f * ImGuiHelpers.GlobalScale, 0)))
        {
            configuration.OverlayWidth = 420f;
            changed = true;
        }

        var opacityPercent = configuration.OverlayOpacity * 100f;
        ImGui.SetNextItemWidth(280f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat(
                "Background opacity",
                ref opacityPercent,
                20f,
                100f,
                "%.0f%%"))
        {
            configuration.OverlayOpacity =
                Math.Clamp(opacityPercent / 100f, 0.2f, 1f);
            changed = true;
        }

        var rowStyle = configuration.OverlayRowStyle;
        ImGui.SetNextItemWidth(280f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("Row style", rowStyle.ToString()))
        {
            foreach (var option in Enum.GetValues<OverlayRowStyle>())
            {
                var selected = rowStyle == option;
                if (ImGui.Selectable(option.ToString(), selected))
                {
                    configuration.OverlayRowStyle = option;
                    changed = true;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.TextDisabled("Detailed rows also show timer notes when available.");

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.92f, 0.75f, 0.39f, 1f), "Visibility");
        ImGui.Separator();

        var hideWhenEmpty = configuration.OverlayHideWhenNoActiveTimers;
        if (ImGui.Checkbox("Hide when no overlay timers are active", ref hideWhenEmpty))
        {
            configuration.OverlayHideWhenNoActiveTimers = hideWhenEmpty;
            changed = true;
        }

        var showInCombat = configuration.OverlayShowInCombat;
        if (ImGui.Checkbox("Show during combat", ref showInCombat))
        {
            configuration.OverlayShowInCombat = showInCombat;
            changed = true;
        }

        var showInDuty = configuration.OverlayShowInDuty;
        if (ImGui.Checkbox("Show while inside duties", ref showInDuty))
        {
            configuration.OverlayShowInDuty = showInDuty;
            changed = true;
        }

        var showInCutscenes = configuration.OverlayShowInCutscenes;
        if (ImGui.Checkbox("Show during cutscenes", ref showInCutscenes))
        {
            configuration.OverlayShowInCutscenes = showInCutscenes;
            changed = true;
        }

        var showWhenUiHidden = configuration.OverlayShowWhenUiHidden;
        if (ImGui.Checkbox("Show when the game UI is hidden", ref showWhenUiHidden))
        {
            configuration.OverlayShowWhenUiHidden = showWhenUiHidden;
            changed = true;
        }

        if (changed)
        {
            configuration.OverlayEnabled =
                configuration.OverlayMode != OverlayDisplayMode.Off;
            configuration.Save();
            plugin.ApplyUiHideSettings();
        }

        ImGui.Spacing();
        ImGui.Separator();

        var resetButtonWidth = 190f * ImGuiHelpers.GlobalScale;
        if (ImGui.Button("Reset Position", new Vector2(resetButtonWidth, 0)))
        {
            configuration.OverlayPositionSet = false;
            configuration.OverlayPositionX = 0f;
            configuration.OverlayPositionY = 0f;
            configuration.Save();
            overlayWindow.RequestPositionReset();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Size & Style", new Vector2(resetButtonWidth, 0)))
        {
            configuration.OverlayScale = DefaultScale;
            configuration.OverlayWidth = DefaultWidth;
            configuration.OverlayOpacity = DefaultOpacity;
            configuration.OverlayRowStyle = OverlayRowStyle.Compact;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Use /etimers help for the complete command and overlay guide.");
    }

    private bool DrawModeRadio(
        string label,
        OverlayDisplayMode mode,
        string description)
    {
        var selected = plugin.Configuration.OverlayMode == mode;
        var changed = false;

        if (ImGui.RadioButton(label, selected))
        {
            plugin.Configuration.OverlayMode = mode;
            changed = true;
        }

        ImGui.SameLine();
        ImGui.TextDisabled(description);
        return changed;
    }
}
