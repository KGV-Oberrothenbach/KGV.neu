using System.Text;
using System.Text.RegularExpressions;

namespace KGV.Core.Utilities;

public static partial class SichtbarerDateiname
{
    public static string BuildFoto(string? fotoart, string? garten, string? medium, DateTimeOffset erstelltAm)
        => $"{erstelltAm:yyyy-MM-dd}_{SanitizeToken(fotoart, "Foto")}_{SanitizeToken(garten, "Garten")}_{SanitizeToken(medium, "Medium")}.jpg";

    public static string BuildPdf(string? originalDateiname, string? dokumenttitel)
    {
        var original = SanitizePdfBase(Path.GetFileNameWithoutExtension(originalDateiname ?? string.Empty));
        var titel = SanitizePdfBase(Path.GetFileNameWithoutExtension(dokumenttitel ?? string.Empty));
        var baseName = IsTechnicalPdfCacheName(original) ? titel : original;
        baseName = string.IsNullOrWhiteSpace(baseName) ? titel : baseName;
        return string.IsNullOrWhiteSpace(baseName) ? "KGV_Dokument.pdf" : $"{baseName}.pdf";
    }

    private static string SanitizeToken(string? value, string fallback)
    {
        var token = Sanitize(value, fallback);
        return string.IsNullOrWhiteSpace(token) ? fallback : token;
    }

    private static string SanitizePdfBase(string value)
    {
        var withoutUuid = UuidPattern().Replace(value, string.Empty);
        return Sanitize(withoutUuid, string.Empty);
    }

    private static bool IsTechnicalPdfCacheName(string value)
        => string.Equals(value, "kgv_pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "kgv-pdf", StringComparison.OrdinalIgnoreCase);

    private static string Sanitize(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var builder = new StringBuilder(value.Length);
        var pendingSeparator = false;
        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                    builder.Append('_');
                builder.Append(character);
                pendingSeparator = false;
                continue;
            }

            if (character == '-')
            {
                if (pendingSeparator && builder.Length > 0)
                    builder.Append('_');
                builder.Append(character);
                pendingSeparator = false;
                continue;
            }

            pendingSeparator = builder.Length > 0;
        }

        return builder.Length == 0 ? fallback : builder.ToString();
    }

    [GeneratedRegex(@"(?i)(?<![0-9a-f])[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}(?![0-9a-f])|(?<![0-9a-f])[0-9a-f]{32}(?![0-9a-f])")]
    private static partial Regex UuidPattern();
}
