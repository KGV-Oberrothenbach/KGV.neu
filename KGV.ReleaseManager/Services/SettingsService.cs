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
    private const string DefaultAndroidKeystorePath = @"C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand\_secrets\Android\kgv-upload.keystore";
    private const string DefaultAndroidKeystoreAlias = "kgvupload";
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
        {
            var defaultSettings = CreateDefaultSettings();
            var saveResult = Save(defaultSettings);
            var message = saveResult.Success
                ? $"Keine gespeicherten Einstellungen gefunden. Lokale Settings-Datei wurde mit bestätigten Standardpfaden angelegt: {_settingsFilePath}"
                : $"Keine gespeicherten Einstellungen gefunden. Es wird mit bestätigten Standardpfaden gestartet. Die lokale Settings-Datei konnte noch nicht angelegt werden: {saveResult.Message}";
            return (defaultSettings, message, false);
        }

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
            var defaultSettings = CreateDefaultSettings();
            var saveResult = Save(defaultSettings);
            var message = saveResult.Success
                ? $"Gespeicherte Einstellungen konnten nicht geladen werden. Die lokale Settings-Datei wurde mit bestätigten Standardpfaden neu angelegt. Grund: {ex.Message}"
                : $"Gespeicherte Einstellungen konnten nicht geladen werden. Es wird mit den bestätigten Standardpfaden gestartet. Grund: {ex.Message}";
            return (defaultSettings, message, false);
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

        if (string.IsNullOrWhiteSpace(settings.AndroidKeystorePath) && File.Exists(DefaultAndroidKeystorePath))
        {
            settings.AndroidKeystorePath = DefaultAndroidKeystorePath;
        }

        if (string.IsNullOrWhiteSpace(settings.AndroidKeystoreAlias))
        {
            settings.AndroidKeystoreAlias = DefaultAndroidKeystoreAlias;
        }

        if (string.IsNullOrWhiteSpace(settings.InnoSetupCompilerPath) && File.Exists(DefaultInnoSetupCompilerPath))
        {
            settings.InnoSetupCompilerPath = DefaultInnoSetupCompilerPath;
        }
    }
}
