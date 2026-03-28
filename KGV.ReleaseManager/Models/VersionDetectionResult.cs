namespace KGV.ReleaseManager.Models;

public sealed class VersionDetectionResult
{
    public string CurrentVersion { get; set; } = string.Empty;
    public bool IsCurrentVersionShared { get; set; }
    public string WpfVersion { get; set; } = string.Empty;
    public string WpfSourcePath { get; set; } = string.Empty;
    public bool IsWpfVersionDetected { get; set; }
    public string AndroidVersion { get; set; } = string.Empty;
    public string AndroidVersionCode { get; set; } = string.Empty;
    public string AndroidSourcePath { get; set; } = string.Empty;
    public bool IsAndroidVersionDetected { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
