using System;
using System.IO;
using KGV.Core.Models;

namespace KGV.Core.Utilities
{
    public static class PachtvertragDokumentFactory
    {
        private const string TemplateResourceName = "KGV.Core.Templates.PachtvertragTemplate.html";

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, string? status = null)
            => CreateUploadRequest(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn, null, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot, string? status = null)
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
            var templateContext = BuildTemplateContext(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn.Date, bankverbindungSnapshot);
            var renderedHtml = PachtvertragTemplateFactory.CreateRenderedHtml(LoadTemplateHtml(), templateContext);
            EnsureTemplateFullyRendered(renderedHtml);
            var content = PachtvertragHtmlPdfRenderer.Build(title, renderedHtml);

            return new DokumentUploadRequest
            {
                MitgliedId = hauptmitglied.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = content
            };
        }

        private static PachtvertragTemplateContext BuildTemplateContext(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            return new PachtvertragTemplateContext
            {
                KgvLogoDataUri = BuildLogoDataUri(),
                VereinskonfigurationSnapshot = bankverbindungSnapshot,
                Paechter1 = hauptmitglied,
                Paechter2 = nebenmitglied,
                ParzelleNummer = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim(),
                ParzelleFlaecheQm = parzelle.FlaecheQm!.Value,
                Pachtbeginn = vertragsbeginn,
                PachtProQm = saison.PachtProQm!.Value,
                BankeinzugVereinbart = false,
                Ausstellungsdatum = DateTime.Today
            };
        }

        private static string LoadTemplateHtml()
        {
            var assembly = typeof(PachtvertragDokumentFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream(TemplateResourceName)
                ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage ist nicht im Projekt eingebunden.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void EnsureTemplateFullyRendered(string renderedHtml)
        {
            if (string.IsNullOrWhiteSpace(renderedHtml))
                throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage konnte nicht gerendert werden.");
            if (renderedHtml.Contains("{{", StringComparison.Ordinal) || renderedHtml.Contains("}}", StringComparison.Ordinal))
                throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält noch nicht aufgelöste Platzhalter.");
        }

        private static string BuildLogoDataUri()
            => $"data:image/png;base64,{Convert.ToBase64String(VereinsdokumentBranding.GetLogoBytes())}";
    }
}