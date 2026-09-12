using System;

namespace EorzeaTimers.Models;

public enum TimerRepeatMode
{
    None,
    Interval,
    Daily,
    Weekly,
    SelectedWeekdays,
    Monthly,
}

public enum RepeatIntervalUnit
{
    Minutes,
    Hours,
    Days,
}

public enum TimerRepeatAnchor
{
    OriginalSchedule,
    DismissalTime,
}

public static class TimerSchedule
{
    public static string GetRepeatModeName(TimerRepeatMode mode)
    {
        return mode switch
        {
            TimerRepeatMode.Interval => "Custom interval",
            TimerRepeatMode.Daily => "Daily",
            TimerRepeatMode.Weekly => "Weekly",
            TimerRepeatMode.SelectedWeekdays => "Selected weekdays",
            TimerRepeatMode.Monthly => "Monthly",
            _ => "Does not repeat",
        };
    }

    public static string GetIntervalUnitName(RepeatIntervalUnit unit)
    {
        return unit switch
        {
            RepeatIntervalUnit.Minutes => "Minutes",
            RepeatIntervalUnit.Days => "Days",
            _ => "Hours",
        };
    }

    public static string GetAnchorName(TimerRepeatAnchor anchor)
    {
        return anchor == TimerRepeatAnchor.DismissalTime
            ? "When dismissed"
            : "Original schedule";
    }

    public static string GetStatusText(TimerEntry timer)
    {
        if (timer.IsSnoozed)
        {
            return "Snoozed";
        }

        var linkedStatus = GameLinkedTimers.GetShortStatus(timer);
        if (linkedStatus.Length > 0)
        {
            return linkedStatus;
        }

        return timer.RepeatMode == TimerRepeatMode.None
            ? string.Empty
            : "Repeats";
    }

    public static long GetNextOccurrenceUnixSeconds(
        TimerEntry timer,
        long afterUnixSeconds)
    {
        if (timer.RepeatMode == TimerRepeatMode.None)
        {
            return timer.EndUnixSeconds;
        }

        var after = DateTimeOffset.FromUnixTimeSeconds(afterUnixSeconds);
        var recurrenceAnchorUnixSeconds = timer.RecurrenceAnchorUnixSeconds > 0
            ? timer.RecurrenceAnchorUnixSeconds
            : timer.EndUnixSeconds;
        var anchor = DateTimeOffset.FromUnixTimeSeconds(recurrenceAnchorUnixSeconds);

        if (timer.RepeatAnchor == TimerRepeatAnchor.DismissalTime)
        {
            anchor = after;
        }

        if (timer.RepeatAnchor == TimerRepeatAnchor.OriginalSchedule
            && anchor > after)
        {
            return anchor.ToUnixTimeSeconds();
        }

        return timer.RepeatMode switch
        {
            TimerRepeatMode.Interval =>
                GetNextInterval(timer, anchor, after).ToUnixTimeSeconds(),
            TimerRepeatMode.Daily =>
                GetNextDaily(anchor, after).ToUnixTimeSeconds(),
            TimerRepeatMode.Weekly =>
                GetNextWeekly(anchor, after).ToUnixTimeSeconds(),
            TimerRepeatMode.SelectedWeekdays =>
                GetNextSelectedWeekday(timer, anchor, after).ToUnixTimeSeconds(),
            TimerRepeatMode.Monthly =>
                GetNextMonthly(timer, anchor, after).ToUnixTimeSeconds(),
            _ => timer.EndUnixSeconds,
        };
    }

    public static string FormatNextOccurrence(TimerEntry timer, long afterUnixSeconds)
    {
        if (timer.RepeatMode == TimerRepeatMode.None)
        {
            return "Does not repeat";
        }

        var next = DateTimeOffset.FromUnixTimeSeconds(
            GetNextOccurrenceUnixSeconds(timer, afterUnixSeconds));
        return next.LocalDateTime.ToString("ddd, MMM d, yyyy h:mm tt");
    }

    private static DateTimeOffset GetNextInterval(
        TimerEntry timer,
        DateTimeOffset anchor,
        DateTimeOffset after)
    {
        var count = Math.Clamp(timer.RepeatInterval, 1, 10000);
        var interval = timer.RepeatIntervalUnit switch
        {
            RepeatIntervalUnit.Minutes => TimeSpan.FromMinutes(count),
            RepeatIntervalUnit.Days => TimeSpan.FromDays(count),
            _ => TimeSpan.FromHours(count),
        };

        if (timer.RepeatAnchor == TimerRepeatAnchor.DismissalTime)
        {
            return after.Add(interval);
        }

        if (anchor > after)
        {
            return anchor;
        }

        var occurrencesToAdvance =
            (long)Math.Floor((after - anchor).TotalSeconds / interval.TotalSeconds) + 1;
        return anchor.AddTicks(interval.Ticks * occurrencesToAdvance);
    }

    private static DateTimeOffset GetNextDaily(
        DateTimeOffset anchor,
        DateTimeOffset after)
    {
        if (anchor > after)
        {
            return anchor;
        }

        if (anchor == after)
        {
            return FromLocal(anchor.LocalDateTime.AddDays(1));
        }

        var candidate = FromLocal(
            after.LocalDateTime.Date
                .Add(anchor.LocalDateTime.TimeOfDay));

        return candidate > after
            ? candidate
            : FromLocal(candidate.LocalDateTime.AddDays(1));
    }

    private static DateTimeOffset GetNextWeekly(
        DateTimeOffset anchor,
        DateTimeOffset after)
    {
        if (anchor > after)
        {
            return anchor;
        }

        if (anchor == after)
        {
            return FromLocal(anchor.LocalDateTime.AddDays(7));
        }

        var anchorLocal = anchor.LocalDateTime;
        var afterLocal = after.LocalDateTime;
        var daysAhead = ((int)anchorLocal.DayOfWeek - (int)afterLocal.DayOfWeek + 7) % 7;
        var candidateLocal = afterLocal.Date
            .AddDays(daysAhead)
            .Add(anchorLocal.TimeOfDay);
        var candidate = FromLocal(candidateLocal);

        return candidate > after
            ? candidate
            : FromLocal(candidateLocal.AddDays(7));
    }

    private static DateTimeOffset GetNextSelectedWeekday(
        TimerEntry timer,
        DateTimeOffset anchor,
        DateTimeOffset after)
    {
        var mask = timer.RepeatWeekdayMask & 0x7F;
        if (mask == 0)
        {
            mask = 1 << (int)anchor.LocalDateTime.DayOfWeek;
        }

        var timeOfDay = timer.RepeatAnchor == TimerRepeatAnchor.DismissalTime
            ? after.LocalDateTime.TimeOfDay
            : anchor.LocalDateTime.TimeOfDay;
        var startDate = after.LocalDateTime.Date;

        for (var daysAhead = 0; daysAhead <= 7; daysAhead++)
        {
            var candidateLocal = startDate.AddDays(daysAhead).Add(timeOfDay);
            var dayBit = 1 << (int)candidateLocal.DayOfWeek;
            var candidate = FromLocal(candidateLocal);
            if ((mask & dayBit) != 0 && candidate > after)
            {
                return candidate;
            }
        }

        return FromLocal(startDate.AddDays(7).Add(timeOfDay));
    }

    private static DateTimeOffset GetNextMonthly(
        TimerEntry timer,
        DateTimeOffset anchor,
        DateTimeOffset after)
    {
        var targetDay = Math.Clamp(timer.RepeatDayOfMonth, 1, 31);
        var timeOfDay = timer.RepeatAnchor == TimerRepeatAnchor.DismissalTime
            ? after.LocalDateTime.TimeOfDay
            : anchor.LocalDateTime.TimeOfDay;
        var afterLocal = after.LocalDateTime;

        for (var monthsAhead = 0; monthsAhead <= 24; monthsAhead++)
        {
            var month = new DateTime(afterLocal.Year, afterLocal.Month, 1)
                .AddMonths(monthsAhead);
            var day = Math.Min(targetDay, DateTime.DaysInMonth(month.Year, month.Month));
            var candidate = FromLocal(
                new DateTime(month.Year, month.Month, day).Add(timeOfDay));

            if (candidate > after)
            {
                return candidate;
            }
        }

        return FromLocal(afterLocal.AddMonths(1));
    }

    private static DateTimeOffset FromLocal(DateTime localDateTime)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Local));
    }
}
