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
    private const string CommandName = "/timers";
    private static readonly TimeSpan ChangelogLoginDelay = TimeSpan.FromSeconds(3);

    internal static string CurrentVersion { get; } =
        typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "0.1.0.0";

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

    internal Configuration Configuration { get; }

    private readonly WindowSystem windowSystem = new("EorzeaTimers");
    private readonly MainWindow mainWindow;
    private readonly ChangelogWindow changelogWindow;

    private bool changelogPendingAfterLogin;
    private DateTime? changelogEligibleAtUtc;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        mainWindow = new MainWindow(this);
        changelogWindow = new ChangelogWindow(this);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(changelogWindow);

        changelogPendingAfterLogin =
            !string.Equals(Configuration.LastAcknowledgedVersion, CurrentVersion, StringComparison.Ordinal);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Eorzea Timers. Use /timers changes to view the changelog.",
        });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainWindow;
        PluginInterface.UiBuilder.OpenConfigUi += OpenMainWindow;
        Framework.Update += OnFrameworkUpdate;

        Log.Information("Eorzea Timers 0.1.0.0 loaded.");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainWindow;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenMainWindow;
        Framework.Update -= OnFrameworkUpdate;

        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Trim().Equals("changes", StringComparison.OrdinalIgnoreCase))
        {
            OpenChangelogWindow();
            return;
        }

        mainWindow.Toggle();
    }

    private void OpenMainWindow()
    {
        mainWindow.IsOpen = true;
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

        changelogEligibleAtUtc ??= DateTime.UtcNow + ChangelogLoginDelay;
        if (DateTime.UtcNow < changelogEligibleAtUtc.Value)
        {
            return;
        }

        changelogWindow.IsOpen = true;
        changelogPendingAfterLogin = false;
        changelogEligibleAtUtc = null;
    }
}
