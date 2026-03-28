using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class VersionService
{
    private static readonly Regex VersionAttributeRegex = new(
        "(AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\\(\\\"(?<version>[^\\\"]+)\\\"\\)",
        RegexOptions.Compiled);

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
        var wpfAssemblyInfoPath = Path.Combine(sourceRepoPath, "KGV.Wpf", "AssemblyInfo.cs");
        var mauiProjectPath = Path.Combine(sourceRepoPath, "KGV.Maui", "KGV.Maui.csproj");
        var androidManifestPath = Path.Combine(sourceRepoPath, "KGV.Maui", "Platforms", "Android", "AndroidManifest.xml");

        result.WpfSourcePath = wpfProjectPath;
        result.AndroidSourcePath = mauiProjectPath;
        result.WpfVersion = DetectWpfVersion(wpfProjectPath, wpfAssemblyInfoPath);
        result.AndroidVersion = DetectAndroidDisplayVersion(mauiProjectPath, androidManifestPath, out var androidVersionCode);
        result.AndroidVersionCode = androidVersionCode;

        if (!string.IsNullOrWhiteSpace(result.WpfVersion) && !string.IsNullOrWhiteSpace(result.AndroidVersion))
        {
            if (string.Equals(result.WpfVersion, result.AndroidVersion, StringComparison.OrdinalIgnoreCase))
            {
                result.CurrentVersion = result.WpfVersion;
                result.StatusMessage = $"WPF- und Android-Version stimmen überein: {result.CurrentVersion}";
                return result;
            }

            result.CurrentVersion = result.WpfVersion;
            result.StatusMessage = $"WPF-Version erkannt: {result.WpfVersion}. Android-Version erkannt: {result.AndroidVersion}.";
            result.WarningMessage = $"Versionsdrift erkannt: WPF = {result.WpfVersion}, Android = {result.AndroidVersion}. Die Zielversion wird vorerst aus der WPF-Version abgeleitet.";
            return result;
        }

        if (!string.IsNullOrWhiteSpace(result.WpfVersion))
        {
            result.CurrentVersion = result.WpfVersion;
            result.StatusMessage = $"WPF-Version erkannt: {result.WpfVersion}. Für Android wurde keine lesbare Display-Version gefunden.";
            result.WarningMessage = "Android-Version konnte nicht sauber erkannt werden.";
            return result;
        }

        if (!string.IsNullOrWhiteSpace(result.AndroidVersion))
        {
            result.CurrentVersion = result.AndroidVersion;
            result.StatusMessage = $"Android-Version erkannt: {result.AndroidVersion}. Für WPF wurde keine lesbare Version gefunden.";
            result.WarningMessage = "WPF-Version konnte nicht sauber erkannt werden.";
            return result;
        }

        result.ErrorMessage = "Es konnte weder für WPF noch für Android eine Version sauber erkannt werden.";
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

    private static string DetectWpfVersion(string projectPath, string assemblyInfoPath)
    {
        var version = DetectVersionFromProjectFile(projectPath, "Version", "AssemblyVersion", "FileVersion", "InformationalVersion");
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        return DetectVersionFromAssemblyInfo(assemblyInfoPath);
    }

    private static string DetectAndroidDisplayVersion(string projectPath, string manifestPath, out string versionCode)
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

            var fallbackVersion = GetPropertyValue(document, "Version", "AssemblyVersion", "FileVersion", "InformationalVersion");
            if (!string.IsNullOrWhiteSpace(fallbackVersion))
            {
                return fallbackVersion.Trim();
            }

            if (!File.Exists(manifestPath))
            {
                return string.Empty;
            }

            var manifest = XDocument.Load(manifestPath);
            var androidNs = manifest.Root?.GetNamespaceOfPrefix("android") ?? XNamespace.None;
            var manifestVersionName = manifest.Root?.Attribute(androidNs + "versionName")?.Value;
            if (string.IsNullOrWhiteSpace(versionCode))
            {
                versionCode = manifest.Root?.Attribute(androidNs + "versionCode")?.Value ?? string.Empty;
            }

            return manifestVersionName?.Trim() ?? string.Empty;
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

    private static string DetectVersionFromAssemblyInfo(string assemblyInfoPath)
    {
        if (!File.Exists(assemblyInfoPath))
        {
            return string.Empty;
        }

        try
        {
            var content = File.ReadAllText(assemblyInfoPath);
            var match = VersionAttributeRegex.Matches(content)
                .Select(m => m.Groups["version"].Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return match?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetPropertyValue(XDocument document, params string[] propertyNames)
    {
        var properties = document.Root?
            .Elements("PropertyGroup")
            .Elements()
            .ToDictionary(element => element.Name.LocalName, element => element.Value, StringComparer.OrdinalIgnoreCase);

        if (properties is null)
        {
            return string.Empty;
        }

        foreach (var propertyName in propertyNames)
        {
            if (properties.TryGetValue(propertyName, out var value) && !string.IsNullOrWhiteSpace(value))
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
