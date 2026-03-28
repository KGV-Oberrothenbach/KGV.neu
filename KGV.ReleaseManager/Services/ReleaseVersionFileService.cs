using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseVersionFileService
{
    private static readonly Regex AssemblyVersionRegex = new("(AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\\(\\\"(?<version>[^\\\"]+)\\\"\\)", RegexOptions.Compiled);

    public VersionWriteResult WriteTargetVersion(string sourceRepoPath, string targetVersion)
    {
        if (string.IsNullOrWhiteSpace(sourceRepoPath) || !Directory.Exists(sourceRepoPath))
        {
            return new VersionWriteResult { Message = "Quellrepo wurde nicht gefunden." };
        }

        if (string.IsNullOrWhiteSpace(targetVersion))
        {
            return new VersionWriteResult { Message = "Zielversion fehlt." };
        }

        var backups = new Dictionary<string, VersionFileBackup>(StringComparer.OrdinalIgnoreCase);
        var updatedFiles = new List<string>();
        var androidVersionCode = string.Empty;

        var mauiProjectPath = Path.Combine(sourceRepoPath, "KGV.Maui", "KGV.Maui.csproj");
        if (File.Exists(mauiProjectPath))
        {
            var mauiDocument = XDocument.Load(mauiProjectPath, LoadOptions.PreserveWhitespace);
            var mauiUpdated = false;

            var displayVersionElement = FindFirstPropertyElement(mauiDocument, "ApplicationDisplayVersion");
            if (displayVersionElement is not null)
            {
                BackupFile(backups, mauiProjectPath);
                displayVersionElement.Value = targetVersion;
                mauiUpdated = true;
            }

            var versionCodeElement = FindFirstPropertyElement(mauiDocument, "ApplicationVersion");
            if (versionCodeElement is not null)
            {
                if (!int.TryParse(versionCodeElement.Value, out var currentVersionCode))
                {
                    return new VersionWriteResult { Message = "ApplicationVersion in `KGV.Maui.csproj` ist keine gültige Ganzzahl." };
                }

                BackupFile(backups, mauiProjectPath);
                versionCodeElement.Value = (currentVersionCode + 1).ToString();
                androidVersionCode = versionCodeElement.Value;
                mauiUpdated = true;
            }

            if (mauiUpdated)
            {
                mauiDocument.Save(mauiProjectPath, SaveOptions.DisableFormatting);
                updatedFiles.Add(mauiProjectPath);
            }
        }

        var wpfProjectPath = Path.Combine(sourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj");
        if (File.Exists(wpfProjectPath))
        {
            var wpfDocument = XDocument.Load(wpfProjectPath, LoadOptions.PreserveWhitespace);
            var wpfUpdated = false;
            foreach (var propertyName in new[] { "Version", "AssemblyVersion", "FileVersion", "InformationalVersion" })
            {
                var element = FindFirstPropertyElement(wpfDocument, propertyName);
                if (element is null)
                {
                    continue;
                }

                BackupFile(backups, wpfProjectPath);
                element.Value = targetVersion;
                wpfUpdated = true;
            }

            if (wpfUpdated)
            {
                wpfDocument.Save(wpfProjectPath, SaveOptions.DisableFormatting);
                updatedFiles.Add(wpfProjectPath);
            }
        }

        var wpfAssemblyInfoPath = Path.Combine(sourceRepoPath, "KGV.Wpf", "AssemblyInfo.cs");
        if (File.Exists(wpfAssemblyInfoPath))
        {
            var originalContent = File.ReadAllText(wpfAssemblyInfoPath);
            var updatedContent = AssemblyVersionRegex.Replace(
                originalContent,
                match => match.Value.Replace(match.Groups["version"].Value, targetVersion));

            if (!string.Equals(originalContent, updatedContent, StringComparison.Ordinal))
            {
                BackupFile(backups, wpfAssemblyInfoPath);
                File.WriteAllText(wpfAssemblyInfoPath, updatedContent);
                updatedFiles.Add(wpfAssemblyInfoPath);
            }
        }

        if (updatedFiles.Count == 0)
        {
            return new VersionWriteResult
            {
                Message = "Es wurden keine real vorhandenen Versionsfelder zum Schreiben gefunden.",
                Backups = backups.Values.ToList(),
                UpdatedFiles = updatedFiles
            };
        }

        return new VersionWriteResult
        {
            Success = true,
            Message = $"Zielversion {targetVersion} in {updatedFiles.Count} Datei(en) geschrieben.",
            AndroidVersionCode = androidVersionCode,
            Backups = backups.Values.ToList(),
            UpdatedFiles = updatedFiles
        };
    }

    public void RestoreBackups(IEnumerable<VersionFileBackup> backups)
    {
        foreach (var backup in backups.Reverse())
        {
            File.WriteAllText(backup.FilePath, backup.OriginalContent);
        }
    }

    private static XElement? FindFirstPropertyElement(XDocument document, string propertyName)
    {
        return document.Root?
            .Elements("PropertyGroup")
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static void BackupFile(IDictionary<string, VersionFileBackup> backups, string path)
    {
        if (backups.ContainsKey(path))
        {
            return;
        }

        backups[path] = new VersionFileBackup
        {
            FilePath = path,
            OriginalContent = File.ReadAllText(path)
        };
    }
}
