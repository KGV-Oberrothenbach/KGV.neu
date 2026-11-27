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
    public static IReadOnlyList<PdfTemplateRenderOperation> BuildRenderPlan(IReadOnlyList<PdfTemplateFieldSpec> fields, IReadOnlyDictionary<string, string> values, PdfTemplateRenderOptions? options = null)
    {
        options ??= PdfTemplateRenderOptions.Default; var result = new List<PdfTemplateRenderOperation>();
        foreach (var field in fields)
        {
            if (string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase) || !values.TryGetValue(field.Name, out var value)) continue;
            if (string.Equals(field.Kind, "checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (options.UseLegacyTextCheckbox(field.Name))
                {
                    if (IsTruthy(value)) result.Add(new(field.Page, "text", field.Name, options.CheckboxGlyph, field.X + options.SingleLineHorizontalPadding, field.Y + options.SingleLineVerticalOffset, field.Width - 2 * options.SingleLineHorizontalPadding, options.DefaultFontSize * options.LineHeightFactor, options.FontName, options.DefaultFontSize));
                    continue;
                }
                result.Add(new(field.Page, "checkbox", field.Name, IsTruthy(value) ? options.CheckboxGlyph : string.Empty, field.X, field.Y, field.Width, field.Height, options.FontName, options.CheckboxFontSize)); continue;
            }
            if (string.IsNullOrWhiteSpace(value)) continue;
            var fontSize = options.UseSmallFont(field.Name, value) ? options.SmallFontSize : options.DefaultFontSize;
            var multiline = string.Equals(field.Kind, "multiline", StringComparison.OrdinalIgnoreCase) || value.Contains('\n'); var y = field.Y + (multiline ? options.MultilineTopPadding : options.SingleLineVerticalOffset); var lineHeight = fontSize * options.LineHeightFactor;
            foreach (var line in SplitLines(value)) { if (multiline && y + lineHeight > field.Y + field.Height + 1) break; result.Add(new(field.Page, multiline ? "multiline" : "text", field.Name, line, field.X + (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding), y, field.Width - 2 * (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding), lineHeight, options.FontName, fontSize)); y += multiline ? lineHeight : 0; }
        }
        return result;
    }
    public static byte[] Render(byte[] templatePdf, IReadOnlyList<PdfTemplateFieldSpec> fields, IReadOnlyDictionary<string, string> values, PdfTemplateRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(templatePdf);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);
        options ??= PdfTemplateRenderOptions.Default;
        PdfSharpFontResolverInitializer.EnsureInitialized();
        using var input = new MemoryStream(templatePdf);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        foreach (var page in document.Pages) page.Annotations.Clear();
        var font = new XFont(options.FontName, options.DefaultFontSize, XFontStyle.Regular);
        var smallFont = new XFont(options.FontName, options.SmallFontSize, XFontStyle.Regular);
        var checkboxFont = new XFont(options.FontName, options.CheckboxFontSize, XFontStyle.Bold);
        foreach (var field in fields)
        {
            if (string.Equals(field.Kind, "signature", StringComparison.OrdinalIgnoreCase) || field.Page < 1 || field.Page > document.Pages.Count || !values.TryGetValue(field.Name, out var value)) continue;
            var page = document.Pages[field.Page - 1];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var rect = new XRect(field.X, field.Y, field.Width, field.Height);
            if (string.Equals(field.Kind, "checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (options.UseLegacyTextCheckbox(field.Name))
                {
                    if (IsTruthy(value)) graphics.DrawString(options.CheckboxGlyph, font, XBrushes.Black, new XRect(rect.X + options.SingleLineHorizontalPadding, rect.Y + options.SingleLineVerticalOffset, Math.Max(0, rect.Width - 2 * options.SingleLineHorizontalPadding), rect.Height + 2), XStringFormats.TopLeft);
                    continue;
                }
                graphics.DrawRectangle(new XPen(XColors.Black, .6), rect);
                if (IsTruthy(value)) { var size = graphics.MeasureString(options.CheckboxGlyph, checkboxFont); graphics.DrawString(options.CheckboxGlyph, checkboxFont, XBrushes.Black, new XRect(rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2 - options.CheckboxVerticalOffset, size.Width + 1, size.Height + 1), XStringFormats.TopLeft); }
                continue;
            }
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (options.RequireCompleteText)
            {
                DrawCompleteText(graphics, rect, field, value, options);
                continue;
            }
            var selectedFont = options.UseSmallFont(field.Name, value) ? smallFont : font;
            var lines = SplitLines(value); var multiline = string.Equals(field.Kind, "multiline", StringComparison.OrdinalIgnoreCase) || lines.Count > 1;
            var y = rect.Y + (multiline ? options.MultilineTopPadding : options.SingleLineVerticalOffset);
            foreach (var line in lines) { var height = selectedFont.GetHeight(); if (multiline && y + height > rect.Bottom + 1) break; graphics.DrawString(line, selectedFont, XBrushes.Black, new XRect(rect.X + (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding), y, Math.Max(0, rect.Width - 2 * (multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding)), height + 1), XStringFormats.TopLeft); y += multiline ? height : 0; }
        }
        using var output = new MemoryStream(); document.Save(output); return output.ToArray();
    }

    private static void DrawCompleteText(XGraphics graphics, XRect rect, PdfTemplateFieldSpec field, string value, PdfTemplateRenderOptions options)
    {
        var sourceLines = SplitLines(value);
        var multiline = string.Equals(field.Kind, "multiline", StringComparison.OrdinalIgnoreCase);
        if (!multiline && sourceLines.Count != 1)
            throw new InvalidOperationException($"PDF-Feld '{field.Name}' erwartet eine Zeile.");
        var padding = multiline ? options.MultilineHorizontalPadding : options.SingleLineHorizontalPadding;
        var top = multiline ? options.MultilineTopPadding : Math.Max(0, options.SingleLineVerticalOffset);
        var width = rect.Width - 2 * padding;
        var preferred = options.UseSmallFont(field.Name, value) ? options.SmallFontSize : options.DefaultFontSize;
        for (var size = preferred; size >= 4.999; size = Math.Round(size - 0.2, 1))
        {
            var selected = new XFont(options.FontName, size, XFontStyle.Regular);
            var lines = new List<string>();
            var fits = width > 0;
            foreach (var line in sourceLines)
            {
                if (graphics.MeasureString(line, selected).Width <= width) { lines.Add(line); continue; }
                if (!multiline) { fits = false; break; }
                var current = string.Empty;
                foreach (var word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (graphics.MeasureString(word, selected).Width > width) { fits = false; break; }
                    var next = current.Length == 0 ? word : current + " " + word;
                    if (graphics.MeasureString(next, selected).Width <= width) current = next;
                    else { lines.Add(current); current = word; }
                }
                if (!fits) break;
                if (current.Length > 0) lines.Add(current);
            }
            var height = selected.GetHeight();
            if (!fits || lines.Count == 0 || top + lines.Count * height > rect.Height) continue;
            for (var i = 0; i < lines.Count; i++)
                graphics.DrawString(lines[i], selected, XBrushes.Black, new XRect(rect.X + padding, rect.Y + top + i * height, width, height), XStringFormats.TopLeft);
            return;
        }
        throw new InvalidOperationException($"PDF-Feld '{field.Name}' würde unvollständig dargestellt.");
    }

    private static List<string> SplitLines(string text) => text.Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase).Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase).Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    private static bool IsTruthy(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "ja", StringComparison.OrdinalIgnoreCase);
}

public sealed record PdfTemplateRenderOptions(string FontName, double DefaultFontSize, double SmallFontSize, double CheckboxFontSize, double SingleLineHorizontalPadding, double SingleLineVerticalOffset, double MultilineHorizontalPadding, double MultilineTopPadding, double CheckboxVerticalOffset, string CheckboxGlyph, Func<string, string, bool> UseSmallFont, double LineHeightFactor = 1, Func<string, bool>? LegacyTextCheckbox = null)
{
    public static PdfTemplateRenderOptions Default { get; } = new("Arial", 7, 6.4, 7, 1.8, 0, 2, 1, .4, "X", (_, value) => value.Length > 34);
    public bool RequireCompleteText { get; init; }
    public bool UseLegacyTextCheckbox(string fieldName) => LegacyTextCheckbox?.Invoke(fieldName) == true;
}

public sealed record PdfTemplateRenderOperation(int Page, string Kind, string FieldName, string Text, double X, double Y, double Width, double Height, string FontName, double FontSize);

public static class PdfTemplateRenderProfiles
{
    public static PdfTemplateRenderOptions Mitgliedsantrag { get; } = PdfTemplateRenderOptions.Default with { SingleLineVerticalOffset = -1.1, UseSmallFont = (name, value) => name.Equals("bank_iban", StringComparison.OrdinalIgnoreCase) || (name.Equals("mitglied_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30) || (name.Equals("vertreter_email", StringComparison.OrdinalIgnoreCase) && value.Length > 30) || value.Length > 34 };
    public static PdfTemplateRenderOptions PachtvertragV10 => Pachtvertrag with { RequireCompleteText = true };
    public static PdfTemplateRenderOptions Pachtvertrag { get; } = PdfTemplateRenderOptions.Default with
    {
        DefaultFontSize = 7.2,
        UseSmallFont = (name, value) => name.Equals("bankblock_mehrzeilig", StringComparison.OrdinalIgnoreCase) || value.Length > 34,
        LegacyTextCheckbox = name => name.StartsWith("anlage_", StringComparison.OrdinalIgnoreCase)
    };
}
