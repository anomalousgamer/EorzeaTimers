# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.7.0.0

Stage 7 adds the first game-linked timer source while also including the editor footer improvements originally planned for 0.6.1.0.

- Add a Housing Lottery timer that automatically follows FFXIV's recurring five-day entry and four-day results schedule.
- See the current housing phase and the exact local time of the next transition.
- Keep the linked target protected while still customizing the timer's name, notes, icon, color, display, alerts, sounds, and overlay visibility.
- Recalculate linked timer targets automatically at startup, periodically, and when the housing phase changes.
- Convert a linked timer to a normal manual timer while preserving its current target.
- Identify linked timers in both the timer manager and overlay.
- Snooze linked alerts without permanently breaking their game-schedule connection.
- Use context-sensitive Save, Cancel, Revert Changes, and Close controls.

The Housing Lottery source is calculated from the known FFXIV housing cycle. It does not use fragile memory offsets and does not read a character's private lottery-entry status.

Alert volume above 100% is experimental. FFXIV may clamp louder values or introduce distortion depending on the selected sound and the user's game audio settings.

## Current Features

- Multiple persistent manual countdown timers.
- A game-linked Housing Lottery countdown with automatic phase changes.
- Separate Manual Timers and Game-linked Timers sections.
- Add, edit, duplicate, delete, enable, and disable controls.
- Timer names, optional notes, stable unique IDs, icons, colors, and display formats.
- Target date/time or relative duration input for manual timers.
- Read-only automatic targets for game-linked timers.
- Conversion from a linked timer to a manual timer.
- Automatic linked-timer schedule calculation with no manual synchronization required.
- Automatic and user-selected countdown formats.
- A persistent in-game overlay for active timers.
- Linked-source indicators in the manager and overlay.
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
- Per-timer alert volume from 0% through 200%, with live previewing of unsaved volume changes.
- Custom-interval, daily, weekly, selected-weekday, and monthly repeating manual timers.
- Original-schedule and dismissal-time recurrence behavior.
- Manual timer restarting and following-occurrence previews.
- Persistent repeat and snooze state.
- Quiet advancement past occurrences missed while logged out.
- Linked-timer snoozing that restores the current game target after dismissal.
- Duplicate-alert prevention and safe handling of simultaneous completions.
- Existing completed timers are ignored when the plugin first loads.
- Automatic migration of timers created in earlier releases.
- Safe handling of completed and deleted timers.
- A fixed editor footer with context-sensitive Save, Cancel, Revert Changes, Close, and manual-timer Restart controls.
- A per-version changelog shown three seconds after the character is fully loaded.
- The `/etimers` command for the timer manager.
- The `/etimers overlay` command for opening overlay settings.
- The `/etimers overlay off` command for disabling the overlay.
- The `/etimers overlay persistent` command for persistent mode.
- The `/etimers overlay key` command for hold-to-show mode.
- The `/etimers clickthrough`, `/etimers clickthrough on`, and `/etimers clickthrough off` commands.
- The `/etimers help` command for commands and overlay guidance.
- The `/etimers changes` command for reopening the changelog.

Later releases can add more FFXIV schedule sources through the game-linked timer system introduced here.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll`
as a Dalamud dev plugin.