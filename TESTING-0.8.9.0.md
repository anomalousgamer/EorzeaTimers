# Eorzea Timers 0.8.9.0 preview test

These source files are a preview of the planned 0.9 overlay features, update availability notifications, and an opt-in, read-only FC vessel probe. Build them on Windows with the normal Dalamud SDK and publish to your testers as `v0.8.9.0`; reserve `v0.9.0.0` for the confirmed release. The `repo.json` URLs assume the GitHub release contains `latest.zip`.

## Publish for your testers

1. Back up your existing project, then copy the provided files into the matching locations in your EorzeaTimers solution. Preserve each folder path.
2. In Visual Studio, select **Release** and **x64** and choose **Build Solution**. Stop and send the build errors if it fails.
3. Check `EorzeaTimers/bin/x64/Release/EorzeaTimers/latest.zip` (your SDK's generated release package). It should contain `EorzeaTimers.dll`, `EorzeaTimers.deps.json`, `EorzeaTimers.json`, and `LICENSE.md`.
4. Create a GitHub release tagged `v0.8.9.0` and attach that `latest.zip`. The release asset must exist before testers update.
5. Commit and push the source and the supplied `repo.json` to your existing plugin repository. Confirm the manifest points to `v0.8.9.0/latest.zip`, then ask testers to update Eorzea Timers in Dalamud.

## Update notification

1. With 0.8.9.0 installed, log in and wait at least eight seconds after the character loads. Run `/etimers checkupdates`. If 0.8.9.0 is the latest published version, chat should say no update is available.
2. When you publish a newer version (such as the confirmed 0.9.0.0), keep 0.8.9.0 installed on a test client and log in. Once Dalamud has refreshed its repository and finished any automatic updates, Eorzea Timers should show one notification and one chat message naming the available version. Automatic checks repeat roughly every 12 minutes while logged in. If Dalamud automatically installs the newer version before the plugin loads, use the installed-version changelog to confirm the update instead.
3. Run `/etimers checkupdates` again. It should report that the version is available without another toast. Log out and back in within the same game session: the same version should not trigger another automatic notification.

The check uses Dalamud's cached repository information; `/etimers checkupdates` does not force an immediate network refresh. Publishing 0.8.9.0 alone cannot trigger an **available-update** notification from an already installed 0.8.9.0 client. The preview also keeps the existing changelog shown shortly after installing it.

## Vessel probe

1. Install the preview through your normal published release. In chat, use `/etimers vesselprobe on`.
2. Log out of the character and back in. Wait at least ten seconds. Do not open Ctrl+U or visit the workshop yet.
3. Use `/etimers vesselprobe now`; save the `[Vessel probe]` lines in the Dalamud log as the **before Ctrl+U** observation.
4. Open Ctrl+U > Estate > Exploratory/Subaquatic Voyages. Wait for the vessel names and statuses to appear. Use `/etimers vesselprobe now` again and save its log lines as **after Estate**.
5. If convenient, visit the FC workshop panel, run `/etimers vesselprobe now` once more, and save those lines as **workshop**.
6. Use `/etimers vesselprobe off` to stop recording.

Please send the three labeled `[Vessel probe]` samples and screenshots showing the vessel statuses at that time. The probe logs vessel names and timestamps only if the workshop structure is actually loaded. It does not read the remote Estate payload or send a headless refresh request; a null workshop pointer does **not** mean Ctrl+U cannot show vessels remotely.

## Overlay preview

In `/etimers overlay`, choose a border color. In a timer's editor, check **Undock from main overlay** and save. Drag its separate overlay around; drag it onto the main overlay or another detached timer to dock it back into the main list. Check that notes, clicks, hiding, lock, and timer persistence still behave as expected.

## Build status

The source was checked for consistent version metadata and valid `repo.json` here. This environment does not have the Dalamud SDK or .NET SDK, so a Windows Release x64 build and in-game test are required before publishing.
