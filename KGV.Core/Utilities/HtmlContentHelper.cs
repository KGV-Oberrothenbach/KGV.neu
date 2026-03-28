using System;
using System.Net;
using System.Text.RegularExpressions;

namespace KGV.Core.Utilities;

public static class HtmlContentHelper
{
    private static readonly Regex HtmlDocumentRegex = new(@"<\s*(?:!doctype|html)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<\s*/?\s*[a-zA-Z][^>]*>", RegexOptions.Compiled);

    public static string BuildHtmlDocument(string? html, string? emptyMessage = null)
    {
        var content = string.IsNullOrWhiteSpace(html)
            ? BuildEmptyMessageHtml(emptyMessage)
            : html.Trim();

        if (HtmlDocumentRegex.IsMatch(content))
            return content;

        var body = HtmlTagRegex.IsMatch(content)
            ? content
            : ConvertPlainTextToHtml(content);

        return "<!DOCTYPE html>\n"
            + "<html>\n"
            + "<head>\n"
            + "    <meta charset=\"utf-8\" />\n"
            + "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n"
            + "    <style>\n"
            + "        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 16px; color: #222; line-height: 1.5; word-break: break-word; }\n"
            + "        img { max-width: 100%; height: auto; }\n"
            + "        table { border-collapse: collapse; max-width: 100%; }\n"
            + "        td, th { border: 1px solid #ccc; padding: 4px; text-align: left; }\n"
            + "        a { color: #1d5f91; }\n"
            + "    </style>\n"
            + "</head>\n"
            + $"<body>{body}</body>\n"
            + "</html>";
    }

    private static string BuildEmptyMessageHtml(string? emptyMessage)
    {
        var message = string.IsNullOrWhiteSpace(emptyMessage)
            ? "Noch kein HTML-Inhalt vorhanden."
            : emptyMessage.Trim();

        return $"<p style='color:#666;'>{WebUtility.HtmlEncode(message)}</p>";
    }

    private static string ConvertPlainTextToHtml(string text)
    {
        return WebUtility.HtmlEncode(text)
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal);
    }
}
