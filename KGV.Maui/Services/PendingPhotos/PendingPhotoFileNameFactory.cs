using System.Globalization;
using System.Text;

namespace KGV.Maui.Services.PendingPhotos;

public static class PendingPhotoFileNameFactory
{
    public static string Create(string operationType, string parzelle, string medium, DateTimeOffset? now = null)
    {
        var clock = now ?? DateTimeOffset.Now;

        var datePart = clock.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var timePart = clock.ToString("HH-mm-ss", CultureInfo.InvariantCulture);

        var parzellePart = SanitizeToken(parzelle);
        var mediumPart = SanitizeToken(medium);

        var prefix = string.IsNullOrWhiteSpace(operationType) ? string.Empty : SanitizeToken(operationType) + "_";
        return $"{prefix}{datePart}_{timePart}_{parzellePart}_{mediumPart}.jpg";
    }

    private static string SanitizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "-";

        value = value.Trim();
        var sb = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (c is '-' or '_')
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '/' || c == '\\')
                sb.Append('-');
        }

        var sanitized = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "-" : sanitized;
    }
}
