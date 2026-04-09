using System;
using System.Collections.Generic;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class MitgliedsvertragDokumentFactory
    {
        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, string? status = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (member.Id <= 0)
                throw new InvalidOperationException("Bitte zuerst ein gültiges Mitglied auswählen.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Mitgliedsvertrag;
            var effectiveDate = member.MitgliedSeit?.Date ?? DateTime.Today;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, normalizedStatus, effectiveDate);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);
            var content = VereinsdokumentPdfBuilder.BuildDocument(
                title,
                "Mitgliedsvertrag",
                FormularDokumentStatus.ToDisplayName(normalizedStatus),
                effectiveDate,
                BuildSections(member, effectiveDate),
                ["Ort, Datum", "Unterschrift Mitglied", "Unterschrift Verein"],
                "Dieser Mitgliedsvertrag wird im bestehenden Dokumentpfad des Mitglieds als Vereins-PDF erzeugt und dokumentiert die aktuell im System hinterlegten Stammdaten.");

            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = content
            };
        }

        private static IReadOnlyCollection<VereinsdokumentAbschnitt> BuildSections(MitgliedRecord member, DateTime effectiveDate)
        {
            var person = new List<string>
            {
                $"Mitglied-ID: {member.Id}",
                $"Vorname: {Safe(member.Vorname)}",
                $"Nachname: {Safe(member.Name)}"
            };

            if (member.Geburtsdatum.HasValue)
                person.Add($"Geburtsdatum: {FormatDate(member.Geburtsdatum)}");

            var adresse = new List<string>
            {
                $"Straße / Hausnummer: {Safe(member.Adresse)}",
                $"PLZ / Ort: {BuildPostalLine(member)}"
            };

            var kontakt = new List<string>();
            AddIfPresent(kontakt, "E-Mail", member.Email);
            AddIfPresent(kontakt, "Telefon", member.Telefon);
            AddIfPresent(kontakt, "Mobilnummer", member.Handy);
            AddIfPresent(kontakt, "WhatsApp-Einwilligung", member.WhatsappEinwilligung ? "Ja" : null);

            var mitgliedschaft = new List<string>
            {
                $"Mitgliedskontext: {BuildMitgliedskontext(member)}",
                $"Vertrags-/Eintrittsdatum: {FormatDate(member.MitgliedSeit ?? effectiveDate)}"
            };

            if (!string.IsNullOrWhiteSpace(member.ArbeitsstundenAltersregelTyp) && !string.Equals(member.ArbeitsstundenAltersregelTyp, "keine", StringComparison.OrdinalIgnoreCase))
                mitgliedschaft.Add($"Arbeitsstunden-Altersregel: {member.ArbeitsstundenAltersregelTyp.Trim()}");

            return new[]
            {
                new VereinsdokumentAbschnitt("Mitglied", person),
                new VereinsdokumentAbschnitt("Adresse", adresse),
                new VereinsdokumentAbschnitt("Kontakt", kontakt.DefaultIfEmpty("Keine weiteren Kontaktangaben hinterlegt.")),
                new VereinsdokumentAbschnitt("Mitgliedschaft / Vertrag", mitgliedschaft),
                new VereinsdokumentAbschnitt("Hinweis", ["Der Vertrag wird zunächst als unsigniertes Dokument im bestehenden Dokumentpfad des Mitglieds abgelegt."])
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

        private static string Safe(string? value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}