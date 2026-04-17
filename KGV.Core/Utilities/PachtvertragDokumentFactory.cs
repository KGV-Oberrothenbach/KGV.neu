using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities
{
    public static class PachtvertragDokumentFactory
    {
        private const string PdfTemplateResourceName = "KGV.Core.Templates.Pachtvertrag_Vorlage.pdf";

        private static readonly IReadOnlyList<PdfFormFieldSpec> FieldSpecs = new[]
        {
            new PdfFormFieldSpec("ausstellungsdatum", PdfFormFieldKind.Text, 1, 427.8666, 119.5333, 127.4667, 16),

            new PdfFormFieldSpec("paechter1_name", PdfFormFieldKind.Text, 1, 63.8667, 212, 205, 13),
            new PdfFormFieldSpec("paechter1_vorname", PdfFormFieldKind.Text, 1, 79.3333, 226, 189, 13),
            new PdfFormFieldSpec("paechter1_geburtsdatum", PdfFormFieldKind.Text, 1, 94.6666, 240, 173.4, 13),
            new PdfFormFieldSpec("paechter1_mitgliedsnummer", PdfFormFieldKind.Text, 1, 104.5333, 254, 163, 13),

            new PdfFormFieldSpec("paechter2_name", PdfFormFieldKind.Text, 1, 328.3333, 212, 180, 13),
            new PdfFormFieldSpec("paechter2_vorname", PdfFormFieldKind.Text, 1, 341.6667, 226, 180, 13),
            new PdfFormFieldSpec("paechter2_geburtsdatum", PdfFormFieldKind.Text, 1, 357.4666, 240, 150, 13),
            new PdfFormFieldSpec("paechter2_mitgliedsnummer", PdfFormFieldKind.Text, 1, 364.9333, 254, 138, 13),

            new PdfFormFieldSpec("parzelle_nummer", PdfFormFieldKind.Text, 1, 504.4667, 318.3333, 19.3333, 11),
            new PdfFormFieldSpec("parzelle_flaeche_qm", PdfFormFieldKind.Text, 1, 108.2667, 329.3333, 33.6, 11),
            new PdfFormFieldSpec("pachtbeginn", PdfFormFieldKind.Text, 1, 225.8666, 522.4667, 46.8, 11),
            new PdfFormFieldSpec("pachtende", PdfFormFieldKind.Text, 1, 429.6667, 522.2, 49.8, 11),

            new PdfFormFieldSpec("pacht_pro_qm", PdfFormFieldKind.Text, 2, 32.9333, 78, 150, 14),
            new PdfFormFieldSpec("parzelle_flaeche_qm_wiederholung", PdfFormFieldKind.Text, 2, 207.6, 78, 150, 14),
            new PdfFormFieldSpec("jahrespacht", PdfFormFieldKind.Text, 2, 390, 82, 150, 14),
            new PdfFormFieldSpec("pachtzahlung_faellig_bis", PdfFormFieldKind.Text, 2, 193.7333, 100.2, 47.7333, 10),

            new PdfFormFieldSpec("altvertrag_datum", PdfFormFieldKind.Text, 4, 437, 688.1333, 43.4667, 11),

            new PdfFormFieldSpec("bankblock_mehrzeilig", PdfFormFieldKind.MultilineText, 5, 28.9333, 112.8, 260, 50),
            new PdfFormFieldSpec("pacht_laufendes_jahr", PdfFormFieldKind.Text, 5, 295.2, 113.6, 165, 12),

            new PdfFormFieldSpec("unterschrift_ort", PdfFormFieldKind.Text, 5, 34, 352, 190, 14),
            new PdfFormFieldSpec("unterschrift_datum", PdfFormFieldKind.Text, 5, 293.0667, 374.9333, 190, 14),

            new PdfFormFieldSpec("unterschrift_paechter1", PdfFormFieldKind.SignaturePlaceholder, 5, 34.6667, 396, 160, 56),
            new PdfFormFieldSpec("unterschrift_paechter2", PdfFormFieldKind.SignaturePlaceholder, 5, 213.7333, 396, 160, 56),
            new PdfFormFieldSpec("unterschrift_verpaechter", PdfFormFieldKind.SignaturePlaceholder, 5, 391.1333, 396, 160, 56),
            new PdfFormFieldSpec("anlagen_unterschrift_paechter1", PdfFormFieldKind.SignaturePlaceholder, 5, 35.7333, 546, 160, 56),
            new PdfFormFieldSpec("anlagen_unterschrift_paechter2", PdfFormFieldKind.SignaturePlaceholder, 5, 214, 546, 160, 56)
        };

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            string? status = null)
            => CreateUploadRequest(
                hauptmitglied,
                nebenmitglied,
                parzelle,
                saison,
                vertragsbeginn,
                altvertragDatum: null,
                gesetzlicherVertreterSnapshot: null,
                bankverbindungSnapshot: null,
                status: status);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot,
            string? status = null)
            => CreateUploadRequest(
                hauptmitglied,
                nebenmitglied,
                parzelle,
                saison,
                vertragsbeginn,
                altvertragDatum: null,
                gesetzlicherVertreterSnapshot,
                bankverbindungSnapshot,
                status);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            DateTime? altvertragDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot,
            string? status = null)
        {
            ArgumentNullException.ThrowIfNull(hauptmitglied);
            ArgumentNullException.ThrowIfNull(parzelle);
            ArgumentNullException.ThrowIfNull(saison);

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

            if (bankverbindungSnapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration mit vollständigen Bankdaten hinterlegt.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Pachtvertrag;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(hauptmitglied, dokumenttyp, normalizedStatus, vertragsbeginn.Date);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);

            var templateContext = BuildTemplateContext(
                hauptmitglied,
                nebenmitglied,
                parzelle,
                saison,
                vertragsbeginn.Date,
                altvertragDatum,
                bankverbindungSnapshot);

            var data = PachtvertragTemplateFactory.BuildData(templateContext);
            var content = FillAndFlattenPdfForm(LoadTemplatePdfBytes(), data);

            return new DokumentUploadRequest
            {
                MitgliedId = hauptmitglied.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = content
            };
        }

        private static PachtvertragTemplateContext BuildTemplateContext(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            DateTime? altvertragDatum,
            MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            return new PachtvertragTemplateContext
            {
                VereinskonfigurationSnapshot = bankverbindungSnapshot,
                Paechter1 = hauptmitglied,
                Paechter2 = nebenmitglied,
                ParzelleNummer = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim(),
                ParzelleFlaecheQm = parzelle.FlaecheQm!.Value,
                Pachtbeginn = vertragsbeginn,
                PachtProQm = saison.PachtProQm!.Value,
                Ausstellungsdatum = DateTime.Today,
                AltvertragDatum = altvertragDatum,
                UnterschriftOrt = "Zwickau"
            };
        }

        private static byte[] LoadTemplatePdfBytes()
        {
            var assembly = typeof(PachtvertragDokumentFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream(PdfTemplateResourceName)
                ?? throw new InvalidOperationException("Die Pachtvertrag-PDF-Vorlage ist nicht im Projekt eingebunden.");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static byte[] FillAndFlattenPdfForm(byte[] pdfBytes, PachtvertragTemplateData data)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new InvalidOperationException("Die Pachtvertrag-PDF-Vorlage ist leer oder konnte nicht geladen werden.");

            using var input = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

            var map = BuildFieldMap(data);
            DrawFieldValues(document, map);
            RemoveFormAnnotations(document);

            using var output = new MemoryStream();
            document.Save(output);
            return output.ToArray();
        }

        private static Dictionary<string, string> BuildFieldMap(PachtvertragTemplateData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ausstellungsdatum"] = data.Ausstellungsdatum,

                ["paechter1_name"] = data.Paechter1Name,
                ["paechter1_vorname"] = data.Paechter1Vorname,
                ["paechter1_geburtsdatum"] = data.Paechter1Geburtsdatum,
                ["paechter1_mitgliedsnummer"] = data.Paechter1Mitgliedsnummer,

                ["paechter2_name"] = data.Paechter2Name,
                ["paechter2_vorname"] = data.Paechter2Vorname,
                ["paechter2_geburtsdatum"] = data.Paechter2Geburtsdatum,
                ["paechter2_mitgliedsnummer"] = data.Paechter2Mitgliedsnummer,

                ["parzelle_nummer"] = data.ParzelleNummer,
                ["parzelle_flaeche_qm"] = data.ParzelleFlaecheQm,
                ["pachtbeginn"] = data.Pachtbeginn,
                ["pachtende"] = data.Pachtende,

                ["pacht_pro_qm"] = data.PachtProQm,
                ["parzelle_flaeche_qm_wiederholung"] = data.ParzelleFlaecheQmWiederholung,
                ["jahrespacht"] = data.Jahrespacht,
                ["pachtzahlung_faellig_bis"] = data.PachtzahlungFaelligBis,

                ["altvertrag_datum"] = data.AltvertragDatum,

                ["bankblock_mehrzeilig"] = data.BankblockMehrzeilig,
                ["pacht_laufendes_jahr"] = data.PachtLaufendesJahr,

                ["unterschrift_ort"] = data.UnterschriftOrt,
                ["unterschrift_datum"] = data.UnterschriftDatum,

                ["unterschrift_paechter1"] = string.Empty,
                ["unterschrift_paechter2"] = string.Empty,
                ["unterschrift_verpaechter"] = string.Empty,
                ["anlagen_unterschrift_paechter1"] = string.Empty,
                ["anlagen_unterschrift_paechter2"] = string.Empty
            };
        }

        private static void DrawFieldValues(PdfDocument document, IReadOnlyDictionary<string, string> map)
        {
            var valueFont = new XFont("Arial", 7.2, XFontStyle.Regular);
            var smallValueFont = new XFont("Arial", 6.4, XFontStyle.Regular);

            foreach (var field in FieldSpecs)
            {
                if (field.Kind == PdfFormFieldKind.SignaturePlaceholder)
                    continue;

                var pageIndex = field.Page - 1;
                if (pageIndex < 0 || pageIndex >= document.Pages.Count)
                    continue;

                if (!map.TryGetValue(field.Name, out var value))
                    continue;

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var page = document.Pages[pageIndex];
                using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

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
            const double verticalOffset = 0d;

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
                        // Values are already drawn into the page content.
                    }
                }
            }

            try
            {
                document.Internals.Catalog.Elements.Remove("/AcroForm");
            }
            catch
            {
                // Values are already drawn into the page content.
            }
        }

        private static bool ShouldUseSmallFont(string fieldName, string value)
            => fieldName.Equals("bankblock_mehrzeilig", StringComparison.OrdinalIgnoreCase)
               || value.Length > 34;

        private static List<string> SplitLines(string? text)
            => (text ?? string.Empty)
                .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(x => string.IsNullOrWhiteSpace(x) ? string.Empty : x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

        private enum PdfFormFieldKind
        {
            Text,
            MultilineText,
            SignaturePlaceholder
        }

        private sealed record PdfFormFieldSpec(
            string Name,
            PdfFormFieldKind Kind,
            int Page,
            double X,
            double Y,
            double Width,
            double Height)
        {
            public XRect Rect => new(X, Y, Width, Height);
        }
    }
}