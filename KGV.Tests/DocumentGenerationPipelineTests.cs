using KGV.Core.Models;
using KGV.Core.Interfaces;
using KGV.Core.Diagnostics;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

public sealed class DocumentGenerationPipelineTests
{
    [Fact]
    public void ApplySignatures_creates_final_pdf_without_mutating_preview_metadata()
    {
        var preview = Request(CreatePdf(1));
        var final = Request(Array.Empty<byte>());
        final.Titel = "Mitgliedsantrag (signiert)";
        final.FileName = "mitgliedsantrag-signiert.pdf";

        var result = DocumentGenerationPipeline.ApplySignatures(preview, final, new[]
        {
            new PdfSignatureCaptureAssignment("unterschrift", Capture())
        });

        Assert.Same(final, result);
        Assert.NotEmpty(result.FileContent);
        Assert.Equal(17, result.MitgliedId);
        Assert.Null(result.ParzelleId);
        Assert.Equal("Mitgliedsantrag (signiert)", result.Titel);
        Assert.Equal("Mitgliedsantrag (unsigniert)", preview.Titel);
    }

    [Fact]
    public void ApplySignatures_preserves_parcel_owned_lease_request()
    {
        var preview = Request(CreatePdf(3));
        preview.MitgliedId = null;
        preview.ParzelleId = 42;
        var final = Request(Array.Empty<byte>());
        final.MitgliedId = null;
        final.ParzelleId = 42;

        var result = DocumentGenerationPipeline.ApplySignatures(preview, final, new[]
        {
            new PdfSignatureCaptureAssignment("unterschrift", Capture())
        });

        using var document = PdfReader.Open(new MemoryStream(result.FileContent), PdfDocumentOpenMode.InformationOnly);
        Assert.Equal(3, document.Pages.Count);
        Assert.Null(result.MitgliedId);
        Assert.Equal(42, result.ParzelleId);
    }

    [Fact]
    public void ApplySignatures_fails_before_final_request_for_missing_placeholder_or_render_input()
    {
        var final = Request(Array.Empty<byte>());
        var missing = Assert.Throws<DocumentPipelineException>(() => DocumentGenerationPipeline.ApplySignatures(
            Request(CreatePdf(1)), final, new[] { new PdfSignatureCaptureAssignment("missing", Capture()) }));
        Assert.Equal("SIGNATURE", missing.Stage);
        Assert.Equal("PIPELINE_SIGNATURE", missing.DiagnosticCode);
        Assert.Empty(final.FileContent);

        var noPreview = Assert.Throws<DocumentPipelineException>(() => DocumentGenerationPipeline.ApplySignatures(
            Request(Array.Empty<byte>()), Request(Array.Empty<byte>()), Array.Empty<PdfSignatureCaptureAssignment>()));
        Assert.Contains("Vorschau", noPreview.Message);
        Assert.DoesNotContain("unterschrift", missing.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Template_load_failure_has_a_safe_pipeline_stage()
    {
        var provider = new FailingTemplateProvider(new InvalidOperationException("token=must-not-reach-the-user"));

        var error = await Assert.ThrowsAsync<DocumentPipelineException>(() => MitgliedsantragDokumentFactory.CreateUploadRequestAsync(
            new MitgliedRecord(), 0m, 0m, DateTime.Today, null,
            MitgliedsantragZweitePrivateParteiArt.Keine, null, null, provider));

        Assert.Equal("TEMPLATE_LOAD", error.Stage);
        Assert.Equal("mitgliedsantrag", error.DocumentType);
        Assert.Equal("PIPELINE_TEMPLATE_LOAD", error.DiagnosticCode);
        Assert.DoesNotContain("token", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Template_factories_request_the_supported_field_spec_and_renderer_versions()
    {
        var membershipProvider = new FailingTemplateProvider(new InvalidOperationException());
        await Assert.ThrowsAsync<DocumentPipelineException>(() => MitgliedsantragDokumentFactory.CreateUploadRequestAsync(
            new MitgliedRecord(), 0m, 0m, DateTime.Today, null,
            MitgliedsantragZweitePrivateParteiArt.Keine, null, null, membershipProvider));
        Assert.Equal(new PdfTemplateRequest("mitgliedsantrag", "mitgliedsantrag-field-spec/v1", "pdfsharpcore-ma/v1"), membershipProvider.LastRequest);

        var leaseProvider = new FailingTemplateProvider(new InvalidOperationException());
        await Assert.ThrowsAsync<DocumentPipelineException>(() => PachtvertragDokumentFactory.CreateUploadRequestAsync(
            new MitgliedRecord(), null, new ParzelleRecord(), new SaisonRecord(), DateTime.Today, null, null, null, null, leaseProvider));
        Assert.Equal(new PdfTemplateRequest("pachtvertrag", "pachtvertrag-field-spec/v2", "top-left/v1"), leaseProvider.LastRequest);
    }

    private static DokumentUploadRequest Request(byte[] content) => new()
    {
        MitgliedId = 17,
        Titel = "Mitgliedsantrag (unsigniert)",
        FileName = "mitgliedsantrag-unsigniert.pdf",
        MimeType = "application/pdf",
        FileContent = content,
        SignaturePlaceholders = new[] { new SignaturePlaceholder { Name = "unterschrift", Page = 1, X = 10, Y = 10, Width = 100, Height = 30 } }
    };

    private static byte[] CreatePdf(int pages)
    {
        using var document = new PdfDocument();
        for (var index = 0; index < pages; index++) document.AddPage();
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static DigitalSignatureCapture Capture() => new()
    {
        CanvasWidth = 100,
        CanvasHeight = 30,
        Strokes = new[] { new DigitalSignatureStroke { Points = new[] { new DigitalSignaturePoint { X = 0, Y = 0 }, new DigitalSignaturePoint { X = 100, Y = 30 } } } }
    };

    private sealed class FailingTemplateProvider(Exception exception) : IPdfTemplateProvider
    {
        public PdfTemplateRequest? LastRequest { get; private set; }

        public Task<PdfTemplateAsset> GetActiveAsync(PdfTemplateRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromException<PdfTemplateAsset>(exception);
        }
    }
}
