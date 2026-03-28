namespace KGV.ReleaseManager.Models;

public sealed class ReleaseNotesAnalysisResult
{
    public bool Success { get; set; }
    public bool HasAnchor { get; set; }
    public bool IsSuggestedStartState { get; set; }
    public string Message { get; set; } = string.Empty;
    public string LogSourcePath { get; set; } = string.Empty;
    public string LastKnownReleaseText { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
    public string AnchorHeading { get; set; } = string.Empty;
    public string ChangesPreview { get; set; } = string.Empty;
    public string ExportText { get; set; } = string.Empty;
}
