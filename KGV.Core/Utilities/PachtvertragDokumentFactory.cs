using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities
{
    public static class PachtvertragDokumentFactory
    {
        private const string TemplateResourceName = "KGV.Core.Templates.Pachtvertrag_KGV_bereinigt_mit_Feldern.pdf";
        private static readonly string[] RequiredTextFields =
        {
            "tenant_1_name",
            "tenant_1_birthdate",
            "tenant_2_name",
            "tenant_2_birthdate",
            "tenant_address",
            "tenant_phone",
            "parcel_number",
            "parcel_area_sqm",
            "contract_start_date",
            "rent_per_qm",
            "member_fee_display",
            "rent_display",
            "total_display",
            "sign_place",
            "sign_date"
        };

        private static readonly string[] RequiredSignatureFields =
        {
            "signature_landlord",
            "signature_tenant_primary",
            "signature_tenant_secondary",
            "signature_attachment_ack_primary",
            "signature_attachment_ack_secondary"
        };

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, string? status = null)
            => CreateUploadRequest(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn, null, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot, string? status = null)
        {
            if (hauptmitglied == null)
                throw new ArgumentNullException(nameof(hauptmitglied));
            if (parzelle == null)
                throw new ArgumentNullException(nameof(parzelle));
            if (saison == null)
                throw new ArgumentNullException(nameof(saison));
            if (hauptmitglied.Id <= 0)
                throw new InvalidOperationException("Bitte zuerst ein gültiges Mitglied auswählen.");
            if (parzelle.Id <= 0)
                throw new InvalidOperationException("Bitte zuerst eine gültige Parzelle auswählen.");
            if (!parzelle.FlaecheQm.HasValue || parzelle.FlaecheQm.Value <= 0)
                throw new InvalidOperationException("Für die Parzelle fehlt eine gültige Fläche in m².");
            if (!saison.PachtProQm.HasValue)
                throw new InvalidOperationException($"Für die Saison {saison.Jahr} fehlt pacht_pro_qm.");
            if (saison.PachtProQm.Value <= 0)
                throw new InvalidOperationException($"Für die Saison {saison.Jahr} ist pacht_pro_qm ungültig.");
            if (!saison.Mitgliedsbeitrag.HasValue)
                throw new InvalidOperationException($"Für die Saison {saison.Jahr} fehlt mitgliedsbeitrag.");
            if (bankverbindungSnapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration mit vollständigen Bankdaten hinterlegt.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Pachtvertrag;
            var pachtzins = decimal.Round(parzelle.FlaecheQm.Value * saison.PachtProQm.Value, 2, MidpointRounding.AwayFromZero);
            var mitgliedsbeitrag = decimal.Round(saison.Mitgliedsbeitrag.Value, 2, MidpointRounding.AwayFromZero);
            var gesamtbetrag = decimal.Round(pachtzins + mitgliedsbeitrag, 2, MidpointRounding.AwayFromZero);
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(hauptmitglied, dokumenttyp, normalizedStatus, vertragsbeginn.Date);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);

            return new DokumentUploadRequest
            {
                MitgliedId = hauptmitglied.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = BuildPdf(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn.Date, pachtzins, mitgliedsbeitrag, gesamtbetrag, gesetzlicherVertreterSnapshot, bankverbindungSnapshot)
            };
        }

        private static byte[] BuildPdf(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, decimal pachtzins, decimal mitgliedsbeitrag, decimal gesamtbetrag, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            PdfSharpFontResolverInitializer.EnsureInitialized();

            using var templateStream = OpenTemplateStream();
            using var input = new MemoryStream();
            templateStream.CopyTo(input);
            input.Position = 0;

            var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
            var form = document.AcroForm ?? throw new InvalidOperationException("Die offizielle Pachtvertrag-Vorlage enthält keine auslesbaren Formularfelder.");
            form.Elements.SetBoolean("/NeedAppearances", true);

            EnsureRequiredFields(form);

            SetTextField(form, "tenant_1_name", BuildFullName(hauptmitglied));
            SetTextField(form, "tenant_1_birthdate", FormatDate(hauptmitglied.Geburtsdatum));
            SetTextField(form, "tenant_2_name", nebenmitglied == null ? string.Empty : BuildFullName(nebenmitglied));
            SetTextField(form, "tenant_2_birthdate", nebenmitglied == null ? string.Empty : FormatDate(nebenmitglied.Geburtsdatum));
            SetTextField(form, "tenant_address", BuildAddress(hauptmitglied));
            SetTextField(form, "tenant_phone", BuildPhone(hauptmitglied));
            SetTextField(form, "parcel_number", parzelle.GartenNr?.Trim() ?? string.Empty);
            SetTextField(form, "parcel_area_sqm", FormatNumber(parzelle.FlaecheQm.Value));
            SetTextField(form, "contract_start_date", FormatDate(vertragsbeginn));
            SetTextField(form, "contract_end_date", string.Empty);
            SetTextField(form, "rent_per_qm", FormatCurrency(saison.PachtProQm.Value));
            SetTextField(form, "member_fee_display", FormatCurrency(mitgliedsbeitrag));
            SetTextField(form, "rent_display", FormatCurrency(pachtzins));
            SetTextField(form, "total_display", FormatCurrency(gesamtbetrag));
            SetTextField(form, "rent_due_date", string.Empty);
            SetTextField(form, "sign_place", ResolveSignPlace(hauptmitglied));
            SetTextField(form, "sign_date", FormatDate(DateTime.Today));
            AppendZusatzseite(document, hauptmitglied, nebenmitglied, parzelle, vertragsbeginn, gesetzlicherVertreterSnapshot, bankverbindungSnapshot);

            using var output = new MemoryStream();
            document.Save(output, false);
            return output.ToArray();
        }

        private static void AppendZusatzseite(PdfDocument document, MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, DateTime vertragsbeginn, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(210);
            page.Height = XUnit.FromMillimeter(297);

            using var graphics = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 17, XFontStyle.Bold);
            var sectionFont = new XFont("Arial", 11, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 10.5, XFontStyle.Regular);
            var hintFont = new XFont("Arial", 9.5, XFontStyle.Regular);
            var borderPen = new XPen(XColor.FromArgb(208, 214, 224), 0.8);
            const double margin = 42d;
            var contentWidth = page.Width.Point - margin * 2;
            double cursorY = margin;

            graphics.DrawString("Zusätzliche Vertragsangaben", titleFont, XBrushes.Black,
                new XRect(margin, cursorY, contentWidth, 24), XStringFormats.TopLeft);
            cursorY += 30;

            graphics.DrawString(
                "Diese Ergänzungsseite hält den für dieses Dokument verwendeten Stand zu Bankverbindung und – falls erforderlich – zum gesetzlichen Vertreter als Snapshot fest.",
                hintFont,
                XBrushes.DimGray,
                new XRect(margin, cursorY, contentWidth, 32),
                XStringFormats.TopLeft);
            cursorY += 42;

            cursorY = DrawBlock(graphics, borderPen, sectionFont, bodyFont, margin, contentWidth, cursorY, "Vertragskontext", new[]
            {
                $"Pächter/in: {BuildFullName(hauptmitglied)}",
                $"Nebenmitglied: {(nebenmitglied == null ? "-" : BuildFullName(nebenmitglied))}",
                $"Parzelle: {parzelle.GartenNr?.Trim() ?? string.Empty}",
                $"Vertragsbeginn: {FormatDate(vertragsbeginn)}"
            });

            cursorY = DrawBlock(graphics, borderPen, sectionFont, bodyFont, margin, contentWidth, cursorY, "Bankverbindung Verein", new[]
            {
                $"Kontoinhaber: {Safe(bankverbindungSnapshot.Kontoinhaber)}",
                $"Bank: {Safe(bankverbindungSnapshot.Bankname)}",
                $"IBAN: {Safe(bankverbindungSnapshot.Iban)}",
                $"BIC: {Safe(bankverbindungSnapshot.Bic)}"
            });

            if (gesetzlicherVertreterSnapshot != null)
            {
                cursorY = DrawBlock(graphics, borderPen, sectionFont, bodyFont, margin, contentWidth, cursorY, "Gesetzliche/r Vertreter/in", BuildVertreterLines(gesetzlicherVertreterSnapshot));
            }
        }

        private static double DrawBlock(XGraphics graphics, XPen borderPen, XFont sectionFont, XFont bodyFont, double margin, double contentWidth, double cursorY, string title, IReadOnlyCollection<string> lines)
        {
            var normalizedLines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var blockHeight = 18 + (normalizedLines.Length * 16) + 18;
            var rect = new XRect(margin, cursorY, contentWidth, blockHeight);
            graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
            graphics.DrawRectangle(borderPen, rect);
            graphics.DrawString(title, sectionFont, XBrushes.Black,
                new XRect(rect.X + 12, rect.Y + 10, rect.Width - 24, 16), XStringFormats.TopLeft);

            var lineY = rect.Y + 32;
            foreach (var line in normalizedLines)
            {
                graphics.DrawString(line, bodyFont, XBrushes.Black,
                    new XRect(rect.X + 12, lineY, rect.Width - 24, 14), XStringFormats.TopLeft);
                lineY += 16;
            }

            return rect.Bottom + 18;
        }

        private static IReadOnlyCollection<string> BuildVertreterLines(MitgliedsantragVertreterSnapshot snapshot)
        {
            var lines = new[]
            {
                $"Name / Vorname: {BuildNameLine(snapshot.Nachname, snapshot.Vorname)}",
                $"Anschrift: {BuildAddress(snapshot.Adresse, snapshot.Plz, snapshot.Ort)}",
                $"Kontakt: {BuildContact(snapshot.Telefon, snapshot.Handy, snapshot.Email)}"
            }
            .Where(x => !x.EndsWith(": -", StringComparison.Ordinal))
            .ToArray();

            return lines.Length == 0 ? new[] { "Keine zusätzlichen Vertreterdaten hinterlegt." } : lines;
        }

        private static Stream OpenTemplateStream()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName);
            if (stream == null)
                throw new InvalidOperationException("Die offizielle Pachtvertrag-Vorlage ist nicht im Projekt eingebunden.");

            return stream;
        }

        private static void SetTextField(PdfAcroForm form, string fieldName, string value)
        {
            if (form.Fields[fieldName] is not PdfTextField textField)
                throw new InvalidOperationException($"Das Pflichtfeld '{fieldName}' fehlt in der offiziellen Pachtvertrag-Vorlage oder ist kein Textfeld.");

            textField.Text = value ?? string.Empty;
        }

        private static void EnsureRequiredFields(PdfAcroForm form)
        {
            foreach (var fieldName in RequiredTextFields)
            {
                if (form.Fields[fieldName] is not PdfTextField)
                    throw new InvalidOperationException($"Das Pflichtfeld '{fieldName}' fehlt in der offiziellen Pachtvertrag-Vorlage oder ist kein Textfeld.");
            }

            foreach (var fieldName in RequiredSignatureFields)
            {
                if (form.Fields[fieldName] == null)
                    throw new InvalidOperationException($"Das Signaturfeld '{fieldName}' fehlt in der offiziellen Pachtvertrag-Vorlage.");
            }
        }

        private static string BuildFullName(MitgliedRecord member)
            => string.Join(" ", new[] { member.Vorname, member.Name }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        private static string BuildAddress(MitgliedRecord member)
        {
            var line1 = string.Join(" ", new[] { member.Adresse }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            var line2 = string.Join(" ", new[] { member.Plz, member.Ort }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            return string.Join(Environment.NewLine, new[] { line1, line2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string BuildPhone(MitgliedRecord member)
            => string.Join(" / ", new[] { member.Telefon, member.Handy }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        private static string BuildContact(string? telefon, string? handy, string? email)
            => string.Join(" / ", new[] { telefon, handy, email }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        private static string BuildAddress(string? adresse, string? plz, string? ort)
        {
            var line1 = Safe(adresse);
            var line2 = string.Join(" ", new[] { plz, ort }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            if (string.IsNullOrWhiteSpace(line2))
                return line1;
            if (string.Equals(line1, "-", StringComparison.Ordinal))
                return line2;
            return $"{line1}, {line2}";
        }

        private static string BuildNameLine(string? nachname, string? vorname)
        {
            var parts = new[] { Safe(nachname), Safe(vorname) }
                .Where(x => !string.Equals(x, "-", StringComparison.Ordinal))
                .ToArray();
            return parts.Length == 0 ? "-" : string.Join(" / ", parts);
        }

        private static string ResolveSignPlace(MitgliedRecord member)
            => string.IsNullOrWhiteSpace(member.Ort) ? string.Empty : member.Ort.Trim();

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatCurrency(decimal value)
            => value.ToString("0.00 €", CultureInfo.GetCultureInfo("de-DE"));

        private static string FormatNumber(decimal value)
            => value.ToString("0.##", CultureInfo.GetCultureInfo("de-DE"));

        private static string Safe(string? value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}