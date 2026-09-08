using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility;
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
    private bool rowDragOccurred;
    private bool overlayStylePushed;

    public TimerOverlayWindow(Plugin plugin)
        : base("Eorzea Timers###EorzeaTimersOverlay")
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
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoTitleBar;

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

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.035f, 0.04f, 0.05f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.78f, 0.61f, 0.28f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.60f, 0.47f, 0.23f, 0.72f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
        overlayStylePushed = true;
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
            DrawEmptyRow();
        }

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            rowDragOccurred = false;
        }

        SaveNativeWindowStateIfChanged();
        TrackPosition();
    }

    public override void PostDraw()
    {
        if (!overlayStylePushed)
        {
            return;
        }

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        overlayStylePushed = false;
    }

    internal void RequestPositionReset()
    {
        positionNeedsApply = true;
        pendingPosition = null;
        stablePositionFrames = 0;
    }

    private void DrawTimerRow(TimerEntry timer)
    {
        var configuration = plugin.Configuration;
        var detailed = configuration.OverlayRowStyle == OverlayRowStyle.Detailed;
        var remainingText = TimerAppearance.FormatTimer(timer);
        var rowStart = ImGui.GetCursorPos();
        var globalScale = ImGuiHelpers.GlobalScale;
        var lineHeight = ImGui.GetTextLineHeight();
        var padding = ImGui.GetStyle().FramePadding;
        var rowHeight = detailed
            ? lineHeight * 2f + padding.Y * 3f
            : lineHeight + padding.Y * 2f;
        var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

        ImGui.InvisibleButton(
            $"##OverlayTimer_{timer.Id}",
            new Vector2(rowWidth, rowHeight));

        HandleRowInteraction(timer.Id);

        var rowEnd = ImGui.GetCursorPos();
        var iconColor = TimerAppearance.GetColor(timer.Color);
        var horizontalPadding = 8f * globalScale;
        var iconWidth = 24f * globalScale;

        if (ImGui.IsItemHovered() && !configuration.OverlayClickThrough)
        {
            var rowMinimum = ImGui.GetItemRectMin();
            var rowMaximum = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddRectFilled(
                rowMinimum,
                rowMaximum,
                ImGui.GetColorU32(new Vector4(0.12f, 0.28f, 0.50f, 0.42f)),
                4f * globalScale);
        }

        ImGui.SetCursorPos(rowStart + new Vector2(horizontalPadding, padding.Y));
        using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
        {
            ImGui.TextColored(iconColor, TimerAppearance.GetIconGlyph(timer.Icon));
        }

        var textStartX = rowStart.X + horizontalPadding + iconWidth;
        var nameY = rowStart.Y + padding.Y;
        ImGui.SetCursorPos(new Vector2(textStartX, nameY));
        ImGui.TextColored(iconColor, timer.Name);

        var remainingWidth = ImGui.CalcTextSize(remainingText).X;
        var contentRight = ImGui.GetWindowContentRegionMax().X;
        var remainingX = MathF.Max(
            textStartX + 60f * globalScale,
            contentRight - remainingWidth - horizontalPadding);
        ImGui.SetCursorPos(new Vector2(remainingX, nameY));
        ImGui.TextColored(new Vector4(0.95f, 0.88f, 0.70f, 1f), remainingText);

        if (detailed)
        {
            ImGui.SetCursorPos(
                new Vector2(textStartX, nameY + lineHeight + padding.Y * 0.5f));
            if (string.IsNullOrWhiteSpace(timer.Notes))
            {
                ImGui.TextDisabled("No notes");
            }
            else
            {
                ImGui.TextDisabled(timer.Notes);
            }
        }

        ImGui.SetCursorPos(rowEnd);
    }

    private void DrawEmptyRow()
    {
        var rowStart = ImGui.GetCursorPos();
        var padding = ImGui.GetStyle().FramePadding;
        var rowHeight = ImGui.GetTextLineHeight() + padding.Y * 2f;

        ImGui.InvisibleButton(
            "##EmptyOverlayRow",
            new Vector2(MathF.Max(1f, ImGui.GetContentRegionAvail().X), rowHeight));
        HandleRowInteraction(null);

        var rowEnd = ImGui.GetCursorPos();
        ImGui.SetCursorPos(rowStart + padding);
        ImGui.TextDisabled("No active timers.");
        ImGui.SetCursorPos(rowEnd);
    }

    private void HandleRowInteraction(Guid? timerId)
    {
        var configuration = plugin.Configuration;
        var canInteract = !configuration.OverlayClickThrough;
        var canMove =
            canInteract
            && !configuration.OverlayLocked
            && !configuration.OverlayPinned;

        if (canMove
            && ImGui.IsItemActive()
            && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            rowDragOccurred = true;
            ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);
        }

        if (canInteract
            && timerId.HasValue
            && ImGui.IsItemDeactivated()
            && ImGui.IsItemHovered()
            && !rowDragOccurred)
        {
            plugin.OpenTimer(timerId.Value);
        }
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

}
