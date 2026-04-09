using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using KGV.Core.Models;
using PdfSharpCore.Pdf.AcroForms;
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

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Pachtvertrag;
            var pachtzins = decimal.Round(parzelle.FlaecheQm.Value * saison.PachtProQm.Value, 2, MidpointRounding.AwayFromZero);
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(hauptmitglied, dokumenttyp, normalizedStatus, vertragsbeginn.Date);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);

            return new DokumentUploadRequest
            {
                MitgliedId = hauptmitglied.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = BuildPdf(hauptmitglied, nebenmitglied, parzelle, saison, vertragsbeginn.Date, pachtzins)
            };
        }

        private static byte[] BuildPdf(MitgliedRecord hauptmitglied, MitgliedRecord? nebenmitglied, ParzelleRecord parzelle, SaisonRecord saison, DateTime vertragsbeginn, decimal pachtzins)
        {
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
            SetTextField(form, "rent_display", FormatCurrency(pachtzins));
            SetTextField(form, "total_display", FormatCurrency(pachtzins));
            SetTextField(form, "member_fee_display", string.Empty);
            SetTextField(form, "rent_due_date", string.Empty);
            SetTextField(form, "sign_place", ResolveSignPlace(hauptmitglied));
            SetTextField(form, "sign_date", FormatDate(DateTime.Today));

            using var output = new MemoryStream();
            document.Save(output, false);
            return output.ToArray();
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

        private static string ResolveSignPlace(MitgliedRecord member)
            => string.IsNullOrWhiteSpace(member.Ort) ? string.Empty : member.Ort.Trim();

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatCurrency(decimal value)
            => value.ToString("0.00 €", CultureInfo.GetCultureInfo("de-DE"));

        private static string FormatNumber(decimal value)
            => value.ToString("0.##", CultureInfo.GetCultureInfo("de-DE"));
    }
}