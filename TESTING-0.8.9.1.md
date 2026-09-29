# Eorzea Timers 0.8.9.1 preview test

This preview includes the existing 0.9 overlay features and update notifications, an in-game vessel report, and text wrapping fixes. Version 0.9.0.0 remains reserved for the confirmed release.

## Build and publish

1. Back up your project. Extract the complete source package and open its `EorzeaTimers.sln`, or copy the source files into the matching folders of your existing solution. The ZIP includes the entire source project. `VesselDiagnostics.cs` belongs beside `Plugin.cs`; `VesselDiagnosticsWindow.cs` and `TextLayout.cs` belong in `Windows`.
2. In Visual Studio, select **Release** and **x64**, then choose **Build Solution**. If the build fails, send the errors before publishing.
3. Use the SDK-generated `EorzeaTimers/bin/x64/Release/EorzeaTimers/latest.zip` as the release asset. Do not upload the source ZIP as `latest.zip`.
4. Create the GitHub release `v0.8.9.1` and attach that generated `latest.zip`.
5. Commit and push the source plus the supplied `repo.json`. Its download URLs point to `v0.8.9.1/latest.zip`, so publish the asset before pushing the manifest.
6. Update Eorzea Timers through the normal installed Dalamud repository. Confirm that `/etimers changes` shows **0.8.9.1**.

## Vessel test, entirely in game

1. Prepare **one vessel on a voyage** and, if possible, **one idle vessel showing None**. Both idle vessels can identify the window, but will not give us an active return value to compare.
2. Leave the workshop. Run `/etimers vesselprobe on`. The Vessel Report window opens and capture is enabled across restarts.
3. **Exit FFXIV completely**, then launch it again and log into the same character outside the workshop. This establishes a fresh process baseline. Logging out only to the title screen does not establish that the client cache was cleared.
4. Wait **10 seconds** after the character loads. Do not open Ctrl+U or visit the workshop. Run `/etimers vesselprobe` to open the report, then press **1. Before Ctrl+U**. Do not press New report: that would clear the automatic login observation.
5. Open **Ctrl+U**. Wait two seconds, then press **2. Timers open** in the report window.
6. Click **Estate**. Wait two seconds, then press **3. Estate open**.
7. Click **Exploratory/Subaquatic Voyages**. Wait for the vessel names and statuses to appear, then wait five more seconds. Press **4. Voyages visible**.
8. Close the game's voyage/Timers windows. Wait **five seconds**, then press **5. Windows closed**.
9. Optional: visit the workshop vessel panel and press **6. Workshop** while its data is visible. Do this after the remote test, within the 15-minute capture period.
10. Press **Stop & copy report**. Return to this conversation and press **Ctrl+V**. Also say which vessel was deployed and which showed None.

No Dalamud log file is needed for this test. If the full paste is too long, use **Copy part 1 of N**, paste it, then repeat for the remaining numbered parts. These parts come from the same saved report copy.

Review the report before sharing: it can include vessel names and related game-window text. Reports are held in memory, so copy them before closing FFXIV, unloading the plugin, or starting a new report. The capture setting persists; the report itself does not.

If capture reaches a size/time limit, copy what it recorded first. Use **New report** only to begin a separate test. The Advanced addon selector is optional; leave it alone unless we need to investigate a window the automatic filter missed.

## What this test establishes

The report should show which addon and agent feed the voyage display, what values arrive when it opens or refreshes, whether the data includes raw numbers as well as display text, and which window data remains accessible after closing it. Workshop observations provide a comparison with the already mapped return fields.

A numeric field is only an observation until its meaning is verified. Display text such as 41 minutes is not an exact return timestamp. Global UI arrays, arbitrary pointer targets, and vector contents are not decoded. This test may identify the next structure to inspect; it does not guarantee that the headless request will be identified in one run.

This build does not create voyage timers automatically, send refresh requests, or open/click/close game UI. The final goal remains silent login synchronization with preserved absolute return times.

## Text wrapping and overlay checks

1. Open `/etimers changes`. At the default size and at its minimum width, every changelog bullet and the title should wrap. Scroll through the changes; **Don't show again for this version** and **Got it** should remain accessible.
2. Open `/etimers overlay`. Check that mode descriptions and the longer instructions fit within the settings window.
3. Create a temporary timer with a long name and several sentences of notes. Enable **Show notes in overlay** and save. At narrow overlay widths, the text should wrap and rows should expand without overlapping the next timer. A long name should move its countdown below the title.
4. Check the timer list, linked-timer catalog, and editor descriptions. Long text should remain inside its panel. Use **Test Completion Alert** to check the long title and notes in the popup.
5. Confirm border colors, undocking, drag-to-dock, note toggling, position locking, and click-through still work. Check overlay scale at 0.75x and 2x. Delete the temporary timer afterwards.

## Update notifications

Run `/etimers checkupdates`. With 0.8.9.1 installed and no newer release published, no update notification is expected. To test an available-update notification, use an older installed version while a newer version is published and visible in Dalamud's repository cache. Automatic updates may install it before the old plugin can announce it; the new-version changelog confirms the installed update in that case.

## Validation

The complete source was compiled in Release x64 against Dalamud API 15 using .NET 10: zero warnings and zero errors. FFXIV was not available in the build environment, so the native capture and rendered layouts still require the in-game checks above before calling this release confirmed.
