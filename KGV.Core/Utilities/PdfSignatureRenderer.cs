using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities;

/// <summary>Places captured signatures into validated, template-defined placeholders.</summary>
public static class PdfSignatureRenderer
{
    public static IReadOnlyList<PdfSignatureRenderOperation> BuildRenderPlan(
        IReadOnlyList<(SignaturePlaceholder placeholder, DigitalSignatureCapture capture)> signatures)
    {
        ArgumentNullException.ThrowIfNull(signatures);

        return signatures.Select(item => BuildOperation(item.placeholder, item.capture)).ToArray();
    }

    public static byte[] Render(
        byte[] originalPdfContent,
        IReadOnlyList<(SignaturePlaceholder placeholder, DigitalSignatureCapture capture)> signatures)
    {
        if ((originalPdfContent?.Length ?? 0) <= 0)
            throw new InvalidOperationException("Die Dokumentfassung konnte nicht geladen werden.");

        var plan = BuildRenderPlan(signatures);
        PdfSharpFontResolverInitializer.EnsureInitialized();

        using var inputStream = new MemoryStream(originalPdfContent!, writable: false);
        using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

        for (var index = 0; index < plan.Count; index++)
        {
            var operation = plan[index];
            if (operation.Page > document.Pages.Count)
                throw new InvalidOperationException($"Die Signaturfläche '{operation.Name}' verweist auf die nicht vorhandene Seite {operation.Page}.");

            var page = document.Pages[operation.Page - 1];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
            DrawSignature(graphics, operation, signatures[index].capture);
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static PdfSignatureRenderOperation BuildOperation(SignaturePlaceholder placeholder, DigitalSignatureCapture capture)
    {
        ArgumentNullException.ThrowIfNull(placeholder);
        ArgumentNullException.ThrowIfNull(capture);

        if (string.IsNullOrWhiteSpace(placeholder.Name))
            throw new InvalidOperationException("Die Signaturfläche hat keinen Namen.");
        if (placeholder.Page < 1)
            throw new InvalidOperationException($"Die Signaturfläche '{placeholder.Name}' hat eine ungültige Seite.");
        if (!double.IsFinite(placeholder.X) || !double.IsFinite(placeholder.Y)
            || !double.IsFinite(placeholder.Width) || !double.IsFinite(placeholder.Height)
            || placeholder.Width <= 0 || placeholder.Height <= 0)
            throw new InvalidOperationException($"Die Signaturfläche '{placeholder.Name}' hat ungültige Abmessungen.");
        if (!capture.HasContent)
            throw new InvalidOperationException($"Die Signatur für '{placeholder.Name}' ist leer.");

        var scale = Math.Min(placeholder.Width / capture.CanvasWidth, placeholder.Height / capture.CanvasHeight);
        var width = capture.CanvasWidth * scale;
        var height = capture.CanvasHeight * scale;
        return new PdfSignatureRenderOperation(
            placeholder.Name,
            placeholder.Page,
            placeholder.X + (placeholder.Width - width) / 2d,
            placeholder.Y + (placeholder.Height - height) / 2d,
            width,
            height,
            scale);
    }

    private static void DrawSignature(XGraphics graphics, PdfSignatureRenderOperation operation, DigitalSignatureCapture signature)
    {
        var pen = new XPen(XColor.FromArgb(33, 33, 33), 2.2);
        foreach (var stroke in signature.Strokes.Where(stroke => stroke?.Points?.Count > 0))
        {
            var points = stroke.Points
                .Select(point => new XPoint(operation.X + point.X * operation.Scale, operation.Y + point.Y * operation.Scale))
                .ToArray();

            if (points.Length == 1)
            {
                graphics.DrawEllipse(XBrushes.Black, points[0].X, points[0].Y, 2, 2);
                continue;
            }

            for (var index = 1; index < points.Length; index++)
                graphics.DrawLine(pen, points[index - 1], points[index]);
        }
    }
}

public sealed record PdfSignatureRenderOperation(string Name, int Page, double X, double Y, double Width, double Height, double Scale);
