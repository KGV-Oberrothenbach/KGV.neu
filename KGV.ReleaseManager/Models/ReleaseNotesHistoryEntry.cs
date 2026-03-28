namespace KGV.ReleaseManager.Models;

public sealed class ReleaseNotesHistoryEntry
{
    public string Version { get; set; } = string.Empty;
    public DateTime SavedAtUtc { get; set; }
    public string LogSourcePath { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
    public string LogAnchorHeading { get; set; } = string.Empty;
    public string ExportText { get; set; } = string.Empty;
    public string RawLogExcerpt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string WpfReleaseText { get; set; } = string.Empty;
    public string AndroidReleaseText { get; set; } = string.Empty;
    public string ImportedRawText { get; set; } = string.Empty;
}
