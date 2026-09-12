using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using EorzeaTimers.Models;

namespace EorzeaTimers.Windows;

public sealed class CompletionAlertWindow : Window
{
    private static readonly int[] SnoozePresets = [5, 10, 15, 30, 60];

    private sealed record CompletionAlert(
        Guid? TimerId,
        string Name,
        string Notes,
        TimerIcon Icon,
        TimerColor Color,
        bool IsTest);

    private readonly Plugin plugin;
    private readonly Queue<CompletionAlert> pendingAlerts = new();
    private CompletionAlert? currentAlert;
    private bool stylePushed;

    public CompletionAlertWindow(Plugin plugin)
        : base("Timer Complete###EorzeaTimersCompletionAlert")
    {
        this.plugin = plugin;

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

        if (currentAlert?.TimerId is Guid timerId
            && !plugin.IsTimerAwaitingCompletion(timerId))
        {
            currentAlert = null;
        }

        while (currentAlert is null && pendingAlerts.Count > 0)
        {
            var candidate = pendingAlerts.Dequeue();
            if (!candidate.TimerId.HasValue
                || plugin.IsTimerAwaitingCompletion(candidate.TimerId.Value))
            {
                currentAlert = candidate;
            }
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
            MinimumSize = new Vector2(360f, 0f),
            MaximumSize = new Vector2(360f, float.MaxValue),
        };

        var displaySize = ImGui.GetIO().DisplaySize;
        Position = new Vector2(
            MathF.Max(20f, displaySize.X - 390f),
            MathF.Max(20f, displaySize.Y - 260f));
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
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.72f, 0.75f, 0.80f, 1f));
            ImGui.TextWrapped(currentAlert.Notes);
            ImGui.PopStyleColor();
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

        if (currentAlert.IsTest || !currentAlert.TimerId.HasValue)
        {
            if (ImGui.Button("Dismiss", new Vector2(-1f, 0f)))
            {
                currentAlert = null;
            }

            return;
        }

        DrawSnoozeSelector();
        ImGui.Spacing();

        var buttonWidth =
            (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;
        if (ImGui.Button("Dismiss", new Vector2(buttonWidth, 0f)))
        {
            plugin.DismissCompletionAlert(currentAlert.TimerId.Value);
            currentAlert = null;
        }

        ImGui.SameLine();
        var snoozeMinutes = plugin.Configuration.DefaultSnoozeMinutes;
        if (ImGui.Button(
                $"Snooze ({FormatMinutes(snoozeMinutes)})",
                new Vector2(buttonWidth, 0f)))
        {
            plugin.SnoozeTimer(currentAlert.TimerId.Value, snoozeMinutes);
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
        pendingAlerts.Enqueue(
            new CompletionAlert(
                timer.Id,
                timer.Name,
                timer.Notes,
                timer.Icon,
                timer.Color,
                false));
    }

    internal void Enqueue(
        string name,
        string notes,
        TimerIcon icon,
        TimerColor color,
        bool isTest)
    {
        pendingAlerts.Enqueue(
            new CompletionAlert(null, name, notes, icon, color, isTest));
    }

    private void DrawSnoozeSelector()
    {
        var minutes = plugin.Configuration.DefaultSnoozeMinutes;
        var isPreset = IsSnoozePreset(minutes);

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Snooze for");
        ImGui.SameLine(100f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1f);

        var preview = isPreset ? FormatMinutes(minutes) : $"Custom ({FormatMinutes(minutes)})";
        if (ImGui.BeginCombo("##SnoozeDuration", preview))
        {
            foreach (var preset in SnoozePresets)
            {
                var selected = minutes == preset;
                if (ImGui.Selectable(FormatMinutes(preset), selected))
                {
                    plugin.Configuration.DefaultSnoozeMinutes = preset;
                    plugin.Configuration.Save();
                    minutes = preset;
                    isPreset = true;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            if (ImGui.Selectable("Custom...", !isPreset))
            {
                if (isPreset)
                {
                    plugin.Configuration.DefaultSnoozeMinutes = 90;
                    plugin.Configuration.Save();
                }

                isPreset = false;
            }

            ImGui.EndCombo();
        }

        if (!isPreset)
        {
            var customMinutes = plugin.Configuration.DefaultSnoozeMinutes;
            ImGui.SetNextItemWidth(120f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("Custom minutes", ref customMinutes))
            {
                plugin.Configuration.DefaultSnoozeMinutes =
                    Math.Clamp(customMinutes, 1, 10080);
                plugin.Configuration.Save();
            }
        }
    }

    private static bool IsSnoozePreset(int minutes)
    {
        foreach (var preset in SnoozePresets)
        {
            if (preset == minutes)
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatMinutes(int minutes)
    {
        return minutes >= 60 && minutes % 60 == 0
            ? $"{minutes / 60}h"
            : $"{minutes}m";
    }
}
