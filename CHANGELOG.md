# Changelog

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