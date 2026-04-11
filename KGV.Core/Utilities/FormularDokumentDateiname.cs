using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class FormularDokumentDateiname
    {
        private static readonly Regex DateinameRegex = new(
            @"^.+-\d+-\d{4}-\d{2}-\d{2}-(?<typ>[a-z0-9]+)-(?<status>[a-z0-9]+)\.pdf$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string BuildMitgliedDateiname(MitgliedRecord member, string dokumenttyp, string status, DateTime? referenceDate = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            var normalizedType = FormularDokumentTyp.Normalize(dokumenttyp);
            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var effectiveDate = (referenceDate ?? DateTime.Today).Date;
            var memberSegment = BuildMemberSegment(member.Name, member.Vorname);

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{memberSegment}-{member.Id}-{effectiveDate:yyyy-MM-dd}-{normalizedType}-{normalizedStatus}.pdf");
        }

        public static string BuildTitel(string dokumenttyp, string status)
        {
            return $"{FormularDokumentTyp.ToDisplayName(dokumenttyp)} ({FormularDokumentStatus.ToDisplayName(status)})";
        }

        public static bool TryParse(string? value, out string dokumenttyp, out string status)
        {
            dokumenttyp = string.Empty;
            status = string.Empty;

            var candidate = ExtractFileName(value);
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            var match = DateinameRegex.Match(candidate);
            if (!match.Success)
                return false;

            dokumenttyp = FormularDokumentTyp.Normalize(match.Groups["typ"].Value);
            status = FormularDokumentStatus.Normalize(match.Groups["status"].Value);
            return !string.IsNullOrWhiteSpace(dokumenttyp) && !string.IsNullOrWhiteSpace(status);
        }

        private static string ExtractFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim().Replace('\\', '/');
            return Path.GetFileName(trimmed);
        }

        private static string BuildMemberSegment(string? nachname, string? vorname)
        {
            var lastName = SanitizeSegment(nachname, "Mitglied");
            var firstName = SanitizeSegment(vorname, "OhneVorname");
            return $"{lastName}_{firstName}";
        }

        private static string SanitizeSegment(string? value, string fallback)
        {
            var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            var normalized = ReplaceGermanUmlauts(source).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    continue;
                }

                if (character is ' ' or '-' or '_' or '.')
                    builder.Append('_');
            }

            var sanitized = Regex.Replace(builder.ToString(), "_+", "_").Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private static string ReplaceGermanUmlauts(string value)
        {
            return value
                .Replace("Ä", "Ae", StringComparison.Ordinal)
                .Replace("Ö", "Oe", StringComparison.Ordinal)
                .Replace("Ü", "Ue", StringComparison.Ordinal)
                .Replace("ä", "ae", StringComparison.Ordinal)
                .Replace("ö", "oe", StringComparison.Ordinal)
                .Replace("ü", "ue", StringComparison.Ordinal)
                .Replace("ß", "ss", StringComparison.Ordinal);
        }
    }
}
