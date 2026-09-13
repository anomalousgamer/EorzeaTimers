using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using GamePlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;
using GameUiState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState;

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
    DailyReset,
    DutyRouletteReset,
    AlliedSocietyReset,
    DailyHuntReset,
    FrontlineChallengeReset,
    MiniCactpotReset,
    GrandCompanyReset,
    GrandCompanyDeliveryReset,
    SquadronAllowanceReset,
    RowenaDeliveryReset,
    WeeklyReset,
    WeeklyTomestoneReset,
    RaidLootReset,
    ChallengeLogReset,
    CustomDeliveriesReset,
    WondrousTailsExpiration,
    LeveAllowance,
    TreasureMapAllowance,
    SquadronMission,
    SquadronTraining,
    RetainerVenture,
    RetainerMarketExpiration,
    OceanFishing,
    Gate,
    FashionReportTheme,
    FashionReportJudging,
    IslandSanctuaryCycle,
    IslandSanctuarySeason,
    CosmicExplorationReset,
    IslandGranary,
    FreeCompanyAirship,
    FreeCompanySubmersible,
    JumboCactpotJapan,
    JumboCactpotNorthAmerica,
    JumboCactpotEurope,
    JumboCactpotOceania,
}

public enum HousingLotteryPhase
{
    Entry,
    Results,
}

public sealed record GameTimerDefinition(
    GameTimerSource Source,
    string Category,
    string Name,
    string Description,
    TimerIcon Icon,
    TimerColor Color);

public readonly record struct GameTimerSnapshot(
    GameTimerSource Source,
    string SourceName,
    string PhaseName,
    string GeneratedNote,
    long PeriodStartUnixSeconds,
    long PeriodEndUnixSeconds);

public static class GameLinkedTimers
{
    private static readonly DateTimeOffset HousingScheduleStartUtc =
        new(2022, 5, 26, 15, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan HousingEntryDuration = TimeSpan.FromDays(5);
    private static readonly TimeSpan HousingResultsDuration = TimeSpan.FromDays(4);

    public static IReadOnlyList<GameTimerDefinition> Definitions { get; } =
    [
        new(GameTimerSource.HousingLottery, "World", "Housing Lottery", "Current entry or results period.", TimerIcon.House, TimerColor.Gold),

        new(GameTimerSource.DailyReset, "Daily resets", "Daily Reset", "General daily reset at 15:00 UTC.", TimerIcon.Clock, TimerColor.Blue),
        new(GameTimerSource.DutyRouletteReset, "Daily resets", "Duty Roulettes", "Daily Duty Roulette rewards.", TimerIcon.Battle, TimerColor.Blue),
        new(GameTimerSource.AlliedSocietyReset, "Daily resets", "Allied Society Quests", "Daily Allied Society quest allowances.", TimerIcon.Person, TimerColor.Green),
        new(GameTimerSource.DailyHuntReset, "Daily resets", "Daily Hunt Bills", "Daily hunt-mark bills.", TimerIcon.Battle, TimerColor.Orange),
        new(GameTimerSource.FrontlineChallengeReset, "Daily resets", "Frontline Daily Challenge", "Daily Frontline Challenge reward.", TimerIcon.Battle, TimerColor.Red),
        new(GameTimerSource.MiniCactpotReset, "Daily resets", "Mini Cactpot", "Daily Mini Cactpot ticket reset.", TimerIcon.Gil, TimerColor.Gold),

        new(GameTimerSource.GrandCompanyReset, "Grand Company", "Grand Company Reset", "General Grand Company reset at 20:00 UTC.", TimerIcon.Battle, TimerColor.Blue),
        new(GameTimerSource.GrandCompanyDeliveryReset, "Grand Company", "Supply & Provisioning", "Grand Company delivery list reset.", TimerIcon.Workshop, TimerColor.Blue),
        new(GameTimerSource.SquadronAllowanceReset, "Grand Company", "Squadron Training Allowance", "Daily squadron training allowance reset.", TimerIcon.Person, TimerColor.Blue),
        new(GameTimerSource.RowenaDeliveryReset, "Grand Company", "Collectable Deliveries", "Daily Rowena collectable delivery reset.", TimerIcon.Workshop, TimerColor.Purple),

        new(GameTimerSource.WeeklyReset, "Weekly resets", "Weekly Reset", "General weekly reset on Tuesday at 08:00 UTC.", TimerIcon.Clock, TimerColor.Purple),
        new(GameTimerSource.WeeklyTomestoneReset, "Weekly resets", "Tomestone Cap", "Weekly capped tomestone allowance.", TimerIcon.Star, TimerColor.Purple),
        new(GameTimerSource.RaidLootReset, "Weekly resets", "Raid Loot Lockouts", "Weekly raid loot lockouts.", TimerIcon.Battle, TimerColor.Red),
        new(GameTimerSource.ChallengeLogReset, "Weekly resets", "Challenge Log", "Read from the character's game timer data.", TimerIcon.Star, TimerColor.Purple),
        new(GameTimerSource.CustomDeliveriesReset, "Weekly resets", "Custom Deliveries", "Read from the custom-delivery system.", TimerIcon.Person, TimerColor.Pink),
        new(GameTimerSource.WondrousTailsExpiration, "Weekly resets", "Wondrous Tails", "The current journal's actual expiration.", TimerIcon.Star, TimerColor.Gold),

        new(GameTimerSource.LeveAllowance, "Allowances", "Leve Allowance", "Next leve allowance read from the game.", TimerIcon.Sprout, TimerColor.Green),
        new(GameTimerSource.TreasureMapAllowance, "Allowances", "Treasure Map Allowance", "Next map allowance read from the game.", TimerIcon.Gil, TimerColor.Gold),

        new(GameTimerSource.SquadronMission, "Character activity", "Squadron Mission", "Current squadron mission return time.", TimerIcon.Person, TimerColor.Blue),
        new(GameTimerSource.SquadronTraining, "Character activity", "Squadron Training", "Current squadron training completion time.", TimerIcon.Person, TimerColor.Green),
        new(GameTimerSource.RetainerVenture, "Character activity", "Next Retainer Venture", "Earliest active retainer venture completion.", TimerIcon.Person, TimerColor.Pink),
        new(GameTimerSource.RetainerMarketExpiration, "Character activity", "Retainer Market Expiration", "Earliest loaded retainer market-listing expiration.", TimerIcon.Gil, TimerColor.Orange),
        new(GameTimerSource.IslandGranary, "Character activity", "Island Granary", "Earliest active granary expedition completion.", TimerIcon.Sprout, TimerColor.Green),
        new(GameTimerSource.FreeCompanyAirship, "Character activity", "FC Airship Voyage", "Earliest loaded Free Company airship return.", TimerIcon.Workshop, TimerColor.Blue),
        new(GameTimerSource.FreeCompanySubmersible, "Character activity", "FC Submersible Voyage", "Earliest loaded Free Company submersible return.", TimerIcon.Workshop, TimerColor.Blue),

        new(GameTimerSource.OceanFishing, "Scheduled content", "Ocean Fishing", "Next voyage departure on the two-hour schedule.", TimerIcon.Battle, TimerColor.Blue),
        new(GameTimerSource.Gate, "Scheduled content", "Gold Saucer GATE", "Next GATE at :00, :20, or :40.", TimerIcon.Star, TimerColor.Gold),
        new(GameTimerSource.JumboCactpotJapan, "Scheduled content", "Jumbo Cactpot - Japan", "Saturday drawing for Japanese data centers.", TimerIcon.Gil, TimerColor.Gold),
        new(GameTimerSource.JumboCactpotNorthAmerica, "Scheduled content", "Jumbo Cactpot - North America", "Saturday drawing for North American data centers.", TimerIcon.Gil, TimerColor.Gold),
        new(GameTimerSource.JumboCactpotEurope, "Scheduled content", "Jumbo Cactpot - Europe", "Saturday drawing for European data centers.", TimerIcon.Gil, TimerColor.Gold),
        new(GameTimerSource.JumboCactpotOceania, "Scheduled content", "Jumbo Cactpot - Oceania", "Saturday drawing for Oceanian data centers.", TimerIcon.Gil, TimerColor.Gold),
        new(GameTimerSource.FashionReportTheme, "Scheduled content", "Fashion Report Theme", "Next weekly theme change.", TimerIcon.Star, TimerColor.Pink),
        new(GameTimerSource.FashionReportJudging, "Scheduled content", "Fashion Report Judging", "Next judging-period transition.", TimerIcon.Star, TimerColor.Pink),
        new(GameTimerSource.IslandSanctuaryCycle, "Scheduled content", "Island Sanctuary Cycle", "Next daily Isleworks cycle.", TimerIcon.Sprout, TimerColor.Green),
        new(GameTimerSource.IslandSanctuarySeason, "Scheduled content", "Island Sanctuary Season", "Next weekly Isleworks season.", TimerIcon.Workshop, TimerColor.Green),
        new(GameTimerSource.CosmicExplorationReset, "Scheduled content", "Cosmic Exploration Reset", "Next Cosmic Exploration daily reset.", TimerIcon.Star, TimerColor.Purple),
    ];

    public static GameTimerDefinition? GetDefinition(GameTimerSource source)
    {
        foreach (var definition in Definitions)
        {
            if (definition.Source == source)
            {
                return definition;
            }
        }

        return null;
    }

    public static unsafe bool TryGetSnapshot(
        GameTimerSource source,
        DateTimeOffset now,
        out GameTimerSnapshot snapshot)
    {
        try
        {
            snapshot = source switch
            {
                GameTimerSource.HousingLottery => GetHousingLotterySnapshot(now),

                GameTimerSource.DailyReset => GetDailySnapshot(source, now, 15, "Next daily reset", "Daily reset · 15:00 UTC"),
                GameTimerSource.DutyRouletteReset => GetDailySnapshot(source, now, 15, "Roulette rewards reset", "Duty Roulette rewards · Daily reset"),
                GameTimerSource.AlliedSocietyReset => GetDailySnapshot(source, now, 15, "Quest allowances reset", "Allied Society quests · Daily reset"),
                GameTimerSource.DailyHuntReset => GetDailySnapshot(source, now, 15, "Daily bills reset", "Daily hunt bills · Daily reset"),
                GameTimerSource.FrontlineChallengeReset => GetDailySnapshot(source, now, 15, "Challenge reward resets", "Frontline Daily Challenge · Daily reset"),
                GameTimerSource.MiniCactpotReset => GetDailySnapshot(source, now, 15, "Tickets reset", "Mini Cactpot tickets · Daily reset"),

                GameTimerSource.GrandCompanyReset => GetDailySnapshot(source, now, 20, "Next GC reset", "Grand Company reset · 20:00 UTC"),
                GameTimerSource.GrandCompanyDeliveryReset => GetDailySnapshot(source, now, 20, "Delivery list resets", "Supply & Provisioning · Grand Company reset"),
                GameTimerSource.SquadronAllowanceReset => GetDailySnapshot(source, now, 20, "Allowance resets", "Squadron training allowance · Grand Company reset"),
                GameTimerSource.RowenaDeliveryReset => GetDailySnapshot(source, now, 20, "Delivery list resets", "Collectable deliveries · Grand Company reset"),

                GameTimerSource.WeeklyReset => GetWeeklySnapshot(source, now, DayOfWeek.Tuesday, 8, "Next weekly reset", "Weekly reset · Tuesday 08:00 UTC"),
                GameTimerSource.WeeklyTomestoneReset => GetWeeklySnapshot(source, now, DayOfWeek.Tuesday, 8, "Tomestone cap resets", "Capped tomestones · Weekly reset"),
                GameTimerSource.RaidLootReset => GetWeeklySnapshot(source, now, DayOfWeek.Tuesday, 8, "Loot lockouts reset", "Raid loot lockouts · Weekly reset"),

                GameTimerSource.ChallengeLogReset => GetChallengeLogSnapshot(now),
                GameTimerSource.CustomDeliveriesReset => GetCustomDeliveriesSnapshot(now),
                GameTimerSource.WondrousTailsExpiration => GetWondrousTailsSnapshot(now),
                GameTimerSource.LeveAllowance => GetLeveSnapshot(now),
                GameTimerSource.TreasureMapAllowance => GetTreasureMapSnapshot(now),
                GameTimerSource.SquadronMission => GetSquadronSnapshot(now, false),
                GameTimerSource.SquadronTraining => GetSquadronSnapshot(now, true),
                GameTimerSource.RetainerVenture => GetRetainerSnapshot(now, false),
                GameTimerSource.RetainerMarketExpiration => GetRetainerSnapshot(now, true),
                GameTimerSource.IslandGranary => GetIslandGranarySnapshot(now),
                GameTimerSource.FreeCompanyAirship => GetWorkshopVoyageSnapshot(now, false),
                GameTimerSource.FreeCompanySubmersible => GetWorkshopVoyageSnapshot(now, true),

                GameTimerSource.OceanFishing => GetIntervalSnapshot(source, now, TimeSpan.FromHours(2), DateTimeOffset.UnixEpoch, "Next voyage departs", "Ocean Fishing · Two-hour departure schedule"),
                GameTimerSource.Gate => GetIntervalSnapshot(source, now, TimeSpan.FromMinutes(20), DateTimeOffset.UnixEpoch, "Next GATE begins", "Gold Saucer GATE · :00, :20, or :40"),
                GameTimerSource.JumboCactpotJapan => GetWeeklySnapshot(source, now, DayOfWeek.Saturday, 12, "Winning number drawn", "Jumbo Cactpot · Japan · Saturday 12:00 UTC"),
                GameTimerSource.JumboCactpotNorthAmerica => GetWeeklySnapshot(source, now, DayOfWeek.Sunday, 2, "Winning number drawn", "Jumbo Cactpot · North America · Sunday 02:00 UTC"),
                GameTimerSource.JumboCactpotEurope => GetWeeklySnapshot(source, now, DayOfWeek.Saturday, 19, "Winning number drawn", "Jumbo Cactpot · Europe · Saturday 19:00 UTC"),
                GameTimerSource.JumboCactpotOceania => GetWeeklySnapshot(source, now, DayOfWeek.Saturday, 9, "Winning number drawn", "Jumbo Cactpot · Oceania · Saturday 09:00 UTC"),
                GameTimerSource.FashionReportTheme => GetWeeklySnapshot(source, now, DayOfWeek.Tuesday, 8, "Theme changes", "Fashion Report · New weekly theme"),
                GameTimerSource.FashionReportJudging => GetFashionReportJudgingSnapshot(now),
                GameTimerSource.IslandSanctuaryCycle => GetDailySnapshot(source, now, 8, "Next cycle begins", "Island Sanctuary · Next Isleworks cycle"),
                GameTimerSource.IslandSanctuarySeason => GetWeeklySnapshot(source, now, DayOfWeek.Tuesday, 8, "Next season begins", "Island Sanctuary · Next Isleworks season"),
                GameTimerSource.CosmicExplorationReset => GetDailySnapshot(source, now, 9, "Next reset", "Cosmic Exploration · Daily reset at 09:00 UTC"),
                _ => default,
            };

            return source != GameTimerSource.None
                && snapshot.PeriodEndUnixSeconds > 0;
        }
        catch
        {
            snapshot = default;
            return false;
        }
    }

    public static string GetSourceName(GameTimerSource source)
    {
        return GetDefinition(source)?.Name ?? "Unknown game timer";
    }

    public static string GetShortStatus(TimerEntry timer)
    {
        if (timer.SourceType != TimerSourceType.GameLinked)
        {
            return string.Empty;
        }

        return timer.LinkedSourceAvailable
            ? timer.LinkedPhaseName
            : "Linked source unavailable";
    }

    public static string GetUnavailableMessage(GameTimerSource source)
    {
        return source switch
        {
            GameTimerSource.ChallengeLogReset or GameTimerSource.TreasureMapAllowance =>
                "Log in fully and open the in-game Timers window once if this data has not loaded.",
            GameTimerSource.CustomDeliveriesReset =>
                "Custom Delivery data is not available for this character right now.",
            GameTimerSource.WondrousTailsExpiration =>
                "No current Wondrous Tails journal was found.",
            GameTimerSource.SquadronMission =>
                "No active squadron mission was found.",
            GameTimerSource.SquadronTraining =>
                "No active squadron training was found.",
            GameTimerSource.RetainerVenture =>
                "No loaded active retainer venture was found. Open a summoning bell to refresh retainer data.",
            GameTimerSource.RetainerMarketExpiration =>
                "No loaded retainer market expiration was found. Open a summoning bell to refresh retainer data.",
            GameTimerSource.IslandGranary =>
                "No active granary expedition is loaded. Visit your Island Sanctuary to load its current data.",
            GameTimerSource.FreeCompanyAirship =>
                "No active airship voyage is loaded. Visit the Free Company workshop to load voyage data.",
            GameTimerSource.FreeCompanySubmersible =>
                "No active submersible voyage is loaded. Visit the Free Company workshop to load voyage data.",
            _ => "The linked source is currently unavailable.",
        };
    }

    public static bool IsActivitySource(GameTimerSource source)
    {
        return source is GameTimerSource.SquadronMission
            or GameTimerSource.SquadronTraining
            or GameTimerSource.RetainerVenture
            or GameTimerSource.RetainerMarketExpiration
            or GameTimerSource.IslandGranary
            or GameTimerSource.FreeCompanyAirship
            or GameTimerSource.FreeCompanySubmersible
            or GameTimerSource.WondrousTailsExpiration;
    }

    private static GameTimerSnapshot GetDailySnapshot(
        GameTimerSource source,
        DateTimeOffset now,
        int utcHour,
        string phase,
        string note)
    {
        var next = new DateTimeOffset(
            now.UtcDateTime.Year,
            now.UtcDateTime.Month,
            now.UtcDateTime.Day,
            utcHour,
            0,
            0,
            TimeSpan.Zero);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return Snapshot(source, phase, note, next.AddDays(-1), next);
    }

    private static GameTimerSnapshot GetWeeklySnapshot(
        GameTimerSource source,
        DateTimeOffset now,
        DayOfWeek weekday,
        int utcHour,
        string phase,
        string note)
    {
        var daysAhead = ((int)weekday - (int)now.UtcDateTime.DayOfWeek + 7) % 7;
        var next = new DateTimeOffset(
                now.UtcDateTime.Year,
                now.UtcDateTime.Month,
                now.UtcDateTime.Day,
                utcHour,
                0,
                0,
                TimeSpan.Zero)
            .AddDays(daysAhead);
        if (next <= now)
        {
            next = next.AddDays(7);
        }

        return Snapshot(source, phase, note, next.AddDays(-7), next);
    }

    private static GameTimerSnapshot GetIntervalSnapshot(
        GameTimerSource source,
        DateTimeOffset now,
        TimeSpan interval,
        DateTimeOffset anchor,
        string phase,
        string note)
    {
        var intervalSeconds = (long)interval.TotalSeconds;
        var elapsed = now.ToUnixTimeSeconds() - anchor.ToUnixTimeSeconds();
        var cycle = Math.DivRem(elapsed, intervalSeconds, out var remainder);
        if (remainder < 0)
        {
            remainder += intervalSeconds;
            cycle--;
        }

        var start = anchor.AddSeconds(cycle * intervalSeconds);
        var end = start.AddSeconds(intervalSeconds);
        return Snapshot(source, phase, note, start, end);
    }

    private static GameTimerSnapshot GetFashionReportJudgingSnapshot(DateTimeOffset now)
    {
        var nextFriday = GetNextWeeklyTransition(now, DayOfWeek.Friday, 8);
        var nextTuesday = GetNextWeeklyTransition(now, DayOfWeek.Tuesday, 8);
        if (nextFriday < nextTuesday)
        {
            return Snapshot(
                GameTimerSource.FashionReportJudging,
                "Judging opens",
                "Fashion Report · Judging opens Friday at 08:00 UTC",
                nextTuesday.AddDays(-7),
                nextFriday);
        }

        return Snapshot(
            GameTimerSource.FashionReportJudging,
            "Judging closes",
            "Fashion Report · Judging closes Tuesday at 08:00 UTC",
            nextFriday.AddDays(-7),
            nextTuesday);
    }

    private static DateTimeOffset GetNextWeeklyTransition(
        DateTimeOffset now,
        DayOfWeek weekday,
        int utcHour)
    {
        var daysAhead = ((int)weekday - (int)now.UtcDateTime.DayOfWeek + 7) % 7;
        var result = new DateTimeOffset(
                now.UtcDateTime.Year,
                now.UtcDateTime.Month,
                now.UtcDateTime.Day,
                utcHour,
                0,
                0,
                TimeSpan.Zero)
            .AddDays(daysAhead);
        return result <= now ? result.AddDays(7) : result;
    }

    private static GameTimerSnapshot GetLeveSnapshot(DateTimeOffset now)
    {
        var target = QuestManager.GetNextLeveAllowancesUnixTimestamp();
        return FromLiveTarget(
            GameTimerSource.LeveAllowance,
            now,
            target,
            "Next allowance",
            "Leve allowance · Read from game data");
    }

    private static unsafe GameTimerSnapshot GetTreasureMapSnapshot(DateTimeOffset now)
    {
        var uiState = GameUiState.Instance();
        var target = uiState == null ? 0 : uiState->GetNextMapAllowanceTimestamp();
        return FromLiveTarget(
            GameTimerSource.TreasureMapAllowance,
            now,
            target,
            "Next allowance",
            "Treasure map allowance · Read from game data");
    }

    private static unsafe GameTimerSnapshot GetChallengeLogSnapshot(DateTimeOffset now)
    {
        var uiState = GameUiState.Instance();
        var target = uiState == null ? 0 : uiState->GetNextChallengeLogResetTimestamp();
        return FromLiveTarget(
            GameTimerSource.ChallengeLogReset,
            now,
            target,
            "Challenge Log resets",
            "Challenge Log · Read from game data");
    }

    private static unsafe GameTimerSnapshot GetCustomDeliveriesSnapshot(DateTimeOffset now)
    {
        var manager = SatisfactionSupplyManager.Instance();
        if (manager == null)
        {
            return default;
        }

        var reset = manager->GetResetDateTime();
        var resetUtc = new DateTimeOffset(DateTime.SpecifyKind(reset, DateTimeKind.Utc));
        return FromLiveTarget(
            GameTimerSource.CustomDeliveriesReset,
            now,
            resetUtc.ToUnixTimeSeconds(),
            "Allowances reset",
            $"Custom Deliveries · {manager->GetRemainingAllowances()} allowances remaining");
    }

    private static unsafe GameTimerSnapshot GetWondrousTailsSnapshot(DateTimeOffset now)
    {
        var player = GamePlayerState.Instance();
        if (player == null || !player->HasWeeklyBingoJournal)
        {
            return default;
        }

        return FromLiveTarget(
            GameTimerSource.WondrousTailsExpiration,
            now,
            player->GetWeeklyBingoExpireUnixTimestamp(),
            "Journal expires",
            $"Wondrous Tails · {player->WeeklyBingoNumPlacedStickers}/9 stickers placed");
    }

    private static unsafe GameTimerSnapshot GetSquadronSnapshot(DateTimeOffset now, bool training)
    {
        var player = GamePlayerState.Instance();
        if (player == null)
        {
            return default;
        }

        var source = training ? GameTimerSource.SquadronTraining : GameTimerSource.SquadronMission;
        var active = training ? player->ActiveGcArmyTraining : player->ActiveGcArmyExpedition;
        var target = training ? player->SquadronTrainingCompletionTimestamp : player->SquadronMissionCompletionTimestamp;
        if (active == 0)
        {
            return default;
        }

        return FromLiveTarget(
            source,
            now,
            target,
            training ? "Training completes" : "Mission returns",
            training
                ? "Squadron training · Read from character data"
                : "Squadron mission · Read from character data");
    }

    private static unsafe GameTimerSnapshot GetRetainerSnapshot(DateTimeOffset now, bool marketExpiration)
    {
        var manager = RetainerManager.Instance();
        if (manager == null || !manager->IsReady)
        {
            return default;
        }

        var nowUnix = now.ToUnixTimeSeconds();
        long earliest = long.MaxValue;
        var matching = 0;
        var count = manager->GetRetainerCount();
        for (uint index = 0; index < count; index++)
        {
            var retainer = manager->GetRetainerBySortedIndex(index);
            if (retainer == null)
            {
                continue;
            }

            var target = marketExpiration ? retainer->MarketExpire : retainer->VentureComplete;
            var active = marketExpiration
                ? target > nowUnix
                : retainer->VentureId != 0 && target > nowUnix;
            if (!active || target <= 0)
            {
                continue;
            }

            matching++;
            earliest = Math.Min(earliest, target);
        }

        if (matching == 0 || earliest == long.MaxValue)
        {
            return default;
        }

        var source = marketExpiration
            ? GameTimerSource.RetainerMarketExpiration
            : GameTimerSource.RetainerVenture;
        return FromLiveTarget(
            source,
            now,
            earliest,
            marketExpiration ? "Next listing expires" : "Next venture completes",
            marketExpiration
                ? $"Retainer market listings · {matching} loaded expiration{(matching == 1 ? string.Empty : "s")}"
                : $"Retainer ventures · {matching} active");
    }

    private static unsafe GameTimerSnapshot GetIslandGranarySnapshot(DateTimeOffset now)
    {
        var manager = MJIManager.Instance();
        if (manager == null || manager->GranariesState == null)
        {
            return default;
        }

        var nowUnix = now.ToUnixTimeSeconds();
        long earliest = long.MaxValue;
        var activeCount = 0;
        var granaries = (MJIGranaryState*)manager->GranariesState;
        for (var index = 0; index < MJIGranariesState.MaxGranaries; index++)
        {
            var granary = granaries[index];
            if (granary.ActiveExpeditionId == 0 || granary.FinishTime <= nowUnix)
            {
                continue;
            }

            activeCount++;
            earliest = Math.Min(earliest, granary.FinishTime);
        }

        if (activeCount == 0 || earliest == long.MaxValue)
        {
            return default;
        }

        return FromLiveTarget(
            GameTimerSource.IslandGranary,
            now,
            earliest,
            "Next expedition completes",
            $"Island granaries · {activeCount} active expedition{(activeCount == 1 ? string.Empty : "s")}");
    }

    private static unsafe GameTimerSnapshot GetWorkshopVoyageSnapshot(
        DateTimeOffset now,
        bool submersible)
    {
        var housing = HousingManager.Instance();
        var workshop = housing == null ? null : housing->WorkshopTerritory;
        if (workshop == null)
        {
            return default;
        }

        var nowUnix = now.ToUnixTimeSeconds();
        long earliest = long.MaxValue;
        var activeCount = 0;
        if (submersible)
        {
            var voyages = (HousingWorkshopSubmersibleSubData*)&workshop->Submersible;
            for (var index = 0; index < 4; index++)
            {
                if (voyages[index].RegisterTime == 0 || voyages[index].ReturnTime <= nowUnix)
                {
                    continue;
                }

                activeCount++;
                earliest = Math.Min(earliest, voyages[index].ReturnTime);
            }
        }
        else
        {
            var voyages = (HousingWorkshopAirshipSubData*)&workshop->Airship;
            for (var index = 0; index < 4; index++)
            {
                if (voyages[index].RegisterTime == 0 || voyages[index].ReturnTime <= nowUnix)
                {
                    continue;
                }

                activeCount++;
                earliest = Math.Min(earliest, voyages[index].ReturnTime);
            }
        }

        if (activeCount == 0 || earliest == long.MaxValue)
        {
            return default;
        }

        var source = submersible
            ? GameTimerSource.FreeCompanySubmersible
            : GameTimerSource.FreeCompanyAirship;
        var vesselName = submersible ? "submersible" : "airship";
        return FromLiveTarget(
            source,
            now,
            earliest,
            "Next voyage returns",
            $"Free Company {vesselName}s · {activeCount} active voyage{(activeCount == 1 ? string.Empty : "s")}");
    }

    private static GameTimerSnapshot FromLiveTarget(
        GameTimerSource source,
        DateTimeOffset now,
        long targetUnixSeconds,
        string phase,
        string note)
    {
        if (targetUnixSeconds <= now.ToUnixTimeSeconds())
        {
            return default;
        }

        var target = DateTimeOffset.FromUnixTimeSeconds(targetUnixSeconds);
        return Snapshot(source, phase, note, now, target);
    }

    private static GameTimerSnapshot GetHousingLotterySnapshot(DateTimeOffset now)
    {
        var entrySeconds = (long)HousingEntryDuration.TotalSeconds;
        var cycleSeconds = entrySeconds + (long)HousingResultsDuration.TotalSeconds;
        var elapsedSeconds = now.ToUnixTimeSeconds() - HousingScheduleStartUtc.ToUnixTimeSeconds();

        var cycleIndex = Math.DivRem(elapsedSeconds, cycleSeconds, out var cycleOffset);
        if (cycleOffset < 0)
        {
            cycleOffset += cycleSeconds;
            cycleIndex--;
        }

        var cycleStart = HousingScheduleStartUtc.AddSeconds(cycleIndex * cycleSeconds);
        if (cycleOffset < entrySeconds)
        {
            return Snapshot(
                GameTimerSource.HousingLottery,
                "Entry period ends",
                "Housing Lottery · Entry period",
                cycleStart,
                cycleStart.Add(HousingEntryDuration));
        }

        return Snapshot(
            GameTimerSource.HousingLottery,
            "Results period ends",
            "Housing Lottery · Results period",
            cycleStart.Add(HousingEntryDuration),
            cycleStart.AddSeconds(cycleSeconds));
    }

    private static GameTimerSnapshot Snapshot(
        GameTimerSource source,
        string phase,
        string note,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return new GameTimerSnapshot(
            source,
            GetSourceName(source),
            phase,
            note,
            start.ToUnixTimeSeconds(),
            end.ToUnixTimeSeconds());
    }
}
