using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using EorzeaTimers.Models;

namespace EorzeaTimers.Windows;

public sealed class TimerOverlayWindow : Window
{
    private const int StableFramesBeforePositionSave = 8;

    private readonly Plugin plugin;
    private bool positionNeedsApply = true;
    private Vector2? pendingPosition;
    private int stablePositionFrames;

    public TimerOverlayWindow(Plugin plugin)
        : base("Eorzea Timers###EorzeaTimersOverlay")
    {
        this.plugin = plugin;

        IsOpen = true;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;
        AllowPinning = true;
        AllowClickthrough = true;
    }

    public override bool DrawConditions()
    {
        var configuration = plugin.Configuration;

        if (!configuration.OverlayEnabled || !Plugin.PlayerState.IsLoaded)
        {
            return false;
        }

        if (configuration.OverlayHideWhenNoActiveTimers
            && !configuration.Timers.Exists(timer => timer.IsActive))
        {
            return false;
        }

        if (!configuration.OverlayShowInCombat
            && Plugin.Condition[ConditionFlag.InCombat])
        {
            return false;
        }

        if (!configuration.OverlayShowInDuty
            && Plugin.Condition.Any(
                ConditionFlag.BoundByDuty,
                ConditionFlag.BoundByDuty56,
                ConditionFlag.BoundByDuty95))
        {
            return false;
        }

        if (!configuration.OverlayShowInCutscenes
            && Plugin.Condition.Any(
                ConditionFlag.OccupiedInCutSceneEvent,
                ConditionFlag.WatchingCutscene,
                ConditionFlag.WatchingCutscene78))
        {
            return false;
        }

        if (!configuration.OverlayShowWhenUiHidden && Plugin.GameGui.GameUiHidden)
        {
            return false;
        }

        return true;
    }

    public override void PreDraw()
    {
        var configuration = plugin.Configuration;

        Flags =
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoSavedSettings;

        if (configuration.OverlayLocked)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }

        BgAlpha = Math.Clamp(configuration.OverlayOpacity, 0.2f, 1f);
        IsPinned = configuration.OverlayPinned;
        IsClickthrough = configuration.OverlayClickThrough;

        var width = Math.Clamp(configuration.OverlayWidth, 200f, 500f);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(width, 0f),
            MaximumSize = new Vector2(width, float.MaxValue),
        };

        if (positionNeedsApply)
        {
            Position = configuration.OverlayPositionSet
                ? new Vector2(
                    configuration.OverlayPositionX,
                    configuration.OverlayPositionY)
                : GetDefaultPosition(width);

            PositionCondition = ImGuiCond.Always;
            positionNeedsApply = false;
        }
        else
        {
            // Position must be null after the initial restore. Keeping a value
            // here causes Dalamud to submit SetNextWindowPos every frame, which
            // prevents the user from dragging the overlay.
            Position = null;
        }
    }

    public override void Draw()
    {
        var configuration = plugin.Configuration;
        ImGui.SetWindowFontScale(Math.Clamp(configuration.OverlayScale, 0.75f, 2f));

        var drewTimer = false;
        foreach (var timer in configuration.Timers)
        {
            if (!timer.IsActive)
            {
                continue;
            }

            if (drewTimer)
            {
                ImGui.Separator();
            }

            DrawTimerRow(timer);
            drewTimer = true;
        }

        if (!drewTimer)
        {
            ImGui.TextDisabled("No active timers.");
        }

        SaveNativeWindowStateIfChanged();
        TrackPosition();
    }

    internal void RequestPositionReset()
    {
        positionNeedsApply = true;
        pendingPosition = null;
        stablePositionFrames = 0;
    }

    private void DrawTimerRow(TimerEntry timer)
    {
        var remainingText = FormatRemaining(timer);
        var rowStart = ImGui.GetCursorPos();
        var rowHeight = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2f;

        if (ImGui.Selectable(
                $"##OverlayTimer_{timer.Id}",
                false,
                ImGuiSelectableFlags.None,
                new Vector2(-1f, rowHeight)))
        {
            plugin.OpenTimer(timer.Id);
        }

        var rowEnd = ImGui.GetCursorPos();
        var padding = ImGui.GetStyle().FramePadding;
        ImGui.SetCursorPos(rowStart + padding);
        ImGui.TextUnformatted(timer.Name);

        var remainingWidth = ImGui.CalcTextSize(remainingText).X;
        var contentRight = ImGui.GetWindowContentRegionMax().X;
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(
            ImGui.GetCursorPosX(),
            contentRight - remainingWidth - padding.X));
        ImGui.TextUnformatted(remainingText);

        ImGui.SetCursorPos(rowEnd);
    }

    private void SaveNativeWindowStateIfChanged()
    {
        var configuration = plugin.Configuration;
        var changed = false;

        if (configuration.OverlayPinned != IsPinned)
        {
            configuration.OverlayPinned = IsPinned;
            changed = true;
        }

        if (configuration.OverlayClickThrough != IsClickthrough)
        {
            configuration.OverlayClickThrough = IsClickthrough;
            changed = true;
        }

        if (changed)
        {
            configuration.Save();
        }
    }

    private void TrackPosition()
    {
        var currentPosition = ImGui.GetWindowPos();
        if (!float.IsFinite(currentPosition.X) || !float.IsFinite(currentPosition.Y))
        {
            return;
        }

        var configuration = plugin.Configuration;
        var savedPosition = new Vector2(
            configuration.OverlayPositionX,
            configuration.OverlayPositionY);

        if (configuration.OverlayPositionSet
            && Vector2.DistanceSquared(currentPosition, savedPosition) < 0.25f)
        {
            pendingPosition = null;
            stablePositionFrames = 0;
            return;
        }

        if (pendingPosition.HasValue
            && Vector2.DistanceSquared(currentPosition, pendingPosition.Value) < 0.25f)
        {
            stablePositionFrames++;
        }
        else
        {
            pendingPosition = currentPosition;
            stablePositionFrames = 0;
        }

        if (stablePositionFrames < StableFramesBeforePositionSave)
        {
            return;
        }

        configuration.OverlayPositionX = currentPosition.X;
        configuration.OverlayPositionY = currentPosition.Y;
        configuration.OverlayPositionSet = true;
        configuration.Save();

        pendingPosition = null;
        stablePositionFrames = 0;
    }

    private static Vector2 GetDefaultPosition(float width)
    {
        var displaySize = ImGui.GetIO().DisplaySize;
        return new Vector2(
            MathF.Max(20f, displaySize.X - width - 40f),
            60f);
    }

    private static string FormatRemaining(TimerEntry timer)
    {
        var remaining =
            DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds)
            - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            return "Complete";
        }

        if (remaining.TotalDays >= 1)
        {
            return
                $"{(int)remaining.TotalDays}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        return
            $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
}
