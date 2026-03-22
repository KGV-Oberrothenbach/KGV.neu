using System;
using System.Linq;

namespace KGV.Core.Utilities;

public static class TemporalInputParser
{
    public static bool TryNormalizeTimeText(string? input, out string normalizedText, out TimeSpan? value)
    {
        normalizedText = string.Empty;
        value = null;

        if (string.IsNullOrWhiteSpace(input))
            return true;

        var text = input.Trim().Replace('.', ':');
        int hours;
        int minutes;

        if (text.Contains(':'))
        {
            var parts = text.Split(':');
            if (parts.Length != 2
                || string.IsNullOrWhiteSpace(parts[0])
                || string.IsNullOrWhiteSpace(parts[1])
                || !int.TryParse(parts[0], out hours)
                || !int.TryParse(parts[1], out minutes))
                return false;
        }
        else
        {
            if (!text.All(char.IsDigit))
                return false;

            switch (text.Length)
            {
                case 1:
                case 2:
                    if (!int.TryParse(text, out hours))
                        return false;
                    minutes = 0;
                    break;
                case 3:
                    if (!int.TryParse(text[..1], out hours) || !int.TryParse(text[1..], out minutes))
                        return false;
                    break;
                case 4:
                    if (!int.TryParse(text[..2], out hours) || !int.TryParse(text[2..], out minutes))
                        return false;
                    break;
                default:
                    return false;
            }
        }

        if (hours is < 0 or > 23 || minutes is < 0 or > 59)
            return false;

        value = new TimeSpan(hours, minutes, 0);
        normalizedText = value.Value.ToString(@"hh\:mm");
        return true;
    }
}
