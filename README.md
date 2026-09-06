# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.2.0.0

Stage 2 provides:

* Multiple persistent manual countdown timers.
* Add, edit, duplicate, delete, enable, and disable controls.
* Timer names, optional notes, and stable unique IDs.
* Target date/time or relative duration input.
* Independent countdown state and selection highlighting.
* Automatic migration of an existing Stage 1 timer.
* Safe handling of completed and deleted timers.
* The `/etimers` command.
* A per-version changelog shown three seconds after the character is fully loaded.
* The `/etimers changes` command for reopening the changelog.

Later releases will add the compact overlay, appearance customization, completion alerts, repeating timers, and game-linked timer sources.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll` as a Dalamud dev plugin.
