namespace KGV.ReleaseManager.Models;

public sealed class ReleaseManagerSettings
{
    public string SourceRepoPath { get; set; } = string.Empty;
    public string WpfTargetRepoPath { get; set; } = string.Empty;
    public string ReleaseOutputRootPath { get; set; } = string.Empty;
    public string ApkOutputPath { get; set; } = string.Empty;
    public string AabOutputPath { get; set; } = string.Empty;
    public string AndroidKeystorePath { get; set; } = string.Empty;
    public string AndroidPackageName { get; set; } = "de.kgv.oberrothenbach";
    public string PlayTrackName { get; set; } = "internal";
    public string StoreUrl { get; set; } = string.Empty;
    public string InnoSetupCompilerPath { get; set; } = string.Empty;

    public void Normalize()
    {
        SourceRepoPath = SourceRepoPath?.Trim() ?? string.Empty;
        WpfTargetRepoPath = WpfTargetRepoPath?.Trim() ?? string.Empty;
        ReleaseOutputRootPath = ReleaseOutputRootPath?.Trim() ?? string.Empty;
        ApkOutputPath = ApkOutputPath?.Trim() ?? string.Empty;
        AabOutputPath = AabOutputPath?.Trim() ?? string.Empty;
        AndroidKeystorePath = AndroidKeystorePath?.Trim() ?? string.Empty;
        AndroidPackageName = AndroidPackageName?.Trim() ?? string.Empty;
        PlayTrackName = PlayTrackName?.Trim() ?? string.Empty;
        StoreUrl = StoreUrl?.Trim() ?? string.Empty;
        InnoSetupCompilerPath = InnoSetupCompilerPath?.Trim() ?? string.Empty;
    }
}
