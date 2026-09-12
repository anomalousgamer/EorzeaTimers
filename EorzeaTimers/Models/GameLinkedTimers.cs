using System;

namespace EorzeaTimers.Models;

public enum TimerSourceType
{
    Manual,
    GameLinked,
}

public enum GameTimerSource
{
    None,
    HousingLottery,
}

public enum HousingLotteryPhase
{
    Entry,
    Results,
}

public readonly record struct GameTimerSnapshot(
    GameTimerSource Source,
    string SourceName,
    string PhaseName,
    long PeriodStartUnixSeconds,
    long PeriodEndUnixSeconds);

public static class GameLinkedTimers
{
    // FFXIV's recurring housing lottery schedule uses five entry days followed
    // by four results days. This known entry-period start anchors the cycle.
    private static readonly DateTimeOffset HousingScheduleStartUtc =
        new(2022, 5, 26, 15, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan HousingEntryDuration = TimeSpan.FromDays(5);
    private static readonly TimeSpan HousingResultsDuration = TimeSpan.FromDays(4);

    public static bool TryGetSnapshot(
        GameTimerSource source,
        DateTimeOffset now,
        out GameTimerSnapshot snapshot)
    {
        switch (source)
        {
            case GameTimerSource.HousingLottery:
                snapshot = GetHousingLotterySnapshot(now);
                return true;
            default:
                snapshot = default;
                return false;
        }
    }

    public static string GetSourceName(GameTimerSource source)
    {
        return source switch
        {
            GameTimerSource.HousingLottery => "Housing Lottery",
            _ => "Unknown game timer",
        };
    }

    public static string GetShortStatus(TimerEntry timer)
    {
        if (timer.SourceType != TimerSourceType.GameLinked)
        {
            return string.Empty;
        }

        if (!TryGetSnapshot(timer.GameSource, DateTimeOffset.UtcNow, out var snapshot))
        {
            return "Linked source unavailable";
        }

        return timer.GameSource == GameTimerSource.HousingLottery
            ? $"Housing {snapshot.PhaseName.ToLowerInvariant()}"
            : snapshot.PhaseName;
    }

    private static GameTimerSnapshot GetHousingLotterySnapshot(DateTimeOffset now)
    {
        var entrySeconds = (long)HousingEntryDuration.TotalSeconds;
        var cycleSeconds =
            entrySeconds + (long)HousingResultsDuration.TotalSeconds;
        var elapsedSeconds =
            now.ToUnixTimeSeconds() - HousingScheduleStartUtc.ToUnixTimeSeconds();

        var cycleIndex = Math.DivRem(elapsedSeconds, cycleSeconds, out var cycleOffset);
        if (cycleOffset < 0)
        {
            cycleOffset += cycleSeconds;
            cycleIndex--;
        }

        var cycleStart = HousingScheduleStartUtc.AddSeconds(cycleIndex * cycleSeconds);
        HousingLotteryPhase phase;
        DateTimeOffset periodStart;
        DateTimeOffset periodEnd;

        if (cycleOffset < entrySeconds)
        {
            phase = HousingLotteryPhase.Entry;
            periodStart = cycleStart;
            periodEnd = cycleStart.Add(HousingEntryDuration);
        }
        else
        {
            phase = HousingLotteryPhase.Results;
            periodStart = cycleStart.Add(HousingEntryDuration);
            periodEnd = cycleStart.AddSeconds(cycleSeconds);
        }

        return new GameTimerSnapshot(
            GameTimerSource.HousingLottery,
            GetSourceName(GameTimerSource.HousingLottery),
            phase == HousingLotteryPhase.Entry ? "Entry period" : "Results period",
            periodStart.ToUnixTimeSeconds(),
            periodEnd.ToUnixTimeSeconds());
    }
}
