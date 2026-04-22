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

        private record PdfColumn(string ColumnKey, string Label, bool Visible, int SortOrder);

        public static byte[] BuildExportPdf(string exportKey, IReadOnlyList<AppExportColumnDefinitionRecord> columns, IReadOnlyList<Dictionary<string, string>> rows)
        {
            PdfSharpFontResolverInitializer.EnsureInitialized();

            try { Console.WriteLine($"EXPORTDBG: PDF_BUILD start exportKey={exportKey} rows_passed={rows?.Count ?? 0}"); } catch {}
            if (rows != null && rows.Count > 0)
            {
                try { Console.WriteLine($"EXPORTDBG: PDF_BUILD sample_raw_keys={string.Join(",", rows[0].Keys)}"); } catch {}
            }

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

            // prepare effective columns
            var effectiveCols = BuildEffectiveColumns(exportKey, columns);

            // compute column widths
            var weights = effectiveCols.Select(c => ColumnWeight(c)).ToArray();
            var totalWeight = weights.Sum();
            if (totalWeight <= 0) totalWeight = 1;
            var colWidths = weights.Select(w => usableWidth * (w / totalWeight)).ToArray();

            // draw header
            DrawHeaderRow(gfx, headerFont, effectiveCols, colWidths, x, y, exportKey);
            y += HeaderHeight + 6;

            var textFormatter = new XTextFormatter(gfx);

            for (int r = 0; r < rows.Count; r++)
            {
                // track if this will cause a page break (for diagnostics)
                var willPageBreak = false;
                double rowH = 0;
                var row = rows[r];
                for (int c = 0; c < effectiveCols.Count; c++)
                {
                    var col = effectiveCols[c];
                    var value = GetPdfCellValue(exportKey, col, row);
                    if (IsBooleanLike(value))
                        value = value.Equals("Ja", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "☑" : "☐";

                    var cellWidth = colWidths[c] - 8;
                    var lines = EstimateLines(gfx, value, cellFont, cellWidth);
                    var lineHeight = gfx.MeasureString("Ag", cellFont).Height + 2;
                    rowH = Math.Max(rowH, lines * lineHeight);
                }

                if (rowH < 14) rowH = 14;

                if (y + rowH > page.Height - PageMargin)
                {
                    willPageBreak = true;
                    page = doc.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    textFormatter = new XTextFormatter(gfx);
                    x = PageMargin;
                    y = PageMargin;
                    DrawHeaderRow(gfx, headerFont, effectiveCols, colWidths, x, y, exportKey);
                    y += HeaderHeight + 6;
                }

                for (int c = 0; c < effectiveCols.Count; c++)
                {
                    var col = effectiveCols[c];
                    var value = GetPdfCellValue(exportKey, col, row);
                    if (IsBooleanLike(value))
                        value = value.Equals("Ja", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "☑" : "☐";

                    var rect = new XRect(x + GetOffset(colWidths, c) + 4, y, colWidths[c] - 8, rowH);
                    textFormatter.Alignment = XParagraphAlignment.Left;
                    textFormatter.DrawString(TruncateForCell(value, 1000), cellFont, XBrushes.Black, rect);
                }
                // diagnostics: count non-empty rendered rows and first-rendered row
                // (use Console.WriteLine guarded to avoid throwing in production)
                try
                {
                    var hasNonEmpty = row.Values.Any(v => !string.IsNullOrWhiteSpace(v));
                    if (hasNonEmpty)
                    {
                        // increment a simple counter stored in the document info (unsafe to store global) - instead log per-row sample for first few
                        if (r < 3)
                        {
                            try { Console.WriteLine($"EXPORTDBG: PDF_RENDER rowIndex={r} hasValues=true sample={string.Join(",", row.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).Take(6).Select(kv => kv.Key + "=" + (kv.Value.Length > 80 ? kv.Value.Substring(0, 80) + "..." : kv.Value)))}"); } catch {}
                        }
                    }
                    else
                    {
                        if (r < 3)
                        {
                            try { Console.WriteLine($"EXPORTDBG: PDF_RENDER rowIndex={r} hasValues=false"); } catch {}
                        }
                    }
                    if (willPageBreak)
                    {
                        try { Console.WriteLine($"EXPORTDBG: PDF_RENDER pageBreakAtRow={r}"); } catch {}
                    }
                }
                catch { }

                y += rowH + 4;
            }
            try { Console.WriteLine($"EXPORTDBG: PDF_BUILD complete rows_passed={rows?.Count ?? 0}"); } catch {}

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }

        private static List<PdfColumn> BuildEffectiveColumns(string exportKey, IReadOnlyList<AppExportColumnDefinitionRecord> columns)
        {
            var result = new List<PdfColumn>();
            var addressKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "adresse", "plz", "ort", "strasse", "hausnummer" };
            var contactKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "telefon", "handy", "mobil" };

            foreach (var c in columns)
            {
                var key = (c.ColumnKey ?? c.Name ?? string.Empty).ToLowerInvariant();
                if (string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase) && addressKeys.Contains(key))
                {
                    if (!result.Any(rc => addressKeys.Contains((rc.ColumnKey ?? string.Empty).ToLowerInvariant())))
                        result.Add(new PdfColumn("__pdf_address__", "Adresse", true, c.Sortierung));
                    continue;
                }

                if (string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase) && contactKeys.Contains(key))
                {
                    if (!result.Any(rc => contactKeys.Contains((rc.ColumnKey ?? string.Empty).ToLowerInvariant()) || (rc.ColumnKey ?? string.Empty) == "__pdf_contact__"))
                        result.Add(new PdfColumn("__pdf_contact__", "Kontakt", true, c.Sortierung));
                    continue;
                }

                result.Add(new PdfColumn(c.ColumnKey ?? c.Name ?? string.Empty, c.LabelLang ?? c.LabelKurz ?? c.ColumnKey ?? string.Empty, c.StandardSichtbar, c.Sortierung));
            }

            return result.OrderBy(c => c.SortOrder).ToList();
        }

        private static double ColumnWeight(PdfColumn col)
        {
            var cn = (col.ColumnKey ?? col.Label ?? string.Empty).ToLowerInvariant();
            if (cn.StartsWith("nr", StringComparison.OrdinalIgnoreCase) || cn == "__pdf_address__" || cn == "__pdf_contact__")
                return 2;
            if (IsShortFieldName(cn) || cn == "aktiv" || cn == "wa" || cn == "re" || cn == "info" || cn == "app")
                return 1;
            return 3;
        }

        private static string TruncateForCell(string value, int max = 200)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= max) return value;
            return value.Substring(0, max - 3) + "...";
        }

        private static string GetPdfCellValue(string exportKey, PdfColumn col, Dictionary<string, string> row)
        {
            var key = (col.ColumnKey ?? string.Empty).ToLowerInvariant();
            if (key == "__pdf_address__")
            {
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

            row.TryGetValue(col.ColumnKey ?? string.Empty, out var val);
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

        private static string GetShortHeader(string exportKey, PdfColumn col)
        {
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

                if (!string.IsNullOrWhiteSpace(col.ColumnKey) && map.TryGetValue(col.ColumnKey, out var s))
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

                if (!string.IsNullOrWhiteSpace(col.ColumnKey) && map.TryGetValue(col.ColumnKey, out var s))
                    return s;
                if (!string.IsNullOrWhiteSpace(col.Label) && map.TryGetValue(col.Label, out var s2))
                    return s2;
            }

            return col.Label ?? col.ColumnKey ?? string.Empty;
        }

        private static void DrawHeaderRow(XGraphics gfx, XFont headerFont, List<PdfColumn> cols, double[] colWidths, double x, double y, string exportKey)
        {
            for (int i = 0; i < cols.Count; i++)
            {
                var rect = new XRect(x + GetOffset(colWidths, i), y, colWidths[i], HeaderHeight);
                gfx.DrawRectangle(XBrushes.LightGray, rect);
                gfx.DrawString(GetShortHeader(exportKey, cols[i]) ?? cols[i].Label ?? cols[i].ColumnKey ?? string.Empty, headerFont, XBrushes.Black, rect, XStringFormats.Center);
            }
        }
    }
}
