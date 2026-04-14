using System;
using System.Collections.Generic;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public sealed record MitgliedsantragBeitragVorschlag(
        int SaisonJahr,
        DateTime BeginnDatum,
        decimal Jahresbeitrag,
        decimal VorgeschlagenerBeitrag,
        bool IstHalberBeitrag,
        bool IstNebenmitglied);

    public static class MitgliedsantragBeitragHelper
    {
        public static MitgliedsantragBeitragVorschlag CreateSuggestion(MitgliedRecord member, IEnumerable<SaisonRecord>? saisons, DateTime? today = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            var aktuellesDatum = (today ?? DateTime.Today).Date;
            var saisonJahr = aktuellesDatum.Year;
            var saison = (saisons ?? Array.Empty<SaisonRecord>())
                .FirstOrDefault(x => SaisonverwaltungHelper.GetSaisonJahr(x) == saisonJahr);

            if (saison == null)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} fehlt der Mitgliedsbeitrag.");

            var istNebenmitglied = member.HauptmitgliedId.HasValue && member.HauptmitgliedId.Value > 0;
            var jahresbeitragRaw = istNebenmitglied
                ? saison.MitgliedsbeitragNebenmitglied
                : saison.Mitgliedsbeitrag;
            var beitragColumnName = istNebenmitglied ? "mitgliedsbeitrag_nebenmitglied" : "mitgliedsbeitrag";
            if (!jahresbeitragRaw.HasValue)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} fehlt {beitragColumnName}.");
            if (jahresbeitragRaw.Value < 0m)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} ist {beitragColumnName} ungültig.");

            var beginnDatum = (member.MitgliedSeit ?? aktuellesDatum).Date;
            var jahresbeitrag = NormalizeBeitrag(jahresbeitragRaw.Value);
            // Monatsgenaue Anteilsberechnung: für das Aufnahmejahr werden 1/12 des Jahresbeitrags
            // je angefangenem Restmonat des laufenden Jahres berechnet (inkl. Eintrittsmonat).
            decimal vorgeschlagenerBeitrag;
            bool istHalberBeitrag = false;
            if (beginnDatum.Year == saisonJahr)
            {
                var monate = 12 - beginnDatum.Month + 1; // Einschließlich Eintrittsmonat
                if (monate < 1) monate = 1;
                vorgeschlagenerBeitrag = NormalizeBeitrag(jahresbeitrag * MonateToFraction(monate));
                // historisch bedingtes Flag: setze true wenn nur noch die zweite Jahreshälfte betroffen (ältere UI)
                istHalberBeitrag = monate <= 6 && beginnDatum >= new DateTime(saisonJahr, 7, 1);
            }
            else
            {
                // Bei Beginn in anderem Jahr: vollen Jahresbeitrag vorschlagen
                vorgeschlagenerBeitrag = jahresbeitrag;
            }

            return new MitgliedsantragBeitragVorschlag(
                saisonJahr,
                beginnDatum,
                jahresbeitrag,
                vorgeschlagenerBeitrag,
                istHalberBeitrag,
                istNebenmitglied);
        }

        public static decimal NormalizeBeitrag(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static decimal MonateToFraction(int monate)
            => Math.Round((decimal)monate / 12m, 8);
    }
}
