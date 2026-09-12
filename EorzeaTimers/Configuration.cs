using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using EorzeaTimers.Models;

namespace EorzeaTimers;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 8;

    public List<TimerEntry> Timers { get; set; } = new();

    // Retained so existing Stage 1 configurations can be migrated automatically.
    public TimerEntry? Timer { get; set; }

    public string LastAcknowledgedVersion { get; set; } = string.Empty;

    // Retained so configurations from versions before 0.4.1.0 can migrate.
    public bool OverlayEnabled { get; set; } = true;

    public OverlayDisplayMode OverlayMode { get; set; } = OverlayDisplayMode.Persistent;

    public VirtualKey OverlayHoldKey { get; set; } = VirtualKey.F10;

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

    public OverlayRowStyle OverlayRowStyle { get; set; } = OverlayRowStyle.Compact;

    public bool OverlayPositionSet { get; set; }

    public float OverlayPositionX { get; set; }

    public float OverlayPositionY { get; set; }

    public int DefaultSnoozeMinutes { get; set; } = 5;

    internal bool MigrateToCurrentVersion()
    {
        var changed = false;
        var migratingToVersionSix = Version < 6;
        var migratingToVersionSeven = Version < 7;
        var migratingToVersionEight = Version < 8;

        if (migratingToVersionSix)
        {
            OverlayMode = OverlayEnabled
                ? OverlayDisplayMode.Persistent
                : OverlayDisplayMode.Off;
            changed = true;
        }

        // Early Stage 3 builds defaulted the overlay to pinned, which prevented
        // users from dragging it. Unpin it once when migrating that configuration.
        if (Version == 3 && OverlayPinned)
        {
            OverlayPinned = false;
            changed = true;
        }

        // Pin and Lock performed the same job for this overlay. Pin was removed
        // in 0.4.1.0 so Lock now owns all position and resize protection.
        if (OverlayPinned)
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

            if (migratingToVersionSix && !timer.ShowInOverlay)
            {
                timer.ShowInOverlay = true;
                changed = true;
            }

            if (migratingToVersionSeven)
            {
                timer.ShowCompletionPopup = true;
                timer.PlaySoundOnCompletion = true;
                timer.PrintCompletionToChat = false;
                changed = true;
            }

            if (migratingToVersionEight)
            {
                timer.ShowNotesInOverlay =
                    OverlayRowStyle == OverlayRowStyle.Detailed;
                timer.CompletionSound = CompletionSound.StandardNotification;
                timer.RepeatMode = TimerRepeatMode.None;
                timer.RepeatIntervalUnit = RepeatIntervalUnit.Hours;
                timer.RepeatInterval = 1;
                timer.RepeatAnchor = TimerRepeatAnchor.OriginalSchedule;
                timer.RecurrenceAnchorUnixSeconds = timer.EndUnixSeconds;

                var remainingSeconds =
                    timer.EndUnixSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                timer.RestartDurationSeconds = remainingSeconds >= 60
                    ? remainingSeconds
                    : 3600;

                var targetLocal =
                    DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds).LocalDateTime;
                timer.RepeatWeekdayMask = 1 << (int)targetLocal.DayOfWeek;
                timer.RepeatDayOfMonth = targetLocal.Day;
                timer.IsSnoozed = false;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TimerIcon), timer.Icon))
            {
                timer.Icon = TimerIcon.Clock;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TimerColor), timer.Color))
            {
                timer.Color = TimerColor.Default;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TimerDisplayFormat), timer.DisplayFormat))
            {
                timer.DisplayFormat = TimerDisplayFormat.Auto;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(CompletionSound), timer.CompletionSound))
            {
                timer.CompletionSound = CompletionSound.StandardNotification;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TimerRepeatMode), timer.RepeatMode))
            {
                timer.RepeatMode = TimerRepeatMode.None;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(RepeatIntervalUnit), timer.RepeatIntervalUnit))
            {
                timer.RepeatIntervalUnit = RepeatIntervalUnit.Hours;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TimerRepeatAnchor), timer.RepeatAnchor))
            {
                timer.RepeatAnchor = TimerRepeatAnchor.OriginalSchedule;
                changed = true;
            }

            var validRepeatInterval = Math.Clamp(timer.RepeatInterval, 1, 10000);
            if (timer.RepeatInterval != validRepeatInterval)
            {
                timer.RepeatInterval = validRepeatInterval;
                changed = true;
            }

            var validWeekdayMask = timer.RepeatWeekdayMask & 0x7F;
            if (timer.RepeatMode == TimerRepeatMode.SelectedWeekdays
                && validWeekdayMask == 0)
            {
                var targetDay = DateTimeOffset
                    .FromUnixTimeSeconds(timer.EndUnixSeconds)
                    .LocalDateTime
                    .DayOfWeek;
                validWeekdayMask = 1 << (int)targetDay;
            }

            if (timer.RepeatWeekdayMask != validWeekdayMask)
            {
                timer.RepeatWeekdayMask = validWeekdayMask;
                changed = true;
            }

            var validMonthDay = Math.Clamp(timer.RepeatDayOfMonth, 1, 31);
            if (timer.RepeatDayOfMonth != validMonthDay)
            {
                timer.RepeatDayOfMonth = validMonthDay;
                changed = true;
            }

            if (timer.RecurrenceAnchorUnixSeconds <= 0)
            {
                timer.RecurrenceAnchorUnixSeconds = timer.EndUnixSeconds;
                changed = true;
            }

            var validRestartDuration =
                Math.Clamp(timer.RestartDurationSeconds, 60, 315360000);
            if (timer.RestartDurationSeconds != validRestartDuration)
            {
                timer.RestartDurationSeconds = validRestartDuration;
                changed = true;
            }
        }

        if (!Enum.IsDefined(typeof(OverlayDisplayMode), OverlayMode))
        {
            OverlayMode = OverlayDisplayMode.Persistent;
            changed = true;
        }

        var overlayEnabledForMode = OverlayMode != OverlayDisplayMode.Off;
        if (OverlayEnabled != overlayEnabledForMode)
        {
            OverlayEnabled = overlayEnabledForMode;
            changed = true;
        }

        if (!OverlayKeys.IsSupported(OverlayHoldKey))
        {
            OverlayHoldKey = VirtualKey.F10;
            changed = true;
        }

        if (!Enum.IsDefined(typeof(OverlayRowStyle), OverlayRowStyle))
        {
            OverlayRowStyle = OverlayRowStyle.Compact;
            changed = true;
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

        var validSnoozeMinutes = Math.Clamp(DefaultSnoozeMinutes, 1, 10080);
        if (DefaultSnoozeMinutes != validSnoozeMinutes)
        {
            DefaultSnoozeMinutes = validSnoozeMinutes;
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

        if (Version != 8)
        {
            Version = 8;
            changed = true;
        }

        return changed;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
