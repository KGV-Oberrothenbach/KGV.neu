using System;
using System.Collections.Generic;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class SaisonverwaltungHelper
    {
        public static int GetSaisonJahr(SaisonRecord? saison)
        {
            if (saison == null)
                return DateTime.Today.Year;

            return saison.Jahr > 0 ? saison.Jahr : saison.Id;
        }

        public static bool IsEditable(SaisonRecord? saison, DateTime? today = null)
        {
            if (saison == null)
                return false;

            var currentYear = (today ?? DateTime.Today).Year;
            return GetSaisonJahr(saison) >= currentYear;
        }

        public static SaisonRecord CreateNextSaisonProposal(IEnumerable<SaisonRecord>? saisons, DateTime? today = null)
        {
            var currentYear = (today ?? DateTime.Today).Year;
            var previous = (saisons ?? Array.Empty<SaisonRecord>())
                .OrderByDescending(GetSaisonJahr)
                .FirstOrDefault();

            var nextYear = previous != null
                ? Math.Max(GetSaisonJahr(previous) + 1, currentYear)
                : currentYear;

            return new SaisonRecord
            {
                Id = nextYear,
                Jahr = nextYear,
                PflichtstundenSoll = previous?.PflichtstundenSoll ?? 0m,
                EuroProFehlstunde = previous?.EuroProFehlstunde ?? 25m,
                PachtProQm = previous?.PachtProQm,
                Mitgliedsbeitrag = previous?.Mitgliedsbeitrag,
                MitgliedsbeitragNebenmitglied = previous?.MitgliedsbeitragNebenmitglied,
                Aufnahmegebuehr = previous?.Aufnahmegebuehr,
                GebuehrBauantrag = previous?.GebuehrBauantrag,
                Bemerkung = previous?.Bemerkung
            };
        }

        public static SaisonRecord NormalizeForSave(SaisonRecord saison)
        {
            if (saison == null)
                throw new ArgumentNullException(nameof(saison));

            var jahr = GetSaisonJahr(saison);
            if (jahr < 1900 || jahr > 3000)
                throw new InvalidOperationException("Bitte ein gültiges Saisonjahr angeben.");

            return new SaisonRecord
            {
                Id = jahr,
                Jahr = jahr,
                PflichtstundenSoll = saison.PflichtstundenSoll,
                EuroProFehlstunde = saison.EuroProFehlstunde,
                PachtProQm = saison.PachtProQm,
                Mitgliedsbeitrag = saison.Mitgliedsbeitrag,
                MitgliedsbeitragNebenmitglied = saison.MitgliedsbeitragNebenmitglied,
                Aufnahmegebuehr = saison.Aufnahmegebuehr,
                GebuehrBauantrag = saison.GebuehrBauantrag,
                Bemerkung = string.IsNullOrWhiteSpace(saison.Bemerkung) ? null : saison.Bemerkung.Trim()
            };
        }
    }
}