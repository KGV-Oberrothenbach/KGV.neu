namespace KGV.ReleaseManager.Models;

public sealed class ReleaseTargetSelection
{
    public bool BuildWpf { get; set; }
    public bool BuildApk { get; set; }
    public bool BuildAab { get; set; }
}
