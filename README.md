# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.6.0.0

Stage 6 adds repeating schedules, snooze controls, individual overlay notes, and selectable completion sounds.

- Repeat timers at a custom interval, daily, weekly, on selected weekdays, or monthly.
- Preserve the original schedule or calculate the next occurrence from dismissal time.
- Snooze completion alerts for preset or custom durations.
- Restart a saved timer manually and preview its following occurrence.
- Choose from the existing notification sound and twelve FFXIV chat sound effects.
- Preview a timer's selected sound before saving.
- Show notes separately for each overlay timer and toggle them with right-click.
- Keep Save, Cancel, and Restart visible while the editor settings scroll.
- Advance missed repeating timers quietly after login instead of producing old alerts.

## Current Features

- Multiple persistent manual countdown timers.
- Add, edit, duplicate, delete, enable, and disable controls.
- Timer names, optional notes, stable unique IDs, icons, colors, and display formats.
- Target date/time or relative duration input.
- Automatic and user-selected countdown formats.
- A persistent in-game overlay for active timers.
- Per-timer overlay notes rendered beneath timer names.
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
- Right-click overlay timers to show or hide their notes without opening the editor.
- Per-timer completion popup, sound, and chat-message settings.
- A completion-alert test button that uses the current editor settings.
- Queued completion popups with a Dismiss button.
- Preset and custom snooze durations.
- The existing standard notification plus twelve selectable FFXIV chat sounds.
- Sound previewing directly from the timer editor.
- Custom-interval, daily, weekly, selected-weekday, and monthly repeating timers.
- Original-schedule and dismissal-time recurrence behavior.
- Manual timer restarting and following-occurrence previews.
- Persistent repeat and snooze state.
- Quiet advancement past occurrences missed while logged out.
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

Later releases will add game-linked timer sources, including selected FFXIV schedules.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.