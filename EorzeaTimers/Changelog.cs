using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Hotfix 0.3.1.0 - Overlay Interaction";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Corrected the default pinned state so the overlay can be dragged immediately.",
        "Existing Stage 3 configurations are automatically unpinned once.",
        "Clicking an overlay timer now opens and selects that timer in the main window.",
        "Clarified which overlay settings prevent dragging.",
    ];
}
