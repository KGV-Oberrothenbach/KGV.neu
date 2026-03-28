namespace KGV.ReleaseManager.Models;

public sealed class ReleaseExecutionRequest
{
    public string SourceRepoPath { get; set; } = string.Empty;
    public string WpfTargetRepoPath { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public string ReleaseOutputRootPath { get; set; } = string.Empty;
    public string ApkOutputPath { get; set; } = string.Empty;
    public string AabOutputPath { get; set; } = string.Empty;
    public string InnoSetupCompilerPath { get; set; } = string.Empty;
    public string AndroidKeystorePath { get; set; } = string.Empty;
    public string AndroidKeystoreAlias { get; set; } = string.Empty;
    public string AndroidPackageName { get; set; } = string.Empty;
    public string AndroidStorePassword { get; set; } = string.Empty;
    public string AndroidKeyPassword { get; set; } = string.Empty;
    public bool BuildWpf { get; set; }
    public bool BuildApk { get; set; }
    public bool BuildAab { get; set; }
}
