using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 1 - Foundation and One Timer";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Created Eorzea Timers from a clean project.",
        "Added the first version of the timer manager and editor GUI.",
        "Added one persistent manual countdown timer.",
        "Added target date/time and duration input modes.",
        "Added edit, save, cancel, and delete controls.",
        "Added the per-version changelog window.",
    ];
}
