# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.1.0.0

Stage 1 provides:

- A timer manager and editor inspired by the planned FFXIV-style interface.
- One persistent manual countdown timer.
- Target date/time or relative duration input.
- Edit, save, cancel, and delete controls.
- The `/timers` command.
- A per-version changelog shown three seconds after the character is fully loaded.
- The `/timers changes` command for reopening the changelog.

Later releases will add multiple timers, the compact overlay, customization,
completion alerts, repeating timers, and game-linked timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.
