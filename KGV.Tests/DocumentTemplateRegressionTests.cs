using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

/// <summary>Regression checks for the canonical published template source assets and their field contracts.</summary>
public sealed class DocumentTemplateRegressionTests
{
    [Fact]
    public async Task Membership_application_adult_preview_and_final_keep_one_page_and_expected_signers()
    {
        var preview = await MitgliedsantragDokumentFactory.CreateUploadRequestAsync(Member(), 120m, 25m, new DateTime(2026, 4, 1), null, MitgliedsantragZweitePrivateParteiArt.Keine, Club(), FormularDokumentStatus.Unsigniert, Provider);

        AssertValidPdf(preview.FileContent, 2);
        Assert.Equal(6, preview.SignaturePlaceholders.Count);
        Assert.Contains(preview.SignaturePlaceholders, x => x.Name == "unterschrift_antragsteller_antrag");
        Assert.Contains(preview.SignaturePlaceholders, x => x.Name == "unterschrift_antragsteller_datenschutz");

        var final = FinalFrom(preview);
        var signed = DocumentGenerationPipeline.ApplySignatures(preview, final, new[]
        {
            new PdfSignatureCaptureAssignment("unterschrift_antragsteller", Capture()),
            new PdfSignatureCaptureAssignment("unterschrift_verein", Capture()),
            new PdfSignatureCaptureAssignment("datenschutz_unterschrift_antragsteller", Capture())
        });
        AssertValidPdf(signed.FileContent, 1);
    }

    [Fact]
    public async Task Membership_application_guardian_keeps_all_secondary_signers_on_the_template_page()
    {
        var guardian = new MitgliedsantragVertreterSnapshot { Vorname = "Erika", Nachname = "Musterfrau", Adresse = "Nebenweg 2", Plz = "08000", Ort = "Zwickau" };
        var request = await MitgliedsantragDokumentFactory.CreateUploadRequestAsync(Member(), 120m, 25m, new DateTime(2026, 4, 1), guardian, MitgliedsantragZweitePrivateParteiArt.GesetzlicherVertreter, Club(), FormularDokumentStatus.Unsigniert, Provider);

        AssertValidPdf(request.FileContent, 2);
        Assert.Contains(request.SignaturePlaceholders, x => x.Name == "unterschrift_zweite_partei_antrag");
        Assert.Contains(request.SignaturePlaceholders, x => x.Name == "unterschrift_zweite_partei_datenschutz");
        Assert.Contains(request.SignaturePlaceholders, placeholder => placeholder.Page == 2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Lease_contract_scenarios_keep_three_pages_and_template_signature_contract(bool secondaryMember, bool guardian)
    {
        var secondParty = secondaryMember ? new MitgliedRecord { Id = 18, Mitgliedsnummer = 1018, Vorname = "Erika", Name = "Musterfrau" } : null;
        var guardianSnapshot = guardian ? new MitgliedsantragVertreterSnapshot { Vorname = "Eva", Nachname = "Vertreterin" } : null;
        var request = await PachtvertragDokumentFactory.CreateUploadRequestAsync(
            Member(), secondParty, Parcel(), Season(), new DateTime(2026, 4, 1), null, guardianSnapshot, Club(), FormularDokumentStatus.Unsigniert, Provider,
            paechter2Override: guardian ? new MitgliedRecord { Id = 19, Mitgliedsnummer = 1019, Vorname = "Eva", Name = "Vertreterin" } : null);

        AssertValidPdf(request.FileContent, 3);
        Assert.Equal(4, request.SignaturePlaceholders.Count);
        Assert.All(request.SignaturePlaceholders, placeholder => Assert.Equal(3, placeholder.Page));
        Assert.Contains(request.SignaturePlaceholders, x => x.Name == "unterschrift_paechter_2");
    }

    [Fact]
    public async Task Canonical_signature_placeholders_are_unique_positive_and_inside_their_production_templates()
    {
        var membership = await Provider.GetActiveAsync(new PdfTemplateRequest("mitgliedsantrag", "mitgliedsantrag-field-spec/v5", "top-left/v1"));
        var lease = await Provider.GetActiveAsync(new PdfTemplateRequest("pachtvertrag", "pachtvertrag-field-spec/v9", "top-left/v1"));
        AssertPlaceholderPages(ToPlaceholders(membership.Fields), 1, 1, 2, 2, 2, 2);
        AssertPlaceholderPages(ToPlaceholders(lease.Fields), 3, 3, 3, 3);
    }

    private static void AssertPlaceholderPages(IReadOnlyList<SignaturePlaceholder> placeholders, params int[] expectedPages)
    {
        Assert.Equal(placeholders.Count, placeholders.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(expectedPages.Order(), placeholders.Select(x => x.Page).Order());
        Assert.All(placeholders, placeholder =>
        {
            Assert.True(placeholder.X >= 0 && placeholder.Y >= 0);
            Assert.True(placeholder.Width > 0 && placeholder.Height > 0);
        });
    }

    private static IReadOnlyList<SignaturePlaceholder> ToPlaceholders(IReadOnlyList<PdfTemplateFieldSpec> fields) => fields
        .Where(field => string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase))
        .Select(field => new SignaturePlaceholder { Name = field.Name, Page = field.Page, X = field.X, Y = field.Y, Width = field.Width, Height = field.Height })
        .ToArray();

    private static readonly CanonicalPdfTemplateProvider Provider = new();

    private static void AssertValidPdf(byte[] content, int pages)
    {
        using var pdf = PdfReader.Open(new MemoryStream(content), PdfDocumentOpenMode.InformationOnly);
        Assert.Equal(pages, pdf.Pages.Count);
    }

    private static DokumentUploadRequest FinalFrom(DokumentUploadRequest preview) => new()
    {
        MitgliedId = preview.MitgliedId,
        ParzelleId = preview.ParzelleId,
        Titel = preview.Titel,
        FileName = preview.FileName,
        MimeType = preview.MimeType,
        SignaturePlaceholders = preview.SignaturePlaceholders
    };

    private static MitgliedRecord Member() => new()
    {
        Id = 17, Mitgliedsnummer = 1017, Vorname = "Max", Name = "Mustermann", Geburtsdatum = new DateTime(1980, 1, 1),
        Adresse = "Gartenweg 1", Plz = "08000", Ort = "Zwickau", Email = "max@example.test", Telefon = "0123"
    };

    private static ParzelleRecord Parcel() => new() { Id = 42, GartenNr = "42", Anlage = "Nord", FlaecheQm = 300m };
    private static SaisonRecord Season() => new() { Jahr = 2026, PachtProQm = .25m, PachtFaelligAm = new DateTime(2026, 3, 31) };
    private static VereinskonfigurationSnapshot Club() => new()
    {
        VereinName = "KGV Musterverein", VereinRegisterangabe = "VR 123", DokumentOrt = "Zwickau",
        Kontoinhaber = "KGV Musterverein", Bankname = "Musterbank", Iban = "DE00123456780000000000", Bic = "MUSTDEFFXXX"
    };

    private static DigitalSignatureCapture Capture() => new()
    {
        CanvasWidth = 100, CanvasHeight = 30,
        Strokes = new[] { new DigitalSignatureStroke { Points = new[] { new DigitalSignaturePoint { X = 0, Y = 0 }, new DigitalSignaturePoint { X = 100, Y = 30 } } } }
    };
}
