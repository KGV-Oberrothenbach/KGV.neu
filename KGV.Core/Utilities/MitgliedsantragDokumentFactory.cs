using System;
using System.Collections.Generic;
using System.Linq;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class MitgliedsantragDokumentFactory
    {
        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, beginnDatum, null, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, beginnDatum, gesetzlicherVertreterSnapshot, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot, string? status = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (mitgliedsbeitrag < 0m)
                throw new InvalidOperationException("Der Mitgliedsbeitrag darf nicht negativ sein.");
            if (bankverbindungSnapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration mit vollständigen Bankdaten hinterlegt.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Mitgliedsantrag;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, normalizedStatus, DateTime.Today);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);
            var content = VereinsdokumentPdfBuilder.BuildDocument(
                title,
                "Mitgliedsantrag",
                FormularDokumentStatus.ToDisplayName(normalizedStatus),
                DateTime.Today,
                BuildSections(member, mitgliedsbeitrag, beginnDatum.Date, gesetzlicherVertreterSnapshot, bankverbindungSnapshot),
                BuildSignatures(gesetzlicherVertreterSnapshot),
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

        private static IReadOnlyCollection<string> BuildSignatures(MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot)
        {
            return gesetzlicherVertreterSnapshot == null
                ? new[] { "Ort, Datum", "Unterschrift Antragsteller/in", "Unterschrift Verein" }
                : new[] { "Ort, Datum", "Unterschrift Antragsteller/in", "Unterschrift gesetzliche/r Vertreter/in", "Unterschrift Verein" };
        }

        private static IReadOnlyCollection<VereinsdokumentAbschnitt> BuildSections(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            var beitragsjahr = beginnDatum.Year;
            var antragsteller = new List<string>
            {
                $"Mitglied-ID: {member.Id}",
                $"Name / Vorname: {BuildNameLine(member.Name, member.Vorname)}",
                $"Geburtsdatum: {FormatDate(member.Geburtsdatum)}"
            };

            var adresse = new List<string>
            {
                $"Anschrift: {BuildAddressLine(member.Adresse, member.Plz, member.Ort)}"
            };

            var kontakt = new List<string>();
            AddIfPresent(kontakt, "Kontakt", BuildContactLine(member.Telefon, member.Handy, member.Email));
            AddIfPresent(kontakt, "WhatsApp-Einwilligung", member.WhatsappEinwilligung ? "Ja" : string.Empty);

            var kontext = new List<string>
            {
                $"Mitgliedskontext: {BuildMitgliedskontext(member)}",
                $"Aufnahme ab: {FormatDate(beginnDatum)}"
            };

            var beitrag = new List<string>
            {
                $"Jährlicher Beitrag: {FormatCurrency(mitgliedsbeitrag)}",
                $"Beitrag {beitragsjahr}: Der Mitgliedsbeitrag ist jährlich zu zahlen. Für dieses Jahr ist ein Beitrag von {FormatCurrency(mitgliedsbeitrag)} zu zahlen."
            };

            var bankverbindung = new List<string>
            {
                $"Kontoinhaber: {Safe(bankverbindungSnapshot.Kontoinhaber)}",
                $"Bank: {Safe(bankverbindungSnapshot.Bankname)}",
                $"IBAN: {Safe(bankverbindungSnapshot.Iban)}",
                $"BIC: {Safe(bankverbindungSnapshot.Bic)}"
            };

            var abschnitte = new List<VereinsdokumentAbschnitt>
            {
                new("Antragsteller/in", antragsteller),
                new("Adresse", adresse),
                new("Kontakt", kontakt.DefaultIfEmpty("Keine weiteren Kontaktangaben hinterlegt.")),
                new("Mitgliedsart / Kontext", kontext),
                new("Beitrag / Zahlung", beitrag),
                new("Bankverbindung Verein", bankverbindung)
            };

            if (gesetzlicherVertreterSnapshot != null)
            {
                abschnitte.Add(new VereinsdokumentAbschnitt("Gesetzliche/r Vertreter/in", BuildVertreterLines(gesetzlicherVertreterSnapshot)));
            }

            abschnitte.Add(new VereinsdokumentAbschnitt("Hinweis", ["Dieser Antrag wird im bestehenden Dokumentpfad des Mitglieds als PDF-Vereinsvorlage abgelegt."]));
            return abschnitte;
        }

        private static IReadOnlyCollection<string> BuildVertreterLines(MitgliedsantragVertreterSnapshot snapshot)
        {
            var lines = new List<string>
            {
                $"Name / Vorname: {BuildNameLine(snapshot.Nachname, snapshot.Vorname)}"
            };

            var anschrift = BuildAddressLine(snapshot.Adresse, snapshot.Plz, snapshot.Ort);
            if (!string.Equals(anschrift, "-", StringComparison.Ordinal))
                lines.Add($"Anschrift: {anschrift}");

            var kontakt = BuildContactLine(snapshot.Telefon, snapshot.Handy, snapshot.Email);
            if (!string.Equals(kontakt, "-", StringComparison.Ordinal))
                lines.Add($"Kontakt: {kontakt}");

            return lines;
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

        private static string BuildAddressLine(string? adresse, string? plz, string? ort)
        {
            var strasse = Safe(adresse);
            var ortLine = string.Join(" ", new[] { plz, ort }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));
            if (string.IsNullOrWhiteSpace(ortLine))
                return strasse;
            if (string.Equals(strasse, "-", StringComparison.Ordinal))
                return ortLine;
            return $"{strasse}, {ortLine}";
        }

        private static string BuildNameLine(string? nachname, string? vorname)
        {
            var parts = new[] { Safe(nachname), Safe(vorname) }
                .Where(x => !string.Equals(x, "-", StringComparison.Ordinal))
                .ToArray();
            return parts.Length == 0 ? "-" : string.Join(" / ", parts);
        }

        private static string BuildContactLine(string? telefon, string? handy, string? email)
        {
            var parts = new[] { telefon, handy, email }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToArray();
            return parts.Length == 0 ? "-" : string.Join(" / ", parts);
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
