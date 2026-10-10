using System.Text;

namespace KGV.Core.Calendar;

public static class IcsCalendarBuilder
{
    public static string Build(CalendarEventData item, DateTime? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//KGV Oberrothenbach//KGV Software//DE", "CALSCALE:GREGORIAN", "METHOD:PUBLISH" };
        if (item.StartTime.HasValue)
            lines.AddRange(TimezoneLines);
        lines.Add("BEGIN:VEVENT");
        lines.Add($"UID:{Escape(item.Uid)}");
        lines.Add($"DTSTAMP:{(utcNow ?? DateTime.UtcNow).ToUniversalTime():yyyyMMdd'T'HHmmss'Z'}");
        lines.Add($"SUMMARY:{Escape(item.Title)}");
        if (!string.IsNullOrWhiteSpace(item.Description)) lines.Add($"DESCRIPTION:{Escape(item.Description)}");
        if (!string.IsNullOrWhiteSpace(item.Location)) lines.Add($"LOCATION:{Escape(item.Location)}");
        if (item.StartTime.HasValue)
        {
            lines.Add($"DTSTART;TZID=Europe/Berlin:{item.Date.Date.Add(item.StartTime.Value):yyyyMMdd'T'HHmmss}");
            if (item.EndTime.HasValue) lines.Add($"DTEND;TZID=Europe/Berlin:{item.Date.Date.Add(item.EndTime.Value):yyyyMMdd'T'HHmmss}");
        }
        else
        {
            lines.Add($"DTSTART;VALUE=DATE:{item.Date:yyyyMMdd}");
            lines.Add($"DTEND;VALUE=DATE:{item.Date.AddDays(1):yyyyMMdd}");
        }
        lines.Add("END:VEVENT"); lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines) + "\r\n";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
    private static readonly string[] TimezoneLines = ["BEGIN:VTIMEZONE", "TZID:Europe/Berlin", "X-LIC-LOCATION:Europe/Berlin", "BEGIN:DAYLIGHT", "TZOFFSETFROM:+0100", "TZOFFSETTO:+0200", "TZNAME:CEST", "DTSTART:19700329T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU", "END:DAYLIGHT", "BEGIN:STANDARD", "TZOFFSETFROM:+0200", "TZOFFSETTO:+0100", "TZNAME:CET", "DTSTART:19701025T030000", "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU", "END:STANDARD", "END:VTIMEZONE"];
}
