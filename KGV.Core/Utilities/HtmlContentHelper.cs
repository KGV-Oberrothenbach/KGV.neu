using System;
using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace KGV.Core.Utilities;

public static class HtmlContentHelper
{
    private static readonly Regex HtmlTagRegex = new(@"<\s*/?\s*[a-zA-Z][^>]*>", RegexOptions.Compiled);

    public static string SanitizeFragment(string? html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear(); sanitizer.AllowedTags.UnionWith(["p","br","strong","b","em","i","u","s","h1","h2","h3","h4","ul","ol","li","blockquote","a","table","thead","tbody","tr","th","td","hr","code","pre"]);
        sanitizer.AllowedAttributes.Clear(); sanitizer.AllowedAttributes.UnionWith(["href","title","colspan","rowspan"]);
        sanitizer.AllowedSchemes.Clear(); sanitizer.AllowedSchemes.UnionWith(["http","https","mailto","tel"]);
        return sanitizer.Sanitize(html ?? string.Empty);
    }

    public static string BuildHtmlDocument(string? html, string? emptyMessage = null)
    {
        var content = string.IsNullOrWhiteSpace(html)
            ? BuildEmptyMessageHtml(emptyMessage)
            : html.Trim();

        var body = HtmlTagRegex.IsMatch(content)
            ? SanitizeFragment(content)
            : ConvertPlainTextToHtml(content);

        return "<!DOCTYPE html>\n"
            + "<html>\n"
            + "<head>\n"
            + "    <meta charset=\"utf-8\" />\n"
            + "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n"
            + "    <meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; script-src 'none'; object-src 'none'; frame-src 'none'; base-uri 'none'; form-action 'none'; style-src 'unsafe-inline'\" />\n"
            + "    <style>\n"
            + "        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 16px; color: #222; line-height: 1.5; word-break: break-word; }\n"
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
