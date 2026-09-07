using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Hotfix 0.3.2.0 - Overlay Movement";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Corrected overlay position restoration so it is applied only once.",
        "The overlay can now be dragged normally when Lock, Pin, and Click-through are disabled.",
        "Moved overlay positions continue to save automatically.",
    ];
}
