using System;

namespace KGV.Core.Models
{
    public static class OperationalDataFilter
    {
        public static bool IsOperationalMember(MitgliedRecord? member)
        {
            if (member == null)
                return false;
        // New behavior: rely solely on explicit DB flag is_demo.
        // Treat nullable/absent as NOT demo (i.e., visible) - property is non-nullable bool in models.
        return !member.IsDemo;
        }

        public static bool IsOperationalAppUser(MitgliedRecord? member, string? displayName, string? email)
        {
            if (member != null)
                return IsOperationalMember(member);

            return !ContainsMarker(displayName)
                && !ContainsMarker(email);
        }

        public static bool IsOperationalImpressumKontakt(ImpressumKontaktItem? item)
        {
            if (item == null)
                return false;

            return !ContainsMarker(item.Name)
                && !ContainsMarker(item.Email)
                && !ContainsMarker(item.Telefon)
                && !ContainsMarker(item.Handy)
                && !ContainsMarker(item.Adresse);
        }

        public static bool IsOperationalArbeitseinsatz(ArbeitseinsatzRecord? item)
        {
            if (item == null)
                return false;
        // Rely on explicit is_demo flag only
        return !item.IsDemo;
        }

        public static bool IsOperationalTermin(TerminRecord? item)
        {
            if (item == null)
                return false;
        // Rely on explicit is_demo flag only
        return !item.IsDemo;
        }

        public static bool IsOperationalBekanntmachung(BekanntmachungRecord? item)
        {
            if (item == null)
                return false;
        // Rely on explicit is_demo flag only
        return !item.IsDemo;
        }

        public static bool IsOperationalText(string? value)
            => !ContainsMarker(value);

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