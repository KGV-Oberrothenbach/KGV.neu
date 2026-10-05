using System;

namespace KGV.Core.Utilities;

public static class Vereinszeit
{
    private static readonly TimeZoneInfo BerlinTimeZone = FindBerlinTimeZone();

    public static DateTime Now
    {
        get
        {
            var berlinNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BerlinTimeZone);
            return DateTime.SpecifyKind(berlinNow, DateTimeKind.Unspecified);
        }
    }

    public static DateTime NowToMinute()
    {
        var now = Now;
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo FindBerlinTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
