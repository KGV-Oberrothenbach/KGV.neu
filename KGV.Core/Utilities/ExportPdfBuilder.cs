using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing.Layout;

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

            // Build effective columns for PDF (collapse address/contact groups for mitgliederliste)
            var effectiveCols = new List<AppExportColumnDefinitionRecord>();
            var skipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var addressKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "adresse", "plz", "ort", "strasse", "hausnummer" };
            var contactKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "telefon", "handy", "mobil" };

            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                var n = (c.Name ?? string.Empty).ToLowerInvariant();
                if (string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase) && addressKeys.Contains(n))
                {
                    if (!effectiveCols.Any(ec => addressKeys.Contains((ec.Name ?? string.Empty).ToLowerInvariant())))
                        effectiveCols.Add(new AppExportColumnDefinitionRecord { Name = "__pdf_address__", Label = "Adresse", Visible = true, SortOrder = c.SortOrder });
                    continue;
                }

                if (string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase) && contactKeys.Contains(n))
                {
                    if (!effectiveCols.Any(ec => contactKeys.Contains((ec.Name ?? string.Empty).ToLowerInvariant()) || (ec.Name ?? string.Empty) == "__pdf_contact__"))
                        effectiveCols.Add(new AppExportColumnDefinitionRecord { Name = "__pdf_contact__", Label = "Kontakt", Visible = true, SortOrder = c.SortOrder });
                    continue;
                }

                // otherwise include as-is
                effectiveCols.Add(c);
            }

            // compute column widths with weights: small for checkbox-like, medium for short, large for text
            var weights = new double[effectiveCols.Count];
            for (int i = 0; i < effectiveCols.Count; i++)
            {
                var cn = (effectiveCols[i].Name ?? string.Empty).ToLowerInvariant();
                if (cn.StartsWith("nr") || cn == "__pdf_address__" || cn == "__pdf_contact__")
                    weights[i] = 2; // mid
                else if (IsShortFieldName(cn) || cn == "aktiv" || cn == "wa" || cn == "re" || cn == "info" || cn == "app")
                    weights[i] = 1; // small
                else
                    weights[i] = 3; // larger
            }

            var totalWeight = weights.Sum();
            var colWidths = weights.Select(w => usableWidth * (w / totalWeight)).ToArray();

            Func<AppExportColumnDefinitionRecord, string> headerText = col => GetShortHeader(exportKey, col);

            // draw header on first page
            DrawHeaderRow(gfx, headerFont, effectiveCols, colWidths, x, y);
            y += HeaderHeight + 6;

            var textFormatter = new XTextFormatter(gfx);

            int currentRowOnPage = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                // estimate per-row height based on wrapped text in each cell
                double rowH = 0;
                var row = rows[r];
                for (int c = 0; c < effectiveCols.Count; c++)
                {
                    var col = effectiveCols[c];
                    var key = col.Name ?? string.Empty;
                    string value = GetPdfCellValue(exportKey, col, row);

                    if (IsBooleanLike(value))
                        value = value.Equals("Ja", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "☑" : "☐";

                    var cellWidth = colWidths[c] - 8;
                    // approximate lines required
                    var lines = EstimateLines(gfx, value, cellFont, cellWidth);
                    var lineHeight = gfx.MeasureString("Ag", cellFont).Height + 2;
                    rowH = Math.Max(rowH, lines * lineHeight);
                }

                if (rowH < 14) rowH = 14;

                // check for page break
                if (y + rowH > page.Height - PageMargin)
                {
                    // new page
                    page = doc.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    textFormatter = new XTextFormatter(gfx);
                    x = PageMargin;
                    y = PageMargin;
                    DrawHeaderRow(gfx, headerFont, effectiveCols, colWidths, x, y);
                    y += HeaderHeight + 6;
                }

                // render cells
                for (int c = 0; c < effectiveCols.Count; c++)
                {
                    var col = effectiveCols[c];
                    var key = col.Name ?? string.Empty;
                    string value = GetPdfCellValue(exportKey, col, row);

                    if (IsBooleanLike(value))
                        value = value.Equals("Ja", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "☑" : "☐";

                    var rect = new XRect(x + GetOffset(colWidths, c) + 4, y, colWidths[c] - 8, rowH);
                    textFormatter.Alignment = XParagraphAlignment.Left;
                    textFormatter.DrawString(TruncateForCell(value, 1000), cellFont, XBrushes.Black, rect);
                }

                y += rowH + 4;
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

        private static string GetPdfCellValue(string exportKey, AppExportColumnDefinitionRecord col, Dictionary<string, string> row)
        {
            var key = (col.Name ?? string.Empty).ToLowerInvariant();
            if (key == "__pdf_address__")
            {
                // build address: Line1: Adresse (Straße Hsnr), Line2: PLZ Ort
                row.TryGetValue("adresse", out var adr);
                row.TryGetValue("plz", out var plz);
                row.TryGetValue("ort", out var ort);
                var line1 = adr ?? string.Empty;
                var line2 = string.Join(" ", new[] { plz, ort }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                return string.IsNullOrWhiteSpace(line2) ? line1 : line1 + "\n" + line2;
            }

            if (key == "__pdf_contact__")
            {
                row.TryGetValue("telefon", out var tel);
                row.TryGetValue("handy", out var mob);
                var line1 = tel ?? string.Empty;
                var line2 = mob ?? string.Empty;
                return string.IsNullOrWhiteSpace(line2) ? line1 : line1 + "\n" + line2;
            }

            row.TryGetValue(col.Name ?? string.Empty, out var val);
            return val ?? string.Empty;
        }

        private static double GetOffset(double[] colWidths, int index)
        {
            double off = 0;
            for (int i = 0; i < index; i++) off += colWidths[i];
            return off;
        }

        private static int EstimateLines(XGraphics gfx, string text, XFont font, double width)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            var measurement = gfx.MeasureString(text, font);
            // naive estimate: measure average char width
            var avgCharWidth = measurement.Width / Math.Max(1, text.Length);
            var charsPerLine = Math.Max(1, (int)(width / avgCharWidth));
            return (int)Math.Ceiling((double)text.Length / charsPerLine);
        }

        private static bool IsShortFieldName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var shortKeys = new[] { "nr", "id", "med", "wa", "re", "app", "info", "geb", "seit" };
            return shortKeys.Any(k => name.StartsWith(k, StringComparison.OrdinalIgnoreCase));
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

        private static void DrawHeaderRow(XGraphics gfx, XFont headerFont, List<AppExportColumnDefinitionRecord> cols, double[] colWidths, double x, double y)
        {
            for (int i = 0; i < cols.Count; i++)
            {
                var rect = new XRect(x + GetOffset(colWidths, i), y, colWidths[i], HeaderHeight);
                gfx.DrawRectangle(XBrushes.LightGray, rect);
                gfx.DrawString(GetShortHeader("", cols[i]) ?? cols[i].Label ?? cols[i].Name ?? string.Empty, headerFont, XBrushes.Black, rect, XStringFormats.Center);
            }
        }
    }
}
