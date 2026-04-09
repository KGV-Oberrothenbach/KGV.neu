using System;

namespace KGV.Wpf.Models
{
    public sealed class AppUpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string SetupUrl { get; set; } = string.Empty;
        public string VersionedSetupUrl { get; set; } = string.Empty;
        public string PublishedAt { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
        public string Notes { get; set; } = string.Empty;

        public Version? GetParsedVersion()
        {
            return TryParseVersion(Version, out var version)
                ? version
                : null;
        }

        public string GetNotesText()
        {
            return string.IsNullOrWhiteSpace(Notes)
                ? string.Empty
                : Notes.Trim();
        }

        public static bool TryParseVersion(string? value, out Version version)
        {
            version = new Version(0, 0, 0, 0);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var cleaned = value.Trim();

            var plusIndex = cleaned.IndexOf('+');
            if (plusIndex >= 0)
                cleaned = cleaned[..plusIndex];

            var dashIndex = cleaned.IndexOf('-');
            if (dashIndex >= 0)
                cleaned = cleaned[..dashIndex];

            var parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts.Length > 4)
                return false;

            var numbers = new int[4];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out numbers[i]))
                    return false;
            }

            version = new Version(
                numbers[0],
                numbers[1],
                numbers[2],
                numbers[3]);

            return true;
        }
    }
}