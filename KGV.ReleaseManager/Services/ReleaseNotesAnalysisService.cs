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

    public ReleaseNotesAnalysisService(
        LogExtractionService logExtractionService,
        ReleaseNotesImportExportService releaseNotesImportExportService)
    {
        _logExtractionService = logExtractionService;
        _releaseNotesImportExportService = releaseNotesImportExportService;
    }

    public ReleaseNotesAnalysisResult Analyze(
        string sourceRepoPath,
        string currentVersion,
        string targetVersion,
        string lastReleaseText)
    {
        var logSource = _logExtractionService.DetectPrimaryLogSource(sourceRepoPath);

        if (!logSource.IsAvailable)
        {
            return new ReleaseNotesAnalysisResult
            {
                Message = logSource.Message,
                LogSourcePath = logSource.Path,
                LastKnownReleaseText = lastReleaseText
            };
        }

        var deltaResult = _logExtractionService.GetContentSinceLastReleaseMarker(logSource.Path);
        var sections = ReadSections(deltaResult.Content);
        var relevantSections = sections
            .Where(section => !IsInternalReleaseManagerSection(section))
            .ToList();

        if (relevantSections.Count == 0)
        {
            return new ReleaseNotesAnalysisResult
            {
                LogSourcePath = logSource.Path,
                LastKnownReleaseText = lastReleaseText,
                Message = deltaResult.HasMarker
                    ? "Seit dem letzten Release-Marker wurden keine relevanten Änderungen außerhalb des ReleaseManagers gefunden."
                    : "Es wurden keine relevanten Änderungen außerhalb des ReleaseManagers gefunden.",
                SourceDescription = deltaResult.SourceDescription
            };
        }

        var selectedSections = relevantSections;
        var changesPreview = BuildChangesPreview(selectedSections);
        var effectiveTargetVersion = string.IsNullOrWhiteSpace(targetVersion) ? currentVersion : targetVersion;
        var exportText = _releaseNotesImportExportService.CreateExportText(
            currentVersion,
            effectiveTargetVersion,
            logSource.Path,
            deltaResult.SourceDescription,
            changesPreview,
            selectedSections.Select(section => section.Heading).ToList());

        return new ReleaseNotesAnalysisResult
        {
            Success = selectedSections.Count > 0,
            HasAnchor = deltaResult.HasMarker,
            IsSuggestedStartState = !deltaResult.HasMarker,
            Message = deltaResult.Message,
            LogSourcePath = logSource.Path,
            LastKnownReleaseText = lastReleaseText,
            SourceDescription = deltaResult.SourceDescription,
            AnchorHeading = deltaResult.MarkerLine,
            ChangesPreview = changesPreview,
            ExportText = exportText
        };
    }

    private static List<LogSection> ReadSections(string logContent)
    {
        if (string.IsNullOrWhiteSpace(logContent))
        {
            return new List<LogSection>();
        }

        var lines = logContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
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
