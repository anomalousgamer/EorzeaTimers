using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using EorzeaTimers.Models;

namespace EorzeaTimers.Windows;

public sealed class CompletionAlertWindow : Window
{
    private sealed record CompletionAlert(
        string Name,
        string Notes,
        TimerIcon Icon,
        TimerColor Color,
        bool IsTest);

    private readonly Queue<CompletionAlert> pendingAlerts = new();
    private CompletionAlert? currentAlert;
    private bool stylePushed;

    public CompletionAlertWindow()
        : base("Timer Complete###EorzeaTimersCompletionAlert")
    {
        IsOpen = true;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public override bool DrawConditions()
    {
        if (!Plugin.PlayerState.IsLoaded)
        {
            return false;
        }

        if (currentAlert is null && pendingAlerts.Count > 0)
        {
            currentAlert = pendingAlerts.Dequeue();
        }

        return currentAlert is not null;
    }

    public override void PreDraw()
    {
        Flags =
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoTitleBar;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(340f, 0f),
            MaximumSize = new Vector2(340f, float.MaxValue),
        };

        var displaySize = ImGui.GetIO().DisplaySize;
        Position = new Vector2(
            System.MathF.Max(20f, displaySize.X - 370f),
            System.MathF.Max(20f, displaySize.Y - 210f));
        PositionCondition = ImGuiCond.Always;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.035f, 0.04f, 0.05f, 0.98f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.78f, 0.61f, 0.28f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
        stylePushed = true;
    }

    public override void Draw()
    {
        if (currentAlert is null)
        {
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var alertColor = TimerAppearance.GetColor(currentAlert.Color);

        ImGui.TextColored(
            new Vector4(0.92f, 0.75f, 0.39f, 1f),
            currentAlert.IsTest ? "Timer Complete - Test" : "Timer Complete");
        ImGui.Separator();
        ImGui.Spacing();

        using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
        {
            ImGui.TextColored(alertColor, TimerAppearance.GetIconGlyph(currentAlert.Icon));
        }

        ImGui.SameLine();
        ImGui.TextColored(alertColor, currentAlert.Name);
        ImGui.Indent(30f * scale);
        ImGui.TextWrapped(
            currentAlert.IsTest
                ? "This is a test completion alert."
                : "Your timer has finished!");

        if (!string.IsNullOrWhiteSpace(currentAlert.Notes))
        {
            ImGui.TextDisabled(currentAlert.Notes);
        }

        ImGui.Unindent(30f * scale);

        if (pendingAlerts.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled(
                pendingAlerts.Count == 1
                    ? "1 more completed timer is waiting."
                    : $"{pendingAlerts.Count} more completed timers are waiting.");
        }

        ImGui.Spacing();
        if (ImGui.Button("Dismiss", new Vector2(-1f, 0f)))
        {
            currentAlert = null;
        }
    }

    public override void PostDraw()
    {
        if (!stylePushed)
        {
            return;
        }

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        stylePushed = false;
    }

    internal void Enqueue(TimerEntry timer)
    {
        Enqueue(timer.Name, timer.Notes, timer.Icon, timer.Color, false);
    }

    internal void Enqueue(
        string name,
        string notes,
        TimerIcon icon,
        TimerColor color,
        bool isTest)
    {
        pendingAlerts.Enqueue(
            new CompletionAlert(name, notes, icon, color, isTest));
    }
}
