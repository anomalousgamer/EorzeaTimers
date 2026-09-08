using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Hotfix 0.4.1.0 - Overlay Controls";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Fixed Click-through being immediately reset and removed the redundant Pin option.",
        "Added Off, Persistent, and hold-to-show Key Bound overlay modes.",
        "Added a selectable hold key with F10 as the default.",
        "Added Show in overlay to each timer.",
        "Added a bottom-right width resize grip, width presets, and reset controls.",
        "Added /etimers overlay mode commands, click-through commands, and /etimers help.",
        "Completed overlay timers now receive a subtle pulsing border.",
    ];
}
