using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 4 - Timer Appearance";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added selectable icons and colors for every timer.",
        "Added automatic and user-selected countdown display formats.",
        "Added compact and detailed overlay row styles.",
        "Updated the timer manager and overlay with dark panels, gold accents, and blue selection highlighting.",
        "Removed the overlay title bar. Click a timer to open it, or click and drag a timer row to move the overlay.",
        "Existing timers are preserved and receive the default appearance automatically.",
    ];
}
