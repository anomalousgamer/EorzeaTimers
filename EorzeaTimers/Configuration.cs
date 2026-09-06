using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using EorzeaTimers.Models;

namespace EorzeaTimers;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 4;

    public List<TimerEntry> Timers { get; set; } = new();

    // Retained so existing Stage 1 configurations can be migrated automatically.
    public TimerEntry? Timer { get; set; }

    public string LastAcknowledgedVersion { get; set; } = string.Empty;

    public bool OverlayEnabled { get; set; } = true;

    public bool OverlayLocked { get; set; }

    public bool OverlayPinned { get; set; }

    public bool OverlayClickThrough { get; set; }

    public bool OverlayHideWhenNoActiveTimers { get; set; } = true;

    public bool OverlayShowInCombat { get; set; } = true;

    public bool OverlayShowInDuty { get; set; } = true;

    public bool OverlayShowInCutscenes { get; set; }

    public bool OverlayShowWhenUiHidden { get; set; }

    public float OverlayScale { get; set; } = 1f;

    public float OverlayWidth { get; set; } = 280f;

    public float OverlayOpacity { get; set; } = 0.9f;

    public bool OverlayPositionSet { get; set; }

    public float OverlayPositionX { get; set; }

    public float OverlayPositionY { get; set; }

    internal bool MigrateToCurrentVersion()
    {
        var changed = false;

        // Early Stage 3 builds defaulted the overlay to pinned, which prevented
        // users from dragging it. Unpin it once when migrating that configuration.
        if (Version == 3 && OverlayPinned)
        {
            OverlayPinned = false;
            changed = true;
        }

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

        var validScale = Math.Clamp(OverlayScale, 0.75f, 2f);
        if (OverlayScale != validScale)
        {
            OverlayScale = validScale;
            changed = true;
        }

        var validWidth = Math.Clamp(OverlayWidth, 200f, 500f);
        if (OverlayWidth != validWidth)
        {
            OverlayWidth = validWidth;
            changed = true;
        }

        var validOpacity = Math.Clamp(OverlayOpacity, 0.2f, 1f);
        if (OverlayOpacity != validOpacity)
        {
            OverlayOpacity = validOpacity;
            changed = true;
        }

        if (OverlayPositionSet
            && (!float.IsFinite(OverlayPositionX) || !float.IsFinite(OverlayPositionY)))
        {
            OverlayPositionSet = false;
            OverlayPositionX = 0f;
            OverlayPositionY = 0f;
            changed = true;
        }

        if (Version != 4)
        {
            Version = 4;
            changed = true;
        }

        return changed;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
