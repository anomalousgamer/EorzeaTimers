using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EorzeaTimers.Models;

namespace EorzeaTimers.Windows;

public sealed class MainWindow : Window
{
    private static readonly (DayOfWeek Day, string Label)[] WeekdayOptions =
    [
        (DayOfWeek.Sunday, "Sun"),
        (DayOfWeek.Monday, "Mon"),
        (DayOfWeek.Tuesday, "Tue"),
        (DayOfWeek.Wednesday, "Wed"),
        (DayOfWeek.Thursday, "Thu"),
        (DayOfWeek.Friday, "Fri"),
        (DayOfWeek.Saturday, "Sat"),
    ];

    private enum TimerInputMode
    {
        TargetDateTime,
        Duration,
    }

    private sealed record EditorDraftState(
        string Name,
        string Notes,
        string Date,
        string Time,
        bool IsActive,
        bool ShowInOverlay,
        bool ShowNotesInOverlay,
        bool ShowCompletionPopup,
        bool PlaySoundOnCompletion,
        bool PrintCompletionToChat,
        CompletionSound CompletionSound,
        int AlertVolumePercent,
        TimerRepeatMode RepeatMode,
        RepeatIntervalUnit RepeatIntervalUnit,
        int RepeatInterval,
        int RepeatWeekdayMask,
        int RepeatDayOfMonth,
        TimerRepeatAnchor RepeatAnchor,
        TimerIcon Icon,
        TimerColor Color,
        TimerDisplayFormat DisplayFormat,
        int DurationDays,
        int DurationHours,
        int DurationMinutes,
        TimerInputMode InputMode,
        TimerSourceType SourceType,
        GameTimerSource GameSource);

    private readonly Plugin plugin;

    private Guid? selectedTimerId;
    private Guid? selectionBeforeCreate;
    private bool isCreatingNew;

    private string editName = "New Timer";
    private string editNotes = string.Empty;
    private string editDate = string.Empty;
    private string editTime = string.Empty;
    private bool editIsActive = true;
    private bool editShowInOverlay = true;
    private bool editShowNotesInOverlay;
    private bool editShowCompletionPopup = true;
    private bool editPlaySoundOnCompletion = true;
    private bool editPrintCompletionToChat;
    private CompletionSound editCompletionSound = CompletionSound.StandardNotification;
    private int editAlertVolumePercent = 100;
    private TimerRepeatMode editRepeatMode = TimerRepeatMode.None;
    private RepeatIntervalUnit editRepeatIntervalUnit = RepeatIntervalUnit.Hours;
    private int editRepeatInterval = 1;
    private int editRepeatWeekdayMask;
    private int editRepeatDayOfMonth = 1;
    private TimerRepeatAnchor editRepeatAnchor = TimerRepeatAnchor.OriginalSchedule;
    private TimerIcon editIcon = TimerIcon.Clock;
    private TimerColor editColor = TimerColor.Default;
    private TimerDisplayFormat editDisplayFormat = TimerDisplayFormat.Auto;
    private int durationDays;
    private int durationHours = 1;
    private int durationMinutes;
    private TimerInputMode inputMode = TimerInputMode.TargetDateTime;
    private TimerSourceType editSourceType = TimerSourceType.Manual;
    private GameTimerSource editGameSource = GameTimerSource.None;
    private EditorDraftState? savedDraftState;
    private string validationMessage = string.Empty;
    private bool validationIsError = true;

    public MainWindow(Plugin plugin)
        : base("Eorzea Timers##EorzeaTimersMain")
    {
        this.plugin = plugin;

        Size = new Vector2(1000, 720);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 620),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        if (plugin.Configuration.Timers.Count > 0)
        {
            SelectTimer(plugin.Configuration.Timers[0].Id);
        }
        else
        {
            BeginNewTimer();
        }
    }

    public override void Draw()
    {
        var available = ImGui.GetContentRegionAvail();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var listWidth = MathF.Max(260f * ImGuiHelpers.GlobalScale, available.X * 0.37f);

        DrawTimerList(new Vector2(listWidth, available.Y));

        ImGui.SameLine(0, spacing);

        DrawEditor(new Vector2(MathF.Max(340f, available.X - listWidth - spacing), available.Y));
    }

    internal void OpenTimer(Guid timerId)
    {
        SelectTimer(timerId);
        IsOpen = true;
        BringToFront();
    }

    internal void UpdateShowNotesSetting(Guid timerId, bool showNotes)
    {
        if (!isCreatingNew && selectedTimerId == timerId)
        {
            editShowNotesInOverlay = showNotes;
            if (savedDraftState is not null)
            {
                savedDraftState = savedDraftState with
                {
                    ShowNotesInOverlay = showNotes,
                };
            }
        }
    }

    private void DrawTimerList(Vector2 size)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.045f, 0.05f, 0.06f, 0.96f));
        using var child = ImRaii.Child("TimerList", size, true);
        if (!child.Success)
        {
            ImGui.PopStyleColor();
            return;
        }

        ImGui.TextColored(new Vector4(0.92f, 0.75f, 0.39f, 1f), "Timers");
        ImGui.SameLine();
        ImGui.TextDisabled($"({plugin.Configuration.Timers.Count})");
        ImGui.Separator();
        ImGui.Spacing();

        var footerHeight =
            ImGui.GetFrameHeightWithSpacing() * 3f + 6f * ImGuiHelpers.GlobalScale;

        using (var rows = ImRaii.Child(
                   "TimerRows",
                   new Vector2(-1, -footerHeight),
                   false))
        {
            if (rows.Success)
            {
                if (plugin.Configuration.Timers.Count == 0)
                {
                    ImGui.TextDisabled("No timer has been created yet.");
                }
                else
                {
                    DrawTimerGroup(
                        "Manual Timers",
                        timer => timer.SourceType == TimerSourceType.Manual);
                    DrawTimerGroup(
                        "Game-linked Timers",
                        timer => timer.SourceType == TimerSourceType.GameLinked);
                }
            }
        }

        var fullWidth = ImGui.GetContentRegionAvail().X;
        var halfWidth = (fullWidth - ImGui.GetStyle().ItemSpacing.X) / 2f;
        if (ImGui.Button("+ Manual Timer", new Vector2(halfWidth, 0)))
        {
            BeginNewTimer();
        }

        ImGui.SameLine();
        if (ImGui.Button("+ Housing Timer", new Vector2(halfWidth, 0)))
        {
            BeginNewHousingTimer();
        }

        var hasSelectedTimer = !isCreatingNew && GetSelectedTimer() is not null;

        using (ImRaii.Disabled(!hasSelectedTimer))
        {
            if (ImGui.Button("Duplicate", new Vector2(halfWidth, 0)))
            {
                DuplicateSelectedTimer();
            }
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(!hasSelectedTimer))
        {
            if (ImGui.Button("Delete", new Vector2(halfWidth, 0)))
            {
                DeleteSelectedTimer();
            }
        }

        if (ImGui.Button("Overlay Settings", new Vector2(fullWidth, 0)))
        {
            plugin.OpenOverlaySettingsWindow();
        }

        ImGui.PopStyleColor();
    }

    private void DrawTimerGroup(string label, Func<TimerEntry, bool> predicate)
    {
        var matchingTimers = plugin.Configuration.Timers.FindAll(
            timer => predicate(timer));
        if (matchingTimers.Count == 0)
        {
            return;
        }

        ImGui.TextDisabled(label);
        foreach (var timer in matchingTimers)
        {
            DrawTimerRow(timer);
        }

        ImGui.Spacing();
    }

    private void DrawTimerRow(TimerEntry timer)
    {
        var selected = !isCreatingNew && selectedTimerId == timer.Id;
        var rowHeight = 58f * ImGuiHelpers.GlobalScale;
        var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var rowStart = ImGui.GetCursorPos();

        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.12f, 0.28f, 0.50f, 0.92f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.16f, 0.36f, 0.62f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.18f, 0.40f, 0.68f, 1f));
        }

        if (ImGui.Selectable(
                $"##Timer_{timer.Id}",
                selected,
                ImGuiSelectableFlags.None,
                new Vector2(rowWidth, rowHeight)))
        {
            SelectTimer(timer.Id);
        }

        if (selected)
        {
            ImGui.PopStyleColor(3);
        }

        var rowEnd = ImGui.GetCursorPos();
        var scale = ImGuiHelpers.GlobalScale;
        var color = TimerAppearance.GetColor(timer.Color);

        ImGui.SetCursorPos(rowStart + new Vector2(10f * scale, 8f * scale));
        using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
        {
            ImGui.TextColored(color, TimerAppearance.GetIconGlyph(timer.Icon));
        }

        ImGui.SetCursorPos(rowStart + new Vector2(38f * scale, 6f * scale));
        ImGui.TextColored(color, timer.Name);

        if (timer.SourceType == TimerSourceType.GameLinked)
        {
            var linkX = rowStart.X
                + 38f * scale
                + ImGui.CalcTextSize(timer.Name).X
                + 7f * scale;
            ImGui.SetCursorPos(new Vector2(linkX, rowStart.Y + 6f * scale));
            using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            {
                ImGui.TextDisabled("\uf0c1");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Game-linked timer");
            }
        }

        ImGui.SetCursorPos(rowStart + new Vector2(38f * scale, 30f * scale));
        ImGui.TextDisabled(TimerAppearance.FormatTimer(timer));

        ImGui.SetCursorPos(rowEnd);
    }

    private void DrawEditor(Vector2 size)
    {
        using var child = ImRaii.Child("TimerEditor", size, true);
        if (!child.Success)
        {
            return;
        }

        ImGui.TextColored(
            new Vector4(0.92f, 0.75f, 0.39f, 1f),
            editSourceType == TimerSourceType.GameLinked
                ? isCreatingNew ? "Add Game-linked Timer" : "Edit Game-linked Timer"
                : isCreatingNew ? "Add Timer" : "Edit Timer");
        ImGui.Separator();
        ImGui.Spacing();

        var footerHeight =
            ImGui.GetFrameHeightWithSpacing()
            + ImGui.GetTextLineHeightWithSpacing()
            + 14f * ImGuiHelpers.GlobalScale;

        using (var form = ImRaii.Child(
                   "TimerEditorForm",
                   new Vector2(-1f, -footerHeight),
                   false))
        {
            if (form.Success)
            {
                DrawEditorForm();
            }
        }

        DrawEditorFooter();
    }

    private void DrawEditorForm()
    {

        DrawNameInput();
        ImGui.Spacing();

        DrawSourceInputs();
        ImGui.Spacing();

        if (ImGui.Checkbox("Enabled", ref editIsActive))
        {
            validationMessage = string.Empty;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("Disabled timers remain saved and are shown as disabled.");

        if (ImGui.Checkbox("Show in overlay", ref editShowInOverlay))
        {
            validationMessage = string.Empty;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("The timer keeps counting when hidden from the overlay.");

        if (ImGui.Checkbox("Show notes in overlay", ref editShowNotesInOverlay))
        {
            validationMessage = string.Empty;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("You can also toggle this by right-clicking the overlay timer.");
        ImGui.Spacing();

        if (editSourceType == TimerSourceType.Manual)
        {
            if (ImGui.RadioButton(
                    "Target date and time",
                    inputMode == TimerInputMode.TargetDateTime))
            {
                inputMode = TimerInputMode.TargetDateTime;
                validationMessage = string.Empty;
            }

            DrawTargetDateTimeInputs();
            ImGui.Spacing();

            if (ImGui.RadioButton("Duration", inputMode == TimerInputMode.Duration))
            {
                inputMode = TimerInputMode.Duration;
                validationMessage = string.Empty;
            }

            DrawDurationInputs();
            ImGui.Spacing();

            DrawRepeatInputs();
            ImGui.Spacing();
        }

        DrawAppearanceInputs();
        ImGui.Spacing();

        DrawCompletionAlertInputs();
        ImGui.Spacing();

        DrawNotesInput();

        ImGui.Spacing();
        ImGui.TextDisabled("Active timers can also be shown in the persistent overlay.");
    }

    private void DrawSourceInputs()
    {
        ImGui.TextUnformatted("Timer source");
        ImGui.Separator();

        if (editSourceType == TimerSourceType.Manual)
        {
            ImGui.TextDisabled("Manual - its target is controlled by the settings below.");
            return;
        }

        if (!GameLinkedTimers.TryGetSnapshot(
                editGameSource,
                DateTimeOffset.UtcNow,
                out var snapshot))
        {
            ImGui.TextColored(
                new Vector4(1f, 0.35f, 0.3f, 1f),
                "The linked source is currently unavailable.");
            return;
        }

        var periodEnd = DateTimeOffset
            .FromUnixTimeSeconds(snapshot.PeriodEndUnixSeconds)
            .LocalDateTime;

        ImGui.TextColored(
            new Vector4(0.42f, 0.82f, 0.47f, 1f),
            $"Linked: {snapshot.SourceName}");
        ImGui.TextUnformatted($"Current phase: {snapshot.PhaseName}");
        ImGui.TextUnformatted($"Next transition: {periodEnd:ddd, MMM d, yyyy h:mm tt}");
        ImGui.TextDisabled(
            "Calculated from FFXIV's five-day entry and four-day results schedule.");
        ImGui.TextDisabled(
            "The target updates automatically; appearance, notes, and alerts remain customizable.");

        if (ImGui.Button("Convert to Manual"))
        {
            var targetLocal = periodEnd;
            editSourceType = TimerSourceType.Manual;
            editGameSource = GameTimerSource.None;
            editDate = targetLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            editTime = targetLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
            inputMode = TimerInputMode.TargetDateTime;
            editRepeatMode = TimerRepeatMode.None;
            SetFooterMessage(
                "The timer will become manual when you save these changes.",
                false);
        }
    }

    private void DrawEditorFooter()
    {
        var isDirty = IsDraftDirty();
        ImGui.Separator();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ImGui.TextColored(
                validationIsError
                    ? new Vector4(1f, 0.35f, 0.3f, 1f)
                    : new Vector4(0.42f, 0.82f, 0.47f, 1f),
                validationMessage);
        }
        else
        {
            ImGui.TextDisabled(
                isCreatingNew
                    ? "Create the timer with Save, or discard it with Cancel."
                    : isDirty
                        ? "This timer has unsaved changes."
                        : "No unsaved changes.");
        }

        var buttonWidth = 120f * ImGuiHelpers.GlobalScale;
        var totalButtonWidth = buttonWidth * 2f + ImGui.GetStyle().ItemSpacing.X;
        var startX = ImGui.GetCursorPosX();
        var availableWidth = ImGui.GetContentRegionAvail().X;

        var selectedTimer = !isCreatingNew ? GetSelectedTimer() : null;
        if (selectedTimer?.SourceType == TimerSourceType.Manual)
        {
            if (ImGui.Button("Restart Timer", new Vector2(buttonWidth, 0f))
                && plugin.RestartTimer(selectedTimer.Id))
            {
                SelectTimer(selectedTimer.Id);
                SetFooterMessage(
                    "Timer restarted using its saved schedule.",
                    false);
            }
        }
        else
        {
            ImGui.Dummy(new Vector2(buttonWidth, ImGui.GetFrameHeight()));
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(startX + MathF.Max(buttonWidth, availableWidth - totalButtonWidth));

        using (ImRaii.Disabled(!isDirty))
        {
            if (ImGui.Button("Save", new Vector2(buttonWidth, 0)))
            {
                SaveTimer();
            }
        }

        ImGui.SameLine();

        var secondaryLabel = isCreatingNew
            ? "Cancel"
            : isDirty
                ? "Revert Changes"
                : "Close";
        if (ImGui.Button(secondaryLabel, new Vector2(buttonWidth, 0)))
        {
            CancelDraft();
        }
    }

    private void DrawNameInput()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Name");
        ImGui.SameLine(110f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##TimerName", ref editName, 51);

        var countText = $"{editName.Length}/50";
        var countWidth = ImGui.CalcTextSize(countText).X;
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX() + MathF.Max(0, ImGui.GetContentRegionAvail().X - countWidth));
        ImGui.TextDisabled(countText);
    }

    private void DrawTargetDateTimeInputs()
    {
        using var disabled = ImRaii.Disabled(inputMode != TimerInputMode.TargetDateTime);

        ImGui.Indent(24f * ImGuiHelpers.GlobalScale);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Date");
        ImGui.SameLine(110f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
        ImGui.InputText("##TargetDate", ref editDate, 11);
        ImGui.SameLine();
        ImGui.TextDisabled("YYYY-MM-DD");

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Time");
        ImGui.SameLine(110f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
        ImGui.InputText("##TargetTime", ref editTime, 6);
        ImGui.SameLine();
        ImGui.TextDisabled("24-hour HH:MM");
        ImGui.Unindent(24f * ImGuiHelpers.GlobalScale);
    }

    private void DrawDurationInputs()
    {
        using var disabled = ImRaii.Disabled(inputMode != TimerInputMode.Duration);

        ImGui.Indent(24f * ImGuiHelpers.GlobalScale);

        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("Days", ref durationDays);
        durationDays = Math.Clamp(durationDays, 0, 3650);

        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("Hours", ref durationHours);
        durationHours = Math.Clamp(durationHours, 0, 23);

        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("Minutes", ref durationMinutes);
        durationMinutes = Math.Clamp(durationMinutes, 0, 59);

        ImGui.Unindent(24f * ImGuiHelpers.GlobalScale);
    }

    private void DrawRepeatInputs()
    {
        ImGui.TextUnformatted("Repeating schedule");
        ImGui.Separator();

        var repeats = editRepeatMode != TimerRepeatMode.None;
        if (ImGui.Checkbox("Repeat", ref repeats))
        {
            editRepeatMode = repeats
                ? TimerRepeatMode.Daily
                : TimerRepeatMode.None;

            if (repeats && TryGetEndUnixSeconds(out var targetUnixSeconds, false))
            {
                var targetLocal =
                    DateTimeOffset.FromUnixTimeSeconds(targetUnixSeconds).LocalDateTime;
                editRepeatWeekdayMask = 1 << (int)targetLocal.DayOfWeek;
                editRepeatDayOfMonth = targetLocal.Day;
            }

            validationMessage = string.Empty;
        }

        if (!repeats)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("This timer runs once.");
            return;
        }

        ImGui.Indent(24f * ImGuiHelpers.GlobalScale);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Frequency");
        ImGui.SameLine(140f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.BeginCombo(
                "##RepeatMode",
                TimerSchedule.GetRepeatModeName(editRepeatMode)))
        {
            foreach (var mode in Enum.GetValues<TimerRepeatMode>())
            {
                if (mode == TimerRepeatMode.None)
                {
                    continue;
                }

                var selected = editRepeatMode == mode;
                if (ImGui.Selectable(TimerSchedule.GetRepeatModeName(mode), selected))
                {
                    editRepeatMode = mode;
                    validationMessage = string.Empty;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        switch (editRepeatMode)
        {
            case TimerRepeatMode.Interval:
                DrawIntervalRepeatInputs();
                break;
            case TimerRepeatMode.Daily:
                ImGui.TextDisabled("Repeats every day at the timer's scheduled time.");
                break;
            case TimerRepeatMode.Weekly:
                ImGui.TextDisabled("Repeats every seven days on the same weekday and time.");
                break;
            case TimerRepeatMode.SelectedWeekdays:
                DrawWeekdayRepeatInputs();
                break;
            case TimerRepeatMode.Monthly:
                DrawMonthlyRepeatInputs();
                break;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Next repeat from");
        ImGui.SameLine(140f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.BeginCombo(
                "##RepeatAnchor",
                TimerSchedule.GetAnchorName(editRepeatAnchor)))
        {
            foreach (var anchor in Enum.GetValues<TimerRepeatAnchor>())
            {
                var selected = editRepeatAnchor == anchor;
                if (ImGui.Selectable(TimerSchedule.GetAnchorName(anchor), selected))
                {
                    editRepeatAnchor = anchor;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.TextDisabled(
            editRepeatAnchor == TimerRepeatAnchor.OriginalSchedule
                ? "Missed occurrences are skipped without changing the original schedule."
                : "The next occurrence is calculated from the time the alert is dismissed.");

        if (TryCreateDraftSchedule(out var draftTimer))
        {
            ImGui.TextDisabled(
                $"Following occurrence: {TimerSchedule.FormatNextOccurrence(draftTimer, draftTimer.EndUnixSeconds)}");
        }

        ImGui.Unindent(24f * ImGuiHelpers.GlobalScale);
    }

    private void DrawIntervalRepeatInputs()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Every");
        ImGui.SameLine(140f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##RepeatInterval", ref editRepeatInterval))
        {
            editRepeatInterval = Math.Clamp(editRepeatInterval, 1, 10000);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(130f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo(
                "##RepeatIntervalUnit",
                TimerSchedule.GetIntervalUnitName(editRepeatIntervalUnit)))
        {
            foreach (var unit in Enum.GetValues<RepeatIntervalUnit>())
            {
                var selected = editRepeatIntervalUnit == unit;
                if (ImGui.Selectable(TimerSchedule.GetIntervalUnitName(unit), selected))
                {
                    editRepeatIntervalUnit = unit;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
    }

    private void DrawWeekdayRepeatInputs()
    {
        ImGui.TextUnformatted("Days");
        foreach (var option in WeekdayOptions)
        {
            var dayBit = 1 << (int)option.Day;
            var selected = (editRepeatWeekdayMask & dayBit) != 0;
            if (ImGui.Checkbox($"{option.Label}##RepeatDay_{option.Day}", ref selected))
            {
                if (selected)
                {
                    editRepeatWeekdayMask |= dayBit;
                }
                else
                {
                    editRepeatWeekdayMask &= ~dayBit;
                }

                validationMessage = string.Empty;
            }

            if (option.Day != DayOfWeek.Saturday)
            {
                ImGui.SameLine();
            }
        }
    }

    private void DrawMonthlyRepeatInputs()
    {
        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("Day of month", ref editRepeatDayOfMonth))
        {
            editRepeatDayOfMonth = Math.Clamp(editRepeatDayOfMonth, 1, 31);
        }

        ImGui.TextDisabled("Shorter months use their final day.");
    }

    private void DrawNotesInput()
    {
        ImGui.TextUnformatted("Notes (optional)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline(
            "##TimerNotes",
            ref editNotes,
            201,
            new Vector2(-1, 72f * ImGuiHelpers.GlobalScale));

        var countText = $"{editNotes.Length}/200";
        var countWidth = ImGui.CalcTextSize(countText).X;
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX() + MathF.Max(0, ImGui.GetContentRegionAvail().X - countWidth));
        ImGui.TextDisabled(countText);
    }

    private void DrawAppearanceInputs()
    {
        ImGui.TextUnformatted("Appearance");
        ImGui.Separator();

        ImGui.TextUnformatted("Icon");
        var iconButtonSize = new Vector2(
            34f * ImGuiHelpers.GlobalScale,
            30f * ImGuiHelpers.GlobalScale);

        for (var index = 0; index < TimerAppearance.Icons.Length; index++)
        {
            var icon = TimerAppearance.Icons[index];
            var selected = editIcon == icon;

            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.36f, 0.65f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.20f, 0.44f, 0.76f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.12f, 0.30f, 0.56f, 1f));
            }

            using (Plugin.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            {
                if (ImGui.Button(
                        $"{TimerAppearance.GetIconGlyph(icon)}##TimerIcon_{icon}",
                        iconButtonSize))
                {
                    editIcon = icon;
                }
            }

            if (selected)
            {
                ImGui.PopStyleColor(3);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(TimerAppearance.GetIconName(icon));
            }

            if (index < TimerAppearance.Icons.Length - 1)
            {
                ImGui.SameLine();
            }
        }

        ImGui.TextUnformatted("Color");
        var colorButtonSize = new Vector2(
            30f * ImGuiHelpers.GlobalScale,
            26f * ImGuiHelpers.GlobalScale);

        for (var index = 0; index < TimerAppearance.Colors.Length; index++)
        {
            var colorOption = TimerAppearance.Colors[index];
            var color = TimerAppearance.GetColor(colorOption);
            var selected = editColor == colorOption;

            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(
                ImGuiCol.ButtonHovered,
                new Vector4(
                    MathF.Min(1f, color.X + 0.12f),
                    MathF.Min(1f, color.Y + 0.12f),
                    MathF.Min(1f, color.Z + 0.12f),
                    1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);

            if (selected)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3f * ImGuiHelpers.GlobalScale);
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1f, 1f, 1f, 1f));
            }

            if (ImGui.Button($"##TimerColor_{colorOption}", colorButtonSize))
            {
                editColor = colorOption;
            }

            if (selected)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }

            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(TimerAppearance.GetColorName(colorOption));
            }

            if (index < TimerAppearance.Colors.Length - 1)
            {
                ImGui.SameLine();
            }
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Display format");
        ImGui.SameLine(140f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1);

        if (ImGui.BeginCombo(
                "##TimerDisplayFormat",
                TimerAppearance.GetDisplayFormatName(editDisplayFormat)))
        {
            foreach (var format in TimerAppearance.DisplayFormats)
            {
                var selected = editDisplayFormat == format;
                if (ImGui.Selectable(
                        TimerAppearance.GetDisplayFormatName(format),
                        selected))
                {
                    editDisplayFormat = format;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
    }

    private void DrawCompletionAlertInputs()
    {
        ImGui.TextUnformatted("Completion alerts");
        ImGui.Separator();

        ImGui.Checkbox("Show completion popup", ref editShowCompletionPopup);
        ImGui.Checkbox("Play sound when finished", ref editPlaySoundOnCompletion);

        using (ImRaii.Disabled(!editPlaySoundOnCompletion))
        {
            ImGui.Indent(24f * ImGuiHelpers.GlobalScale);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Completion sound");
            ImGui.SameLine(160f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(190f * ImGuiHelpers.GlobalScale);

            if (ImGui.BeginCombo(
                    "##CompletionSound",
                    CompletionSounds.GetName(editCompletionSound)))
            {
                foreach (var sound in CompletionSounds.Options)
                {
                    var selected = editCompletionSound == sound;
                    if (ImGui.Selectable(CompletionSounds.GetName(sound), selected))
                    {
                        editCompletionSound = sound;
                    }

                    if (selected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Preview Sound"))
            {
                plugin.PreviewCompletionSound(
                    editCompletionSound,
                    editAlertVolumePercent);
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Alert volume");
            ImGui.SameLine(160f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(190f * ImGuiHelpers.GlobalScale);
            ImGui.SliderInt(
                "##AlertVolumePercent",
                ref editAlertVolumePercent,
                0,
                200,
                "%d%%");

            ImGui.SameLine();
            ImGui.TextDisabled(
                editAlertVolumePercent > 100
                    ? "Experimental boost"
                    : "Per timer");

            if (editAlertVolumePercent > 100 && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    "FFXIV may clamp louder values or introduce distortion.");
            }

            ImGui.Unindent(24f * ImGuiHelpers.GlobalScale);
        }

        ImGui.Checkbox("Print chat message when finished", ref editPrintCompletionToChat);

        var testButtonWidth = 180f * ImGuiHelpers.GlobalScale;
        if (ImGui.Button("Test Completion Alert", new Vector2(testButtonWidth, 0)))
        {
            if (!editShowCompletionPopup
                && !editPlaySoundOnCompletion
                && !editPrintCompletionToChat)
            {
                SetFooterMessage(
                    "Enable at least one completion alert option to test it.",
                    true);
            }
            else
            {
                var testName = string.IsNullOrWhiteSpace(editName)
                    ? "New Timer"
                    : editName.Trim();

                plugin.TestCompletionAlert(
                    testName,
                    editNotes.Trim(),
                    editIcon,
                    editColor,
                    editShowCompletionPopup,
                    editPlaySoundOnCompletion,
                    editCompletionSound,
                    editAlertVolumePercent,
                    editPrintCompletionToChat);
                SetFooterMessage("Test completion alert sent.", false);
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled("Uses the current unsaved settings.");
    }

    private void BeginNewTimer()
    {
        if (!isCreatingNew)
        {
            selectionBeforeCreate = selectedTimerId;
        }

        selectedTimerId = null;
        isCreatingNew = true;

        var defaultTarget = DateTime.Now.AddHours(1);

        editName = "New Timer";
        editNotes = string.Empty;
        editDate = defaultTarget.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        editTime = defaultTarget.ToString("HH:mm", CultureInfo.InvariantCulture);
        editIsActive = true;
        editShowInOverlay = true;
        editShowNotesInOverlay = false;
        editShowCompletionPopup = true;
        editPlaySoundOnCompletion = true;
        editPrintCompletionToChat = false;
        editCompletionSound = CompletionSound.StandardNotification;
        editAlertVolumePercent = 100;
        editRepeatMode = TimerRepeatMode.None;
        editRepeatIntervalUnit = RepeatIntervalUnit.Hours;
        editRepeatInterval = 1;
        editRepeatWeekdayMask = 1 << (int)defaultTarget.DayOfWeek;
        editRepeatDayOfMonth = defaultTarget.Day;
        editRepeatAnchor = TimerRepeatAnchor.OriginalSchedule;
        editIcon = TimerIcon.Clock;
        editColor = TimerColor.Default;
        editDisplayFormat = TimerDisplayFormat.Auto;
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        editSourceType = TimerSourceType.Manual;
        editGameSource = GameTimerSource.None;
        savedDraftState = null;
        validationMessage = string.Empty;
        validationIsError = true;
    }

    private void BeginNewHousingTimer()
    {
        var existing = plugin.Configuration.Timers.Find(
            timer => timer.SourceType == TimerSourceType.GameLinked
                && timer.GameSource == GameTimerSource.HousingLottery);
        if (existing is not null)
        {
            SelectTimer(existing.Id);
            SetFooterMessage(
                "The Housing Lottery game timer already exists, so it has been selected.",
                false);
            return;
        }

        BeginNewTimer();
        editSourceType = TimerSourceType.GameLinked;
        editGameSource = GameTimerSource.HousingLottery;
        editName = "Housing Lottery";
        editIcon = TimerIcon.House;
        editColor = TimerColor.Gold;

        if (GameLinkedTimers.TryGetSnapshot(
                editGameSource,
                DateTimeOffset.UtcNow,
                out var snapshot))
        {
            var targetLocal = DateTimeOffset
                .FromUnixTimeSeconds(snapshot.PeriodEndUnixSeconds)
                .LocalDateTime;
            editDate = targetLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            editTime = targetLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
    }

    private void SelectTimer(Guid timerId)
    {
        var timer = plugin.Configuration.Timers.Find(entry => entry.Id == timerId);
        if (timer is null)
        {
            return;
        }

        selectedTimerId = timer.Id;
        selectionBeforeCreate = null;
        isCreatingNew = false;
        LoadDraftFromTimer(timer);
    }

    private void LoadDraftFromTimer(TimerEntry timer)
    {
        var targetLocal =
            DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds).LocalDateTime;

        editName = timer.Name;
        editNotes = timer.Notes;
        editDate = targetLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        editTime = targetLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
        editIsActive = timer.IsActive;
        editShowInOverlay = timer.ShowInOverlay;
        editShowNotesInOverlay = timer.ShowNotesInOverlay;
        editShowCompletionPopup = timer.ShowCompletionPopup;
        editPlaySoundOnCompletion = timer.PlaySoundOnCompletion;
        editPrintCompletionToChat = timer.PrintCompletionToChat;
        editCompletionSound = timer.CompletionSound;
        editAlertVolumePercent = timer.AlertVolumePercent;
        editRepeatMode = timer.RepeatMode;
        editRepeatIntervalUnit = timer.RepeatIntervalUnit;
        editRepeatInterval = timer.RepeatInterval;
        editRepeatWeekdayMask = timer.RepeatWeekdayMask;
        editRepeatDayOfMonth = timer.RepeatDayOfMonth;
        editRepeatAnchor = timer.RepeatAnchor;
        editIcon = timer.Icon;
        editColor = timer.Color;
        editDisplayFormat = timer.DisplayFormat;
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        editSourceType = timer.SourceType;
        editGameSource = timer.GameSource;
        validationMessage = string.Empty;
        validationIsError = true;
        savedDraftState = CaptureDraftState();
    }

    private void CancelDraft()
    {
        if (!isCreatingNew)
        {
            if (!IsDraftDirty())
            {
                IsOpen = false;
                return;
            }

            var selected = GetSelectedTimer();
            if (selected is not null)
            {
                LoadDraftFromTimer(selected);
                SetFooterMessage("Unsaved changes were reverted.", false);
            }

            return;
        }

        var previous = selectionBeforeCreate.HasValue
            ? plugin.Configuration.Timers.Find(
                timer => timer.Id == selectionBeforeCreate.Value)
            : null;

        if (previous is not null)
        {
            SelectTimer(previous.Id);
        }
        else if (plugin.Configuration.Timers.Count > 0)
        {
            SelectTimer(plugin.Configuration.Timers[0].Id);
        }
        else
        {
            selectionBeforeCreate = null;
            IsOpen = false;
        }
    }

    private void SaveTimer()
    {
        validationIsError = true;
        var trimmedName = editName.Trim();
        var trimmedNotes = editNotes.Trim();

        if (trimmedName.Length == 0)
        {
            validationMessage = "Enter a timer name.";
            return;
        }

        if (trimmedName.Length > 50)
        {
            validationMessage = "Timer names may contain at most 50 characters.";
            return;
        }

        if (trimmedNotes.Length > 200)
        {
            validationMessage = "Timer notes may contain at most 200 characters.";
            return;
        }

        long endUnixSeconds;
        if (editSourceType == TimerSourceType.GameLinked)
        {
            if (!GameLinkedTimers.TryGetSnapshot(
                    editGameSource,
                    DateTimeOffset.UtcNow,
                    out var snapshot))
            {
                validationMessage = "The linked timer source is unavailable right now.";
                return;
            }

            endUnixSeconds = snapshot.PeriodEndUnixSeconds;
        }
        else if (!TryGetEndUnixSeconds(out endUnixSeconds))
        {
            return;
        }

        if (editSourceType == TimerSourceType.Manual
            && editRepeatMode == TimerRepeatMode.SelectedWeekdays
            && (editRepeatWeekdayMask & 0x7F) == 0)
        {
            validationMessage = "Select at least one weekday for this repeating timer.";
            return;
        }

        if (isCreatingNew && endUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            validationMessage = "A new timer must end in the future.";
            return;
        }

        var timer = GetSelectedTimer();
        var isNewTimer = isCreatingNew || timer is null;
        timer ??= new TimerEntry();

        timer.Name = trimmedName;
        timer.Notes = trimmedNotes;
        timer.EndUnixSeconds = endUnixSeconds;
        timer.SourceType = editSourceType;
        timer.GameSource = editSourceType == TimerSourceType.GameLinked
            ? editGameSource
            : GameTimerSource.None;
        timer.LinkedTargetUnixSeconds = editSourceType == TimerSourceType.GameLinked
            ? endUnixSeconds
            : 0;
        timer.IsActive = editIsActive;
        timer.ShowInOverlay = editShowInOverlay;
        timer.ShowNotesInOverlay = editShowNotesInOverlay;
        timer.ShowCompletionPopup = editShowCompletionPopup;
        timer.PlaySoundOnCompletion = editPlaySoundOnCompletion;
        timer.PrintCompletionToChat = editPrintCompletionToChat;
        timer.CompletionSound = editCompletionSound;
        timer.AlertVolumePercent = Math.Clamp(editAlertVolumePercent, 0, 200);
        timer.RepeatMode = editSourceType == TimerSourceType.GameLinked
            ? TimerRepeatMode.None
            : editRepeatMode;
        timer.RepeatIntervalUnit = editRepeatIntervalUnit;
        timer.RepeatInterval = Math.Clamp(editRepeatInterval, 1, 10000);
        timer.RepeatWeekdayMask = editRepeatWeekdayMask & 0x7F;
        timer.RepeatDayOfMonth = Math.Clamp(editRepeatDayOfMonth, 1, 31);
        timer.RepeatAnchor = editRepeatAnchor;
        timer.RecurrenceAnchorUnixSeconds = endUnixSeconds;
        timer.RestartDurationSeconds = GetDraftDurationSeconds(endUnixSeconds);
        timer.IsSnoozed = false;
        timer.Icon = editIcon;
        timer.Color = editColor;
        timer.DisplayFormat = editDisplayFormat;

        if (isNewTimer)
        {
            plugin.Configuration.Timers.Add(timer);
        }

        plugin.Configuration.Save();
        SelectTimer(timer.Id);
        SetFooterMessage(
            isNewTimer ? "Timer created." : "Changes saved.",
            false);
    }

    private bool TryGetEndUnixSeconds(
        out long endUnixSeconds,
        bool showValidation = true)
    {
        endUnixSeconds = 0;

        if (inputMode == TimerInputMode.TargetDateTime)
        {
            var combined = $"{editDate.Trim()} {editTime.Trim()}";
            if (!DateTime.TryParseExact(
                    combined,
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var targetLocal))
            {
                if (showValidation)
                {
                    validationMessage =
                        "Use YYYY-MM-DD for the date and HH:MM for the time.";
                }

                return false;
            }

            targetLocal = DateTime.SpecifyKind(targetLocal, DateTimeKind.Local);
            endUnixSeconds = new DateTimeOffset(targetLocal).ToUnixTimeSeconds();
            return true;
        }

        var duration =
            TimeSpan.FromDays(durationDays)
            + TimeSpan.FromHours(durationHours)
            + TimeSpan.FromMinutes(durationMinutes);

        if (duration <= TimeSpan.Zero)
        {
            if (showValidation)
            {
                validationMessage = "Duration must be longer than zero minutes.";
            }

            return false;
        }

        endUnixSeconds = DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds();
        return true;
    }

    private long GetDraftDurationSeconds(long endUnixSeconds)
    {
        if (inputMode == TimerInputMode.Duration)
        {
            var duration =
                TimeSpan.FromDays(durationDays)
                + TimeSpan.FromHours(durationHours)
                + TimeSpan.FromMinutes(durationMinutes);
            return Math.Clamp((long)duration.TotalSeconds, 60, 315360000);
        }

        var remaining =
            endUnixSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Clamp(remaining, 60, 315360000);
    }

    private bool TryCreateDraftSchedule(out TimerEntry draftTimer)
    {
        draftTimer = new TimerEntry();
        if (!TryGetEndUnixSeconds(out var endUnixSeconds, false))
        {
            return false;
        }

        draftTimer.EndUnixSeconds = endUnixSeconds;
        draftTimer.RepeatMode = editRepeatMode;
        draftTimer.RepeatIntervalUnit = editRepeatIntervalUnit;
        draftTimer.RepeatInterval = Math.Clamp(editRepeatInterval, 1, 10000);
        draftTimer.RepeatWeekdayMask = editRepeatWeekdayMask & 0x7F;
        draftTimer.RepeatDayOfMonth = Math.Clamp(editRepeatDayOfMonth, 1, 31);
        draftTimer.RepeatAnchor = editRepeatAnchor;
        draftTimer.RecurrenceAnchorUnixSeconds = endUnixSeconds;
        return true;
    }

    private void DuplicateSelectedTimer()
    {
        var source = GetSelectedTimer();
        if (source is null)
        {
            return;
        }

        var duplicate = new TimerEntry
        {
            Id = Guid.NewGuid(),
            Name = CreateDuplicateName(source.Name),
            Notes = source.Notes,
            EndUnixSeconds = source.EndUnixSeconds,
            SourceType = TimerSourceType.Manual,
            GameSource = GameTimerSource.None,
            LinkedTargetUnixSeconds = 0,
            IsActive = source.IsActive,
            ShowInOverlay = source.ShowInOverlay,
            ShowNotesInOverlay = source.ShowNotesInOverlay,
            ShowCompletionPopup = source.ShowCompletionPopup,
            PlaySoundOnCompletion = source.PlaySoundOnCompletion,
            PrintCompletionToChat = source.PrintCompletionToChat,
            CompletionSound = source.CompletionSound,
            AlertVolumePercent = source.AlertVolumePercent,
            RepeatMode = source.RepeatMode,
            RepeatIntervalUnit = source.RepeatIntervalUnit,
            RepeatInterval = source.RepeatInterval,
            RepeatWeekdayMask = source.RepeatWeekdayMask,
            RepeatDayOfMonth = source.RepeatDayOfMonth,
            RepeatAnchor = source.RepeatAnchor,
            RecurrenceAnchorUnixSeconds = source.RecurrenceAnchorUnixSeconds,
            RestartDurationSeconds = source.RestartDurationSeconds,
            IsSnoozed = source.IsSnoozed,
            Icon = source.Icon,
            Color = source.Color,
            DisplayFormat = source.DisplayFormat,
        };

        var sourceIndex = plugin.Configuration.Timers.IndexOf(source);
        plugin.Configuration.Timers.Insert(sourceIndex + 1, duplicate);
        plugin.Configuration.Save();
        SelectTimer(duplicate.Id);

        if (source.SourceType == TimerSourceType.GameLinked)
        {
            SetFooterMessage(
                "The linked timer was duplicated as a manual timer snapshot.",
                false);
        }
    }

    private void DeleteSelectedTimer()
    {
        var selected = GetSelectedTimer();
        if (selected is null)
        {
            return;
        }

        var selectedIndex = plugin.Configuration.Timers.IndexOf(selected);
        plugin.Configuration.Timers.RemoveAt(selectedIndex);
        plugin.Configuration.Save();

        selectedTimerId = null;
        selectionBeforeCreate = null;

        if (plugin.Configuration.Timers.Count == 0)
        {
            isCreatingNew = false;
            BeginNewTimer();
            return;
        }

        var nextIndex = Math.Min(selectedIndex, plugin.Configuration.Timers.Count - 1);
        SelectTimer(plugin.Configuration.Timers[nextIndex].Id);
    }

    private TimerEntry? GetSelectedTimer()
    {
        if (!selectedTimerId.HasValue)
        {
            return null;
        }

        return plugin.Configuration.Timers.Find(
            timer => timer.Id == selectedTimerId.Value);
    }

    private bool IsDraftDirty()
    {
        return isCreatingNew
            || savedDraftState is null
            || savedDraftState != CaptureDraftState();
    }

    private EditorDraftState CaptureDraftState()
    {
        return new EditorDraftState(
            editName,
            editNotes,
            editDate,
            editTime,
            editIsActive,
            editShowInOverlay,
            editShowNotesInOverlay,
            editShowCompletionPopup,
            editPlaySoundOnCompletion,
            editPrintCompletionToChat,
            editCompletionSound,
            editAlertVolumePercent,
            editRepeatMode,
            editRepeatIntervalUnit,
            editRepeatInterval,
            editRepeatWeekdayMask,
            editRepeatDayOfMonth,
            editRepeatAnchor,
            editIcon,
            editColor,
            editDisplayFormat,
            durationDays,
            durationHours,
            durationMinutes,
            inputMode,
            editSourceType,
            editGameSource);
    }

    private void SetFooterMessage(string message, bool isError)
    {
        validationMessage = message;
        validationIsError = isError;
    }

    private static string CreateDuplicateName(string sourceName)
    {
        const string suffix = " Copy";
        var baseName = sourceName.Trim();

        if (baseName.Length > 50 - suffix.Length)
        {
            baseName = baseName[..(50 - suffix.Length)];
        }

        return baseName + suffix;
    }

}
