using System;
using System.Text;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseNotesImportExportService
{
    public string CreateChatPrompt(string currentVersion, string nextVersion, string logExcerpt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bitte erstelle aus den folgenden Änderungen eine Release-Zusammenfassung.");
        sb.AppendLine($"Bisherige Version: {currentVersion}");
        sb.AppendLine($"Neue Version: {nextVersion}");
        sb.AppendLine();
        sb.AppendLine("Format:");
        sb.AppendLine("- Kurztext");
        sb.AppendLine("- Details");
        sb.AppendLine("- Abschnitte: Neu, Verbessert, Behoben");
        sb.AppendLine();
        sb.AppendLine("Änderungen / Logauszug:");
        sb.AppendLine(logExcerpt);
        return sb.ToString();
    }

    public string NormalizeImportedSummary(string rawText)
    {
        return string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText.Trim().Replace("\r\n", Environment.NewLine);
    }
}
