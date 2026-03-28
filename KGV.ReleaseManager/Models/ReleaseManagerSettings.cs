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
    public string InnoSetupCompilerPath { get; set; } = string.Empty;
}
