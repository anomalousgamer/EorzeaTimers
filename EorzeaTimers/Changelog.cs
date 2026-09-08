using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Hotfix 0.4.2.0 - Equal Overlay Rows";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Fixed the resize grip making the bottom timer appear taller than the others.",
        "The resize grip now floats inside the existing bottom-right corner without adding layout height.",
        "The grip remains subtle until hovered and is still easy to drag.",
        "Added a temporary 0.4.2.0-only 4/20 joke image to the main plugin window.",
    ];
}
