using System.Text;

namespace KGV.Core.Calendar;

public static class IcsCalendarDateiname
{
    public static string Build(string? title, DateTime date)
        => $"{SanitizeTitle(title)}_{date:yyyy-MM-dd}.ics";

    private static string SanitizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Termin";

        var builder = new StringBuilder(title.Length);
        var pendingSeparator = false;
        foreach (var character in title.Trim())
        {
            if (char.IsWhiteSpace(character) || character is '_' or '.')
            {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (char.IsControl(character) || character is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
            {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (pendingSeparator)
            {
                builder.Append('_');
                pendingSeparator = false;
            }

            builder.Append(character);
        }

        return builder.Length == 0 ? "Termin" : builder.ToString();
    }
}
