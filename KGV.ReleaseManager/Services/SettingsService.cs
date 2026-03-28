using System.IO;
using System.Text.Json;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public (ReleaseManagerSettings Settings, string Message, bool LoadedFromDisk) Load()
    {
        if (!File.Exists(_settingsFilePath))
            return (new ReleaseManagerSettings(), "Keine gespeicherten Einstellungen gefunden. Es wird mit leeren Standardwerten gestartet.", false);

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ReleaseManagerSettings>(json) ?? new ReleaseManagerSettings();
            settings.Normalize();
            return (settings, $"Einstellungen aus `{_settingsFilePath}` geladen.", true);
        }
        catch (Exception ex)
        {
            return (new ReleaseManagerSettings(), $"Gespeicherte Einstellungen konnten nicht geladen werden. Es wird mit Standardwerten gestartet. Grund: {ex.Message}", false);
        }
    }

    public (bool Success, string Message) Save(ReleaseManagerSettings settings)
    {
        try
        {
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
}
