using KGV.Core.Models;

namespace KGV.Core.Utilities;

/// <summary>Technical transition from a rendered preview to a signed upload request.</summary>
public static class DocumentGenerationPipeline
{
    public static DokumentUploadRequest ApplySignatures(
        DokumentUploadRequest preview,
        DokumentUploadRequest final,
        IReadOnlyList<PdfSignatureCaptureAssignment> signatureCaptures)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(final);
        ArgumentNullException.ThrowIfNull(signatureCaptures);

        if (preview.FileContent.Length == 0)
            throw new InvalidOperationException("Die PDF-Vorschau konnte nicht erzeugt werden.");

        var placeholders = preview.SignaturePlaceholders
            .ToDictionary(placeholder => placeholder.Name, StringComparer.OrdinalIgnoreCase);
        var signatures = new List<(SignaturePlaceholder placeholder, DigitalSignatureCapture capture)>();

        foreach (var assignment in signatureCaptures)
        {
            if (assignment.Capture == null || !assignment.Capture.HasContent)
                throw new InvalidOperationException($"Die Signatur für '{assignment.PlaceholderName}' ist leer.");
            if (!placeholders.TryGetValue(assignment.PlaceholderName, out var placeholder))
                throw new InvalidOperationException($"Die Signaturfläche '{assignment.PlaceholderName}' fehlt in der Dokumentvorlage.");

            signatures.Add((placeholder, assignment.Capture));
        }

        final.FileContent = PdfSignatureRenderer.Render(preview.FileContent, signatures);
        return final;
    }
}

public sealed record PdfSignatureCaptureAssignment(string PlaceholderName, DigitalSignatureCapture Capture);
