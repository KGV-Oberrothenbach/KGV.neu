using System;

namespace KGV.Core.Models
{
    public static class OperationalDataFilter
    {
        public static bool IsOperationalMember(MitgliedRecord? member)
        {
            if (member == null)
                return false;

            return !ContainsMarker(member.Vorname)
                && !ContainsMarker(member.Name)
                && !ContainsMarker(member.Email);
        }

        private static bool ContainsMarker(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();
            return normalized.Contains("demo", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("test", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("play store", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("playstore", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("example.com", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("example.org", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("example.net", StringComparison.OrdinalIgnoreCase);
        }
    }
}
