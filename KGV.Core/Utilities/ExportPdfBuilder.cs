using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities
{
    public static class ExportPdfBuilder
    {
        private const double PageMargin = 28;
        private const double HeaderHeight = 28;

        public static byte[] BuildExportPdf(string exportKey, IReadOnlyList<AppExportColumnDefinitionRecord> columns, IReadOnlyList<Dictionary<string, string>> rows)
        {
            PdfSharpFontResolverInitializer.EnsureInitialized();

            var doc = new PdfDocument();
            doc.Info.Title = exportKey ?? "export";

            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            page.Orientation = PdfSharpCore.PageOrientation.Landscape;

            var gfx = XGraphics.FromPdfPage(page);

            var headerFont = new XFont("Arial", 12, XFontStyle.Bold);
            var cellFont = new XFont("Arial", 9, XFontStyle.Regular);

            double usableWidth = page.Width - PageMargin * 2;
            double x = PageMargin;
            double y = PageMargin;

            // Determine column widths (simple equal split, can be tuned)
            int colCount = Math.Max(1, columns.Count);
            double colWidth = usableWidth / colCount;

            // Short header mapping for known export keys
            Func<AppExportColumnDefinitionRecord, string> headerText = col => GetShortHeader(exportKey, col);

            // Render header
            for (int i = 0; i < columns.Count; i++)
            {
                var rect = new XRect(x + i * colWidth, y, colWidth, HeaderHeight);
                gfx.DrawRectangle(XBrushes.LightGray, rect);
                gfx.DrawString(headerText(columns[i]) ?? columns[i].Label ?? columns[i].Name ?? string.Empty, headerFont, XBrushes.Black, rect, XStringFormats.Center);
            }

            y += HeaderHeight + 6;

            double rowHeight = 16; // base height
            int rowsPerPage = (int)Math.Floor((page.Height - PageMargin - y) / rowHeight);
            if (rowsPerPage <= 0) rowsPerPage = 1;

            int currentRowOnPage = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                if (currentRowOnPage >= rowsPerPage)
                {
                    // new page
                    page = doc.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    x = PageMargin;
                    y = PageMargin;

                    // header on new page
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var rect = new XRect(x + i * colWidth, y, colWidth, HeaderHeight);
                        gfx.DrawRectangle(XBrushes.LightGray, rect);
                        gfx.DrawString(headerText(columns[i]) ?? columns[i].Label ?? columns[i].Name ?? string.Empty, headerFont, XBrushes.Black, rect, XStringFormats.Center);
                    }

                    y += HeaderHeight + 6;
                    currentRowOnPage = 0;
                }

                var row = rows[r];

                // render each column cell; for member/contacts handle multiline
                for (int c = 0; c < columns.Count; c++)
                {
                    var col = columns[c];
                    var key = col.Name ?? string.Empty;
                    string value = row.ContainsKey(key) ? row[key] ?? string.Empty : string.Empty;

                    // compact/checkbox handling
                    if (IsBooleanLike(value))
                    {
                        value = value.Equals("Ja", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "☑" : "☐";
                    }

                    var rect = new XRect(x + c * colWidth + 4, y, colWidth - 8, rowHeight);
                    gfx.DrawString(TruncateForCell(value), cellFont, XBrushes.Black, rect, XStringFormats.TopLeft);
                }

                y += rowHeight;
                currentRowOnPage++;
            }

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }

        private static string TruncateForCell(string value, int max = 200)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= max) return value;
            return value.Substring(0, max - 3) + "...";
        }

        private static bool IsBooleanLike(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            var v = val.Trim();
            return v.Equals("Ja", StringComparison.OrdinalIgnoreCase) || v.Equals("Nein", StringComparison.OrdinalIgnoreCase) || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase) || v.Equals("0") || v.Equals("1");
        }

        private static string GetShortHeader(string exportKey, AppExportColumnDefinitionRecord col)
        {
            // prefer explicitly mapped short names for mitgliederliste
            if (string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase))
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "nr", "Nr" },
                    { "nummer", "Nr" },
                    { "wa", "WA" },
                    { "re", "RE" },
                    { "info", "Info" },
                    { "app", "App" },
                    { "geb", "Geb." },
                    { "geburtsdatum", "Geb." },
                    { "seit", "Seit" },
                    { "gaerten", "Gärten" },
                    { "gaerten_count", "Gärten" },
                    { "garten", "Gärten" },
                    { "aktiv", "Aktiv" }
                };

                if (!string.IsNullOrWhiteSpace(col.Name) && map.TryGetValue(col.Name, out var s))
                    return s;

                if (!string.IsNullOrWhiteSpace(col.Label) && map.TryGetValue(col.Label, out var s2))
                    return s2;
            }

            if (string.Equals(exportKey, "rfid_status", StringComparison.OrdinalIgnoreCase))
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "medium", "Med" },
                    { "strom", "Strom" },
                    { "wasser", "Wasser" },
                    { "beide", "Beide" },
                    { "fehlt", "Fehlt" },
                    { "status", "Status" }
                };

                if (!string.IsNullOrWhiteSpace(col.Name) && map.TryGetValue(col.Name, out var s))
                    return s;

                if (!string.IsNullOrWhiteSpace(col.Label) && map.TryGetValue(col.Label, out var s2))
                    return s2;
            }

            return col.Label ?? col.Name ?? string.Empty;
        }
    }
}
