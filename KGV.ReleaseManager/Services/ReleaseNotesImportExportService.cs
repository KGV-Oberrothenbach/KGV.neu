using System.Text;
using System.Text.RegularExpressions;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseNotesImportExportService
{
    public string CreateExportText(
        string currentVersion,
        string nextVersion,
        string logSourcePath,
        string sourceDescription,
        string changesPreview,
        IReadOnlyList<string> includedHeadings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Zielversion: {nextVersion}");
        sb.AppendLine($"Aktueller Versionsstand: {currentVersion}");
        sb.AppendLine($"Logquelle: {logSourcePath}");
        sb.AppendLine($"Ausgewerteter Logbereich: {sourceDescription}");

        if (includedHeadings.Count > 0)
        {
            sb.AppendLine("Berücksichtigte Logüberschriften:");
            foreach (var heading in includedHeadings)
            {
                sb.AppendLine($"- {heading}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Rohzusammenfassung relevanter Änderungen:");
        sb.AppendLine(changesPreview);
        sb.AppendLine();
        sb.AppendLine("ChatGPT-Prompt:");
        sb.AppendLine(CreateChatPrompt(currentVersion, nextVersion, changesPreview));
        return sb.ToString();
    }

    public string CreateChatPrompt(string currentVersion, string nextVersion, string logExcerpt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Erstelle aus den folgenden KGV-Änderungen eine saubere Release-Zusammenfassung.");
        sb.AppendLine("ReleaseManager-interne Änderungen, Build-/Tooling-Arbeiten und rein technische ReleaseManager-Notizen dürfen nicht in der Endfassung auftauchen.");
        sb.AppendLine($"Bisherige Version: {currentVersion}");
        sb.AppendLine($"Neue Version: {nextVersion}");
        sb.AppendLine();
        sb.AppendLine("Gib die Antwort exakt in diesem Format zurück:");
        sb.AppendLine($"# Release {nextVersion}");
        sb.AppendLine("Titel: <kurzer Release-Titel>");
        sb.AppendLine("Kurzbeschreibung: <1-2 Sätze für Endnutzer>");
        sb.AppendLine("## WPF / Download");
        sb.AppendLine("### Neu");
        sb.AppendLine("- ...");
        sb.AppendLine("### Verbessert");
        sb.AppendLine("- ...");
        sb.AppendLine("### Behoben");
        sb.AppendLine("- ...");
        sb.AppendLine("## Android / Play Store");
        sb.AppendLine("### Neu");
        sb.AppendLine("- ...");
        sb.AppendLine("### Verbessert");
        sb.AppendLine("- ...");
        sb.AppendLine("### Behoben");
        sb.AppendLine("- ...");
        sb.AppendLine();
        sb.AppendLine("Nutze nur Änderungen mit Endnutzerrelevanz für KGV.Wpf und KGV.Maui/KGV-Android.");
        sb.AppendLine();
        sb.AppendLine("Änderungen / Logauszug:");
        sb.AppendLine(logExcerpt);
        return sb.ToString().Trim();
    }

    public string NormalizeImportedSummary(string rawText)
    {
        return string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText.Trim().Replace("\r\n", Environment.NewLine);
    }

    public ReleaseNotesImportResult ParseImportedSummary(string rawText)
    {
        var normalizedText = NormalizeImportedSummary(rawText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return new ReleaseNotesImportResult
            {
                Message = "Die importierte Zusammenfassung ist leer."
            };
        }

        var wpfSection = ExtractSection(normalizedText, "WPF / Download", "WPF");
        var androidSection = ExtractSection(normalizedText, "Android / Play Store", "Android", "Play Store");

        if (string.IsNullOrWhiteSpace(wpfSection) && string.IsNullOrWhiteSpace(androidSection))
        {
            return new ReleaseNotesImportResult
            {
                Message = "Die importierte Zusammenfassung muss mindestens einen der Abschnitte `## WPF / Download` oder `## Android / Play Store` enthalten.",
                NormalizedText = normalizedText
            };
        }

        return new ReleaseNotesImportResult
        {
            Success = true,
            Message = "Importtext wurde erkannt und kann gespeichert werden.",
            Title = ExtractTitle(normalizedText),
            ShortDescription = ExtractShortDescription(normalizedText),
            WpfReleaseText = wpfSection,
            AndroidReleaseText = androidSection,
            NormalizedText = normalizedText
        };
    }

    private static string ExtractTitle(string normalizedText)
    {
        var titleMatch = Regex.Match(normalizedText, "^Titel:\\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            return titleMatch.Groups["value"].Value.Trim();
        }

        var headingMatch = Regex.Match(normalizedText, "^#\\s+(?<value>.+)$", RegexOptions.Multiline);
        return headingMatch.Success
            ? headingMatch.Groups["value"].Value.Trim()
            : string.Empty;
    }

    private static string ExtractShortDescription(string normalizedText)
    {
        var match = Regex.Match(normalizedText, "^Kurzbeschreibung:\\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static string ExtractSection(string normalizedText, params string[] sectionTitles)
    {
        foreach (var sectionTitle in sectionTitles)
        {
            var pattern = $"^##\\s+{Regex.Escape(sectionTitle)}\\s*$";
            var match = Regex.Match(normalizedText, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            var start = match.Index + match.Length;
            var nextHeading = Regex.Match(normalizedText[start..], "^##\\s+.+$", RegexOptions.Multiline);
            var content = nextHeading.Success
                ? normalizedText[start..(start + nextHeading.Index)]
                : normalizedText[start..];

            return content.Trim();
        }

        return string.Empty;
    }
}
