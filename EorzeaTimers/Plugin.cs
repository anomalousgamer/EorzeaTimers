using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EorzeaTimers.Models;
using EorzeaTimers.Windows;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EorzeaTimers;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/etimers";
    private const string ChatTag = "Eorzea Timers";
    private const uint CompletionSoundEffectId = 23;
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

    [PluginService]
    internal static IKeyState KeyState { get; private set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;

    internal Configuration Configuration { get; }

    private readonly WindowSystem windowSystem = new("EorzeaTimers");
    private readonly MainWindow mainWindow;
    private readonly ChangelogWindow changelogWindow;
    private readonly TimerOverlayWindow overlayWindow;
    private readonly OverlaySettingsWindow overlaySettingsWindow;
    private readonly CompletionAlertWindow completionAlertWindow;
    private readonly HashSet<Guid> completionAlertedTimerIds = new();

    private bool changelogPendingAfterLogin;
    private DateTime? changelogEligibleAtUtc;

    public Plugin()
    {
        Configuration =
            PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var configurationChanged = Configuration.MigrateToCurrentVersion();

        var loadedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var timer in Configuration.Timers)
        {
            if (timer.EndUnixSeconds <= loadedAtUnixSeconds)
            {
                completionAlertedTimerIds.Add(timer.Id);

                if (timer.IsActive && timer.RepeatMode != TimerRepeatMode.None)
                {
                    ScheduleNextRepeat(timer, loadedAtUnixSeconds);
                    configurationChanged = true;
                }
            }
        }

        if (configurationChanged)
        {
            Configuration.Save();
        }

        mainWindow = new MainWindow(this);
        changelogWindow = new ChangelogWindow(this);
        overlayWindow = new TimerOverlayWindow(this);
        overlaySettingsWindow = new OverlaySettingsWindow(this, overlayWindow);
        completionAlertWindow = new CompletionAlertWindow(this);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(changelogWindow);
        windowSystem.AddWindow(overlayWindow);
        windowSystem.AddWindow(overlaySettingsWindow);
        windowSystem.AddWindow(completionAlertWindow);

        changelogPendingAfterLogin =
            !string.Equals(
                Configuration.LastAcknowledgedVersion,
                CurrentVersion,
                StringComparison.Ordinal);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "Open Eorzea Timers. Use /etimers help for commands and overlay controls.",
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
        var commandParts = arguments
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            mainWindow.Toggle();
            return;
        }

        switch (commandParts[0].ToLowerInvariant())
        {
            case "changes":
                OpenChangelogWindow();
                return;
            case "overlay":
                HandleOverlayCommand(commandParts);
                return;
            case "clickthrough":
                HandleClickThroughCommand(commandParts);
                return;
            case "help":
                PrintHelp();
                return;
            default:
                ChatGui.PrintError(
                    $"Unknown option '{commandParts[0]}'. Use /etimers help.",
                    ChatTag);
                return;
        }
    }

    private void HandleOverlayCommand(string[] commandParts)
    {
        if (commandParts.Length == 1)
        {
            OpenOverlaySettingsWindow();
            return;
        }

        if (commandParts.Length != 2)
        {
            ChatGui.PrintError(
                "Use /etimers overlay, /etimers overlay off, /etimers overlay persistent, or /etimers overlay key.",
                ChatTag);
            return;
        }

        var mode = commandParts[1].ToLowerInvariant() switch
        {
            "off" => OverlayDisplayMode.Off,
            "persistent" => OverlayDisplayMode.Persistent,
            "key" => OverlayDisplayMode.KeyBound,
            _ => (OverlayDisplayMode?)null,
        };

        if (!mode.HasValue)
        {
            ChatGui.PrintError(
                $"Unknown overlay mode '{commandParts[1]}'. Use off, persistent, or key.",
                ChatTag);
            return;
        }

        Configuration.OverlayMode = mode.Value;
        Configuration.OverlayEnabled = mode.Value != OverlayDisplayMode.Off;
        Configuration.Save();

        var modeMessage = mode.Value switch
        {
            OverlayDisplayMode.Off => "Overlay mode set to Off.",
            OverlayDisplayMode.Persistent => "Overlay mode set to Persistent.",
            _ =>
                $"Overlay mode set to Key Bound. Hold {OverlayKeys.GetName(Configuration.OverlayHoldKey)} to show it.",
        };

        ChatGui.Print(modeMessage, ChatTag);
    }

    private void HandleClickThroughCommand(string[] commandParts)
    {
        if (commandParts.Length > 2)
        {
            ChatGui.PrintError(
                "Use /etimers clickthrough, /etimers clickthrough on, or /etimers clickthrough off.",
                ChatTag);
            return;
        }

        bool enabled;
        if (commandParts.Length == 1)
        {
            enabled = !Configuration.OverlayClickThrough;
        }
        else
        {
            switch (commandParts[1].ToLowerInvariant())
            {
                case "on":
                    enabled = true;
                    break;
                case "off":
                    enabled = false;
                    break;
                default:
                    ChatGui.PrintError(
                        $"Unknown click-through option '{commandParts[1]}'. Use on or off.",
                        ChatTag);
                    return;
            }
        }

        Configuration.OverlayClickThrough = enabled;
        Configuration.Save();

        ChatGui.Print(
            enabled
                ? "Overlay click-through enabled. Use /etimers clickthrough off to disable it."
                : "Overlay click-through disabled.",
            ChatTag);
    }

    private void PrintHelp()
    {
        ChatGui.Print("Commands", ChatTag);
        ChatGui.Print("/etimers - Opens or closes the timer manager.", ChatTag);
        ChatGui.Print("/etimers help - Shows this guide.", ChatTag);
        ChatGui.Print("/etimers overlay - Opens overlay settings.", ChatTag);
        ChatGui.Print("/etimers overlay off - Hides the overlay.", ChatTag);
        ChatGui.Print("/etimers overlay persistent - Keeps the overlay visible.", ChatTag);
        ChatGui.Print(
            $"/etimers overlay key - Shows the overlay only while {OverlayKeys.GetName(Configuration.OverlayHoldKey)} is held.",
            ChatTag);
        ChatGui.Print("/etimers clickthrough - Toggles click-through.", ChatTag);
        ChatGui.Print("/etimers clickthrough on - Enables click-through.", ChatTag);
        ChatGui.Print("/etimers clickthrough off - Disables click-through.", ChatTag);
        ChatGui.Print("/etimers changes - Opens the changelog.", ChatTag);
        ChatGui.Print("Overlay controls", ChatTag);
        ChatGui.Print("Click a timer row to open that timer.", ChatTag);
        ChatGui.Print("Drag a timer row to move the overlay.", ChatTag);
        ChatGui.Print("Drag the bottom-right grip to resize its width.", ChatTag);
        ChatGui.Print("Lock prevents moving and resizing but still allows timer clicks.", ChatTag);
        ChatGui.Print("Click-through passes mouse input to the game and disables overlay interaction.", ChatTag);
        ChatGui.Print("Use Show in overlay in the timer editor to include or exclude each timer.", ChatTag);
        ChatGui.Print("Right-click an overlay timer to show or hide its notes.", ChatTag);
        ChatGui.Print("Completion popup, sound choice, chat, repeat, and test options are set per timer.", ChatTag);
        ChatGui.Print("Completion popups can be dismissed or snoozed.", ChatTag);
    }

    private void OpenMainWindow()
    {
        mainWindow.IsOpen = true;
    }

    internal void OpenOverlaySettingsWindow()
    {
        overlaySettingsWindow.IsOpen = true;
    }

    internal void OpenTimer(Guid timerId)
    {
        mainWindow.OpenTimer(timerId);
    }

    internal void ToggleTimerNotes(Guid timerId)
    {
        var timer = Configuration.Timers.Find(entry => entry.Id == timerId);
        if (timer is null)
        {
            return;
        }

        timer.ShowNotesInOverlay = !timer.ShowNotesInOverlay;
        mainWindow.UpdateShowNotesSetting(timerId, timer.ShowNotesInOverlay);
        Configuration.Save();
    }

    internal bool IsTimerAwaitingCompletion(Guid timerId)
    {
        var timer = Configuration.Timers.Find(entry => entry.Id == timerId);
        return timer is not null
            && timer.IsActive
            && timer.EndUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    internal void TestCompletionAlert(
        string name,
        string notes,
        TimerIcon icon,
        TimerColor color,
        bool showPopup,
        bool playSound,
        CompletionSound completionSound,
        bool printToChat)
    {
        if (showPopup)
        {
            completionAlertWindow.Enqueue(name, notes, icon, color, true);
        }

        if (printToChat)
        {
            ChatGui.Print($"Test alert: {name} has finished.", ChatTag);
        }

        if (playSound)
        {
            PlayCompletionSound(completionSound);
        }
    }

    internal void PreviewCompletionSound(CompletionSound sound)
    {
        PlayCompletionSound(sound);
    }

    internal void DismissCompletionAlert(Guid timerId)
    {
        var timer = Configuration.Timers.Find(entry => entry.Id == timerId);
        if (timer is null)
        {
            return;
        }

        var changed = false;
        if (timer.RepeatMode != TimerRepeatMode.None)
        {
            ScheduleNextRepeat(timer, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            completionAlertedTimerIds.Remove(timer.Id);
            changed = true;
        }

        if (timer.IsSnoozed)
        {
            timer.IsSnoozed = false;
            changed = true;
        }

        if (changed)
        {
            Configuration.Save();
        }
    }

    internal void SnoozeTimer(Guid timerId, int minutes)
    {
        var timer = Configuration.Timers.Find(entry => entry.Id == timerId);
        if (timer is null)
        {
            return;
        }

        var safeMinutes = Math.Clamp(minutes, 1, 10080);
        timer.EndUnixSeconds = DateTimeOffset.UtcNow
            .AddMinutes(safeMinutes)
            .ToUnixTimeSeconds();
        timer.IsActive = true;
        timer.IsSnoozed = true;
        completionAlertedTimerIds.Remove(timer.Id);

        Configuration.DefaultSnoozeMinutes = safeMinutes;
        Configuration.Save();
    }

    internal bool RestartTimer(Guid timerId)
    {
        var timer = Configuration.Timers.Find(entry => entry.Id == timerId);
        if (timer is null)
        {
            return false;
        }

        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (timer.RepeatMode == TimerRepeatMode.None)
        {
            timer.EndUnixSeconds = nowUnixSeconds
                + Math.Clamp(timer.RestartDurationSeconds, 60, 315360000);
            timer.RecurrenceAnchorUnixSeconds = timer.EndUnixSeconds;
            timer.IsSnoozed = false;
        }
        else
        {
            ScheduleNextRepeat(timer, nowUnixSeconds);
        }

        timer.IsActive = true;
        completionAlertedTimerIds.Remove(timer.Id);
        Configuration.Save();
        return true;
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
        UpdateCompletionAlerts();

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

    private void UpdateCompletionAlerts()
    {
        if (!PlayerState.IsLoaded)
        {
            return;
        }

        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var existingTimerIds = new HashSet<Guid>();
        var newlyCompleted = new List<TimerEntry>();

        foreach (var timer in Configuration.Timers)
        {
            existingTimerIds.Add(timer.Id);

            if (timer.EndUnixSeconds > nowUnixSeconds)
            {
                completionAlertedTimerIds.Remove(timer.Id);
                continue;
            }

            if (timer.IsActive && completionAlertedTimerIds.Add(timer.Id))
            {
                newlyCompleted.Add(timer);
            }
        }

        completionAlertedTimerIds.RemoveWhere(
            timerId => !existingTimerIds.Contains(timerId));

        if (newlyCompleted.Count == 0)
        {
            return;
        }

        CompletionSound? soundToPlay = null;
        var timersChanged = false;
        foreach (var timer in newlyCompleted)
        {
            if (timer.ShowCompletionPopup)
            {
                completionAlertWindow.Enqueue(timer);
            }

            if (timer.PrintCompletionToChat)
            {
                ChatGui.Print($"Timer complete: {timer.Name} has finished.", ChatTag);
            }

            if (timer.PlaySoundOnCompletion && !soundToPlay.HasValue)
            {
                soundToPlay = timer.CompletionSound;
            }

            if (!timer.ShowCompletionPopup
                && timer.RepeatMode != TimerRepeatMode.None)
            {
                ScheduleNextRepeat(timer, nowUnixSeconds);
                timersChanged = true;
            }
            else if (!timer.ShowCompletionPopup && timer.IsSnoozed)
            {
                timer.IsSnoozed = false;
                timersChanged = true;
            }
        }

        // Several timers completing on the same update share one sound so the
        // user gets a clear alert instead of several effects playing together.
        if (soundToPlay.HasValue)
        {
            PlayCompletionSound(soundToPlay.Value);
        }

        if (timersChanged)
        {
            Configuration.Save();
        }
    }

    private static void ScheduleNextRepeat(TimerEntry timer, long afterUnixSeconds)
    {
        timer.EndUnixSeconds =
            TimerSchedule.GetNextOccurrenceUnixSeconds(timer, afterUnixSeconds);
        timer.IsSnoozed = false;
    }

    private static unsafe void PlayCompletionSound(CompletionSound sound)
    {
        try
        {
            var chatSoundEffectId = CompletionSounds.GetChatSoundEffectId(sound);
            if (chatSoundEffectId.HasValue)
            {
                UIGlobals.PlayChatSoundEffect(chatSoundEffectId.Value);
            }
            else
            {
                UIGlobals.PlaySoundEffect(CompletionSoundEffectId);
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not play the timer completion sound.");
        }
    }
}
