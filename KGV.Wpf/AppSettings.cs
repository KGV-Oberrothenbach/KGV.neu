using System;
using System.IO;
using System.Text.Json;

namespace KGV
{
    public static class AppSettings
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KGV");

        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "user-settings.json");

        private sealed class UserSettings
        {
            public string? LastEmail { get; set; }
        }

        private static UserSettings _settings = new();

        public static string? LastEmail
        {
            get => _settings.LastEmail;
            set => _settings.LastEmail = value;
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
                _settings = string.IsNullOrWhiteSpace(json)
                    ? new UserSettings()
                    : JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
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
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
            }
        }
    }
}
