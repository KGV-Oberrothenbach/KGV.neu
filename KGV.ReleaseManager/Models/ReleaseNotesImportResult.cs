namespace KGV.ReleaseManager.Models;

public sealed class ReleaseNotesImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string WpfReleaseText { get; set; } = string.Empty;
    public string AndroidReleaseText { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
}
