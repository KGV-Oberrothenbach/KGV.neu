using System.IO;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseArtifactService
{
    public (string ScriptPath, string Message) FindInnoSetupScript(string sourceRepoPath)
    {
        if (string.IsNullOrWhiteSpace(sourceRepoPath) || !Directory.Exists(sourceRepoPath))
        {
            return (string.Empty, "Quellrepo wurde nicht gefunden.");
        }

        var scripts = Directory.GetFiles(sourceRepoPath, "*.iss", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("\\_Archiv\\", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return scripts.Count switch
        {
            1 => (scripts[0], $"Inno-Setup-Skript gefunden: {scripts[0]}"),
            0 => (string.Empty, "Kein Inno-Setup-Skript (`*.iss`) im Quellrepo gefunden."),
            _ => (string.Empty, $"Mehrere Inno-Setup-Skripte gefunden ({scripts.Count}). Bitte Bereinigung oder eindeutige Auswahl ergänzen.")
        };
    }

    public string FindNewestArtifact(string searchRoot, string searchPattern, DateTime notBeforeUtc)
    {
        if (string.IsNullOrWhiteSpace(searchRoot) || !Directory.Exists(searchRoot))
        {
            return string.Empty;
        }

        return Directory.GetFiles(searchRoot, searchPattern, SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .Where(info => info.LastWriteTimeUtc >= notBeforeUtc)
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .ThenByDescending(info => info.Length)
            .Select(info => info.FullName)
            .FirstOrDefault() ?? string.Empty;
    }

    public string CopyArtifact(string sourceFilePath, string targetDirectory, string? targetFileName = null)
    {
        Directory.CreateDirectory(targetDirectory);

        var fileName = string.IsNullOrWhiteSpace(targetFileName)
            ? Path.GetFileName(sourceFilePath)
            : targetFileName.Trim();

        var destinationPath = Path.Combine(targetDirectory, fileName);
        File.Copy(sourceFilePath, destinationPath, true);
        return destinationPath;
    }

    public (string TargetDirectory, string Message) ResolveWpfTargetDirectory(string wpfTargetRepoPath)
    {
        if (string.IsNullOrWhiteSpace(wpfTargetRepoPath) || !Directory.Exists(wpfTargetRepoPath))
        {
            return (string.Empty, "Das lokale WPF-Zielrepo wurde nicht gefunden.");
        }

        var hasRootReleaseFiles = File.Exists(Path.Combine(wpfTargetRepoPath, "releases.json"))
            || File.Exists(Path.Combine(wpfTargetRepoPath, "KGV-Setup.exe"))
            || Directory.GetFiles(wpfTargetRepoPath, "KGV-Setup-*.exe", SearchOption.TopDirectoryOnly).Length > 0;

        return hasRootReleaseFiles
            ? (wpfTargetRepoPath, $"WPF-Zielstruktur erkannt: {wpfTargetRepoPath}")
            : (string.Empty, "Die Zielstruktur im lokalen WPF-Repo ist nicht eindeutig bestimmbar.");
    }
}
