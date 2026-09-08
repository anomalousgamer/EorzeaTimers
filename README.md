# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.4.1.0

Hotfix 0.4.1.0 improves overlay modes, controls, resizing, and timer visibility.

- Choose Off, Persistent, or Key Bound overlay modes.
- In Key Bound mode, hold the selected key to show the overlay and release it to hide it.
- Choose whether each timer appears in the overlay.
- Use working click-through behavior with commands for enabling and disabling it.
- Resize the overlay from its bottom-right grip or use the width slider and presets.
- Lock both the overlay position and size with one clear setting.
- See completed timers with a subtle pulsing border.
- Use `/etimers help` for the complete in-game guide.

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
- A bottom-right overlay width resize grip and width presets.
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