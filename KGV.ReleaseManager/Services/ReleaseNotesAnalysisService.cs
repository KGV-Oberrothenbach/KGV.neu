using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseNotesAnalysisService
{
    private static readonly string[] InternalMarkers =
    {
        "KGV.ReleaseManager",
        "Release Manager",
        "ReleaseManager"
    };

    private readonly LogExtractionService _logExtractionService;
    private readonly ReleaseNotesImportExportService _releaseNotesImportExportService;
    private readonly ReleaseNotesHistoryService _releaseNotesHistoryService;

    public ReleaseNotesAnalysisService(
        LogExtractionService logExtractionService,
        ReleaseNotesImportExportService releaseNotesImportExportService,
        ReleaseNotesHistoryService releaseNotesHistoryService)
    {
        _logExtractionService = logExtractionService;
        _releaseNotesImportExportService = releaseNotesImportExportService;
        _releaseNotesHistoryService = releaseNotesHistoryService;
    }

    public ReleaseNotesAnalysisResult Analyze(string sourceRepoPath, string currentVersion, string targetVersion)
    {
        var logSource = _logExtractionService.DetectPrimaryLogSource(sourceRepoPath);
        var latestEntry = _releaseNotesHistoryService.GetLatestEntry();
        var lastReleaseText = _releaseNotesHistoryService.BuildLatestReleaseStatusText();

        if (!logSource.IsAvailable)
        {
            return new ReleaseNotesAnalysisResult
            {
                Message = logSource.Message,
                LogSourcePath = logSource.Path,
                LastKnownReleaseText = lastReleaseText
            };
        }

        var sections = ReadSections(logSource.Path);
        var relevantSections = sections
            .Where(section => !IsInternalReleaseManagerSection(section))
            .ToList();

        if (relevantSections.Count == 0)
        {
            return new ReleaseNotesAnalysisResult
            {
                LogSourcePath = logSource.Path,
                LastKnownReleaseText = lastReleaseText,
                Message = "Es wurden keine relevanten Änderungen außerhalb des ReleaseManagers gefunden.",
                SourceDescription = "Es konnten nur ReleaseManager-interne Abschnitte erkannt werden."
            };
        }

        var selectedSections = new List<LogSection>();
        var hasAnchor = false;
        var isSuggestedStartState = false;
        var sourceDescription = string.Empty;
        var message = string.Empty;

        if (latestEntry is not null && !string.IsNullOrWhiteSpace(latestEntry.LogAnchorHeading))
        {
            var anchorIndex = sections.FindIndex(section => HeadingEquals(section.Heading, latestEntry.LogAnchorHeading));
            if (anchorIndex >= 0)
            {
                hasAnchor = true;
                selectedSections = relevantSections
                    .TakeWhile(section => !HeadingEquals(section.Heading, latestEntry.LogAnchorHeading))
                    .ToList();
                sourceDescription = $"Primäre Auswertung bis zum letzten gespeicherten Release-Anker: {latestEntry.LogAnchorHeading}";
                message = selectedSections.Count == 0
                    ? "Seit dem letzten gespeicherten Release-Anker wurden keine neuen relevanten Änderungen gefunden."
                    : "Änderungen seit dem letzten gespeicherten Release-Anker wurden ermittelt.";
            }
        }

        if (selectedSections.Count == 0 && latestEntry is not null && latestEntry.SavedAtUtc != default && !hasAnchor)
        {
            selectedSections = relevantSections
                .Where(section => section.HeadingDate.HasValue && section.HeadingDate.Value.Date > latestEntry.SavedAtUtc.Date)
                .ToList();

            if (selectedSections.Count > 0)
            {
                hasAnchor = true;
                sourceDescription = $"Fallback-Auswertung über Logdaten nach dem zuletzt gespeicherten Release vom {latestEntry.SavedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
                message = "Der gespeicherte Release-Anker wurde im Log nicht direkt gefunden; es wird der jüngere Logbereich nach Datum ausgewertet.";
            }
        }

        if (selectedSections.Count == 0 && !hasAnchor)
        {
            selectedSections = relevantSections.Take(1).ToList();
            isSuggestedStartState = true;
            sourceDescription = string.IsNullOrWhiteSpace(lastReleaseText)
                ? "Es wurde der neueste relevante Logabschnitt als Startzustand vorgeschlagen."
                : $"Es wurde der neueste relevante Logabschnitt als Startzustand vorgeschlagen, weil kein belastbarer Release-Anker bestimmt werden konnte.";
            message = "Kein belastbarer letzter Release-Anker gefunden. Der neueste relevante Logabschnitt wird als erster sinnvoller Startzustand vorgeschlagen.";
        }

        var anchorHeading = selectedSections.FirstOrDefault()?.Heading ?? string.Empty;
        var changesPreview = BuildChangesPreview(selectedSections);
        var effectiveTargetVersion = string.IsNullOrWhiteSpace(targetVersion) ? currentVersion : targetVersion;
        var exportText = _releaseNotesImportExportService.CreateExportText(
            currentVersion,
            effectiveTargetVersion,
            logSource.Path,
            sourceDescription,
            changesPreview,
            selectedSections.Select(section => section.Heading).ToList());

        return new ReleaseNotesAnalysisResult
        {
            Success = selectedSections.Count > 0,
            HasAnchor = hasAnchor,
            IsSuggestedStartState = isSuggestedStartState,
            Message = message,
            LogSourcePath = logSource.Path,
            LastKnownReleaseText = lastReleaseText,
            SourceDescription = sourceDescription,
            AnchorHeading = anchorHeading,
            ChangesPreview = changesPreview,
            ExportText = exportText
        };
    }

    private static List<LogSection> ReadSections(string logFilePath)
    {
        var lines = File.ReadAllLines(logFilePath);
        var sections = new List<LogSection>();
        var currentHeading = string.Empty;
        var currentLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AddSection(sections, currentHeading, currentLines);
                currentHeading = line.Trim();
                currentLines = new List<string> { line };
                continue;
            }

            currentLines.Add(line);
        }

        AddSection(sections, currentHeading, currentLines);
        return sections;
    }

    private static void AddSection(List<LogSection> sections, string heading, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var text = string.Join(Environment.NewLine, lines).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        sections.Add(new LogSection
        {
            Heading = string.IsNullOrWhiteSpace(heading) ? "(ohne Überschrift)" : heading,
            Text = text,
            HeadingDate = TryParseHeadingDate(heading)
        });
    }

    private static DateTime? TryParseHeadingDate(string heading)
    {
        var match = Regex.Match(heading ?? string.Empty, "(?<date>\\d{4}-\\d{2}-\\d{2})");
        if (!match.Success)
        {
            return null;
        }

        return DateTime.TryParse(match.Groups["date"].Value, out var parsedDate)
            ? parsedDate.Date
            : null;
    }

    private static bool IsInternalReleaseManagerSection(LogSection section)
    {
        return InternalMarkers.Any(marker =>
            section.Heading.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || section.Text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HeadingEquals(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildChangesPreview(IReadOnlyList<LogSection> sections)
    {
        if (sections.Count == 0)
        {
            return "Keine neuen relevanten Änderungen gefunden.";
        }

        var sb = new StringBuilder();
        foreach (var section in sections)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.Append(section.Text.Trim());
        }

        return sb.ToString().Trim();
    }

    private sealed class LogSection
    {
        public string Heading { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public DateTime? HeadingDate { get; init; }
    }
}
