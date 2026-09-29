using System;
using System.Collections.Generic;
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
    private const float DockSnapDistance = 22f;
    private static readonly List<TimerOverlayWindow> Instances = new();

    private readonly Plugin plugin;
    private readonly Guid? detachedTimerId;
    private bool positionNeedsApply = true;
    private Vector2? pendingPosition;
    private int stablePositionFrames;
    private bool rowDragOccurred;
    private bool resizeDragging;
    private bool resizeDirty;
    private bool overlayStylePushed;
    private Vector2 lastPosition;
    private Vector2 lastSize;
    private bool visibleThisFrame;

    public TimerOverlayWindow(Plugin plugin, Guid? detachedTimerId = null)
        : base(detachedTimerId.HasValue
            ? $"Eorzea Timer###EorzeaTimersOverlay_{detachedTimerId.Value}"
            : "Eorzea Timers###EorzeaTimersOverlay")
    {
        this.plugin = plugin;
        this.detachedTimerId = detachedTimerId;
        Instances.Add(this);

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
        visibleThisFrame = false;

        if (detachedTimerId.HasValue)
        {
            var detached = configuration.Timers.Find(timer => timer.Id == detachedTimerId.Value);
            if (detached is null || !detached.OverlayDetached
                || !detached.IsActive || !detached.ShowInOverlay)
            {
                return false;
            }
        }

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
                timer => timer.IsActive && timer.ShowInOverlay
                    && (detachedTimerId.HasValue || !timer.OverlayDetached)))
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

        visibleThisFrame = true;
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

        var detached = detachedTimerId.HasValue
            ? configuration.Timers.Find(timer => timer.Id == detachedTimerId.Value)
            : null;
        if (positionNeedsApply)
        {
            Position = detached is not null && detached.DetachedPositionSet
                ? new Vector2(detached.DetachedPositionX, detached.DetachedPositionY)
                : detached is not null
                    ? GetDefaultPosition(width) - new Vector2(0f, 80f)
                : configuration.OverlayPositionSet
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
        ImGui.PushStyleColor(ImGuiCol.Border,
            TimerAppearance.GetColor(configuration.OverlayBorderColor));
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
            if (!timer.IsActive || !timer.ShowInOverlay
                || (detachedTimerId.HasValue
                    ? timer.Id != detachedTimerId.Value
                    : timer.OverlayDetached))
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

        if (!detachedTimerId.HasValue)
        {
            DrawResizeGrip();
        }

        lastPosition = ImGui.GetWindowPos();
        lastSize = ImGui.GetWindowSize();

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (rowDragOccurred && detachedTimerId.HasValue)
            {
                TryDock();
            }
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

    internal void Unregister() => Instances.Remove(this);

    private void TryDock()
    {
        if (detachedTimerId is not Guid timerId)
        {
            return;
        }

        foreach (var other in Instances)
        {
            if (other == this || !other.visibleThisFrame || other.lastSize.X <= 0f)
            {
                continue;
            }

            var mineMin = lastPosition - new Vector2(DockSnapDistance);
            var mineMax = lastPosition + lastSize + new Vector2(DockSnapDistance);
            var theirMin = other.lastPosition;
            var theirMax = other.lastPosition + other.lastSize;
            if (mineMin.X > theirMax.X || mineMax.X < theirMin.X
                || mineMin.Y > theirMax.Y || mineMax.Y < theirMin.Y)
            {
                continue;
            }

            var timer = plugin.Configuration.Timers.Find(t => t.Id == timerId);
            if (timer is null)
            {
                return;
            }

            timer.OverlayDetached = false;
            timer.DetachedPositionSet = false;
            plugin.Configuration.Save();
            return;
        }
    }

    private void DrawTimerRow(TimerEntry timer)
    {
        var configuration = plugin.Configuration;
        var showGeneratedNote = timer.ShowNotesInOverlay
            && timer.SourceType == TimerSourceType.GameLinked
            && !string.IsNullOrWhiteSpace(timer.LinkedGeneratedNote);
        var showCustomNote = timer.ShowNotesInOverlay && !string.IsNullOrWhiteSpace(timer.Notes);
        var remainingText = TimerAppearance.FormatTimer(timer);
        var rowStart = ImGui.GetCursorPos();
        var globalScale = ImGuiHelpers.GlobalScale;
        var normalScale = Math.Clamp(configuration.OverlayScale, 0.75f, 2f);
        var lineHeight = ImGui.GetTextLineHeight();
        var padding = ImGui.GetStyle().FramePadding;
        var horizontalPadding = 8f * globalScale;
        var iconWidth = 24f * globalScale;
        var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var textStartX = rowStart.X + horizontalPadding + iconWidth;
        var contentRight = rowStart.X + rowWidth - horizontalPadding;
        var textWidth = MathF.Max(1, contentRight - textStartX);
        var linkWidth = timer.SourceType == TimerSourceType.GameLinked ? 20f * globalScale : 0f;
        var remainingWidth = ImGui.CalcTextSize(remainingText).X;
        var inlineCountdown = ImGui.CalcTextSize(timer.Name).X + linkWidth
            + remainingWidth + 12f * globalScale <= textWidth;
        var nameWidth = MathF.Max(1, textWidth - linkWidth
            - (inlineCountdown ? remainingWidth + 12f * globalScale : 0));
        var nameHeight = MathF.Max(lineHeight, ImGui.CalcTextSize(timer.Name, false, nameWidth).Y);
        var countdownHeight = MathF.Max(lineHeight, ImGui.CalcTextSize(remainingText, false, textWidth).Y);
        var headerHeight = inlineCountdown ? nameHeight : nameHeight + padding.Y * 0.5f + countdownHeight;

        ImGui.SetWindowFontScale(normalScale * 0.82f);
        var generatedHeight = showGeneratedNote ? ImGui.CalcTextSize(timer.LinkedGeneratedNote, false, textWidth).Y : 0;
        var customHeight = showCustomNote ? ImGui.CalcTextSize(timer.Notes, false, textWidth).Y : 0;
        ImGui.SetWindowFontScale(normalScale);
        var notesHeight = generatedHeight + customHeight;
        if (showGeneratedNote && showCustomNote) notesHeight += padding.Y * 0.5f;
        var rowHeight = headerHeight + notesHeight + padding.Y * 2f
            + (notesHeight > 0 ? padding.Y * 0.5f : 0);

        ImGui.InvisibleButton($"##OverlayTimer_{timer.Id}", new Vector2(rowWidth, rowHeight));
        HandleRowInteraction(timer.Id);
        var rowEnd = ImGui.GetCursorPos();
        var rowMinimum = ImGui.GetItemRectMin();
        var rowMaximum = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        var iconColor = TimerAppearance.GetColor(timer.Color);
        if (ImGui.IsItemHovered() && !configuration.OverlayClickThrough)
            drawList.AddRectFilled(rowMinimum, rowMaximum,
                ImGui.GetColorU32(new Vector4(0.12f, 0.28f, 0.50f, 0.42f)), 4f * globalScale);
        if (timer.EndUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            var pulse = (MathF.Sin((float)ImGui.GetTime() * 2.5f) + 1f) * 0.5f;
            var pulseColor = new Vector4(1f, 0.63f, 0.22f, 0.24f + pulse * 0.38f);
            drawList.AddRect(rowMinimum, rowMaximum, ImGui.GetColorU32(pulseColor),
                4f * globalScale, ImDrawFlags.None, 2f * globalScale);
        }
        var nameY = rowStart.Y + padding.Y;
        ImGui.SetCursorPos(new Vector2(rowStart.X + horizontalPadding, nameY));
        using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            ImGui.TextColored(iconColor, TimerAppearance.GetIconGlyph(timer.Icon));

        ImGui.SetCursorPos(new Vector2(textStartX, nameY));
        ImGui.PushTextWrapPos(textStartX + nameWidth);
        ImGui.TextColored(iconColor, timer.Name);
        ImGui.PopTextWrapPos();
        if (linkWidth > 0)
        {
            var linkX = inlineCountdown
                ? textStartX + ImGui.CalcTextSize(timer.Name).X + 6f * globalScale
                : contentRight - linkWidth + 4f * globalScale;
            ImGui.SetCursorPos(new Vector2(linkX, nameY));
            using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
                ImGui.TextDisabled("\uf0c1");
        }
        var remainingX = MathF.Max(textStartX, contentRight - remainingWidth);
        var remainingY = inlineCountdown ? nameY : nameY + nameHeight + padding.Y * 0.5f;
        ImGui.SetCursorPos(new Vector2(remainingX, remainingY));
        ImGui.PushTextWrapPos(contentRight);
        ImGui.TextColored(new Vector4(0.95f, 0.88f, 0.70f, 1f), remainingText);
        ImGui.PopTextWrapPos();
        if (notesHeight > 0)
        {
            ImGui.SetWindowFontScale(normalScale * 0.82f);
            var noteY = nameY + headerHeight + padding.Y * 0.5f;
            ImGui.PushTextWrapPos(contentRight);
            if (showGeneratedNote)
            {
                ImGui.SetCursorPos(new Vector2(textStartX, noteY));
                ImGui.TextDisabled(timer.LinkedGeneratedNote);
                noteY += generatedHeight + padding.Y * 0.5f;
            }
            if (showCustomNote)
            {
                ImGui.SetCursorPos(new Vector2(textStartX, noteY));
                ImGui.TextDisabled(timer.Notes);
            }
            ImGui.PopTextWrapPos();
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
        var mouseOverResizeGrip = !detachedTimerId.HasValue && IsMouseOverResizeGrip();
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
            TextLayout.TooltipWrapped(
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
            TextLayout.TooltipWrapped("Drag to resize overlay width");
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
        var detached = detachedTimerId.HasValue
            ? configuration.Timers.Find(timer => timer.Id == detachedTimerId.Value)
            : null;
        var savedPosition = detached is not null
            ? new Vector2(detached.DetachedPositionX, detached.DetachedPositionY)
            : new Vector2(configuration.OverlayPositionX, configuration.OverlayPositionY);

        if ((detached?.DetachedPositionSet ?? configuration.OverlayPositionSet)
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

        if (detached is not null)
        {
            detached.DetachedPositionX = currentPosition.X;
            detached.DetachedPositionY = currentPosition.Y;
            detached.DetachedPositionSet = true;
        }
        else
        {
            configuration.OverlayPositionX = currentPosition.X;
            configuration.OverlayPositionY = currentPosition.Y;
            configuration.OverlayPositionSet = true;
        }
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
