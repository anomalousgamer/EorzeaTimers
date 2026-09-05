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

    private string editName = "New Timer";
    private string editDate = string.Empty;
    private string editTime = string.Empty;
    private int durationDays;
    private int durationHours = 1;
    private int durationMinutes;
    private TimerInputMode inputMode = TimerInputMode.TargetDateTime;
    private string validationMessage = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Eorzea Timers##EorzeaTimersMain")
    {
        this.plugin = plugin;

        Size = new Vector2(900, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 460),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        LoadDraftFromSavedTimer();
    }

    public override void Draw()
    {
        var available = ImGui.GetContentRegionAvail();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var listWidth = MathF.Max(260f * ImGuiHelpers.GlobalScale, available.X * 0.37f);

        DrawTimerList(new Vector2(listWidth, available.Y));

        ImGui.SameLine(0, spacing);

        DrawEditor(new Vector2(MathF.Max(320f, available.X - listWidth - spacing), available.Y));
    }

    private void DrawTimerList(Vector2 size)
    {
        using var child = ImRaii.Child("TimerList", size, true);
        if (!child.Success)
        {
            return;
        }

        ImGui.TextUnformatted("Timers");
        ImGui.Separator();
        ImGui.Spacing();

        var timer = plugin.Configuration.Timer;
        if (timer is null)
        {
            ImGui.TextDisabled("No timer has been created yet.");
        }
        else
        {
            var rowHeight = 58f * ImGuiHelpers.GlobalScale;
            if (ImGui.Selectable($"##Timer_{timer.Id}", true, ImGuiSelectableFlags.None, new Vector2(-1, rowHeight)))
            {
                LoadDraftFromSavedTimer();
            }

            var rowMin = ImGui.GetItemRectMin();
            var drawList = ImGui.GetWindowDrawList();
            var textColor = ImGui.GetColorU32(ImGuiCol.Text);
            var secondaryColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
            var padding = new Vector2(12f, 8f) * ImGuiHelpers.GlobalScale;

            drawList.AddText(rowMin + padding, textColor, timer.Name);
            drawList.AddText(
                rowMin + padding + new Vector2(0, 24f * ImGuiHelpers.GlobalScale),
                secondaryColor,
                FormatRemaining(timer));
        }

        var buttonAreaHeight = ImGui.GetFrameHeightWithSpacing() * 2f + 8f * ImGuiHelpers.GlobalScale;
        var remainingHeight = ImGui.GetContentRegionAvail().Y;
        if (remainingHeight > buttonAreaHeight)
        {
            ImGui.Dummy(new Vector2(1, remainingHeight - buttonAreaHeight));
        }

        var fullWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Disabled(timer is not null))
        {
            if (ImGui.Button("+  Add Timer", new Vector2(fullWidth, 0)))
            {
                BeginNewTimer();
            }
        }

        var halfWidth = (fullWidth - ImGui.GetStyle().ItemSpacing.X) / 2f;
        using (ImRaii.Disabled(true))
        {
            ImGui.Button("Duplicate", new Vector2(halfWidth, 0));
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(timer is null))
        {
            if (ImGui.Button("Delete", new Vector2(halfWidth, 0)))
            {
                plugin.Configuration.Timer = null;
                plugin.Configuration.Save();
                BeginNewTimer();
            }
        }
    }

    private void DrawEditor(Vector2 size)
    {
        using var child = ImRaii.Child("TimerEditor", size, true);
        if (!child.Success)
        {
            return;
        }

        ImGui.TextUnformatted(plugin.Configuration.Timer is null ? "Add Timer" : "Edit Timer");
        ImGui.Separator();
        ImGui.Spacing();

        DrawNameInput();
        ImGui.Spacing();

        if (ImGui.RadioButton("Target date and time", inputMode == TimerInputMode.TargetDateTime))
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
        ImGui.Separator();
        ImGui.TextDisabled("Stage 1 supports one persistent manual timer.");

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
            LoadDraftFromSavedTimer();
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
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0, ImGui.GetContentRegionAvail().X - countWidth));
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

    private void BeginNewTimer()
    {
        var defaultTarget = DateTime.Now.AddHours(1);

        editName = "New Timer";
        editDate = defaultTarget.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        editTime = defaultTarget.ToString("HH:mm", CultureInfo.InvariantCulture);
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        validationMessage = string.Empty;
    }

    private void LoadDraftFromSavedTimer()
    {
        var timer = plugin.Configuration.Timer;
        if (timer is null)
        {
            BeginNewTimer();
            return;
        }

        var targetLocal = DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds).LocalDateTime;

        editName = timer.Name;
        editDate = targetLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        editTime = targetLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
        durationDays = 0;
        durationHours = 1;
        durationMinutes = 0;
        inputMode = TimerInputMode.TargetDateTime;
        validationMessage = string.Empty;
    }

    private void SaveTimer()
    {
        var trimmedName = editName.Trim();
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

        long endUnixSeconds;

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
                validationMessage = "Use YYYY-MM-DD for the date and HH:MM for the time.";
                return;
            }

            targetLocal = DateTime.SpecifyKind(targetLocal, DateTimeKind.Local);
            endUnixSeconds = new DateTimeOffset(targetLocal).ToUnixTimeSeconds();
        }
        else
        {
            var duration = TimeSpan.FromDays(durationDays)
                           + TimeSpan.FromHours(durationHours)
                           + TimeSpan.FromMinutes(durationMinutes);

            if (duration <= TimeSpan.Zero)
            {
                validationMessage = "Duration must be longer than zero minutes.";
                return;
            }

            endUnixSeconds = DateTimeOffset.UtcNow.Add(duration).ToUnixTimeSeconds();
        }

        if (endUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            validationMessage = "The timer must end in the future.";
            return;
        }

        var timer = plugin.Configuration.Timer ?? new TimerEntry();
        timer.Name = trimmedName;
        timer.EndUnixSeconds = endUnixSeconds;
        timer.IsActive = true;

        plugin.Configuration.Timer = timer;
        plugin.Configuration.Save();
        LoadDraftFromSavedTimer();
    }

    private static string FormatRemaining(TimerEntry timer)
    {
        if (!timer.IsActive)
        {
            return "Inactive";
        }

        var remaining = DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds) - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Complete";
        }

        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
}
