using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 2 - Full Manual Timers";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "v0.2.1.0 Hotfix",
        "Fixed not clicking on timers.\n\n",
        "--------------------------------------------------------\n\n",
        "v0.2.0.0",
        "Create and run multiple manual timers at the same time.",
        "Added add, edit, duplicate, delete, enable, and disable controls.",
        "Added optional notes for every timer.",
        "Added stable unique timer IDs and independent countdown state.",
        "Added selection highlighting to the timer list.",
        "Existing Stage 1 timers migrate automatically.",
        "Completed and deleted timers are handled safely.",
    ];
}
