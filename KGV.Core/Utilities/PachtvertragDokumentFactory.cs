using System;
using System.Collections.Generic;
using System.Globalization;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Diagnostics;

namespace KGV.Core.Utilities
{
    public static class PachtvertragDokumentFactory
    {
        private static readonly IReadOnlyList<PdfFormFieldSpec> FieldSpecs = new[]
        {
            new PdfFormFieldSpec("vertragsnummer", PdfFormFieldKind.Text, 1, 410, 145, 150, 16),
            new PdfFormFieldSpec("verein_name", PdfFormFieldKind.Text, 1, 210, 232, 205, 14),
            new PdfFormFieldSpec("vereinsregister", PdfFormFieldKind.Text, 1, 505, 232, 58, 14),
            new PdfFormFieldSpec("paechter_name", PdfFormFieldKind.Text, 1, 215, 262, 185, 14),
            new PdfFormFieldSpec("mitgliedsnummer", PdfFormFieldKind.Text, 1, 481, 262, 82, 14),
            new PdfFormFieldSpec("paechter_geburtsdatum", PdfFormFieldKind.Text, 1, 175, 292, 130, 14),
            new PdfFormFieldSpec("paechter_anschrift", PdfFormFieldKind.Text, 1, 390, 292, 173, 14),
            new PdfFormFieldSpec("zweite_partei_name", PdfFormFieldKind.Text, 1, 215, 339, 185, 14),
            new PdfFormFieldSpec("zweite_partei_mitgliedsnummer", PdfFormFieldKind.Text, 1, 481, 339, 82, 14),
            new PdfFormFieldSpec("zweite_partei_geburtsdatum", PdfFormFieldKind.Text, 1, 175, 369, 130, 14),
            new PdfFormFieldSpec("zweite_partei_anschrift", PdfFormFieldKind.Text, 1, 390, 369, 173, 14),
            new PdfFormFieldSpec("vertragsbeginn", PdfFormFieldKind.Text, 1, 193, 541, 85, 14),
            new PdfFormFieldSpec("uebergabe_datum", PdfFormFieldKind.Text, 1, 357, 541, 85, 14),
            new PdfFormFieldSpec("vereinsangaben_fusszeile", PdfFormFieldKind.MultilineText, 1, 40, 790, 260, 36),
            new PdfFormFieldSpec("vorstand_fusszeile", PdfFormFieldKind.MultilineText, 1, 365, 790, 188, 36),
            new PdfFormFieldSpec("weitere_vereinbarungen", PdfFormFieldKind.MultilineText, 3, 42, 544, 510, 32),
            new PdfFormFieldSpec("ort_datum", PdfFormFieldKind.Text, 3, 110, 603, 276, 14),
            new PdfFormFieldSpec("unterschrift_paechter_1", PdfFormFieldKind.SignaturePlaceholder, 3, 110, 631, 129, 28),
            new PdfFormFieldSpec("unterschrift_paechter_2", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 631, 113, 28),
            new PdfFormFieldSpec("vereinsunterzeichner_1", PdfFormFieldKind.MultilineText, 3, 153, 714, 131, 29),
            new PdfFormFieldSpec("unterschrift_vorstand_1", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 717, 113, 28),
            new PdfFormFieldSpec("vereinsunterzeichner_2", PdfFormFieldKind.MultilineText, 3, 153, 757, 131, 29),
            new PdfFormFieldSpec("unterschrift_vorstand_2", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 760, 113, 28)
        };

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
            CancellationToken cancellationToken = default,
            int? vertragsnummer = null)
        {
            ArgumentNullException.ThrowIfNull(templateProvider);
            const string documentType = "pachtvertrag";
            const string operation = "preview";
            const string fieldSpecVersion = "pachtvertrag-field-spec/v9";
            const string rendererVersion = "top-left/v1";
            PdfTemplateAsset template;
            try
            {
                template = await templateProvider.GetActiveAsync(new PdfTemplateRequest(documentType, fieldSpecVersion, rendererVersion), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DocumentPipelineException("TEMPLATE_LOAD", documentType, operation, "Die Pachtvertrag-Vorlage konnte nicht geladen werden.", innerException: ex);
            }

            try
            {
                ValidateTemplate(template);
            }
            catch (Exception ex)
            {
                throw new DocumentPipelineException("FIELD_SPEC", documentType, operation, "Die Feldspezifikation der Pachtvertrag-Vorlage ist nicht kompatibel.", template.Version, rendererVersion, ex);
            }

            try
            {
                return CreateUploadRequestCore(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn,
                    altvertragDatum, gesetzlicherVertreterSnapshot, bankverbindungSnapshot, status, paechter2Override, template, vertragsnummer);
            }
            catch (DocumentPipelineException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DocumentPipelineException("RENDER", documentType, operation, "Der Pachtvertrag konnte nicht gerendert werden.", template.Version, rendererVersion, ex);
            }
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
            PdfTemplateAsset template,
            int? vertragsnummer)
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

            if (!saison.PachtFaelligAm.HasValue)
                throw new InvalidOperationException($"Für die Saison {saison.Jahr} fehlt pacht_faellig_am.");

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
                paechter2Override,
                gesetzlicherVertreterSnapshot != null,
                null,
                vertragsnummer);

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
                FileContent = content,
                SignaturePlaceholders = GetSignaturePlaceholders(template.Fields)
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
            MitgliedRecord? paechter2Override,
            bool zweiteParteiIstGesetzlicherVertreter,
            string? vereinsunterlagenNachweis,
            int? vertragsnummer)
        {
            var paechter2 = paechter2Override ?? nebenmitglied;

            return new PachtvertragTemplateContext
            {
                Vertragsnummer = vertragsnummer,
                VereinskonfigurationSnapshot = bankverbindungSnapshot,
                Paechter1 = hauptmitglied,
                Paechter2 = paechter2,
                ZweiteParteiIstGesetzlicherVertreter = zweiteParteiIstGesetzlicherVertreter && paechter2 != null,
                ParzelleNummer = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? "nicht verfügbar" : parzelle.GartenNr.Trim(),
                ParzelleFlaecheQm = parzelle.FlaecheQm!.Value,
                ParzelleAnlage = parzelle.Anlage,
                Pachtbeginn = vertragsbeginn,
                PachtProQm = saison.PachtProQm!.Value,
                ZahlungszielText = saison.PachtFaelligAm.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE")),
                Ausstellungsdatum = DateTime.Today,
                AltvertragDatum = altvertragDatum,
                VereinsunterlagenNachweis = vereinsunterlagenNachweis,
                UnterschriftOrt = "Zwickau"
            };
        }

        private static byte[] FillAndFlattenPdfForm(byte[] pdfBytes, PachtvertragTemplateData data, IReadOnlyList<PdfTemplateFieldSpec> loadedFields)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new InvalidOperationException("Die Pachtvertrag-PDF-Vorlage ist leer oder konnte nicht geladen werden.");

            return PdfTemplateRenderer.Render(pdfBytes, loadedFields, BuildFieldMap(data), PdfTemplateRenderProfiles.Pachtvertrag);
        }

        private static Dictionary<string, string> BuildFieldMap(PachtvertragTemplateData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vertragsnummer"] = data.Vertragsnummer,
                ["verein_name"] = data.VereinName,
                ["vereinsregister"] = data.Vereinsregister,
                ["paechter_name"] = data.Paechter1Vollname,
                ["mitgliedsnummer"] = data.Paechter1Mitgliedsnummer,
                ["paechter_geburtsdatum"] = data.Paechter1Geburtsdatum,
                ["paechter_anschrift"] = data.Paechter1Anschrift,
                ["zweite_partei_name"] = data.Paechter2Vollname,
                ["zweite_partei_mitgliedsnummer"] = data.Paechter2Mitgliedsnummer,
                ["zweite_partei_geburtsdatum"] = data.Paechter2Geburtsdatum,
                ["zweite_partei_anschrift"] = data.Paechter2Anschrift,
                ["vereinsangaben_fusszeile"] = data.VereinsangabenFusszeile,
                ["vorstand_fusszeile"] = string.Empty,

                ["parzelle_nr"] = data.ParzelleNummer,
                ["lage_anlage"] = data.ParzelleAnlage,
                ["flaeche_qm"] = data.ParzelleFlaecheQm,
                ["vertragsbeginn"] = data.Pachtbeginn,
                ["uebergabe_datum"] = data.UebergabeDatum,
                ["probephase_bis"] = data.ProbephaseBis,
                ["pachtzins_jahr"] = data.Jahrespacht,
                ["pacht_pro_qm"] = data.PachtProQm,
                ["faelligkeit"] = data.PachtzahlungFaelligBis,
                ["altvertrag_hinweis"] = data.AltvertragHinweis,
                ["vereinsunterlagen_nachweis"] = data.VereinsunterlagenNachweis,
                ["weitere_vereinbarungen"] = data.WeitereVereinbarungen,
                ["vereins_unterzeichner"] = data.VereinsUnterzeichner,

                ["ort_datum"] = string.Join(", ", new[] { data.UnterschriftOrt, data.UnterschriftDatum }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                ["paechter_druckname"] = data.Paechter1Vollname
            };
        }

        private static PdfFormFieldSpec ToInternalField(PdfTemplateFieldSpec field) => new(
            field.Name,
            field.Kind.Trim().ToLowerInvariant() switch
            {
                "checkbox" => PdfFormFieldKind.Checkbox,
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
                throw new InvalidOperationException("Die zentrale Pachtvertrag-Feldspezifikation entspricht nicht der unterstützten v8-Geometrie.");
        }

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
            int Page,
            double X,
            double Y,
            double Width,
            double Height)
        { }

        private static IReadOnlyList<Models.SignaturePlaceholder> GetSignaturePlaceholders(IReadOnlyList<PdfTemplateFieldSpec> fields)
            => fields
                .Where(field => string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase))
                .Select(field => new Models.SignaturePlaceholder
                {
                    Name = field.Name,
                    Page = field.Page,
                    X = field.X,
                    Y = field.Y,
                    Width = field.Width,
                    Height = field.Height
                })
                .ToArray();
    }
}
