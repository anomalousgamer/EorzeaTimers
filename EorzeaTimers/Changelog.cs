using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 6 - Repeating Timers and Snooze";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added repeating timers with interval, daily, weekly, selected-weekday, and monthly schedules.",
        "Added original-schedule and dismissal-time repeat behavior.",
        "Added preset and custom snooze durations to completion popups.",
        "Added a manual Restart Timer action and next-occurrence previews.",
        "Added twelve selectable FFXIV chat sounds plus the existing standard notification.",
        "Added a Preview Sound button and saved sound choice for every timer.",
        "Added per-timer overlay notes and right-click note toggling.",
        "Migrated the old Compact/Detailed choice into individual timer note settings.",
        "Reworked the editor so Save, Cancel, and Restart remain visible while its settings scroll.",
        "Repeating and snoozed timers persist through restarts, while missed repeats advance quietly.",
    ];
}
