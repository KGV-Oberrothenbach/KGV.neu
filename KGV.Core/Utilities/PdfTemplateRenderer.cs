using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities;

/// <summary>Technical renderer for normalized PDF field specs. Signature fields are intentionally skipped.</summary>
public static class PdfTemplateRenderer
{
    public static byte[] Render(byte[] templatePdf, IReadOnlyList<PdfTemplateFieldSpec> fields, IReadOnlyDictionary<string, string> values, PdfTemplateRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(templatePdf);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);
        options ??= PdfTemplateRenderOptions.Default;
        using var input = new MemoryStream(templatePdf);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        foreach (var page in document.Pages) page.Annotations.Clear();
        var font = new XFont(options.FontName, options.DefaultFontSize, XFontStyle.Regular);
        var smallFont = new XFont(options.FontName, options.SmallFontSize, XFontStyle.Regular);
        var checkboxFont = new XFont(options.FontName, options.CheckboxFontSize, XFontStyle.Bold);
        foreach (var field in fields)
        {
            if (string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase)
                || field.Page < 1 || field.Page > document.Pages.Count) continue;
            if (!values.TryGetValue(field.Name, out var value)) continue;
            var page = document.Pages[field.Page - 1];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var rect = new XRect(field.X, field.Y, field.Width, field.Height);
            if (string.Equals(field.Kind, "checkbox", StringComparison.OrdinalIgnoreCase))
            {
                graphics.DrawRectangle(new XPen(XColors.Black, .6), rect);
                if (IsTruthy(value)) { var size = graphics.MeasureString(options.CheckboxGlyph, checkboxFont); graphics.DrawString(options.CheckboxGlyph, checkboxFont, XBrushes.Black, new XRect(rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2 - options.CheckboxVerticalOffset, size.Width + 1, size.Height + 1), XStringFormats.TopLeft); }
                continue;
            }
            if (string.IsNullOrWhiteSpace(value)) continue;
            var selectedFont = options.UseSmallFont(field.Name, value) ? smallFont : font;
            var lines = SplitLines(value); var multiline = string.Equals(field.Kind, "multiline", StringComparison.OrdinalIgnoreCase) || lines.Count > 1;
            var y = rect.Y + (multiline ? options.MultilineTopPadding : options.SingleLineVerticalOffset);
            foreach (var line in lines) { var height = selectedFont.GetHeight(); if (multiline && y + height > rect.Bottom + 1) break; graphics.DrawString(line, selectedFont, XBrushes.Black, new XRect(rect.X + (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding), y, Math.Max(0, rect.Width - 2 * (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding)), height + 1), XStringFormats.TopLeft); y += multiline ? height : 0; }
        }
        using var output = new MemoryStream(); document.Save(output); return output.ToArray();
    }

    private static List<string> SplitLines(string text) => text.Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase).Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase).Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    private static bool IsTruthy(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "ja", StringComparison.OrdinalIgnoreCase);
}

public sealed record PdfTemplateRenderOptions(string FontName, double DefaultFontSize, double SmallFontSize, double CheckboxFontSize, double SingleLineHorizontalPadding, double SingleLineVerticalOffset, double MultilineHorizontalPadding, double MultilineTopPadding, double CheckboxVerticalOffset, string CheckboxGlyph, Func<string, string, bool> UseSmallFont)
{
    public static PdfTemplateRenderOptions Default { get; } = new("Arial", 7, 6.4, 7, 1.8, 0, 2, 1, .4, "X", (_, value) => value.Length > 34);
}

public static class PdfTemplateRenderProfiles
{
    public static PdfTemplateRenderOptions Mitgliedsantrag { get; } = PdfTemplateRenderOptions.Default with { SingleLineVerticalOffset = -1.1, UseSmallFont = (name, value) => name.Equals("bank_iban", StringComparison.OrdinalIgnoreCase) || (name.Equals("mitglied_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30) || (name.Equals("vertreter_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30) || value.Length > 34 };
    public static PdfTemplateRenderOptions Pachtvertrag { get; } = PdfTemplateRenderOptions.Default with { DefaultFontSize = 7.2, UseSmallFont = (name, value) => name.Equals("bankblock_mehrzeilig", StringComparison.OrdinalIgnoreCase) || value.Length > 34 };
}
