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
    private enum TimerInputMode
    {
        TargetDateTime,
        Duration,
    }

    private readonly Plugin plugin;

    private Guid? selectedTimerId;
    private Guid? selectionBeforeCreate;
    private bool isCreatingNew;

    private string editName = "New Timer";
    private string editNotes = string.Empty;
    private string editDate = string.Empty;
    private string editTime = string.Empty;
    private bool editIsActive = true;
    private int durationDays;
    private int durationHours = 1;
    private int durationMinutes;
    private TimerInputMode inputMode = TimerInputMode.TargetDateTime;
    private string validationMessage = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Eorzea Timers##EorzeaTimersMain")
    {
        this.plugin = plugin;

        Size = new Vector2(900, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 500),
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

    private void DrawTimerList(Vector2 size)
    {
        using var child = ImRaii.Child("TimerList", size, true);
        if (!child.Success)
        {
            return;
        }

        ImGui.TextUnformatted("Timers");
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
                    foreach (var timer in plugin.Configuration.Timers)
                    {
                        DrawTimerRow(timer);
                    }
                }
            }
        }

        var fullWidth = ImGui.GetContentRegionAvail().X;
        if (ImGui.Button("+  Add Timer", new Vector2(fullWidth, 0)))
        {
            BeginNewTimer();
        }

        var halfWidth = (fullWidth - ImGui.GetStyle().ItemSpacing.X) / 2f;
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
    }

    private void DrawTimerRow(TimerEntry timer)
    {
        var selected = !isCreatingNew && selectedTimerId == timer.Id;
        var rowHeight = 58f * ImGuiHelpers.GlobalScale;
        var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var label = $"{timer.Name}\n{FormatRemaining(timer)}###Timer_{timer.Id}";

        if (ImGui.Selectable(
                label,
                selected,
                ImGuiSelectableFlags.None,
                new Vector2(rowWidth, rowHeight)))
        {
            SelectTimer(timer.Id);
        }
    }

    private void DrawEditor(Vector2 size)
    {
        using var child = ImRaii.Child("TimerEditor", size, true);
        if (!child.Success)
        {
            return;
        }

        ImGui.TextUnformatted(isCreatingNew ? "Add Timer" : "Edit Timer");
        ImGui.Separator();
        ImGui.Spacing();

        DrawNameInput();
        ImGui.Spacing();

        if (ImGui.Checkbox("Enabled", ref editIsActive))
        {
            validationMessage = string.Empty;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("Disabled timers remain saved and are shown as disabled.");
        ImGui.Spacing();

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

        DrawNotesInput();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("Active timers can also be shown in the persistent overlay.");

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.3f, 1f), validationMessage);
        }

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + 4f * ImGuiHelpers.GlobalScale;
        var remainingHeight = ImGui.GetContentRegionAvail().Y;
        if (remainingHeight > footerHeight)
        {
            ImGui.Dummy(new Vector2(1, remainingHeight - footerHeight));
        }

        var buttonWidth = 120f * ImGuiHelpers.GlobalScale;
        var totalButtonWidth = buttonWidth * 2f + ImGui.GetStyle().ItemSpacing.X;
        var cursorX = ImGui.GetCursorPosX();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(cursorX + MathF.Max(0, availableWidth - totalButtonWidth));

        if (ImGui.Button("Save", new Vector2(buttonWidth, 0)))
        {
            SaveTimer();
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
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
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        validationMessage = string.Empty;
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
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        validationMessage = string.Empty;
    }

    private void CancelDraft()
    {
        if (!isCreatingNew)
        {
            var selected = GetSelectedTimer();
            if (selected is not null)
            {
                LoadDraftFromTimer(selected);
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
            BeginNewTimer();
        }
    }

    private void SaveTimer()
    {
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

        if (!TryGetEndUnixSeconds(out var endUnixSeconds))
        {
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
        timer.IsActive = editIsActive;

        if (isNewTimer)
        {
            plugin.Configuration.Timers.Add(timer);
        }

        plugin.Configuration.Save();
        SelectTimer(timer.Id);
    }

    private bool TryGetEndUnixSeconds(out long endUnixSeconds)
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
                validationMessage =
                    "Use YYYY-MM-DD for the date and HH:MM for the time.";
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
            validationMessage = "Duration must be longer than zero minutes.";
            return false;
        }

        endUnixSeconds = DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds();
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
            IsActive = source.IsActive,
        };

        var sourceIndex = plugin.Configuration.Timers.IndexOf(source);
        plugin.Configuration.Timers.Insert(sourceIndex + 1, duplicate);
        plugin.Configuration.Save();
        SelectTimer(duplicate.Id);
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

    private static string FormatRemaining(TimerEntry timer)
    {
        if (!timer.IsActive)
        {
            return "Disabled";
        }

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
