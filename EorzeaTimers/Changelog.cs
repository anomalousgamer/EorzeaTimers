using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "0.8.9 Preview - Overlay and Vessel Probe";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Preview of planned 0.9 overlay features: selectable overlay border colors.",
        "Individual timers can be undocked in their editor and moved independently.",
        "Drag an undocked timer onto another overlay to dock it back into the main list.",
        "Added optional /etimers vesselprobe on|off|now diagnostics for the Ctrl+U Estate research.",
        "The vessel probe observes login and UI state without opening windows or sending game requests.",
        "Automatic per-vessel voyage timer creation awaits identification of the remote Estate data source.",

    ];
}
