using KGV.Core.Models;
using KGV.Core.Diagnostics;

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
            throw Failure("Die PDF-Vorschau konnte nicht erzeugt werden.");

        var placeholders = preview.SignaturePlaceholders
            .ToDictionary(placeholder => placeholder.Name, StringComparer.OrdinalIgnoreCase);
        var signatures = new List<(SignaturePlaceholder placeholder, DigitalSignatureCapture capture)>();

        foreach (var assignment in signatureCaptures)
        {
            if (assignment.Capture == null || !assignment.Capture.HasContent)
                throw Failure("Eine erforderliche Signatur ist leer.");
            if (!placeholders.TryGetValue(assignment.PlaceholderName, out var placeholder))
                throw Failure("Eine erforderliche Signaturfläche fehlt in der Dokumentvorlage.");

            signatures.Add((placeholder, assignment.Capture));
        }

        try
        {
            final.FileContent = PdfSignatureRenderer.Render(preview.FileContent, signatures);
        }
        catch (Exception ex) when (ex is not DocumentPipelineException)
        {
            throw Failure("Die Signaturen konnten nicht in das Dokument eingefügt werden.", ex);
        }
        return final;
    }

    private static DocumentPipelineException Failure(string message, Exception? innerException = null)
        => new("SIGNATURE", "generated-document", "final", message, innerException: innerException);
}

public sealed record PdfSignatureCaptureAssignment(string PlaceholderName, DigitalSignatureCapture Capture);
