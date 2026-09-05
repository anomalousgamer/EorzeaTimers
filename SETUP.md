# Eorzea Timers 0.1.0.0 Setup

1. Replace `YOUR_GITHUB_USERNAME` in `EorzeaTimers/EorzeaTimers.csproj`.
2. Open `EorzeaTimers.sln` in Visual Studio.
3. Allow NuGet restore to finish.
4. Select `Release` and choose Build > Build Solution.
5. Add the generated `EorzeaTimers.dll` to Dalamud's Dev Plugin Locations.
6. Enable the dev plugin and run `/timers` in game.
7. Create a timer, save it, reload the plugin, and confirm it continues.
8. Commit the complete source folder and push it to the GitHub repository named `EorzeaTimers`.

The project was created as a clean project and does not contain SamplePlugin source code.
