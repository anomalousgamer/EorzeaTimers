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
    private bool resizeDragging;
    private bool resizeDirty;
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

        if (!Plugin.PlayerState.IsLoaded
            || configuration.OverlayMode == OverlayDisplayMode.Off)
        {
            return false;
        }

        if (configuration.OverlayMode == OverlayDisplayMode.KeyBound
            && (!Plugin.KeyState.IsVirtualKeyValid(configuration.OverlayHoldKey)
                || !Plugin.KeyState[configuration.OverlayHoldKey]))
        {
            return false;
        }

        if (configuration.OverlayHideWhenNoActiveTimers
            && !configuration.Timers.Exists(
                timer => timer.IsActive && timer.ShowInOverlay))
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
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoTitleBar;

        if (configuration.OverlayClickThrough)
        {
            Flags |= ImGuiWindowFlags.NoMouseInputs;
        }

        BgAlpha = Math.Clamp(configuration.OverlayOpacity, 0.2f, 1f);

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
            // here would submit SetNextWindowPos every frame and block dragging.
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
            if (!timer.IsActive || !timer.ShowInOverlay)
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

        DrawResizeGrip();

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            rowDragOccurred = false;
        }

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
        var showNotes =
            timer.ShowNotesInOverlay && !string.IsNullOrWhiteSpace(timer.Notes);
        var remainingText = TimerAppearance.FormatTimer(timer);
        var rowStart = ImGui.GetCursorPos();
        var globalScale = ImGuiHelpers.GlobalScale;
        var lineHeight = ImGui.GetTextLineHeight();
        var padding = ImGui.GetStyle().FramePadding;
        var rowHeight = showNotes
            ? lineHeight * 1.82f + padding.Y * 3f
            : lineHeight + padding.Y * 2f;
        var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

        ImGui.InvisibleButton(
            $"##OverlayTimer_{timer.Id}",
            new Vector2(rowWidth, rowHeight));

        HandleRowInteraction(timer.Id);

        var rowEnd = ImGui.GetCursorPos();
        var rowMinimum = ImGui.GetItemRectMin();
        var rowMaximum = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        var iconColor = TimerAppearance.GetColor(timer.Color);
        var horizontalPadding = 8f * globalScale;
        var iconWidth = 24f * globalScale;

        if (ImGui.IsItemHovered() && !configuration.OverlayClickThrough)
        {
            drawList.AddRectFilled(
                rowMinimum,
                rowMaximum,
                ImGui.GetColorU32(new Vector4(0.12f, 0.28f, 0.50f, 0.42f)),
                4f * globalScale);
        }

        if (timer.EndUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            var pulse = (MathF.Sin((float)ImGui.GetTime() * 2.5f) + 1f) * 0.5f;
            var pulseColor = new Vector4(1f, 0.63f, 0.22f, 0.24f + pulse * 0.38f);
            drawList.AddRect(
                rowMinimum,
                rowMaximum,
                ImGui.GetColorU32(pulseColor),
                4f * globalScale,
                ImDrawFlags.None,
                2f * globalScale);
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

        if (showNotes)
        {
            ImGui.SetCursorPos(
                new Vector2(textStartX, nameY + lineHeight + padding.Y * 0.5f));
            var normalScale = Math.Clamp(configuration.OverlayScale, 0.75f, 2f);
            ImGui.SetWindowFontScale(normalScale * 0.82f);
            ImGui.TextDisabled(timer.Notes);
            ImGui.SetWindowFontScale(normalScale);
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
        ImGui.TextDisabled("No active overlay timers.");
        ImGui.SetCursorPos(rowEnd);
    }

    private void HandleRowInteraction(Guid? timerId)
    {
        var configuration = plugin.Configuration;
        var mouseOverResizeGrip = IsMouseOverResizeGrip();
        var canInteract =
            !configuration.OverlayClickThrough
            && !resizeDragging
            && !mouseOverResizeGrip;
        var canMove = canInteract && !configuration.OverlayLocked;

        if (canMove
            && ImGui.IsItemActive()
            && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            rowDragOccurred = true;
            ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);
        }

        if (canInteract
            && timerId.HasValue
            && ImGui.IsItemHovered()
            && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            plugin.ToggleTimerNotes(timerId.Value);
        }

        if (canInteract
            && timerId.HasValue
            && ImGui.IsItemDeactivated()
            && ImGui.IsItemHovered()
            && !rowDragOccurred)
        {
            plugin.OpenTimer(timerId.Value);
        }

        if (canInteract && timerId.HasValue && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "Left-click to edit. Right-click to show or hide notes. Drag to move.");
        }
    }

    private void DrawResizeGrip()
    {
        var configuration = plugin.Configuration;
        var globalScale = ImGuiHelpers.GlobalScale;
        var canResize =
            !configuration.OverlayLocked
            && !configuration.OverlayClickThrough;
        var hovered = canResize && IsMouseOverResizeGrip() && ImGui.IsWindowHovered();

        if (hovered || resizeDragging)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
        }

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            resizeDragging = true;
            rowDragOccurred = true;
        }

        if (resizeDragging && canResize && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            var widthChange = ImGui.GetIO().MouseDelta.X / globalScale;
            var newWidth = Math.Clamp(configuration.OverlayWidth + widthChange, 200f, 500f);
            if (MathF.Abs(newWidth - configuration.OverlayWidth) > 0.01f)
            {
                configuration.OverlayWidth = newWidth;
                resizeDirty = true;
            }
        }

        if (resizeDragging && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (resizeDirty)
            {
                configuration.Save();
            }

            resizeDragging = false;
            resizeDirty = false;
        }

        var (_, gripMaximum) = GetResizeGripBounds();
        var gripColor = canResize
            ? hovered || resizeDragging
                ? new Vector4(0.98f, 0.82f, 0.46f, 0.95f)
                : new Vector4(0.92f, 0.75f, 0.39f, 0.35f)
            : new Vector4(0.45f, 0.45f, 0.45f, 0.55f);
        var color = ImGui.GetColorU32(gripColor);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddLine(
            gripMaximum - new Vector2(11f * globalScale, 2f * globalScale),
            gripMaximum - new Vector2(2f * globalScale, 11f * globalScale),
            color,
            1.5f * globalScale);
        drawList.AddLine(
            gripMaximum - new Vector2(7f * globalScale, 2f * globalScale),
            gripMaximum - new Vector2(2f * globalScale, 7f * globalScale),
            color,
            1.5f * globalScale);
        drawList.AddLine(
            gripMaximum - new Vector2(3f * globalScale, 2f * globalScale),
            gripMaximum - new Vector2(2f * globalScale, 3f * globalScale),
            color,
            1.5f * globalScale);

        if (hovered)
        {
            ImGui.SetTooltip("Drag to resize overlay width");
        }
    }

    private static bool IsMouseOverResizeGrip()
    {
        var (minimum, maximum) = GetResizeGripBounds();
        var mousePosition = ImGui.GetIO().MousePos;
        return mousePosition.X >= minimum.X
            && mousePosition.X <= maximum.X
            && mousePosition.Y >= minimum.Y
            && mousePosition.Y <= maximum.Y;
    }

    private static (Vector2 Minimum, Vector2 Maximum) GetResizeGripBounds()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var gripSize = 18f * globalScale;
        var maximum =
            ImGui.GetWindowPos()
            + ImGui.GetWindowSize()
            - new Vector2(2f * globalScale, 2f * globalScale);

        return (maximum - new Vector2(gripSize, gripSize), maximum);
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
