using System;

namespace EorzeaTimers.Models;

[Serializable]
public sealed class TimerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Timer";

    public string Notes { get; set; } = string.Empty;

    public long EndUnixSeconds { get; set; }

    public TimerSourceType SourceType { get; set; } = TimerSourceType.Manual;

    public GameTimerSource GameSource { get; set; } = GameTimerSource.None;

    // The source's current canonical target is retained separately so a
    // game-linked alert can be snoozed without breaking its schedule link.
    public long LinkedTargetUnixSeconds { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowInOverlay { get; set; } = true;

    public bool ShowNotesInOverlay { get; set; }

    public bool ShowCompletionPopup { get; set; } = true;

    public bool PlaySoundOnCompletion { get; set; } = true;

    public CompletionSound CompletionSound { get; set; } = CompletionSound.StandardNotification;

    public int AlertVolumePercent { get; set; } = 100;

    public bool PrintCompletionToChat { get; set; }

    public TimerRepeatMode RepeatMode { get; set; } = TimerRepeatMode.None;

    public RepeatIntervalUnit RepeatIntervalUnit { get; set; } = RepeatIntervalUnit.Hours;

    public int RepeatInterval { get; set; } = 1;

    public int RepeatWeekdayMask { get; set; }

    public int RepeatDayOfMonth { get; set; } = 1;

    public TimerRepeatAnchor RepeatAnchor { get; set; } = TimerRepeatAnchor.OriginalSchedule;

    public long RecurrenceAnchorUnixSeconds { get; set; }

    public long RestartDurationSeconds { get; set; } = 3600;

    public bool IsSnoozed { get; set; }

    public TimerIcon Icon { get; set; } = TimerIcon.Clock;

    public TimerColor Color { get; set; } = TimerColor.Default;

    public TimerDisplayFormat DisplayFormat { get; set; } = TimerDisplayFormat.Auto;
}
