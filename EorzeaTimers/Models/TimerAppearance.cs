using System;
using System.Numerics;

namespace EorzeaTimers.Models;

public enum TimerIcon
{
    Clock,
    Battle,
    House,
    Sprout,
    Workshop,
    Garden,
    Person,
    Bell,
    Star,
    Heart,
    Gil,
}

public enum TimerColor
{
    Default,
    Red,
    Orange,
    Gold,
    Green,
    Blue,
    Purple,
    Pink,
}

public enum TimerDisplayFormat
{
    Auto,
    DaysAndClock,
    TotalHours,
    TotalMinutes,
    Words,
    TargetDateAndTime,
}

public enum OverlayRowStyle
{
    Compact,
    Detailed,
}

public static class TimerAppearance
{
    public static readonly TimerIcon[] Icons = Enum.GetValues<TimerIcon>();

    public static readonly TimerColor[] Colors = Enum.GetValues<TimerColor>();

    public static readonly TimerDisplayFormat[] DisplayFormats =
        Enum.GetValues<TimerDisplayFormat>();

    public static string GetIconGlyph(TimerIcon icon)
    {
        return icon switch
        {
            TimerIcon.Battle => "\uf05b",
            TimerIcon.House => "\uf015",
            TimerIcon.Sprout => "\uf4d8",
            TimerIcon.Workshop => "\uf013",
            TimerIcon.Garden => "\uf06c",
            TimerIcon.Person => "\uf007",
            TimerIcon.Bell => "\uf0f3",
            TimerIcon.Star => "\uf005",
            TimerIcon.Heart => "\uf004",
            TimerIcon.Gil => "\uf51e",
            _ => "\uf017",
        };
    }

    public static string GetIconName(TimerIcon icon)
    {
        return icon switch
        {
            TimerIcon.Battle => "Battle",
            TimerIcon.House => "House",
            TimerIcon.Sprout => "Sprout",
            TimerIcon.Workshop => "Workshop",
            TimerIcon.Garden => "Garden",
            TimerIcon.Person => "Person",
            TimerIcon.Bell => "Bell",
            TimerIcon.Star => "Star",
            TimerIcon.Heart => "Heart",
            TimerIcon.Gil => "Gil",
            _ => "Clock",
        };
    }

    public static Vector4 GetColor(TimerColor color)
    {
        return color switch
        {
            TimerColor.Red => new Vector4(0.95f, 0.32f, 0.29f, 1f),
            TimerColor.Orange => new Vector4(1f, 0.58f, 0.22f, 1f),
            TimerColor.Gold => new Vector4(1f, 0.78f, 0.30f, 1f),
            TimerColor.Green => new Vector4(0.42f, 0.82f, 0.47f, 1f),
            TimerColor.Blue => new Vector4(0.38f, 0.66f, 1f, 1f),
            TimerColor.Purple => new Vector4(0.68f, 0.45f, 0.91f, 1f),
            TimerColor.Pink => new Vector4(0.95f, 0.48f, 0.76f, 1f),
            _ => new Vector4(0.74f, 0.86f, 1f, 1f),
        };
    }

    public static string GetColorName(TimerColor color)
    {
        return color == TimerColor.Default ? "Default" : color.ToString();
    }

    public static string GetDisplayFormatName(TimerDisplayFormat format)
    {
        return format switch
        {
            TimerDisplayFormat.DaysAndClock => "Days + clock (1d 02:03:04)",
            TimerDisplayFormat.TotalHours => "Total hours (26:03:04)",
            TimerDisplayFormat.TotalMinutes => "Total minutes (1563:04)",
            TimerDisplayFormat.Words => "Words (1d 2h 3m 4s)",
            TimerDisplayFormat.TargetDateAndTime => "Target date and time",
            _ => "Auto",
        };
    }

    public static string FormatTimer(TimerEntry timer)
    {
        if (!timer.IsActive)
        {
            return "Disabled";
        }

        var target = DateTimeOffset.FromUnixTimeSeconds(timer.EndUnixSeconds);
        var remaining = target - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            return AddScheduleStatus(timer, "Complete");
        }

        if (timer.DisplayFormat == TimerDisplayFormat.TargetDateAndTime)
        {
            return AddScheduleStatus(
                timer,
                target.LocalDateTime.ToString("MMM d, yyyy h:mm tt"));
        }

        var totalDays = (int)remaining.TotalDays;
        var totalHours = (long)remaining.TotalHours;
        var totalMinutes = (long)remaining.TotalMinutes;

        var formatted = timer.DisplayFormat switch
        {
            TimerDisplayFormat.DaysAndClock =>
                $"{totalDays}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}",
            TimerDisplayFormat.TotalHours =>
                $"{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}",
            TimerDisplayFormat.TotalMinutes =>
                $"{totalMinutes:00}:{remaining.Seconds:00}",
            TimerDisplayFormat.Words => FormatWords(remaining),
            _ when remaining.TotalDays >= 1 =>
                $"{totalDays}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}",
            _ when remaining.TotalHours >= 1 =>
                $"{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}",
            _ => $"{totalMinutes:00}:{remaining.Seconds:00}",
        };

        return AddScheduleStatus(timer, formatted);
    }

    private static string AddScheduleStatus(TimerEntry timer, string formatted)
    {
        var status = TimerSchedule.GetStatusText(timer);
        return status.Length == 0 ? formatted : $"{status} · {formatted}";
    }

    private static string FormatWords(TimeSpan remaining)
    {
        var days = (int)remaining.TotalDays;

        if (days > 0)
        {
            return $"{days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s";
        }

        return $"{remaining.Seconds}s";
    }
}
