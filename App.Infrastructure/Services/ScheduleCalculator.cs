using App.Domain.Entities;
using Quartz;
using TimeZoneConverter;

namespace App.Infrastructure.Services;

public static class ScheduleCalculator
{
    public static DateTimeOffset? GetNextOccurrenceUtc(Campaign campaign, DateTimeOffset fromUtc)
    {
        if (string.IsNullOrWhiteSpace(campaign.ScheduleCron))
        {
            return null;
        }

        var cron = new CronExpression(campaign.ScheduleCron)
        {
            TimeZone = ResolveTimeZone(campaign.ScheduleTimezone)
        };

        var next = cron.GetNextValidTimeAfter(fromUtc);
        return next?.ToUniversalTime();
    }

    public static bool ShouldCatchUp(Campaign campaign, DateTimeOffset publishAtUtc, DateTimeOffset nowUtc)
    {
        var overdueMinutes = (nowUtc - publishAtUtc).TotalMinutes;
        return overdueMinutes <= campaign.MissedCatchUpWithinMinutes;
    }

    public static TimeZoneInfo ResolveTimeZone(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TZConvert.GetTimeZoneInfo(timezoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}

