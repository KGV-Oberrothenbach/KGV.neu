using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseMarkerService
{
    private const string PrimaryLogFileName = "KGV_Fortschrittslog_ausfuehrlich.md";
    private const string AlternateLogFileName = "KGV_Fortschritt_ausfuehrlich.md";
    private static readonly Regex ReleaseMarkerRegex = new(
        "^\\s*- \\[RELEASE_MARKER\\] Version (?<version>.+?) erfolgreich erstellt am (?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2})\\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string ResolveProgressLogPath(string sourceRepoPath)
    {
        if (string.IsNullOrWhiteSpace(sourceRepoPath) || !Directory.Exists(sourceRepoPath))
        {
            return string.Empty;
        }

        var primaryPath = Path.Combine(sourceRepoPath, PrimaryLogFileName);
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        var alternatePath = Path.Combine(sourceRepoPath, AlternateLogFileName);
        return File.Exists(alternatePath) ? alternatePath : primaryPath;
    }

    public (bool Success, bool AlreadyPresent, string LogFilePath, string MarkerLine, string Message) AppendReleaseMarker(string sourceRepoPath, string version)
    {
        var logFilePath = ResolveProgressLogPath(sourceRepoPath);
        var markerLine = BuildReleaseMarkerLine(version, DateTime.Now);

        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            return (false, false, string.Empty, markerLine, "Für den Release-Marker wurde keine Fortschrittslog-Datei im Quellrepo gefunden.");
        }

        if (!File.Exists(logFilePath))
        {
            return (false, false, logFilePath, markerLine, $"Die Fortschrittslog-Datei für den Release-Marker wurde nicht gefunden: {logFilePath}");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return (false, false, logFilePath, markerLine, "Der Release-Marker kann ohne Zielversion nicht geschrieben werden.");
        }

        try
        {
            var lines = File.ReadAllLines(logFilePath).ToList();
            var firstHeadingIndex = lines.FindIndex(line => line.StartsWith("## ", StringComparison.Ordinal));
            if (firstHeadingIndex < 0)
            {
                return (false, false, logFilePath, markerLine, $"Die Fortschrittslog-Datei enthält keine Abschnittsüberschrift und kann nicht sicher für Release-Marker erweitert werden: {logFilePath}");
            }

            var sectionEndIndex = lines.FindIndex(firstHeadingIndex + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
            if (sectionEndIndex < 0)
            {
                sectionEndIndex = lines.Count;
            }

            var currentSectionLines = lines.Skip(firstHeadingIndex + 1).Take(sectionEndIndex - firstHeadingIndex - 1).ToList();
            var existingMarkerLine = currentSectionLines.FirstOrDefault(IsReleaseMarkerLine);
            if (!string.IsNullOrWhiteSpace(existingMarkerLine) && string.Equals(existingMarkerLine.Trim(), markerLine, StringComparison.Ordinal))
            {
                return (true, true, logFilePath, markerLine, $"Release-Marker für Version {version} ist bereits im aktuellen Fortschrittsabschnitt vorhanden.");
            }

            var insertIndex = sectionEndIndex;
            while (insertIndex > firstHeadingIndex + 1 && string.IsNullOrWhiteSpace(lines[insertIndex - 1]))
            {
                insertIndex--;
            }

            lines.Insert(insertIndex, markerLine);
            File.WriteAllLines(logFilePath, lines);
            return (true, false, logFilePath, markerLine, $"Release-Marker in `{Path.GetFileName(logFilePath)}` ergänzt: {markerLine}");
        }
        catch (Exception ex)
        {
            return (false, false, logFilePath, markerLine, $"Release-Marker konnte nicht geschrieben werden: {ex.Message}");
        }
    }

    public bool TryGetLatestReleaseMarker(string logFilePath, out string markerLine, out DateTime? markerTimestamp)
    {
        markerLine = string.Empty;
        markerTimestamp = null;

        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            return false;
        }

        foreach (var line in File.ReadLines(logFilePath))
        {
            if (!IsReleaseMarkerLine(line))
            {
                continue;
            }

            markerLine = line.Trim();
            var match = ReleaseMarkerRegex.Match(markerLine);
            if (match.Success
                && DateTime.TryParseExact(match.Groups["timestamp"].Value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTimestamp))
            {
                markerTimestamp = parsedTimestamp;
            }

            return true;
        }

        return false;
    }

    public bool IsReleaseMarkerLine(string line)
        => ReleaseMarkerRegex.IsMatch(line ?? string.Empty);

    public string BuildReleaseMarkerLine(string version, DateTime timestamp)
        => $"- [RELEASE_MARKER] Version {version} erfolgreich erstellt am {timestamp:yyyy-MM-dd HH:mm}";
}
