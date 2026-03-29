using System;
using System.IO;
using System.Linq;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class LogExtractionService
{
    private const string ReleaseMarkerPrefix = "- [RELEASE_MARKER] ";

    public LogSourceStatus DetectPrimaryLogSource(string sourceRepoPath)
    {
        if (string.IsNullOrWhiteSpace(sourceRepoPath))
        {
            return new LogSourceStatus
            {
                Message = "Kein Quellpfad für KGV.neu konfiguriert."
            };
        }

        if (!Directory.Exists(sourceRepoPath))
        {
            return new LogSourceStatus
            {
                Message = $"Der konfigurierte Quellpfad wurde nicht gefunden: {sourceRepoPath}"
            };
        }

        var primaryLogPath = Path.Combine(sourceRepoPath, "KGV_Fortschrittslog_ausfuehrlich.md");
        if (File.Exists(primaryLogPath) && CanReadFile(primaryLogPath))
        {
            return new LogSourceStatus
            {
                IsAvailable = true,
                Path = primaryLogPath,
                Message = "Primäre Logquelle gefunden und lesbar."
            };
        }

        var fallbackLogPath = Path.Combine(sourceRepoPath, "DEV_LOG.md");
        if (File.Exists(fallbackLogPath) && CanReadFile(fallbackLogPath))
        {
            return new LogSourceStatus
            {
                IsAvailable = true,
                IsFallback = true,
                Path = fallbackLogPath,
                Message = "Primäre Logquelle fehlt; `DEV_LOG.md` wurde als lesbarer Fallback erkannt."
            };
        }

        return new LogSourceStatus
        {
            Path = primaryLogPath,
            Message = "Es wurde keine lesbare primäre Logquelle im konfigurierten KGV.neu-Pfad gefunden."
        };
    }

    public string GetLatestSection(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            return string.Empty;
        }

        var lines = File.ReadAllLines(logFilePath);
        if (lines.Length == 0)
        {
            return string.Empty;
        }

        var lastHeadingIndex = Array.FindLastIndex(lines, line => line.StartsWith("## "));
        if (lastHeadingIndex < 0)
        {
            return string.Join(Environment.NewLine, lines);
        }

        return string.Join(Environment.NewLine, lines.Skip(lastHeadingIndex));
    }

    public (string Content, bool HasMarker, string MarkerLine, string SourceDescription, string Message) GetContentSinceLastReleaseMarker(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            return (string.Empty, false, string.Empty, "Keine Logdatei verfügbar.", "Die Logdatei für die Delta-Auswertung wurde nicht gefunden.");
        }

        var lines = File.ReadAllLines(logFilePath);
        if (lines.Length == 0)
        {
            return (string.Empty, false, string.Empty, "Die Logdatei ist leer.", "Die Logdatei enthält keine auswertbaren Inhalte.");
        }

        var markerIndex = Array.FindIndex(lines, IsReleaseMarkerLine);
        if (markerIndex < 0)
        {
            return (
                string.Join(Environment.NewLine, lines).Trim(),
                false,
                string.Empty,
                "Kein Release-Marker gefunden. Der gesamte relevante Logbereich wird verwendet.",
                "Kein Release-Marker gefunden. Die Exportbasis nutzt den gesamten relevanten Logbereich.");
        }

        var markerLine = lines[markerIndex].Trim();
        var deltaLines = lines.Take(markerIndex).ToArray();
        return (
            string.Join(Environment.NewLine, deltaLines).Trim(),
            true,
            markerLine,
            $"Log-Delta seit dem letzten Release-Marker: {markerLine}",
            string.IsNullOrWhiteSpace(string.Join(Environment.NewLine, deltaLines).Trim())
                ? "Seit dem letzten Release-Marker wurden noch keine neuen Logeinträge gefunden."
                : "Log-Delta seit dem letzten Release-Marker wurde ermittelt.");
    }

    private static bool CanReadFile(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream.Length >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsReleaseMarkerLine(string line)
        => (line ?? string.Empty).TrimStart().StartsWith(ReleaseMarkerPrefix, StringComparison.Ordinal);
}
