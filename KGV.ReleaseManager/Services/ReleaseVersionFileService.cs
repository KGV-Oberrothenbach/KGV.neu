using System.IO;
using System.Xml.Linq;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseVersionFileService
{
    public VersionWriteResult WriteTargetVersion(string sourceRepoPath, string targetVersion, bool updateWpf, bool updateAndroid)
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
        if (updateAndroid && File.Exists(mauiProjectPath))
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
        if (updateWpf && File.Exists(wpfProjectPath))
        {
            var wpfDocument = XDocument.Load(wpfProjectPath, LoadOptions.PreserveWhitespace);
            var versionElement = FindOrCreateProjectPropertyElement(wpfDocument, "Version");
            BackupFile(backups, wpfProjectPath);
            versionElement.Value = targetVersion;

            wpfDocument.Save(wpfProjectPath, SaveOptions.DisableFormatting);
            updatedFiles.Add(wpfProjectPath);
        }

        if (updatedFiles.Count == 0)
        {
            return new VersionWriteResult
            {
                Message = "Es wurden keine passenden Versionsfelder in den ausgewählten Projektdateien zum Schreiben gefunden.",
                Backups = backups.Values.ToList(),
                UpdatedFiles = updatedFiles
            };
        }

        return new VersionWriteResult
        {
            Success = true,
            Message = $"Zielversion {targetVersion} in {updatedFiles.Count} ausgewählte Projektdatei(en) geschrieben.",
            AndroidVersionCode = androidVersionCode,
            Backups = backups.Values.ToList(),
            UpdatedFiles = updatedFiles
        };
    }

    public VersionRestoreResult RestoreBackups(IEnumerable<VersionFileBackup> backups)
    {
        var messages = new List<string>();
        var success = true;

        foreach (var backup in backups.Reverse())
        {
            try
            {
                File.WriteAllText(backup.FilePath, backup.OriginalContent);
                messages.Add($"Versionsdatei zurückgesetzt: {backup.FilePath}");
            }
            catch (Exception ex)
            {
                success = false;
                messages.Add($"Versionsdatei konnte nicht zurückgesetzt werden: {backup.FilePath} ({ex.Message})");
            }
        }

        return new VersionRestoreResult
        {
            Success = success,
            Messages = messages
        };
    }

    private static XElement? FindFirstPropertyElement(XDocument document, string propertyName)
    {
        return document.Root?
            .Elements("PropertyGroup")
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static XElement FindOrCreateProjectPropertyElement(XDocument document, string propertyName)
    {
        var existingElement = FindFirstPropertyElement(document, propertyName);
        if (existingElement is not null)
        {
            return existingElement;
        }

        var propertyGroup = document.Root?
            .Elements("PropertyGroup")
            .FirstOrDefault();

        if (propertyGroup is null)
        {
            propertyGroup = new XElement("PropertyGroup");
            document.Root?.AddFirst(propertyGroup);
        }

        var newElement = new XElement(propertyName);
        propertyGroup.Add(newElement);
        return newElement;
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
