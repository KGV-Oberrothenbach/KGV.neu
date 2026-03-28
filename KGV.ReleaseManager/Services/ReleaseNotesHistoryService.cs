using System.IO;
using System.Text.Json;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseNotesHistoryService
{
    private readonly string _historyFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ReleaseNotesHistoryService(string historyFilePath)
    {
        _historyFilePath = historyFilePath;
    }

    public string HistoryFilePath => _historyFilePath;

    public ReleaseNotesHistoryDocument LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new ReleaseNotesHistoryDocument();
        }

        try
        {
            var json = File.ReadAllText(_historyFilePath);
            return JsonSerializer.Deserialize<ReleaseNotesHistoryDocument>(json) ?? new ReleaseNotesHistoryDocument();
        }
        catch
        {
            return new ReleaseNotesHistoryDocument();
        }
    }

    public ReleaseNotesHistoryEntry? GetLatestEntry()
    {
        return LoadHistory().Entries
            .OrderByDescending(entry => entry.SavedAtUtc)
            .FirstOrDefault();
    }

    public (bool Success, string Message) SaveEntry(ReleaseNotesHistoryEntry entry)
    {
        try
        {
            var document = LoadHistory();
            document.Entries.RemoveAll(existing => string.Equals(existing.Version, entry.Version, StringComparison.OrdinalIgnoreCase));
            document.Entries.Add(entry);
            document.Entries = document.Entries
                .OrderByDescending(existing => existing.SavedAtUtc)
                .ToList();

            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(document, _jsonOptions);
            File.WriteAllText(_historyFilePath, json);
            return (true, $"Release-Notizen lokal versioniert gespeichert: {_historyFilePath}");
        }
        catch (Exception ex)
        {
            return (false, $"Release-Notizen konnten nicht gespeichert werden: {ex.Message}");
        }
    }

    public string BuildLatestReleaseStatusText()
    {
        var latestEntry = GetLatestEntry();
        if (latestEntry is null)
        {
            return "Kein letzter gespeicherter Release-Anker vorhanden. Als Startzustand kann der neueste relevante Logabschnitt vorgeschlagen werden.";
        }

        var localTime = latestEntry.SavedAtUtc == default
            ? string.Empty
            : latestEntry.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return string.IsNullOrWhiteSpace(localTime)
            ? $"Letztes gespeichertes Release: {latestEntry.Version}"
            : $"Letztes gespeichertes Release: {latestEntry.Version} vom {localTime}";
    }
}
