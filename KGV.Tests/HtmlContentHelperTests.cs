using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class HtmlContentHelperTests
{
    [Theory]
    [InlineData("<p>Hallo</p><script>alert(1)</script>", "<script")]
    [InlineData("<p onclick=\"alert(1)\">Text</p>", "onclick")]
    [InlineData("<a href=\"javascript:alert(1)\">Klick</a>", "javascript:")]
    [InlineData("<iframe src=\"https://example.org\"></iframe>", "<iframe")]
    [InlineData("<p style=\"position:fixed\">Text</p>", "style=")]
    [InlineData("<img src=\"https://example.org/a.png\">", "<img")]
    public void SanitizeFragment_RemovesUnsafeContent(string html, string forbidden)
        => Assert.DoesNotContain(forbidden, HtmlContentHelper.SanitizeFragment(html), StringComparison.OrdinalIgnoreCase);

    [Fact] public void SanitizeFragment_PreservesAllowedFormattingAndLinks()
    { var html = HtmlContentHelper.SanitizeFragment("<h3>Überschrift</h3><p><strong>Text</strong></p><ul><li>Punkt</li></ul><a href=\"https://example.org\">Web</a><a href=\"mailto:test@example.org\">Mail</a>"); Assert.Contains("<h3>Überschrift</h3>", html); Assert.Contains("https://example.org", html); Assert.Contains("mailto:test@example.org", html); }
    [Fact] public void BuildHtmlDocument_AlwaysUsesSafeShell()
    { var html = HtmlContentHelper.BuildHtmlDocument("<html><head><script>x()</script></head><body><p>Text</p></body></html>"); Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase); Assert.Contains("Content-Security-Policy", html); Assert.Contains("<p>Text</p>", html); }
    [Fact] public void BuildHtmlDocument_EncodesPlainText()
    { var html = HtmlContentHelper.BuildHtmlDocument("A < B & C\nZeile 2"); Assert.Contains("A &lt; B &amp; C<br />Zeile 2", html); }
    [Theory]
    [InlineData("http://example.org")]
    [InlineData("https://example.org")]
    [InlineData("mailto:test@example.org")]
    [InlineData("tel:+491234567")]
    public void SanitizeFragment_PreservesSafeSchemes(string href) => Assert.Contains($"href=\"{href}\"", HtmlContentHelper.SanitizeFragment($"<a href=\"{href}\">Link</a>"));
    [Theory]
    [InlineData("data:text/html,test")]
    [InlineData("file:///etc/passwd")]
    [InlineData("vbscript:msgbox(1)")]
    public void SanitizeFragment_RemovesUnsafeSchemes(string href) => Assert.DoesNotContain(href, HtmlContentHelper.SanitizeFragment($"<a href=\"{href}\">Link</a>"));
    [Fact] public void BuildHtmlDocument_EncodesFallback() { var html = HtmlContentHelper.BuildHtmlDocument(null, "A < B & C"); Assert.Contains("A &lt; B &amp; C", html); Assert.Contains("Content-Security-Policy", html); }
    [Fact] public void BuildHtmlDocument_ShowsAnnouncementEmptyFallback() { var html = HtmlContentHelper.BuildHtmlDocument(null, "Kein Inhalt hinterlegt."); Assert.Contains("Kein Inhalt hinterlegt.", html); Assert.Contains("Content-Security-Policy", html); Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase); }
    [Fact] public void SanitizeFragment_PreservesTableSpans() { var html = HtmlContentHelper.SanitizeFragment("<table><tbody><tr><td colspan=\"2\" rowspan=\"3\">Text</td></tr></tbody></table>"); Assert.Contains("colspan=\"2\"", html); Assert.Contains("rowspan=\"3\"", html); }
}
