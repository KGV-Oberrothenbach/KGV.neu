using System;
using System.IO;
using System.Linq;

namespace KGV.ReleaseManager.Services;

public sealed class LogExtractionService
{
    public string GetLatestSection(string logFilePath)
    {
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
}
