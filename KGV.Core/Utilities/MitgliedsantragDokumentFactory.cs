using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities
{
    public static class MitgliedsantragDokumentFactory
    {
        private const string PdfTemplateResourceName = "KGV.Core.Templates.Mitgliedsantrag_Vorlage_Formularfelder.pdf";
        private const string DefaultOrt = "Zwickau";

        private static readonly IReadOnlyList<PdfFormFieldSpec> FieldSpecs = new[]
        {
            new PdfFormFieldSpec("ausstellungsdatum", PdfFormFieldKind.Text, 442.8, 56.6, 121.0, 9.4),
            new PdfFormFieldSpec("dokument_ort", PdfFormFieldKind.Text, 442.8, 77.9, 121.0, 9.4),

            new PdfFormFieldSpec("mitglied_name", PdfFormFieldKind.Text, 28.3, 154.0, 134.3, 9.5),
            new PdfFormFieldSpec("mitglied_vorname", PdfFormFieldKind.Text, 168.1, 154.0, 128.2, 9.5),
            new PdfFormFieldSpec("mitglied_geburtsdatum", PdfFormFieldKind.Text, 301.9, 154.0, 129.5, 9.5),
            new PdfFormFieldSpec("mitglied_aufnahme_ab", PdfFormFieldKind.Text, 436.9, 154.0, 130.2, 9.5),

            new PdfFormFieldSpec("mitglied_telefon", PdfFormFieldKind.Text, 28.3, 175.2, 134.3, 9.5),
            new PdfFormFieldSpec("mitglied_mobil", PdfFormFieldKind.Text, 168.1, 175.2, 128.2, 9.5),
            new PdfFormFieldSpec("mitglied_email", PdfFormFieldKind.Text, 301.9, 175.2, 265.2, 9.5),

            new PdfFormFieldSpec("mitglied_anschrift_mehrzeilig", PdfFormFieldKind.MultilineText, 29.2, 197.2, 536.4, 28.4),

            new PdfFormFieldSpec("check_whatsapp", PdfFormFieldKind.Checkbox, 107.0, 247.2, 7.0, 7.0),
            new PdfFormFieldSpec("check_rechnung_mail", PdfFormFieldKind.Checkbox, 287.5, 247.2, 7.0, 7.0),
            new PdfFormFieldSpec("check_info_mail", PdfFormFieldKind.Checkbox, 467.5, 247.2, 7.0, 7.0),

            new PdfFormFieldSpec("vertreter_name", PdfFormFieldKind.Text, 28.3, 294.7, 130.6, 9.3),
            new PdfFormFieldSpec("vertreter_vorname", PdfFormFieldKind.Text, 164.4, 294.7, 130.5, 9.3),
            new PdfFormFieldSpec("vertreter_geburtsdatum", PdfFormFieldKind.Text, 300.4, 294.7, 130.6, 9.3),

            new PdfFormFieldSpec("vertreter_telefon", PdfFormFieldKind.Text, 28.3, 316.0, 130.6, 9.3),
            new PdfFormFieldSpec("vertreter_mobil", PdfFormFieldKind.Text, 167.1, 316.0, 125.2, 9.3),
            new PdfFormFieldSpec("vertreter_email", PdfFormFieldKind.Text, 300.4, 316.2, 266.7, 9.0),

            new PdfFormFieldSpec("vertreter_anschrift_mehrzeilig", PdfFormFieldKind.MultilineText, 28.3, 337.2, 538.8, 25.5),

            new PdfFormFieldSpec("mitgliedsbeitrag_jaehrlich", PdfFormFieldKind.Text, 28.3, 401.6, 130.6, 9.3),
            new PdfFormFieldSpec("mitgliedsbeitrag_anteilig", PdfFormFieldKind.Text, 164.4, 401.6, 130.5, 9.3),
            new PdfFormFieldSpec("beitragsmonate", PdfFormFieldKind.Text, 300.4, 401.6, 130.6, 9.3),
            new PdfFormFieldSpec("aufnahmegebuehr", PdfFormFieldKind.Text, 436.5, 401.6, 130.6, 9.3),

            new PdfFormFieldSpec("bank_kontoinhaber", PdfFormFieldKind.Text, 28.3, 459.7, 130.6, 9.3),
            new PdfFormFieldSpec("bank_name", PdfFormFieldKind.Text, 164.4, 459.7, 130.5, 9.3),
            new PdfFormFieldSpec("bank_iban", PdfFormFieldKind.Text, 300.4, 459.7, 130.6, 9.3),
            new PdfFormFieldSpec("bank_bic", PdfFormFieldKind.Text, 436.5, 459.7, 130.6, 9.3),

            new PdfFormFieldSpec("unterschrift_ort", PdfFormFieldKind.Text, 28.3, 549.7, 266.0, 8.8),
            new PdfFormFieldSpec("unterschrift_datum", PdfFormFieldKind.Text, 299.8, 549.7, 267.3, 8.8),

            // Signaturflächen bleiben bewusst leer. Sie werden nur entfernt, nicht beschrieben.
            new PdfFormFieldSpec("unterschrift_antragsteller", PdfFormFieldKind.SignaturePlaceholder, 28.3, 562.0, 176.0, 42.0),
            new PdfFormFieldSpec("unterschrift_vertreter", PdfFormFieldKind.SignaturePlaceholder, 209.0, 562.0, 176.0, 42.0),
            new PdfFormFieldSpec("unterschrift_verein", PdfFormFieldKind.SignaturePlaceholder, 389.0, 562.0, 176.0, 42.0),

            new PdfFormFieldSpec("datenschutz_unterschrift_antragsteller", PdfFormFieldKind.SignaturePlaceholder, 28.3, 733.824, 260.0, 47.176),
            new PdfFormFieldSpec("datenschutz_unterschrift_vertreter", PdfFormFieldKind.SignaturePlaceholder, 307.682, 733.824, 260.0, 47.176)
        };

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord member,
            decimal mitgliedsbeitrag,
            DateTime beginnDatum,
            string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, 0m, beginnDatum, null, null, status);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord member,
            decimal mitgliedsbeitrag,
            DateTime beginnDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, 0m, beginnDatum, gesetzlicherVertreterSnapshot, null, status);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord member,
            decimal mitgliedsbeitrag,
            decimal aufnahmegebuehr,
            DateTime beginnDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot,
            string? status = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            if (mitgliedsbeitrag < 0m)
                throw new InvalidOperationException("Der Mitgliedsbeitrag darf nicht negativ sein.");

            if (aufnahmegebuehr < 0m)
                throw new InvalidOperationException("Die Aufnahmegebühr darf nicht negativ sein.");

            if (bankverbindungSnapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration mit vollständigen Mitgliedsantragsdaten hinterlegt.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Mitgliedsantrag;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, normalizedStatus, DateTime.Today);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);

            var templateData = BuildTemplateData(
                member,
                mitgliedsbeitrag,
                aufnahmegebuehr,
                beginnDatum.Date,
                gesetzlicherVertreterSnapshot,
                bankverbindungSnapshot);

            var pdfTemplate = LoadTemplatePdfBytes();
            var filledPdf = FillAndFlattenPdfForm(pdfTemplate, templateData);

            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = filledPdf
            };
        }

        private static MitgliedsantragTemplateData BuildTemplateData(
            MitgliedRecord member,
            decimal mitgliedsbeitrag,
            decimal aufnahmegebuehr,
            DateTime beginnDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            var istMinderjaehrig = gesetzlicherVertreterSnapshot != null;
            var beitragsmonate = CalculateBeitragsmonate(beginnDatum);
            var anteiligerBeitrag = MitgliedsantragBeitragHelper.NormalizeBeitrag(
                mitgliedsbeitrag * ((decimal)beitragsmonate / 12m));

            var ort = ResolveOrt(bankverbindungSnapshot);
            var beitragsjahr = beginnDatum.Year;

            var erklaerungsteile = new[]
            {
                $"Ich beantrage die Mitgliedschaft im {ResolveVereinName(bankverbindungSnapshot)}.",
                "Die gemachten Angaben sind nach meinem Kenntnisstand vollständig und richtig.",
                Safe(bankverbindungSnapshot.StandardHinweistext)
            };

            var datenschutzteile = new[]
            {
                Safe(bankverbindungSnapshot.DatenschutzText),
                BuildDatenschutzMeta(bankverbindungSnapshot)
            };

            return new MitgliedsantragTemplateData
            {
                BodyClass = istMinderjaehrig ? "minor" : string.Empty,

                VereinName = ResolveVereinName(bankverbindungSnapshot),
                VereinRegisterangabe = Safe(bankverbindungSnapshot.VereinRegisterangabe),
                VereinEmail = Safe(bankverbindungSnapshot.VereinEmail),

                Ausstellungsdatum = FormatDate(DateTime.Today),
                DokumentOrt = ort,

                MitgliedName = Safe(member.Name),
                MitgliedVorname = Safe(member.Vorname),
                MitgliedGeburtsdatum = FormatDate(member.Geburtsdatum),
                MitgliedAufnahmeAb = FormatDate(beginnDatum),
                MitgliedAnschriftMehrzeilig = BuildAddressMultiline(member.Adresse, member.Plz, member.Ort),
                MitgliedTelefon = SafeContact(member.Telefon),
                MitgliedMobil = SafeContact(member.Handy),
                MitgliedEmail = SafeContact(member.Email),

                CheckWhatsapp = member.WhatsappEinwilligung ? "true" : string.Empty,
                CheckRechnungMail = member.EmailRechnungEinwilligung ? "true" : string.Empty,
                CheckInfoMail = member.EmailInfoEinwilligung ? "true" : string.Empty,

                VertreterName = gesetzlicherVertreterSnapshot == null ? string.Empty : Safe(gesetzlicherVertreterSnapshot.Nachname),
                VertreterVorname = gesetzlicherVertreterSnapshot == null ? string.Empty : Safe(gesetzlicherVertreterSnapshot.Vorname),
                VertreterGeburtsdatum = string.Empty,
                VertreterAnschriftMehrzeilig = gesetzlicherVertreterSnapshot == null
                    ? string.Empty
                    : BuildAddressMultiline(gesetzlicherVertreterSnapshot.Adresse, gesetzlicherVertreterSnapshot.Plz, gesetzlicherVertreterSnapshot.Ort),
                VertreterTelefon = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Telefon),
                VertreterMobil = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Handy),
                VertreterEmail = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Email),

                MitgliedsbeitragJaehrlich = FormatCurrency(mitgliedsbeitrag),
                MitgliedsbeitragAnteilig = FormatCurrency(anteiligerBeitrag),
                Beitragsmonate = beitragsmonate.ToString(CultureInfo.InvariantCulture),
                Aufnahmegebuehr = FormatCurrency(aufnahmegebuehr),
                BeitragHinweis1 = BuildBeitragsHinweis(beitragsjahr, mitgliedsbeitrag, beginnDatum),
                BeitragHinweis2 = $"Die einmalige Aufnahmegebühr beträgt {FormatCurrency(aufnahmegebuehr)}.",

                BankKontoinhaber = Safe(bankverbindungSnapshot.Kontoinhaber),
                BankName = Safe(bankverbindungSnapshot.Bankname),
                BankIban = Safe(bankverbindungSnapshot.Iban),
                BankBic = Safe(bankverbindungSnapshot.Bic),

                UnterschriftOrt = ort,
                UnterschriftDatum = FormatDate(DateTime.Today),

                Erklaerungstext = JoinParagraphs(erklaerungsteile),
                Datenschutztext = JoinParagraphs(datenschutzteile),
                Fussnote = BuildFussnote(bankverbindungSnapshot)
            };
        }

        private static byte[] LoadTemplatePdfBytes()
        {
            var assembly = typeof(MitgliedsantragDokumentFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream(PdfTemplateResourceName)
                ?? throw new InvalidOperationException("Die Mitgliedsantrag-PDF-Vorlage ist nicht im Projekt eingebunden.");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static byte[] FillAndFlattenPdfForm(byte[] pdfBytes, MitgliedsantragTemplateData data)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new InvalidOperationException("Die Mitgliedsantrag-PDF-Vorlage ist leer oder konnte nicht geladen werden.");

            using var input = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

            var map = BuildFieldMap(data);
            DrawFieldValues(document, map);
            RemoveFormAnnotations(document);

            using var output = new MemoryStream();
            document.Save(output);
            return output.ToArray();
        }

        private static Dictionary<string, string> BuildFieldMap(MitgliedsantragTemplateData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ausstellungsdatum"] = data.Ausstellungsdatum,
                ["dokument_ort"] = data.DokumentOrt,

                ["mitglied_name"] = data.MitgliedName,
                ["mitglied_vorname"] = data.MitgliedVorname,
                ["mitglied_geburtsdatum"] = data.MitgliedGeburtsdatum,
                ["mitglied_aufnahme_ab"] = data.MitgliedAufnahmeAb,
                ["mitglied_telefon"] = data.MitgliedTelefon,
                ["mitglied_mobil"] = data.MitgliedMobil,
                ["mitglied_email"] = data.MitgliedEmail,
                ["mitglied_anschrift_mehrzeilig"] = data.MitgliedAnschriftMehrzeilig,

                ["check_whatsapp"] = data.CheckWhatsapp,
                ["check_rechnung_mail"] = data.CheckRechnungMail,
                ["check_info_mail"] = data.CheckInfoMail,

                ["vertreter_name"] = data.VertreterName,
                ["vertreter_vorname"] = data.VertreterVorname,
                ["vertreter_geburtsdatum"] = data.VertreterGeburtsdatum,
                ["vertreter_telefon"] = data.VertreterTelefon,
                ["vertreter_mobil"] = data.VertreterMobil,
                ["vertreter_email"] = data.VertreterEmail,
                ["vertreter_anschrift_mehrzeilig"] = data.VertreterAnschriftMehrzeilig,

                ["mitgliedsbeitrag_jaehrlich"] = data.MitgliedsbeitragJaehrlich,
                ["mitgliedsbeitrag_anteilig"] = data.MitgliedsbeitragAnteilig,
                ["beitragsmonate"] = data.Beitragsmonate,
                ["aufnahmegebuehr"] = data.Aufnahmegebuehr,

                ["bank_kontoinhaber"] = data.BankKontoinhaber,
                ["bank_name"] = data.BankName,
                ["bank_iban"] = data.BankIban,
                ["bank_bic"] = data.BankBic,

                ["unterschrift_ort"] = data.UnterschriftOrt,
                ["unterschrift_datum"] = data.UnterschriftDatum,

                ["unterschrift_antragsteller"] = string.Empty,
                ["unterschrift_vertreter"] = string.Empty,
                ["unterschrift_verein"] = string.Empty,
                ["datenschutz_unterschrift_antragsteller"] = string.Empty,
                ["datenschutz_unterschrift_vertreter"] = string.Empty
            };
        }

        private static void DrawFieldValues(PdfDocument document, IReadOnlyDictionary<string, string> map)
        {
            if (document.Pages.Count == 0)
                return;

            var page = document.Pages[0];

            var valueFont = new XFont("Arial", 7.0, XFontStyle.Regular);
            var smallValueFont = new XFont("Arial", 6.4, XFontStyle.Regular);
            var checkboxFont = new XFont("Arial", 7.0, XFontStyle.Bold);

            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

            foreach (var field in FieldSpecs)
            {
                if (field.Kind == PdfFormFieldKind.SignaturePlaceholder)
                    continue;

                if (!map.TryGetValue(field.Name, out var value))
                    continue;

                if (field.Kind == PdfFormFieldKind.Checkbox)
                {
                    DrawCheckbox(graphics, checkboxFont, field.Rect, IsTruthy(value));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                DrawTextValue(graphics, valueFont, smallValueFont, field, value);
            }
        }

        private static void DrawTextValue(XGraphics graphics, XFont normalFont, XFont smallFont, PdfFormFieldSpec field, string value)
        {
            var lines = SplitLines(value);
            if (lines.Count == 0)
                return;

            var font = ShouldUseSmallFont(field.Name, value) ? smallFont : normalFont;

            if (field.Kind == PdfFormFieldKind.MultilineText || lines.Count > 1)
            {
                DrawMultilineText(graphics, font, field.Rect, lines);
                return;
            }

            DrawSingleLineText(graphics, font, field.Rect, lines[0]);
        }

        private static void DrawSingleLineText(XGraphics graphics, XFont font, XRect rect, string value)
        {
            const double horizontalPadding = 1.8d;
            const double verticalOffset = -1.1d;

            graphics.DrawString(
                value,
                font,
                XBrushes.Black,
                new XRect(
                    rect.X + horizontalPadding,
                    rect.Y + verticalOffset,
                    Math.Max(0, rect.Width - horizontalPadding * 2),
                    rect.Height + 2),
                XStringFormats.TopLeft);
        }

        private static void DrawMultilineText(XGraphics graphics, XFont font, XRect rect, IReadOnlyList<string> lines)
        {
            const double horizontalPadding = 2.0d;
            const double topPadding = 1.0d;

            var lineHeight = font.GetHeight();
            var y = rect.Y + topPadding;

            foreach (var line in lines)
            {
                if (y + lineHeight > rect.Bottom + 1)
                    break;

                graphics.DrawString(
                    line,
                    font,
                    XBrushes.Black,
                    new XRect(
                        rect.X + horizontalPadding,
                        y,
                        Math.Max(0, rect.Width - horizontalPadding * 2),
                        lineHeight + 1),
                    XStringFormats.TopLeft);

                y += lineHeight;
            }
        }

        private static void DrawCheckbox(XGraphics graphics, XFont font, XRect rect, bool isChecked)
        {
            var boxPen = new XPen(XColors.Black, 0.6);

            // Rahmen immer zeichnen, weil die PDF-Formular-Annotation später entfernt wird.
            graphics.DrawRectangle(boxPen, rect);

            if (!isChecked)
                return;

            var text = "X";
            var size = graphics.MeasureString(text, font);

            var x = rect.X + (rect.Width - size.Width) / 2d;
            var y = rect.Y + (rect.Height - size.Height) / 2d - 0.4d;

            graphics.DrawString(
                text,
                font,
                XBrushes.Black,
                new XRect(x, y, size.Width + 1, size.Height + 1),
                XStringFormats.TopLeft);
        }

        private static void RemoveFormAnnotations(PdfDocument document)
        {
            for (var pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
            {
                var page = document.Pages[pageIndex];

                if (page.Annotations == null)
                    continue;

                var annotationsToRemove = new List<PdfAnnotation>();

                for (var i = 0; i < page.Annotations.Count; i++)
                {
                    var annotation = page.Annotations[i];
                    if (annotation != null)
                        annotationsToRemove.Add(annotation);
                }

                foreach (var annotation in annotationsToRemove)
                {
                    try
                    {
                        page.Annotations.Remove(annotation);
                    }
                    catch
                    {
                        // Ignore cleanup failures. Values are already drawn into the page content.
                    }
                }
            }

            try
            {
                document.Internals.Catalog.Elements.Remove("/AcroForm");
            }
            catch
            {
                // Ignore cleanup failures. Values are already drawn into the page content.
            }
        }

        private static bool ShouldUseSmallFont(string fieldName, string value)
            => fieldName.Equals("bank_iban", StringComparison.OrdinalIgnoreCase)
               || fieldName.Equals("mitglied_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30
               || fieldName.Equals("vertreter_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30
               || value.Length > 34;

        private static bool IsTruthy(string? value)
            => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "ja", StringComparison.OrdinalIgnoreCase);

        private static int CalculateBeitragsmonate(DateTime beginnDatum)
        {
            var monate = 12 - beginnDatum.Month + 1;
            return Math.Max(1, monate);
        }

        private static string ResolveOrt(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var configuredOrt = Safe(snapshot.DokumentOrt);
            return string.IsNullOrWhiteSpace(configuredOrt) ? DefaultOrt : configuredOrt;
        }

        private static string BuildAddressMultiline(string? adresse, string? plz, string? ort)
        {
            var street = SafeContact(adresse);
            var city = string.Join(" ", new[] { plz, ort }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            if (string.IsNullOrWhiteSpace(city))
                return street;

            if (string.IsNullOrWhiteSpace(street))
                return city;

            return $"{street}\n{city}";
        }

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatCurrency(decimal value)
            => MitgliedsantragBeitragHelper.NormalizeBeitrag(value)
                .ToString("0.00 €", CultureInfo.GetCultureInfo("de-DE"));

        private static string Safe(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string SafeContact(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string ResolveVereinName(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var value = Safe(snapshot.VereinName);
            return string.IsNullOrWhiteSpace(value) ? VereinsdokumentBranding.VereinsName : value;
        }

        private static string BuildDatenschutzMeta(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(snapshot.DatenschutzVersion))
                parts.Add($"Version {snapshot.DatenschutzVersion.Trim()}");

            if (snapshot.DatenschutzStand.HasValue)
                parts.Add($"Stand {FormatDate(snapshot.DatenschutzStand)}");

            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string BuildBeitragsHinweis(int beitragsjahr, decimal jahresbeitrag, DateTime beginnDatum)
        {
            if (beginnDatum.Year != beitragsjahr)
                return $"Der Mitgliedsbeitrag beträgt für das Beitragsjahr {beitragsjahr} {FormatCurrency(jahresbeitrag)} und ist jährlich zu zahlen.";

            var monate = CalculateBeitragsmonate(beginnDatum);
            var anteil = MitgliedsantragBeitragHelper.NormalizeBeitrag(jahresbeitrag * ((decimal)monate / 12m));

            return $"Jahresbeitrag: {FormatCurrency(jahresbeitrag)} · Aufnahmejahr ({monate} Monate: {beginnDatum:dd.MM.yyyy}–31.12.{beitragsjahr}): anteiliger Beitrag {FormatCurrency(anteil)}.";
        }

        private static string BuildFussnote(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(snapshot.VerwendungszweckMitgliedsantrag))
                parts.Add($"Verwendungszweck: {snapshot.VerwendungszweckMitgliedsantrag.Trim()}");

            return parts.Count == 0 ? "Mitgliedsantrag des Vereins" : string.Join(" · ", parts);
        }

        private static List<string> SplitLines(string? text)
            => (text ?? string.Empty)
                .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(x => string.IsNullOrWhiteSpace(x) ? string.Empty : x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

        private static string JoinParagraphs(IEnumerable<string?> parts)
            => string.Join("\n\n", parts
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

        private enum PdfFormFieldKind
        {
            Text,
            MultilineText,
            Checkbox,
            SignaturePlaceholder
        }

        private sealed record PdfFormFieldSpec(
            string Name,
            PdfFormFieldKind Kind,
            double X,
            double Y,
            double Width,
            double Height)
        {
            public XRect Rect => new(X, Y, Width, Height);
        }
    }
}