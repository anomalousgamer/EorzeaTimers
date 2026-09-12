using System;

namespace EorzeaTimers.Models;

[Serializable]
public sealed class TimerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Timer";

    public string Notes { get; set; } = string.Empty;

    public long EndUnixSeconds { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowInOverlay { get; set; } = true;

    public bool ShowCompletionPopup { get; set; } = true;

    public bool PlaySoundOnCompletion { get; set; } = true;

    public bool PrintCompletionToChat { get; set; }

    public TimerIcon Icon { get; set; } = TimerIcon.Clock;

    public TimerColor Color { get; set; } = TimerColor.Default;

    public TimerDisplayFormat DisplayFormat { get; set; } = TimerDisplayFormat.Auto;
}
