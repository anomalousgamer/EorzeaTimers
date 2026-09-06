using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EorzeaTimers.Windows;

namespace EorzeaTimers;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/etimers";
    private static readonly TimeSpan ChangelogLoginDelay = TimeSpan.FromSeconds(3);

    internal static string CurrentVersion { get; } =
        typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "Unknown";

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IPlayerState PlayerState { get; private set; } = null!;

    [PluginService]
    internal static ICondition Condition { get; private set; } = null!;

    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;

    internal Configuration Configuration { get; }

    private readonly WindowSystem windowSystem = new("EorzeaTimers");
    private readonly MainWindow mainWindow;
    private readonly ChangelogWindow changelogWindow;
    private readonly TimerOverlayWindow overlayWindow;
    private readonly OverlaySettingsWindow overlaySettingsWindow;

    private bool changelogPendingAfterLogin;
    private DateTime? changelogEligibleAtUtc;

    public Plugin()
    {
        Configuration =
            PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        if (Configuration.MigrateToCurrentVersion())
        {
            Configuration.Save();
        }

        mainWindow = new MainWindow(this);
        changelogWindow = new ChangelogWindow(this);
        overlayWindow = new TimerOverlayWindow(this);
        overlaySettingsWindow = new OverlaySettingsWindow(this, overlayWindow);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(changelogWindow);
        windowSystem.AddWindow(overlayWindow);
        windowSystem.AddWindow(overlaySettingsWindow);

        changelogPendingAfterLogin =
            !string.Equals(
                Configuration.LastAcknowledgedVersion,
                CurrentVersion,
                StringComparison.Ordinal);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "Open Eorzea Timers. Use /etimers overlay for overlay settings or /etimers changes for the changelog.",
        });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainWindow;
        PluginInterface.UiBuilder.OpenConfigUi += OpenMainWindow;
        Framework.Update += OnFrameworkUpdate;

        ApplyUiHideSettings();

        Log.Information("Eorzea Timers {Version} loaded.", CurrentVersion);
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainWindow;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenMainWindow;
        Framework.Update -= OnFrameworkUpdate;

        PluginInterface.UiBuilder.DisableUserUiHide = false;
        PluginInterface.UiBuilder.DisableCutsceneUiHide = false;

        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "changes":
                OpenChangelogWindow();
                return;
            case "overlay":
                OpenOverlaySettingsWindow();
                return;
            case "toggle":
                Configuration.OverlayEnabled = !Configuration.OverlayEnabled;
                Configuration.Save();
                return;
        }

        mainWindow.Toggle();
    }

    private void OpenMainWindow()
    {
        mainWindow.IsOpen = true;
    }

    internal void OpenOverlaySettingsWindow()
    {
        overlaySettingsWindow.IsOpen = true;
    }

    internal void ApplyUiHideSettings()
    {
        PluginInterface.UiBuilder.DisableUserUiHide =
            Configuration.OverlayShowWhenUiHidden;
        PluginInterface.UiBuilder.DisableCutsceneUiHide =
            Configuration.OverlayShowInCutscenes;
    }

    private void OpenChangelogWindow()
    {
        changelogPendingAfterLogin = false;
        changelogEligibleAtUtc = null;
        changelogWindow.IsOpen = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!changelogPendingAfterLogin)
        {
            return;
        }

        if (!PlayerState.IsLoaded)
        {
            changelogEligibleAtUtc = null;
            return;
        }

        changelogEligibleAtUtc ??=
            DateTime.UtcNow + ChangelogLoginDelay;

        if (DateTime.UtcNow < changelogEligibleAtUtc.Value)
        {
            return;
        }

        changelogWindow.IsOpen = true;
        changelogPendingAfterLogin = false;
        changelogEligibleAtUtc = null;
    }
}
