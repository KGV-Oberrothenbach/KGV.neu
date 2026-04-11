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
        bool IstHalberBeitrag);

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
            if (!saison.Mitgliedsbeitrag.HasValue)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} fehlt mitgliedsbeitrag.");
            if (saison.Mitgliedsbeitrag.Value < 0m)
                throw new InvalidOperationException($"Für die Saison {saisonJahr} ist mitgliedsbeitrag ungültig.");

            var beginnDatum = (member.MitgliedSeit ?? aktuellesDatum).Date;
            var jahresbeitrag = NormalizeBeitrag(saison.Mitgliedsbeitrag.Value);
            var stichtag = new DateTime(saisonJahr, 7, 1);
            var istHalberBeitrag = beginnDatum.Year == saisonJahr && beginnDatum >= stichtag;
            var vorgeschlagenerBeitrag = istHalberBeitrag
                ? NormalizeBeitrag(jahresbeitrag / 2m)
                : jahresbeitrag;

            return new MitgliedsantragBeitragVorschlag(
                saisonJahr,
                beginnDatum,
                jahresbeitrag,
                vorgeschlagenerBeitrag,
                istHalberBeitrag);
        }

        public static decimal NormalizeBeitrag(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
