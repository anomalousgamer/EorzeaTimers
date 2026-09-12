using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 5 - Completion Alerts";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added a custom completion popup with a Dismiss button.",
        "Added per-timer popup, sound, and chat-message options.",
        "Added a Test Completion Alert button that uses the current editor settings.",
        "Added duplicate-alert protection so each timer alerts only once per completion.",
        "Added a queue for simultaneous timer completions and limited each completion batch to one sound.",
        "Existing completed timers do not unexpectedly alert when updating the plugin.",
        "Fixed the overlay moving while dragging its resize grip.",
        "Removed the temporary v0.4.2.0 4/20 image.",
    ];
}
