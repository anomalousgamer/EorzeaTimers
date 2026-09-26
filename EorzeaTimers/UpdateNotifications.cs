using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;

namespace EorzeaTimers;

internal sealed class UpdateNotifications
{
    private static readonly TimeSpan LoginDelay = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(12);

    private readonly HashSet<Version> notifiedVersions = new();
    private DateTime? loginEligibleAtUtc;
    private DateTime nextCheckAtUtc;
    private Task<PluginUpdate?>? pendingCheck;
    private bool manualCheckRequested;

    internal void Update()
    {
        if (!Plugin.PlayerState.IsLoaded)
        {
            loginEligibleAtUtc = null;
            nextCheckAtUtc = DateTime.MinValue;
            return;
        }

        if (pendingCheck is { IsCompleted: true })
        {
            CompleteCheck();
        }

        var now = DateTime.UtcNow;
        loginEligibleAtUtc ??= now + LoginDelay;
        if (now < loginEligibleAtUtc.Value || now < nextCheckAtUtc
            || pendingCheck is not null || !Plugin.PluginInterface.IsAutoUpdateComplete)
        {
            return;
        }

        StartCheck(now);
    }

    internal void CheckNow()
    {
        manualCheckRequested = true;
        if (pendingCheck is null)
        {
            StartCheck(DateTime.UtcNow);
        }
    }

    private void StartCheck(DateTime now)
    {
        nextCheckAtUtc = now + CheckInterval;
        try
        {
            pendingCheck = Plugin.PluginInterface.CheckForUpdateAsync();
        }
        catch (Exception exception)
        {
            Plugin.Log.Warning(exception, "Could not check for Eorzea Timers updates.");
            if (manualCheckRequested)
            {
                Plugin.ChatGui.PrintError("Could not check for updates. Try again later.", "Eorzea Timers");
                manualCheckRequested = false;
            }
        }
    }

    private void CompleteCheck()
    {
        var completedCheck = pendingCheck!;
        pendingCheck = null;

        try
        {
            var update = completedCheck.GetAwaiter().GetResult();
            if (update is null)
            {
                if (manualCheckRequested)
                {
                    Plugin.ChatGui.Print("No update is currently available.", "Eorzea Timers");
                }

                return;
            }

            var version = update.Version;
            if (manualCheckRequested && notifiedVersions.Contains(version))
            {
                Plugin.ChatGui.Print($"Version {version} is available. Update through /xlplugins.", "Eorzea Timers");
                return;
            }

            if (!notifiedVersions.Contains(version))
            {
                var message = $"Version {version} is available. Update through /xlplugins.";
                Plugin.NotificationManager.AddNotification(new Notification
                {
                    Title = "Eorzea Timers update available",
                    Content = message,
                    Type = NotificationType.Info,
                });
                notifiedVersions.Add(version);
                Plugin.ChatGui.Print(message, "Eorzea Timers");
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.Warning(exception, "Could not check for Eorzea Timers updates.");
            if (manualCheckRequested)
            {
                Plugin.ChatGui.PrintError("Could not check for updates. Try again later.", "Eorzea Timers");
            }
        }
        finally
        {
            manualCheckRequested = false;
        }
    }
}
