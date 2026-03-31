using System.Text.Json;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Settings;

public static class AppSettings
{
    private static readonly string SettingsFile = Path.Combine(FileSystem.AppDataDirectory, "user-settings.json");

    private sealed class UserSettings
    {
        public string? LastEmail { get; set; }
        public string? AppMode { get; set; }
        public DateTime? LastBackgroundedAtUtc { get; set; }
    }

    private static UserSettings _settings = new();

    public static string? LastEmail
    {
        get => _settings.LastEmail;
        set => _settings.LastEmail = value;
    }

    public static string? AppMode
    {
        get => _settings.AppMode;
        set => _settings.AppMode = value;
    }

    public static DateTime? LastBackgroundedAtUtc
    {
        get => _settings.LastBackgroundedAtUtc;
        private set => _settings.LastBackgroundedAtUtc = value;
    }

    public static void MarkBackgroundedNowUtc()
    {
        LastBackgroundedAtUtc = DateTime.UtcNow;
        Save();
    }

    public static void ClearBackgroundedTimestamp()
    {
        LastBackgroundedAtUtc = null;
        Save();
    }

    public static TimeSpan? TryGetTimeSinceLastBackgroundUtc(DateTime utcNow)
    {
        var last = LastBackgroundedAtUtc;
        if (last == null)
            return null;

        var delta = utcNow - last.Value;
        if (delta < TimeSpan.Zero)
            return TimeSpan.Zero;

        return delta;
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
            {
                _settings = new UserSettings();
                return;
            }

            var json = File.ReadAllText(SettingsFile);
            if (string.IsNullOrWhiteSpace(json))
            {
                _settings = new UserSettings();
                return;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var loaded = JsonSerializer.Deserialize<UserSettings>(json, opts);
            _settings = loaded ?? new UserSettings();
        }
        catch
        {
            _settings = new UserSettings();
        }
    }

    public static void Save()
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, opts);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
        }
    }
}
