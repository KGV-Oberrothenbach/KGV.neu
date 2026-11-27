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

        private static readonly IReadOnlyList<PdfFormFieldSpec> FieldSpecsV10 = new[]
        {
            new PdfFormFieldSpec("vertragsnummer", PdfFormFieldKind.Text, 1, 395, 145, 165, 15),
            new PdfFormFieldSpec("verein_name", PdfFormFieldKind.Text, 1, 210, 215, 205, 10),
            new PdfFormFieldSpec("vereinsregister", PdfFormFieldKind.Text, 1, 505, 215, 58, 10),
            new PdfFormFieldSpec("paechter_name", PdfFormFieldKind.Text, 1, 215, 245, 185, 10),
            new PdfFormFieldSpec("mitgliedsnummer", PdfFormFieldKind.Text, 1, 481, 245, 82, 10),
            new PdfFormFieldSpec("paechter_geburtsdatum", PdfFormFieldKind.Text, 1, 175, 275, 130, 10),
            new PdfFormFieldSpec("paechter_anschrift", PdfFormFieldKind.MultilineText, 1, 390, 263, 173, 22),
            new PdfFormFieldSpec("zweite_partei_name", PdfFormFieldKind.Text, 1, 215, 322, 185, 10),
            new PdfFormFieldSpec("zweite_partei_mitgliedsnummer", PdfFormFieldKind.Text, 1, 481, 322, 82, 10),
            new PdfFormFieldSpec("zweite_partei_geburtsdatum", PdfFormFieldKind.Text, 1, 175, 352, 130, 10),
            new PdfFormFieldSpec("zweite_partei_anschrift", PdfFormFieldKind.MultilineText, 1, 390, 340, 173, 22),
            new PdfFormFieldSpec("altvertrag_hinweis", PdfFormFieldKind.MultilineText, 1, 40, 409, 515, 35),
            new PdfFormFieldSpec("lage_anlage", PdfFormFieldKind.Text, 1, 139, 489.5, 105, 10),
            new PdfFormFieldSpec("parzelle_nr", PdfFormFieldKind.Text, 1, 326, 489.5, 62, 10),
            new PdfFormFieldSpec("lage_beschreibung", PdfFormFieldKind.Text, 1, 439.5, 489.5, 107.5, 10),
            new PdfFormFieldSpec("flaeche_qm", PdfFormFieldKind.Text, 1, 93.5, 518.5, 45, 10),
            new PdfFormFieldSpec("flaechenbasis_nachweis", PdfFormFieldKind.Text, 1, 374.5, 518.5, 175, 10),
            new PdfFormFieldSpec("vertragsbeginn", PdfFormFieldKind.Text, 1, 193, 544, 85, 10),
            new PdfFormFieldSpec("uebergabe_datum", PdfFormFieldKind.Text, 1, 357.2, 544, 85, 10),
            new PdfFormFieldSpec("pacht_pro_qm_zahl", PdfFormFieldKind.Text, 1, 92, 648, 45, 10),
            new PdfFormFieldSpec("flaeche_qm_wiederholung", PdfFormFieldKind.Text, 1, 335, 648, 47, 10),
            new PdfFormFieldSpec("pachtzins_jahr_zahl", PdfFormFieldKind.Text, 1, 465, 648, 48, 10),
            new PdfFormFieldSpec("faelligkeit", PdfFormFieldKind.Text, 1, 105, 674, 84, 10),
            new PdfFormFieldSpec("vereinsangaben_fusszeile", PdfFormFieldKind.MultilineText, 1, 40, 790, 260, 36),
            new PdfFormFieldSpec("vorstand_fusszeile", PdfFormFieldKind.MultilineText, 1, 365, 790, 188, 36),
            new PdfFormFieldSpec("weitere_vereinbarungen", PdfFormFieldKind.MultilineText, 3, 42, 537, 510, 42),
            new PdfFormFieldSpec("ort_datum", PdfFormFieldKind.Text, 3, 110, 607, 276, 10),
            new PdfFormFieldSpec("unterschrift_paechter_1", PdfFormFieldKind.SignaturePlaceholder, 3, 110, 630, 129, 29),
            new PdfFormFieldSpec("unterschrift_paechter_2", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 630, 113, 29),
            new PdfFormFieldSpec("vereinsunterzeichner_1", PdfFormFieldKind.MultilineText, 3, 153, 712, 131, 30),
            new PdfFormFieldSpec("unterschrift_vorstand_1", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 713, 113, 29),
            new PdfFormFieldSpec("vereinsunterzeichner_2", PdfFormFieldKind.MultilineText, 3, 153, 755, 131, 30),
            new PdfFormFieldSpec("unterschrift_vorstand_2", PdfFormFieldKind.SignaturePlaceholder, 3, 357, 756, 113, 29),
            new PdfFormFieldSpec("zweite_partei_rolle", PdfFormFieldKind.Text, 1, 80, 322, 95, 13),
            new PdfFormFieldSpec("zweite_partei_signatur_rolle", PdfFormFieldKind.Text, 3, 284, 650, 72, 12)
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
                template = await templateProvider.GetActiveAsync(new PdfTemplateRequest(documentType, fieldSpecVersion, rendererVersion) { AdditionalFieldSpecVersions = new[] { "pachtvertrag-field-spec/v10" } }, cancellationToken);
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

            var values = BuildFieldMap(data);
            if (loadedFields.Any(field => field.Name == "zweite_partei_rolle"))
            {
                var role = string.IsNullOrEmpty(data.Paechter2Vollname) ? string.Empty :
                    data.ZweiteParteiBezeichnung == "Gesetzliche/r Vertreter/in" ? "Gesetzl. Vertretung" : "Mitpächter/in 2";
                values["zweite_partei_rolle"] = role.Length == 0 ? "Keine zweite Partei" : role;
                values["zweite_partei_signatur_rolle"] = role;
                values["paechter_anschrift"] = data.Paechter1Anschrift.Replace(", ", "\n");
                values["zweite_partei_anschrift"] = data.Paechter2Anschrift.Replace(", ", "\n");
                values["flaeche_qm_wiederholung"] = data.ParzelleFlaecheQmWiederholung;
                values["pacht_pro_qm_zahl"] = data.PachtProQm.Replace(" €/m²", string.Empty);
                values["pachtzins_jahr_zahl"] = data.Jahrespacht.Replace(" €", string.Empty);
                values["lage_beschreibung"] = string.Empty;
                values["flaechenbasis_nachweis"] = string.Empty;
            }
            return PdfTemplateRenderer.Render(pdfBytes, loadedFields, values, loadedFields.Any(field => field.Name == "zweite_partei_rolle") ? PdfTemplateRenderProfiles.PachtvertragV10 : PdfTemplateRenderProfiles.Pachtvertrag);
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

            var fieldSpecs = template.FieldSpecVersion switch
            {
                "pachtvertrag-field-spec/v9" => FieldSpecs,
                "pachtvertrag-field-spec/v10" => FieldSpecsV10,
                _ => throw new InvalidOperationException("Nicht unterstützte Pachtvertrag-Feldspezifikation.")
            };
            var expected = fieldSpecs.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            var actual = template.Fields.Select(ToInternalField).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (expected.Length != actual.Length || expected.Where((field, index) =>
                    !string.Equals(field.Name, actual[index].Name, StringComparison.Ordinal)
                    || field.Kind != actual[index].Kind
                    || field.Page != actual[index].Page
                    || field.X != actual[index].X || field.Y != actual[index].Y
                    || field.Width != actual[index].Width || field.Height != actual[index].Height).Any())
                throw new InvalidOperationException("Die zentrale Pachtvertrag-Feldspezifikation entspricht nicht der unterstützten versionsgebundenen Geometrie.");
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
