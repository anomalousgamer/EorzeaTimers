using System;

namespace EorzeaTimers.Models;

[Serializable]
public sealed class TimerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Timer";

    public long EndUnixSeconds { get; set; }

    public bool IsActive { get; set; } = true;
}
