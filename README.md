# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.5.0.0

Stage 5 makes timers actively notify the player when they finish.

- Show a custom completion popup with a Dismiss button.
- Choose popup, sound, and chat-message alerts independently for every timer.
- Test completion alerts directly from the timer editor before saving.
- Queue several completed timers and display them one at a time.
- Prevent a completed timer from alerting more than once.
- Avoid unexpected alerts from timers that were already complete when the plugin loaded.
- Keep the overlay stationary while dragging only the resize grip.
- Remove the temporary v0.4.2.0 4/20 image.

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
- Per-timer completion popup, sound, and chat-message settings.
- A completion-alert test button that uses the current editor settings.
- Queued completion popups with a Dismiss button.
- Duplicate-alert prevention and safe handling of simultaneous completions.
- Existing completed timers are ignored when the plugin first loads.
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

Later releases will add snooze controls, repeating timers, and game-linked timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.