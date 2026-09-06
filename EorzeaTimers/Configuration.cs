using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using EorzeaTimers.Models;

namespace EorzeaTimers;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public List<TimerEntry> Timers { get; set; } = new();

    // Retained so existing Stage 1 configurations can be migrated automatically.
    public TimerEntry? Timer { get; set; }

    public string LastAcknowledgedVersion { get; set; } = string.Empty;

    internal bool MigrateToCurrentVersion()
    {
        var changed = false;

        if (Timers is null)
        {
            Timers = new List<TimerEntry>();
            changed = true;
        }

        var legacyTimer = Timer;
        if (legacyTimer is not null)
        {
            if (!Timers.Exists(existing => existing.Id == legacyTimer.Id))
            {
                Timers.Add(legacyTimer);
            }

            Timer = null;
            changed = true;
        }

        var usedIds = new HashSet<Guid>();
        foreach (var timer in Timers)
        {
            if (timer.Id == Guid.Empty || !usedIds.Add(timer.Id))
            {
                timer.Id = Guid.NewGuid();
                usedIds.Add(timer.Id);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(timer.Name))
            {
                timer.Name = "New Timer";
                changed = true;
            }

            if (timer.Notes is null)
            {
                timer.Notes = string.Empty;
                changed = true;
            }
        }

        if (Version != 2)
        {
            Version = 2;
            changed = true;
        }

        return changed;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
