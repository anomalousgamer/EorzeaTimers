# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.4.2.0

Hotfix 0.4.2.0 corrects the visual spacing caused by the overlay resize grip.

- Every overlay timer row now has matching visual height.
- The resize grip floats inside the existing bottom-right corner instead of adding extra space below the final timer.
- The grip stays subtle until hovered and remains draggable.
- A temporary 0.4.2.0-only 4/20 joke image appears in the main plugin window.

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
- Lock and click-through controls.
- Optional hiding when no timers are active.
- Visibility controls for combat, duties, cutscenes, and hidden game UI.
- Titleless click-or-drag overlay interaction.
- Off, Persistent, and hold-to-show Key Bound overlay modes.
- Per-timer Show in overlay controls.
- A floating bottom-right overlay width resize grip and width presets.
- A subtle pulsing border around completed overlay timers.
- Automatic migration of timers created in earlier releases.
- Safe handling of completed and deleted timers.
- A per-version changelog shown three seconds after the character is fully loaded.
- The `/etimers` command for the timer manager.
- The `/etimers overlay` command for opening overlay settings.
- The `/etimers overlay off` command for disabling the overlay.
- The `/etimers overlay persistent` command for persistent mode.
- The `/etimers overlay key` command for hold-to-show mode.
- The `/etimers clickthrough`, `/etimers clickthrough on`, and `/etimers clickthrough off` commands.
- The `/etimers help` command for commands and overlay guidance.
- The `/etimers changes` command for reopening the changelog.

Later releases will add completion alerts, repeating timers, and game-linked
timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.