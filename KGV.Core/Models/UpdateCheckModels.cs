namespace KGV.Core.Models
{
    public sealed class UpdateCheckInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotesUrl { get; set; }
        public bool IsMandatory { get; set; }
    }

    public sealed class UpdateCheckResult
    {
        public bool Success { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string? Message { get; set; }
        public UpdateCheckInfo? Update { get; set; }
    }
}
