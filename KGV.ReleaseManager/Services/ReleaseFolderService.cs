using System.IO;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseFolderService
{
    public string EnsureVersionFolder(string rootPath, string version)
    {
        var safeVersion = string.IsNullOrWhiteSpace(version) ? "unbekannt" : version.Trim();
        var folder = Path.Combine(rootPath, safeVersion);

        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "WPF"));
        Directory.CreateDirectory(Path.Combine(folder, "Android", "APK"));
        Directory.CreateDirectory(Path.Combine(folder, "Android", "AAB"));
        Directory.CreateDirectory(Path.Combine(folder, "Notes"));

        return folder;
    }
}
