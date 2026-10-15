using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities
{
    public static class PachtvertragDokumentFactory
    {
        private const string PdfTemplateResourceName = "KGV.Core.Templates.Pachtvertrag_KGV_Oberrothenbach_neu.pdf";

        private static readonly IReadOnlyList<PdfFormFieldSpec> FieldSpecs = new[]
        {
            new PdfFormFieldSpec("paechter_name", PdfFormFieldKind.Text, 1, 42, 229, 250.64, 17),
            new PdfFormFieldSpec("mitgliedsnummer", PdfFormFieldKind.Text, 1, 302.64, 229, 250.64, 17),
            new PdfFormFieldSpec("paechter_anschrift", PdfFormFieldKind.Text, 1, 42, 257, 250.64, 17),
            new PdfFormFieldSpec("paechter_kontakt", PdfFormFieldKind.Text, 1, 302.64, 257, 250.64, 17),

            new PdfFormFieldSpec("zweite_partei_bezeichnung", PdfFormFieldKind.Text, 1, 42, 285, 510, 14),
            new PdfFormFieldSpec("zweite_partei_name", PdfFormFieldKind.Text, 1, 42, 312, 250.64, 17),
            new PdfFormFieldSpec("zweite_partei_mitgliedsnummer", PdfFormFieldKind.Text, 1, 302.64, 312, 250.64, 17),
            new PdfFormFieldSpec("zweite_partei_anschrift", PdfFormFieldKind.Text, 1, 42, 340, 250.64, 17),
            new PdfFormFieldSpec("zweite_partei_kontakt", PdfFormFieldKind.Text, 1, 302.64, 340, 250.64, 17),

            new PdfFormFieldSpec("parzelle_nr", PdfFormFieldKind.Text, 1, 42, 399, 83, 17),
            new PdfFormFieldSpec("lage_anlage", PdfFormFieldKind.Text, 1, 136, 399, 230, 17),
            new PdfFormFieldSpec("flaeche_qm", PdfFormFieldKind.Text, 1, 377, 399, 114, 17),
            new PdfFormFieldSpec("vertragsbeginn", PdfFormFieldKind.Text, 1, 42, 496, 138, 17),
            new PdfFormFieldSpec("uebergabe_datum", PdfFormFieldKind.Text, 1, 192, 496, 138, 17),
            new PdfFormFieldSpec("befristung", PdfFormFieldKind.Text, 1, 342, 496, 149, 17),
            new PdfFormFieldSpec("pachtzins_jahr", PdfFormFieldKind.Text, 1, 42, 583, 250, 17),
            new PdfFormFieldSpec("faelligkeit", PdfFormFieldKind.Text, 1, 302, 583, 251, 17),

            new PdfFormFieldSpec("kleingartenordnung_fassung", PdfFormFieldKind.Text, 2, 42, 286, 245, 17),
            new PdfFormFieldSpec("kleingartenordnung_ausgehaendigt_am", PdfFormFieldKind.Text, 2, 316, 286, 170, 17),

            new PdfFormFieldSpec("ort_datum", PdfFormFieldKind.Text, 3, 42, 468, 190, 17),
            new PdfFormFieldSpec("paechter_druckname", PdfFormFieldKind.Text, 3, 252, 468, 239, 17),
            new PdfFormFieldSpec("weitere_vereinbarungen", PdfFormFieldKind.MultilineText, 3, 42, 252, 510, 72),
            new PdfFormFieldSpec("anlage_1", PdfFormFieldKind.Text, 3, 42, 365, 10, 10),
            new PdfFormFieldSpec("anlage_2", PdfFormFieldKind.Text, 3, 255, 365, 10, 10),
            new PdfFormFieldSpec("anlage_3", PdfFormFieldKind.Text, 3, 42, 387, 10, 10),
            new PdfFormFieldSpec("anlage_4", PdfFormFieldKind.Text, 3, 255, 387, 10, 10),
            new PdfFormFieldSpec("vereins_unterzeichner", PdfFormFieldKind.Text, 3, 252, 590, 300, 17)
            ,new PdfFormFieldSpec("unterschrift_paechter", PdfFormFieldKind.SignaturePlaceholder, 3, 42, 516, 190, 35)
            ,new PdfFormFieldSpec("unterschrift_zweite_partei", PdfFormFieldKind.SignaturePlaceholder, 3, 42, 576, 190, 35)
            ,new PdfFormFieldSpec("unterschrift_vorstand", PdfFormFieldKind.SignaturePlaceholder, 3, 252, 516, 300, 35)
        };

        // Compatibility-only fallback for isolated local tests. Production paths use IPdfTemplateProvider.
        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            string? status = null,
            MitgliedRecord? paechter2Override = null)
            => CreateUploadRequest(
                hauptmitglied,
                nebenmitglied,
                parzelle,
                saison,
                vertragsbeginn,
                altvertragDatum: null,
                gesetzlicherVertreterSnapshot: null,
                bankverbindungSnapshot: null,
                status: status,
                paechter2Override: paechter2Override);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            VereinskonfigurationSnapshot? bankverbindungSnapshot,
            string? status = null,
            MitgliedRecord? paechter2Override = null)
            => CreateUploadRequest(
                hauptmitglied,
                nebenmitglied,
                parzelle,
                saison,
                vertragsbeginn,
                altvertragDatum: null,
                gesetzlicherVertreterSnapshot,
                bankverbindungSnapshot,
                status,
                paechter2Override: paechter2Override);

        public static DokumentUploadRequest CreateUploadRequest(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            DateTime? altvertragDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            VereinskonfigurationSnapshot? bankverbindungSnapshot,
            string? status = null,
            MitgliedRecord? paechter2Override = null)
            => CreateUploadRequestCore(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn,
                altvertragDatum, gesetzlicherVertreterSnapshot, bankverbindungSnapshot, status, paechter2Override,
                LoadEmbeddedTemplateAsset());

        public static async Task<DokumentUploadRequest> CreateUploadRequestAsync(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            DateTime? altvertragDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            VereinskonfigurationSnapshot? bankverbindungSnapshot,
            string? status,
            IPdfTemplateProvider templateProvider,
            MitgliedRecord? paechter2Override = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(templateProvider);
            var template = await templateProvider.GetActiveAsync("pachtvertrag", "pachtvertrag-field-spec/v2", "top-left/v1", cancellationToken);
            ValidateTemplate(template);
            return CreateUploadRequestCore(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn,
                altvertragDatum, gesetzlicherVertreterSnapshot, bankverbindungSnapshot, status, paechter2Override, template);
        }

        private static DokumentUploadRequest CreateUploadRequestCore(
            MitgliedRecord hauptmitglied,
            MitgliedRecord? nebenmitglied,
            ParzelleRecord parzelle,
            SaisonRecord saison,
            DateTime vertragsbeginn,
            DateTime? altvertragDatum,
            MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot,
            VereinskonfigurationSnapshot? bankverbindungSnapshot,
            string? status,
            MitgliedRecord? paechter2Override,
            PdfTemplateAsset template)
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
                VereinskonfigurationSnapshotFactory.NormalizeForLeaseContract(bankverbindungSnapshot),
                paechter2Override);

            var data = PachtvertragTemplateFactory.BuildData(templateContext);
            var content = FillAndFlattenPdfForm(template.PdfBytes, data, template.Fields);

            return new DokumentUploadRequest
            {
                // Der Pachtvertrag gehört fachlich zur Parzelle. Der Pächter steht im PDF
                // und wird über die zeitlich passende Parzellenbelegung bestimmt.
                ParzelleId = parzelle.Id,
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
            VereinskonfigurationSnapshot bankverbindungSnapshot,
            MitgliedRecord? paechter2Override = null)
        {
            var paechter2 = paechter2Override ?? nebenmitglied;

            return new PachtvertragTemplateContext
            {
                VereinskonfigurationSnapshot = bankverbindungSnapshot,
                Paechter1 = hauptmitglied,
                Paechter2 = paechter2,
                ZweiteParteiIstGesetzlicherVertreter = gesetzlicherVertreterSnapshot != null && paechter2 != null,
                ParzelleNummer = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim(),
                ParzelleFlaecheQm = parzelle.FlaecheQm!.Value,
                ParzelleAnlage = parzelle.Anlage,
                Pachtbeginn = vertragsbeginn,
                PachtProQm = saison.PachtProQm!.Value,
                Ausstellungsdatum = DateTime.Today,
                AltvertragDatum = altvertragDatum,
                UnterschriftOrt = "Zwickau"
            };
        }

        private static PdfTemplateAsset LoadEmbeddedTemplateAsset()
        {
            var assembly = typeof(PachtvertragDokumentFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream(PdfTemplateResourceName)
                ?? throw new InvalidOperationException("Die Pachtvertrag-PDF-Vorlage ist nicht im Projekt eingebunden.");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return new PdfTemplateAsset
            {
                TemplateKey = "pachtvertrag",
                Version = "embedded-fallback",
                FieldSpecVersion = "pachtvertrag-field-spec/v2",
                RendererVersion = "top-left/v1",
                PdfBytes = ms.ToArray(),
                Fields = FieldSpecs.Select(x => new PdfTemplateFieldSpec(x.Name, x.Kind.ToString(), x.Page, x.X, x.Y, x.Width, x.Height)).ToArray()
            };
        }

        private static byte[] FillAndFlattenPdfForm(byte[] pdfBytes, PachtvertragTemplateData data, IReadOnlyList<PdfTemplateFieldSpec> loadedFields)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new InvalidOperationException("Die Pachtvertrag-PDF-Vorlage ist leer oder konnte nicht geladen werden.");

            using var input = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

            var map = BuildFieldMap(data);
            DrawFieldValues(document, map, loadedFields);
            RemoveFormAnnotations(document);

            using var output = new MemoryStream();
            document.Save(output);
            return output.ToArray();
        }

        private static Dictionary<string, string> BuildFieldMap(PachtvertragTemplateData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["paechter_name"] = data.Paechter1Vollname,
                ["mitgliedsnummer"] = data.Paechter1Mitgliedsnummer,
                ["paechter_anschrift"] = data.Paechter1Anschrift,
                ["paechter_kontakt"] = data.Paechter1Kontakt,

                ["zweite_partei_bezeichnung"] = data.ZweiteParteiBezeichnung,
                ["zweite_partei_name"] = data.Paechter2Vollname,
                ["zweite_partei_mitgliedsnummer"] = data.Paechter2Mitgliedsnummer,
                ["zweite_partei_anschrift"] = data.Paechter2Anschrift,
                ["zweite_partei_kontakt"] = data.Paechter2Kontakt,

                ["parzelle_nr"] = data.ParzelleNummer,
                ["lage_anlage"] = data.ParzelleAnlage,
                ["flaeche_qm"] = data.ParzelleFlaecheQm,
                ["vertragsbeginn"] = data.Pachtbeginn,
                ["uebergabe_datum"] = data.UebergabeDatum,
                ["befristung"] = data.Pachtende,
                ["pachtzins_jahr"] = data.Jahrespacht,
                ["faelligkeit"] = data.PachtzahlungFaelligBis,
                ["kleingartenordnung_fassung"] = data.KleingartenordnungFassung,
                ["kleingartenordnung_ausgehaendigt_am"] = data.KleingartenordnungAusgehaendigtAm,
                ["weitere_vereinbarungen"] = data.WeitereVereinbarungen,
                ["anlage_1"] = data.Anlage1 ? "X" : string.Empty,
                ["anlage_2"] = data.Anlage2 ? "X" : string.Empty,
                ["anlage_3"] = data.Anlage3 ? "X" : string.Empty,
                ["anlage_4"] = data.Anlage4 ? "X" : string.Empty,
                ["vereins_unterzeichner"] = data.VereinsUnterzeichner,

                ["ort_datum"] = string.Join(", ", new[] { data.UnterschriftOrt, data.UnterschriftDatum }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                ["paechter_druckname"] = data.Paechter1Vollname
            };
        }

        private static void DrawFieldValues(PdfDocument document, IReadOnlyDictionary<string, string> map, IReadOnlyList<PdfTemplateFieldSpec> loadedFields)
        {
            var valueFont = new XFont("Arial", 7.2, XFontStyle.Regular);
            var smallValueFont = new XFont("Arial", 6.4, XFontStyle.Regular);

            foreach (var field in loadedFields.Select(ToInternalField))
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

        private static PdfFormFieldSpec ToInternalField(PdfTemplateFieldSpec field) => new(
            field.Name,
            field.Kind.Trim().ToLowerInvariant() switch
            {
                "multiline" => PdfFormFieldKind.MultilineText,
                "signatureplaceholder" or "signature" => PdfFormFieldKind.SignaturePlaceholder,
                _ => PdfFormFieldKind.Text
            },
            field.Page,
            field.X,
            field.Y,
            field.Width,
            field.Height);

        private static void ValidateTemplate(PdfTemplateAsset template)
        {
            if (template.PdfBytes == null || template.PdfBytes.Length == 0)
                throw new InvalidOperationException("Die zentrale Pachtvertrag-Vorlage ist leer.");

            var expected = FieldSpecs.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            var actual = template.Fields.Select(ToInternalField).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (expected.Length != actual.Length || expected.Where((field, index) =>
                    !string.Equals(field.Name, actual[index].Name, StringComparison.Ordinal)
                    || field.Kind != actual[index].Kind
                    || field.Page != actual[index].Page
                    || field.X != actual[index].X || field.Y != actual[index].Y
                    || field.Width != actual[index].Width || field.Height != actual[index].Height).Any())
                throw new InvalidOperationException("Die zentrale Pachtvertrag-Feldspezifikation entspricht nicht der unterstützten v1-Geometrie.");
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

        public static IReadOnlyList<Models.SignaturePlaceholder> GetSignaturePlaceholders()
        {
            return FieldSpecs
                .Where(f => f.Kind == PdfFormFieldKind.SignaturePlaceholder)
                .Select(f => new Models.SignaturePlaceholder
                {
                    Name = f.Name,
                    Page = f.Page,
                    X = f.X,
                    Y = f.Y,
                    Width = f.Width,
                    Height = f.Height
                })
                .ToArray();
        }
    }
}
