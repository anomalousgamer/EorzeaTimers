using System.Collections.Generic;

namespace EorzeaTimers;

internal static class Changelog
{
    internal const string Title = "Stage 8 - Linked Timer Catalog";

    internal static IReadOnlyList<string> Latest { get; } =
    [
        "Added a categorized catalog containing 37 selectable game-linked timers.",
        "Added general and named daily-reset timers for roulettes, Allied Society quests, hunt bills, Frontline, and Mini Cactpot.",
        "Added Grand Company reset timers for deliveries, squadron training allowances, and Rowena collectables.",
        "Added general and named weekly-reset timers for tomestones and raid loot.",
        "Added game-data timers for Challenge Log, Custom Deliveries, Wondrous Tails, leve allowances, and treasure-map allowances.",
        "Added current squadron mission and training completion timers.",
        "Added next retainer venture and market-listing expiration timers when retainer data is loaded.",
        "Added Ocean Fishing departures and Gold Saucer GATE schedules.",
        "Added separate Jumbo Cactpot drawing timers for Japan, North America, Europe, and Oceania.",
        "Added Fashion Report theme and judging-period timers.",
        "Added Island Sanctuary cycle and season timers plus the Cosmic Exploration daily reset.",
        "Added live island granary, Free Company airship, and Free Company submersible completion timers when their local data is loaded.",
        "Fixed schedules calculate automatically; character-specific sources use loaded game data.",
        "Unavailable sources are clearly labeled and never replaced with guessed values.",
        "Moved linked phase/status text out of the main countdown row and into toggleable generated notes.",
        "Generated linked notes and custom notes can be shown together beneath a compact timer row.",
        "Existing manual and Housing Lottery timers migrate without losing settings.",
        "Fixed nullable-reference warnings in the completion alert window.",
    ];
}
