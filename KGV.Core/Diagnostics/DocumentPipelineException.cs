namespace KGV.Core.Diagnostics;

/// <summary>Safe, technical classification for a document-pipeline failure.</summary>
public sealed class DocumentPipelineException : InvalidOperationException
{
    public DocumentPipelineException(
        string stage,
        string documentType,
        string operation,
        string message,
        string? templateVersion = null,
        string? rendererVersion = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Stage = stage;
        DocumentType = documentType;
        Operation = operation;
        TemplateVersion = templateVersion;
        RendererVersion = rendererVersion;
    }

    public string Stage { get; }
    public string DocumentType { get; }
    public string Operation { get; }
    public string? TemplateVersion { get; }
    public string? RendererVersion { get; }
    public string DiagnosticCode => $"PIPELINE_{Stage}";
}
