using System;

namespace KGV.Core.Models
{
    public static class OperationalDataFilter
    {
        public static bool IsOperationalMember(MitgliedRecord? member)
        {
            if (member == null)
                return false;
            return string.Equals(member.AufnahmeStatus, "aufgenommen", StringComparison.Ordinal);
        }

        public static bool IsOperationalAppUser(MitgliedRecord? member, string? displayName, string? email)
        {
            if (member != null)
                return IsOperationalMember(member);

            return true;
        }

        public static bool IsOperationalImpressumKontakt(ImpressumKontaktItem? item)
        {
            if (item == null)
                return false;

            return true;
        }

        public static bool IsOperationalArbeitseinsatz(ArbeitseinsatzRecord? item)
        {
            if (item == null)
                return false;
            return true;
        }

        public static bool IsOperationalTermin(TerminRecord? item)
        {
            if (item == null)
                return false;
            return true;
        }

        public static bool IsOperationalBekanntmachung(BekanntmachungRecord? item)
        {
            if (item == null)
                return false;
            return true;
        }

        public static bool IsOperationalText(string? value) => true;
    }
}
