# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.4.0.0

Stage 4 adds timer appearance customization and a cleaner interactive overlay.

- Choose an icon and color for every timer.
- Choose automatic, days-and-clock, total-hours, total-minutes, words, or target-date display formats.
- Switch the overlay between compact and detailed rows.
- Use a consistent dark panel style with gold accents and blue selection highlighting.
- Use a titleless overlay: click a timer to open it, or click and drag a timer row to move the whole overlay.
- Preserve existing timers automatically with default Stage 4 appearance values.

## Current Features

- Multiple persistent manual countdown timers.
- Add, edit, duplicate, delete, enable, and disable controls.
- Timer names, optional notes, stable unique IDs, icons, colors, and display formats.
- Target date/time or relative duration input.
- Automatic and user-selected countdown formats.
- A persistent in-game overlay for active timers.
- Compact and detailed overlay row styles.
- Movable positioning with automatic position saving.
- Adjustable overlay scale, width, and background opacity.
- Lock, pin, and click-through controls.
- Optional hiding when no timers are active.
- Visibility controls for combat, duties, cutscenes, and hidden game UI.
- Titleless click-or-drag overlay interaction.
- Automatic migration of timers created in earlier releases.
- Safe handling of completed and deleted timers.
- A per-version changelog shown three seconds after the character is fully loaded.
- The `/etimers` command for the timer manager.
- The `/etimers overlay` command for opening overlay settings.
- The `/etimers toggle` command for quickly showing or hiding the overlay.
- The `/etimers changes` command for reopening the changelog.

Later releases will add completion alerts, repeating timers, and game-linked
timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.