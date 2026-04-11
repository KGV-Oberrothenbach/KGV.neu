using System;
using System.Collections.Generic;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class MitgliedsantragDokumentFactory
    {
        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, string? status = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (mitgliedsbeitrag < 0m)
                throw new InvalidOperationException("Der Mitgliedsbeitrag darf nicht negativ sein.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Mitgliedsantrag;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, normalizedStatus, DateTime.Today);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);
            var content = VereinsdokumentPdfBuilder.BuildDocument(
                title,
                "Mitgliedsantrag",
                FormularDokumentStatus.ToDisplayName(normalizedStatus),
                DateTime.Today,
                BuildSections(member, mitgliedsbeitrag, beginnDatum.Date),
                ["Ort, Datum", "Unterschrift Antragsteller/in", "Unterschrift Verein"],
                "Hiermit wird der Antrag auf Mitgliedschaft im Kleingartenverein in einer standardisierten Vereinsvorlage dokumentiert.");

            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = content
            };
        }

        private static IReadOnlyCollection<VereinsdokumentAbschnitt> BuildSections(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum)
        {
            var antragsteller = new List<string>
            {
                $"Mitglied-ID: {member.Id}",
                $"Vorname: {Safe(member.Vorname)}",
                $"Nachname: {Safe(member.Name)}",
                $"Geburtsdatum: {FormatDate(member.Geburtsdatum)}"
            };

            var adresse = new List<string>
            {
                $"Straße / Hausnummer: {Safe(member.Adresse)}",
                $"PLZ / Ort: {BuildPostalLine(member)}"
            };

            var kontakt = new List<string>();
            AddIfPresent(kontakt, "E-Mail", member.Email);
            AddIfPresent(kontakt, "Telefon", member.Telefon);
            AddIfPresent(kontakt, "Mobilnummer", member.Handy);
            AddIfPresent(kontakt, "WhatsApp-Einwilligung", member.WhatsappEinwilligung ? "Ja" : string.Empty);

            var kontext = new List<string>
            {
                $"Mitgliedskontext: {BuildMitgliedskontext(member)}",
                $"Mitglied seit: {FormatDate(beginnDatum)}",
                $"Mitgliedsbeitrag: {FormatCurrency(mitgliedsbeitrag)}"
            };

            if (!string.IsNullOrWhiteSpace(member.ArbeitsstundenAltersregelTyp) && !string.Equals(member.ArbeitsstundenAltersregelTyp, "keine", StringComparison.OrdinalIgnoreCase))
                kontext.Add($"Arbeitsstunden-Altersregel: {member.ArbeitsstundenAltersregelTyp.Trim()}");

            return new[]
            {
                new VereinsdokumentAbschnitt("Antragsteller/in", antragsteller),
                new VereinsdokumentAbschnitt("Adresse", adresse),
                new VereinsdokumentAbschnitt("Kontakt", kontakt.DefaultIfEmpty("Keine weiteren Kontaktangaben hinterlegt.")),
                new VereinsdokumentAbschnitt("Mitgliedsart / Kontext", kontext),
                new VereinsdokumentAbschnitt("Hinweis", ["Dieser Antrag wird im bestehenden Dokumentpfad des Mitglieds als PDF-Vereinsvorlage abgelegt."])
            };
        }

        private static void AddIfPresent(ICollection<string> lines, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            lines.Add($"{label}: {value.Trim()}");
        }

        private static string BuildPostalLine(MitgliedRecord member)
        {
            var line = string.Join(" ", new[] { member.Plz, member.Ort }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            return string.IsNullOrWhiteSpace(line) ? "-" : line;
        }

        private static string BuildMitgliedskontext(MitgliedRecord member)
        {
            if (member.HauptmitgliedId.HasValue && member.HauptmitgliedId.Value > 0)
                return $"Nebenmitglied zu Hauptmitglied #{member.HauptmitgliedId.Value}";

            return "Hauptmitglied";
        }

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd.MM.yyyy") : "-";

        private static string FormatCurrency(decimal value)
            => MitgliedsantragBeitragHelper.NormalizeBeitrag(value).ToString("0.00 €");

        private static string Safe(string? value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
