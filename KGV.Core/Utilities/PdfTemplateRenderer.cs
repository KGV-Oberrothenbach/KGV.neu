using System;
using System.Collections.Generic;
using System.IO;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities;

/// <summary>Technical renderer for normalized PDF field specs. Signature fields are intentionally skipped.</summary>
public static class PdfTemplateRenderer
{
    public static byte[] Render(byte[] templatePdf, IReadOnlyList<PdfTemplateFieldSpec> fields, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(templatePdf);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);
        using var input = new MemoryStream(templatePdf);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        foreach (var page in document.Pages) page.Annotations.Clear();
        var font = new XFont("Arial", 7.0, XFontStyle.Regular);
        var checkboxFont = new XFont("Arial", 7.0, XFontStyle.Bold);
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
                if (string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)) graphics.DrawString("X", checkboxFont, XBrushes.Black, rect, XStringFormats.Center);
                continue;
            }
            if (string.IsNullOrWhiteSpace(value)) continue;
            var lines = value.Replace("\r\n", "\n").Split('\n');
            for (var index = 0; index < lines.Length; index++) graphics.DrawString(lines[index], font, XBrushes.Black, new XRect(rect.X + 1.8, rect.Y + index * 8, rect.Width - 3.6, 8), XStringFormats.TopLeft);
        }
        using var output = new MemoryStream(); document.Save(output); return output.ToArray();
    }
}
