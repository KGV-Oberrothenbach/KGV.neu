using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

public sealed class PdfSignatureRendererTests
{
    [Fact]
    public void RenderPlan_centers_signature_and_preserves_aspect_ratio()
    {
        var operation = Assert.Single(PdfSignatureRenderer.BuildRenderPlan(new[]
        {
            (new SignaturePlaceholder { Name = "signature", Page = 1, X = 10, Y = 20, Width = 100, Height = 100 }, Capture(200, 100))
        }));

        Assert.Equal(100, operation.Width, 6);
        Assert.Equal(50, operation.Height, 6);
        Assert.Equal(10, operation.X, 6);
        Assert.Equal(45, operation.Y, 6);
        Assert.Equal(.5, operation.Scale, 6);
    }

    [Fact]
    public void Render_supports_small_and_large_signatures_without_adding_pages()
    {
        var pdf = CreatePdf(2);
        var rendered = PdfSignatureRenderer.Render(pdf, new[]
        {
            (new SignaturePlaceholder { Name = "small", Page = 1, X = 10, Y = 10, Width = 30, Height = 20 }, Capture(20, 20)),
            (new SignaturePlaceholder { Name = "large", Page = 2, X = 10, Y = 10, Width = 100, Height = 40 }, Capture(2000, 500))
        });

        using var document = PdfReader.Open(new MemoryStream(rendered), PdfDocumentOpenMode.InformationOnly);
        Assert.Equal(2, document.Pages.Count);
    }

    [Fact]
    public async Task Canonical_template_placeholders_cover_membership_and_lease_signature_counts()
    {
        var provider = new CanonicalPdfTemplateProvider();
        var membership = ToPlaceholders((await provider.GetActiveAsync(new PdfTemplateRequest("mitgliedsantrag", "mitgliedsantrag-field-spec/v1", "pdfsharpcore-ma/v1"))).Fields);
        var lease = ToPlaceholders((await provider.GetActiveAsync(new PdfTemplateRequest("pachtvertrag", "pachtvertrag-field-spec/v8", "top-left/v1"))).Fields);

        var membershipAlone = new[] { "unterschrift_antragsteller", "unterschrift_verein", "datenschutz_unterschrift_antragsteller" };
        var membershipWithSecondParty = membershipAlone.Concat(new[] { "unterschrift_vertreter", "datenschutz_unterschrift_vertreter" }).ToArray();
        var leaseAlone = new[] { "unterschrift_paechter_1", "unterschrift_vorstand_1", "unterschrift_vorstand_2" };
        var leaseWithSecondParty = leaseAlone.Concat(new[] { "unterschrift_paechter_2" }).ToArray();

        Assert.Equal(3, BuildPlan(membership, membershipAlone).Count);
        Assert.Equal(5, BuildPlan(membership, membershipWithSecondParty).Count);
        Assert.All(BuildPlan(membership, membershipWithSecondParty), operation => Assert.Equal(1, operation.Page));
        Assert.Equal(3, BuildPlan(lease, leaseAlone).Count);
        Assert.Equal(4, BuildPlan(lease, leaseWithSecondParty).Count);
        Assert.All(BuildPlan(lease, leaseWithSecondParty), operation => Assert.Equal(3, operation.Page));
    }

    [Fact]
    public void Render_rejects_empty_capture_and_missing_page()
    {
        var placeholder = new SignaturePlaceholder { Name = "signature", Page = 2, X = 0, Y = 0, Width = 10, Height = 10 };
        var empty = new DigitalSignatureCapture { CanvasWidth = 10, CanvasHeight = 10 };

        var emptyError = Assert.Throws<InvalidOperationException>(() => PdfSignatureRenderer.Render(CreatePdf(1), new[] { (placeholder, empty) }));
        Assert.Contains("leer", emptyError.Message, StringComparison.OrdinalIgnoreCase);

        var pageError = Assert.Throws<InvalidOperationException>(() => PdfSignatureRenderer.Render(CreatePdf(1), new[] { (placeholder, Capture(10, 10)) }));
        Assert.Contains("nicht vorhandene Seite", pageError.Message);
    }

    private static IReadOnlyList<PdfSignatureRenderOperation> BuildPlan(IReadOnlyList<SignaturePlaceholder> placeholders, IEnumerable<string> names)
    {
        var map = placeholders.ToDictionary(placeholder => placeholder.Name, StringComparer.OrdinalIgnoreCase);
        return PdfSignatureRenderer.BuildRenderPlan(names.Select(name => (map[name], Capture(100, 50))).ToArray());
    }

    private static IReadOnlyList<SignaturePlaceholder> ToPlaceholders(IReadOnlyList<PdfTemplateFieldSpec> fields) => fields
        .Where(field => string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase))
        .Select(field => new SignaturePlaceholder { Name = field.Name, Page = field.Page, X = field.X, Y = field.Y, Width = field.Width, Height = field.Height })
        .ToArray();

    private static byte[] CreatePdf(int pageCount)
    {
        using var document = new PdfDocument();
        for (var index = 0; index < pageCount; index++) document.AddPage();
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static DigitalSignatureCapture Capture(double width, double height) => new()
    {
        CanvasWidth = width,
        CanvasHeight = height,
        Strokes = new[] { new DigitalSignatureStroke { Points = new[] { new DigitalSignaturePoint { X = 0, Y = 0 }, new DigitalSignaturePoint { X = width, Y = height } } } }
    };
}
