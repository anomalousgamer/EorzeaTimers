using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "0.8.9.1 Preview - In-game Vessel Reports and Text Wrapping";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Open /etimers vesselprobe for an in-game vessel report with labeled checkpoints and Stop & copy report. No log-file search is needed for the normal test.",
        "The read-only probe now records relevant game window names, agent IDs, setup/refresh values, UI events, and displayed text. Capture can stay enabled across a game restart for a login baseline.",
        "Long reports can be copied in smaller numbered parts. Capture is bounded and can be stopped from the report window.",
        "The changelog now wraps and scrolls while its dismissal controls remain available.",
        "Long descriptions, completion titles, timer names, and overlay notes now wrap. Timer rows expand, and the countdown moves below a long overlay title when needed.",
        "All previous 0.9 preview features remain included: border colors, detachable overlays, drag-to-dock, and update notifications.",
        "Automatic per-vessel timer creation and an invisible login refresh still require confirmation of the remote data source and request.",
        "0.9.0.0 remains reserved for the confirmed release. Eorzea Timers is open source under the MIT License.",
    ];
}
