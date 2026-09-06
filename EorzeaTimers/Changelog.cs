using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 3 - In-Game Timer Overlay";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added a persistent compact overlay for active timers.",
        "Added movable positioning with automatic position saving.",
        "Added adjustable scale, width, and background opacity.",
        "Added lock, pin, and click-through controls.",
        "Added optional hiding when no timers are active.",
        "Added visibility controls for combat, duties, cutscenes, and hidden game UI.",
        "Added /etimers overlay and /etimers toggle commands.",
    ];
}
