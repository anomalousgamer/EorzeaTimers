using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace EorzeaTimers.Windows;

public sealed class OverlaySettingsWindow : Window
{
    private readonly Plugin plugin;
    private readonly TimerOverlayWindow overlayWindow;

    public OverlaySettingsWindow(Plugin plugin, TimerOverlayWindow overlayWindow)
        : base("Eorzea Timers - Overlay Settings###EorzeaTimersOverlaySettings")
    {
        this.plugin = plugin;
        this.overlayWindow = overlayWindow;

        Size = new Vector2(500, 535);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 490),
            MaximumSize = new Vector2(700, 760),
        };
    }

    public override void Draw()
    {
        var configuration = plugin.Configuration;
        var changed = false;

        ImGui.TextUnformatted("Overlay");
        ImGui.Separator();
        ImGui.Spacing();

        var overlayEnabled = configuration.OverlayEnabled;
        if (ImGui.Checkbox("Show persistent timer overlay", ref overlayEnabled))
        {
            configuration.OverlayEnabled = overlayEnabled;
            changed = true;
        }

        var hideWhenEmpty = configuration.OverlayHideWhenNoActiveTimers;
        if (ImGui.Checkbox("Hide when no timers are active", ref hideWhenEmpty))
        {
            configuration.OverlayHideWhenNoActiveTimers = hideWhenEmpty;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Position and interaction");
        ImGui.Separator();

        var locked = configuration.OverlayLocked;
        if (ImGui.Checkbox("Lock overlay position", ref locked))
        {
            configuration.OverlayLocked = locked;
            changed = true;
        }

        var pinned = configuration.OverlayPinned;
        if (ImGui.Checkbox("Pin overlay", ref pinned))
        {
            configuration.OverlayPinned = pinned;
            changed = true;
        }

        var clickThrough = configuration.OverlayClickThrough;
        if (ImGui.Checkbox("Click through overlay", ref clickThrough))
        {
            configuration.OverlayClickThrough = clickThrough;
            changed = true;
        }

        ImGui.TextDisabled(
            "If click-through is enabled, reopen these settings with /etimers overlay.");

        ImGui.Spacing();
        ImGui.TextUnformatted("Size and transparency");
        ImGui.Separator();

        var scale = configuration.OverlayScale;
        ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Scale", ref scale, 0.75f, 2f, "%.2fx"))
        {
            configuration.OverlayScale = Math.Clamp(scale, 0.75f, 2f);
            changed = true;
        }

        var width = configuration.OverlayWidth;
        ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Width", ref width, 200f, 500f, "%.0f px"))
        {
            configuration.OverlayWidth = Math.Clamp(width, 200f, 500f);
            changed = true;
        }

        var opacityPercent = configuration.OverlayOpacity * 100f;
        ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
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

        ImGui.Spacing();
        ImGui.TextUnformatted("Visibility");
        ImGui.Separator();

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
            configuration.Save();
            plugin.ApplyUiHideSettings();
        }

        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.Button(
                "Reset Overlay Position",
                new Vector2(190f * ImGuiHelpers.GlobalScale, 0)))
        {
            configuration.OverlayPositionSet = false;
            configuration.OverlayPositionX = 0f;
            configuration.OverlayPositionY = 0f;
            configuration.Save();
            overlayWindow.RequestPositionReset();
        }
    }
}
