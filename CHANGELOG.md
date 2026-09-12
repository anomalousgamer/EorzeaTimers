# Changelog

## 0.6.0.0 - Stage 6: Repeating Timers and Snooze

- Added custom-interval, daily, weekly, selected-weekday, and monthly repeating schedules.
- Added a choice between preserving the original schedule and scheduling from dismissal time.
- Added preset snooze choices for 5, 10, 15, 30, and 60 minutes.
- Added custom snooze durations from 1 minute through 7 days.
- Added a Snooze button to completion popups.
- Added a manual Restart Timer action using the timer's saved schedule or duration.
- Added a following-occurrence preview to repeating timer settings.
- Added twelve selectable FFXIV chat sound effects while retaining the existing standard notification.
- Added a Preview Sound button to the timer editor.
- Each timer now saves its own completion sound.
- Added per-timer Show notes in overlay settings.
- Right-clicking an interactive overlay timer now toggles its notes without opening the editor.
- Overlay notes use smaller, dimmer text and do not add empty space when no note exists.
- Existing Detailed-mode users automatically keep notes visible on their existing timers.
- Removed the global Compact/Detailed selector after migrating it to per-timer settings.
- Reworked the timer editor into a scrolling form with a fixed footer.
- Save, Cancel, and Restart now remain visible at the default window size.
- Repeating and snoozed timer state persists through logout, game restart, and plugin updates.
- Missed repeating occurrences advance quietly to the next future occurrence after login.
- Several timers completing together continue to share one immediate sound.

## 0.5.0.0 - Stage 5: Completion Alerts

- Added a custom completion popup with a Dismiss button.
- Added per-timer options for completion popup, sound, and chat messages.
- Added a Test Completion Alert button that uses the current unsaved editor settings.
- Added duplicate-alert protection so each timer alerts only once per completion.
- Editing a completed timer back into the future makes it eligible to alert again.
- Added a queue that displays simultaneous completion popups one at a time.
- Several timers completing together now share one sound instead of playing several sounds simultaneously.
- Existing completed timers do not unexpectedly alert when the plugin loads after updating.
- Fixed the overlay moving while dragging its bottom-right resize grip.
- Removed the temporary v0.4.2.0 4/20 image and its texture-loading code.

## 0.4.2.0 - Hotfix: Equal Overlay Rows

- Fixed the resize grip making the bottom timer appear taller than the others.
- The resize grip now floats inside the existing bottom-right corner without adding layout height.
- The grip remains subtle until hovered and is still easy to drag.
- Added a temporary 0.4.2.0-only 4/20 joke image to the main plugin window.

## 0.4.1.0 - Hotfix: Overlay Controls

- Fixed the Click-through setting being immediately reset.
- Removed the redundant Pin option so Lock now controls both movement and resizing.
- Added Off, Persistent, and hold-to-show Key Bound overlay modes.
- Added a selectable hold key with F10 as the default.
- Added a Show in overlay option to every timer.
- Existing timers automatically remain enabled for the overlay during migration.
- Added a bottom-right width resize grip while keeping automatic overlay height.
- Added Small, Medium, and Large width presets.
- Kept continuous sliders for scale, width, and background opacity.
- Added separate Reset Position and Reset Size & Style controls.
- Added `/etimers overlay off`, `/etimers overlay persistent`, and `/etimers overlay key`.
- Added `/etimers clickthrough`, `/etimers clickthrough on`, and `/etimers clickthrough off`.
- Added `/etimers help` with commands, overlay modes, and interaction guidance.
- Added chat confirmation for overlay mode and click-through commands.
- Added a subtle pulsing border around completed timers in the overlay.

## 0.4.0.0 - Stage 4: Timer Appearance

- Added selectable icons and colors for every timer.
- Added automatic and user-selected countdown display formats.
- Added days-and-clock, total-hours, total-minutes, words, and target-date format options.
- Added compact and detailed overlay row styles.
- Updated the timer manager and overlay with dark panels, gold accents, and blue selection highlighting.
- Removed the overlay title bar.
- A normal click on an overlay timer still opens and selects that timer in the main window.
- Clicking and dragging any timer row now moves the whole overlay without opening the timer.
- Existing timers are preserved and receive default appearance settings automatically.

## 0.3.2.0 - Hotfix: Overlay Movement

- Corrected overlay position restoration so it is applied only once instead of every frame.
- Fixed the overlay being unable to move even when Lock, Pin, and Click-through were disabled.
- Moved overlay positions continue to save automatically.

## 0.3.1.0 - Hotfix: Overlay Interaction

- Corrected the default pinned state so the overlay can be dragged immediately.
- Existing Stage 3 configurations are automatically unpinned once.
- Added clickable overlay timers that open and select the timer in the main window.
- Clarified which overlay settings prevent dragging.

## 0.3.0.0 - Stage 3: In-Game Timer Overlay

- Added a persistent compact overlay for active timers.
- Added movable positioning with automatic position saving.
- Added adjustable overlay scale, width, and background opacity.
- Added lock, pin, and click-through controls.
- Added optional hiding when no timers are active.
- Added visibility controls for combat, duties, cutscenes, and hidden game UI.
- Added `/etimers overlay` for opening overlay settings.
- Added `/etimers toggle` for quickly showing or hiding the overlay.

## 0.2.0.0 - Stage 2: Full Manual Timers

- Added support for multiple simultaneous manual timers.
- Added add, edit, duplicate, delete, enable, and disable controls.
- Added optional notes for every timer.
- Added stable unique timer IDs.
- Added independent countdown state and selection highlighting.
- Added automatic migration of the existing Stage 1 timer.
- Added safe handling for completed and deleted timers.

## 0.1.2.0 - Hotfix

- Changed the command from `/timers` to `/etimers` to avoid a command conflict.
- Changed plugin version reporting to use the version embedded from `EorzeaTimers.csproj`.
- Updated command help and logging to use the current command and version.

## 0.1.1.0 - Hotfix

- Corrected the per-version in-game changelog behavior.

## 0.1.0.0 - Stage 1: Foundation and One Timer

- Created the plugin from a clean project.
- Added the first version of the timer manager and editor GUI.
- Added one persistent manual countdown timer.
- Added target date/time and duration input modes.
- Added edit, save, cancel, and delete controls.
- Added the original `/timers` command.
- Added a per-version changelog window with a "Don't show again for this version" option.
- Changelog display waits until the character is loaded, then waits three additional seconds.