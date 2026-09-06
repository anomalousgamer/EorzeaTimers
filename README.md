# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.3.1.0

Hotfix 0.3.1.0:

- Corrected the default pinned state so the overlay can be dragged immediately.
- Existing Stage 3 configurations are automatically unpinned once.
- Clicking an overlay timer now opens and selects that timer in the main window.
- Clarified which overlay settings prevent dragging.

## Stage 3 Features

Stage 3 adds:

- A persistent compact in-game overlay for active timers.
- Movable positioning with automatic position saving.
- Adjustable overlay scale, width, and background opacity.
- Lock, pin, and click-through controls.
- Optional hiding when no timers are active.
- Visibility controls for combat, duties, cutscenes, and hidden game UI.
- The `/etimers overlay` command for opening overlay settings.
- The `/etimers toggle` command for quickly showing or hiding the overlay.

Existing features include:

- Multiple persistent manual countdown timers.
- Add, edit, duplicate, delete, enable, and disable controls.
- Timer names, optional notes, and stable unique IDs.
- Target date/time or relative duration input.
- Independent countdown state and selection highlighting.
- Automatic migration of an existing Stage 1 timer.
- Safe handling of completed and deleted timers.
- The `/etimers` command.
- A per-version changelog shown three seconds after the character is fully loaded.
- The `/etimers changes` command for reopening the changelog.

Later releases will add appearance customization, completion alerts,
repeating timers, and game-linked timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.