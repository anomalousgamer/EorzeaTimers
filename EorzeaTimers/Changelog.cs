using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 7 - Game-linked Housing Timer";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added the first game-linked timer source: the FFXIV Housing Lottery cycle.",
        "Housing timers automatically follow the five-day entry and four-day results schedule.",
        "Added separate Manual Timers and Game-linked Timers sections.",
        "Added a one-click Housing Timer creator with house icon and gold defaults.",
        "Linked targets are read-only while names, notes, alerts, sounds, and appearance stay customizable.",
        "Added Convert to Manual while keeping linked targets automatic.",
        "Added linked-source indicators to the timer manager and overlay.",
        "Game-linked timers preserve their source while snoozed and return to the calculated schedule after dismissal.",
        "Initial and automatic schedule calculation does not produce false completion alerts.",
        "Save is now disabled when an existing timer has no unsaved changes.",
        "Cancel now becomes Revert Changes or Close when appropriate.",
        "Reverting and saving now show a clear editor status message.",
        "Added a separate 0% through 200% completion-alert volume slider to every timer.",
        "Sound previews and test alerts use the current unsaved volume value.",
        "Existing timers migrate to 100% volume without losing their other settings.",
        "Values above 100% are experimental and may be clamped or distorted by FFXIV.",
    ];
}
