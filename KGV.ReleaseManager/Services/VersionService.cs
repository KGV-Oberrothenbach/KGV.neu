using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class VersionService
{
    public VersionDetectionResult DetectVersions(string sourceRepoPath)
    {
        var result = new VersionDetectionResult();

        if (string.IsNullOrWhiteSpace(sourceRepoPath))
        {
            result.ErrorMessage = "Kein Quellpfad für KGV.neu konfiguriert.";
            result.StatusMessage = "Versionsermittlung konnte nicht gestartet werden.";
            return result;
        }

        if (!Directory.Exists(sourceRepoPath))
        {
            result.ErrorMessage = $"Der konfigurierte Quellpfad wurde nicht gefunden: {sourceRepoPath}";
            result.StatusMessage = "Versionsermittlung konnte nicht gestartet werden.";
            return result;
        }

        var wpfProjectPath = Path.Combine(sourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj");
        var mauiProjectPath = Path.Combine(sourceRepoPath, "KGV.Maui", "KGV.Maui.csproj");

        result.WpfSourcePath = wpfProjectPath;
        result.AndroidSourcePath = mauiProjectPath;
        result.WpfVersion = DetectWpfVersion(wpfProjectPath);
        result.AndroidVersion = DetectAndroidDisplayVersion(mauiProjectPath, out var androidVersionCode);
        result.AndroidVersionCode = androidVersionCode;
        result.IsWpfVersionDetected = !string.IsNullOrWhiteSpace(result.WpfVersion);
        result.IsAndroidVersionDetected = !string.IsNullOrWhiteSpace(result.AndroidVersion);

        if (result.IsWpfVersionDetected && result.IsAndroidVersionDetected)
        {
            if (string.Equals(result.WpfVersion, result.AndroidVersion, StringComparison.OrdinalIgnoreCase))
            {
                result.CurrentVersion = result.WpfVersion;
                result.IsCurrentVersionShared = true;
                result.StatusMessage = $"WPF- und Android-Version stimmen überein: {result.CurrentVersion}";
                return result;
            }

            result.StatusMessage = $"WPF-Version erkannt: {result.WpfVersion}. Android-Version erkannt: {result.AndroidVersion}.";
            result.WarningMessage = $"Versionsdrift erkannt: WPF = {result.WpfVersion}, Android = {result.AndroidVersion}. Eine gemeinsame Zielversion wird erst aus der konkreten Release-Auswahl abgeleitet.";
            return result;
        }

        if (result.IsWpfVersionDetected)
        {
            result.CurrentVersion = result.WpfVersion;
            result.StatusMessage = $"WPF-Version erkannt: {result.WpfVersion}. Für Android wurde keine lesbare Version in `KGV.Maui.csproj` gefunden.";
            result.WarningMessage = "Android-Version konnte nicht aus `ApplicationDisplayVersion` gelesen werden.";
            return result;
        }

        if (result.IsAndroidVersionDetected)
        {
            result.CurrentVersion = result.AndroidVersion;
            result.StatusMessage = $"Android-Version erkannt: {result.AndroidVersion}. Für WPF wurde keine lesbare Version in `KGV.Wpf.csproj` gefunden.";
            result.WarningMessage = "WPF-Version konnte nicht aus der Projektdatei gelesen werden.";
            return result;
        }

        result.ErrorMessage = "Es konnte weder für WPF noch für Android eine Version aus den Projektdateien gelesen werden.";
        result.StatusMessage = "Versionsermittlung fehlgeschlagen.";
        return result;
    }

    public string CalculateNextVersion(string version, VersionBumpType bumpType)
    {
        if (!TryParseVersionParts(version, out var major, out var minor, out var patch))
        {
            return string.Empty;
        }

        return bumpType switch
        {
            VersionBumpType.Major => $"{major + 1}.0.0",
            VersionBumpType.Minor => $"{major}.{minor + 1}.0",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }

    private static string DetectWpfVersion(string projectPath)
    {
        return DetectVersionFromProjectFile(projectPath, "Version", "AssemblyVersion", "FileVersion", "InformationalVersion");
    }

    private static string DetectAndroidDisplayVersion(string projectPath, out string versionCode)
    {
        versionCode = string.Empty;

        if (!File.Exists(projectPath))
        {
            return string.Empty;
        }

        try
        {
            var document = XDocument.Load(projectPath);
            versionCode = GetPropertyValue(document, "ApplicationVersion");

            var displayVersion = GetPropertyValue(document, "ApplicationDisplayVersion");
            if (!string.IsNullOrWhiteSpace(displayVersion))
            {
                return displayVersion.Trim();
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DetectVersionFromProjectFile(string projectPath, params string[] propertyNames)
    {
        if (!File.Exists(projectPath))
        {
            return string.Empty;
        }

        try
        {
            var document = XDocument.Load(projectPath);
            return GetPropertyValue(document, propertyNames).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetPropertyValue(XDocument document, params string[] propertyNames)
    {
        var propertyElements = document.Root?
            .Elements()
            .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase))
            .Elements()
            .Where(element => element is not null)
            .ToList();

        if (propertyElements is null || propertyElements.Count == 0)
        {
            return string.Empty;
        }

        foreach (var propertyName in propertyNames)
        {
            var value = propertyElements
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static bool TryParseVersionParts(string version, out int major, out int minor, out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var match = Regex.Match(version.Trim(), "^(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<patch>\\d+)(?:[.-].*)?$");
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["major"].Value, out major)
               && int.TryParse(match.Groups["minor"].Value, out minor)
               && int.TryParse(match.Groups["patch"].Value, out patch);
    }
}
