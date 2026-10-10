using KGV.Core.Calendar;
using Xunit;

namespace KGV.Tests;

public sealed class IcsCalendarBuilderTests
{
    private static readonly DateTime Stamp = new(2026, 10, 10, 9, 50, 0, DateTimeKind.Utc);
    private static CalendarEventData Event(TimeSpan? start = null, TimeSpan? end = null) => new() { Uid = "termin-123@kgv-oberrothenbach", Title = "Grüße,;\\", Description = "Zeile 1\r\nZeile 2", Date = new DateTime(2026, 10, 10), StartTime = start, EndTime = end };

    [Fact] public void AllDay_UsesExclusiveNextDay() { var ics = IcsCalendarBuilder.Build(Event(), Stamp); Assert.Contains("DTSTART;VALUE=DATE:20261010", ics); Assert.Contains("DTEND;VALUE=DATE:20261011", ics); Assert.DoesNotContain("VTIMEZONE", ics); }
    [Fact] public void Timed_UsesBerlinTimezone() { var ics = IcsCalendarBuilder.Build(Event(new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)), Stamp); Assert.Contains("DTSTART;TZID=Europe/Berlin:20261010T090000", ics); Assert.Contains("DTEND;TZID=Europe/Berlin:20261010T110000", ics); Assert.Contains("TZID:Europe/Berlin", ics); }
    [Fact] public void StartOnly_OmitsEnd() { var ics = IcsCalendarBuilder.Build(Event(new TimeSpan(9, 0, 0)), Stamp); Assert.Contains("DTSTART;TZID=Europe/Berlin:20261010T090000", ics); Assert.DoesNotContain("DTEND;TZID", ics); }
    [Fact] public void EndOnly_IsAllDay() { var ics = IcsCalendarBuilder.Build(Event(null, new TimeSpan(11, 0, 0)), Stamp); Assert.Contains("DTSTART;VALUE=DATE:20261010", ics); Assert.DoesNotContain("DTEND;TZID", ics); }
    [Fact] public void EscapesText_AndKeepsUmlauts() { var ics = IcsCalendarBuilder.Build(Event(), Stamp); Assert.Contains("SUMMARY:Grüße\\,\\;\\\\", ics); Assert.Contains("DESCRIPTION:Zeile 1\\nZeile 2", ics); }
    [Fact] public void Uid_IsStable() { Assert.Contains("UID:termin-123@kgv-oberrothenbach", IcsCalendarBuilder.Build(Event(), Stamp)); }
    [Fact] public void EscapesLocation_AndUsesProvidedStamp() { var ics = IcsCalendarBuilder.Build(new CalendarEventData { Uid = "id", Title = "Titel", Date = new DateTime(2026, 10, 10), Location = "Vereinshaus, Eingang; Nord\\West" }, Stamp); Assert.Contains("LOCATION:Vereinshaus\\, Eingang\\; Nord\\\\West", ics); Assert.Contains("DTSTAMP:20261010T095000Z", ics); }
}
