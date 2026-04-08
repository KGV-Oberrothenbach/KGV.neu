using System;

namespace KGV.Core.Models
{
    public static class OperationalDataFilter
    {
        public static bool IsOperationalMember(MitgliedRecord? member)
        {
            if (member == null)
                return false;

            return !member.IsDemo
                && !ContainsMarker(member.Vorname)
                && !ContainsMarker(member.Name)
                && !ContainsMarker(member.Email);
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

            return !item.IsDemo
                && !ContainsMarker(item.Titel)
                && !ContainsMarker(item.Beschreibung)
                && !ContainsMarker(item.Treffpunkt);
        }

        public static bool IsOperationalTermin(TerminRecord? item)
        {
            if (item == null)
                return false;

            return !item.IsDemo
                && !ContainsMarker(item.Titel)
                && !ContainsMarker(item.Beschreibung);
        }

        public static bool IsOperationalBekanntmachung(BekanntmachungRecord? item)
        {
            if (item == null)
                return false;

            return !item.IsDemo
                && !ContainsMarker(item.Titel)
                && !ContainsMarker(item.InhaltHtml);
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