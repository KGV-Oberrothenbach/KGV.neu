using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

public sealed class SignedVertragsdokumentPdfBuilderTests
{
    [Fact]
    public void Build_AcceptsThirdSignatureForVorstand()
    {
        var originalPdf = CreateSinglePagePdf();

        var signedPdf = SignedVertragsdokumentPdfBuilder.Build(
            new MitgliedRecord { Id = 92, Vorname = "Alexandra", Name = "Bräuer" },
            new DocumentInfo
            {
                Title = "Pachtvertrag (unsigniert)",
                Dateiname = "Braeuer_Alexandra-92-2026-09-20-pachtvertrag-unsigniert.pdf"
            },
            originalPdf,
            CreateSignature(),
            CreateSignature(),
            CreateSignature(),
            "Unterschrift Pächter/in",
            "Unterschrift gesetzliche/r Vertreter/in",
            "Unterschrift Vorstand / Verpächter");

        using var stream = new MemoryStream(signedPdf, writable: false);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        Assert.Equal(2, document.Pages.Count);
        Assert.True(signedPdf.Length > originalPdf.Length);
    }

    private static byte[] CreateSinglePagePdf()
    {
        using var document = new PdfDocument();
        document.AddPage();
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static DigitalSignatureCapture CreateSignature() => new()
    {
        CanvasWidth = 200,
        CanvasHeight = 100,
        SignedAt = new DateTime(2026, 9, 20, 12, 0, 0),
        Strokes = new[]
        {
            new DigitalSignatureStroke
            {
                Points = new[]
                {
                    new DigitalSignaturePoint { X = 10, Y = 10 },
                    new DigitalSignaturePoint { X = 180, Y = 80 }
                }
            }
        }
    };
}
