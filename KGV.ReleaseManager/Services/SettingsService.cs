using System.IO;
using System.Text.Json;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class SettingsService
{
    private const string DefaultSourceRepoPath = @"C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand";
    private const string DefaultWpfTargetRepoPath = @"C:\Programmieren\Restore KGV\KGV-WPF";
    private const string DefaultReleaseOutputRootPath = @"C:\Programmieren\Restore KGV\Releases\KGV";
    private const string DefaultApkOutputPath = @"C:\Programmieren\Restore KGV\Releases\KGV\Android\APK";
    private const string DefaultAabOutputPath = @"C:\Programmieren\Restore KGV\Releases\KGV\Android\AAB";
    private const string DefaultInnoSetupCompilerPath = @"C:\Users\Braen\AppData\Local\Programs\Inno Setup 6\ISCC.exe";

    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public (ReleaseManagerSettings Settings, string Message, bool LoadedFromDisk) Load()
    {
        if (!File.Exists(_settingsFilePath))
            return (CreateDefaultSettings(), "Keine gespeicherten Einstellungen gefunden. Es wird mit den bestätigten Standardpfaden gestartet.", false);

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ReleaseManagerSettings>(json) ?? CreateDefaultSettings();
            ApplyFallbackDefaults(settings);
            settings.Normalize();
            return (settings, $"Einstellungen aus `{_settingsFilePath}` geladen.", true);
        }
        catch (Exception ex)
        {
            return (CreateDefaultSettings(), $"Gespeicherte Einstellungen konnten nicht geladen werden. Es wird mit den bestätigten Standardpfaden gestartet. Grund: {ex.Message}", false);
        }
    }

    public (bool Success, string Message) Save(ReleaseManagerSettings settings)
    {
        try
        {
            ApplyFallbackDefaults(settings);
            settings.Normalize();
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
            return (true, $"Einstellungen in `{_settingsFilePath}` gespeichert.");
        }
        catch (Exception ex)
        {
            return (false, $"Einstellungen konnten nicht gespeichert werden: {ex.Message}");
        }
    }

    private static ReleaseManagerSettings CreateDefaultSettings()
    {
        var settings = new ReleaseManagerSettings();
        ApplyFallbackDefaults(settings);
        settings.Normalize();
        return settings;
    }

    private static void ApplyFallbackDefaults(ReleaseManagerSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SourceRepoPath))
        {
            settings.SourceRepoPath = DefaultSourceRepoPath;
        }

        if (string.IsNullOrWhiteSpace(settings.WpfTargetRepoPath))
        {
            settings.WpfTargetRepoPath = DefaultWpfTargetRepoPath;
        }

        if (string.IsNullOrWhiteSpace(settings.ReleaseOutputRootPath))
        {
            settings.ReleaseOutputRootPath = DefaultReleaseOutputRootPath;
        }

        if (string.IsNullOrWhiteSpace(settings.ApkOutputPath))
        {
            settings.ApkOutputPath = DefaultApkOutputPath;
        }

        if (string.IsNullOrWhiteSpace(settings.AabOutputPath))
        {
            settings.AabOutputPath = DefaultAabOutputPath;
        }

        if (string.IsNullOrWhiteSpace(settings.InnoSetupCompilerPath) && File.Exists(DefaultInnoSetupCompilerPath))
        {
            settings.InnoSetupCompilerPath = DefaultInnoSetupCompilerPath;
        }
    }
}
