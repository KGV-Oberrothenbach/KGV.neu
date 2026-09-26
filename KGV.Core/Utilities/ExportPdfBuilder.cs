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

            if (string.Equals(exportKey, "arbeitsstunden_uebersicht", StringComparison.OrdinalIgnoreCase))
                return BuildArbeitsstundenPdf(rows);

            try { Console.WriteLine($"EXPORTDBG: PDF_BUILD start exportKey={exportKey} rows_passed={rows?.Count ?? 0}"); } catch {}
            if (rows != null && rows.Count > 0)
            {
                try { Console.WriteLine($"EXPORTDBG: PDF_BUILD sample_raw_keys={string.Join(",", rows[0].Keys)}"); } catch {}
            }

            var doc = new PdfDocument();
            doc.Info.Title = exportKey ?? "export";
            var isMitgliederliste = string.Equals(exportKey, "mitgliederliste", StringComparison.OrdinalIgnoreCase);

            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            page.Orientation = PdfSharpCore.PageOrientation.Landscape;

            var gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Arial", 15, XFontStyle.Bold);
            var subtitleFont = new XFont("Arial", 8, XFontStyle.Regular);
            var headerFont = new XFont("Arial", isMitgliederliste ? 8 : 12, XFontStyle.Bold);
            var cellFont = new XFont("Arial", isMitgliederliste ? 7.5 : 9, XFontStyle.Regular);

            double usableWidth = page.Width - PageMargin * 2;
            double x = PageMargin;
            double y = PageMargin;

            void DrawMitgliederlistenKopf()
            {
                if (!isMitgliederliste)
                    return;

                gfx.DrawString("Mitgliederliste", titleFont, XBrushes.DarkSlateGray,
                    new XRect(PageMargin, y, usableWidth, 20), XStringFormats.TopLeft);
                y += 20;
                gfx.DrawString($"Erstellt am {DateTime.Now:dd.MM.yyyy HH:mm}", subtitleFont, XBrushes.DimGray,
                    new XRect(PageMargin, y, usableWidth, 12), XStringFormats.TopLeft);
                y += 18;
            }

            // prepare effective columns
            var effectiveCols = BuildEffectiveColumns(exportKey, columns);

            // compute column widths
            var weights = effectiveCols.Select(c => ColumnWeight(c)).ToArray();
            var totalWeight = weights.Sum();
            if (totalWeight <= 0) totalWeight = 1;
            var colWidths = weights.Select(w => usableWidth * (w / totalWeight)).ToArray();

            DrawMitgliederlistenKopf();
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
                    DrawMitgliederlistenKopf();
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

        private static byte[] BuildArbeitsstundenPdf(IReadOnlyList<Dictionary<string, string>> rows)
        {
            var doc = new PdfDocument();
            doc.Info.Title = "Arbeitsstundenübersicht";

            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var subtitleFont = new XFont("Arial", 9, XFontStyle.Regular);
            var groupFont = new XFont("Arial", 12, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 8, XFontStyle.Bold);
            var cellFont = new XFont("Arial", 8, XFontStyle.Regular);
            const double margin = 32;
            const double rowHeight = 17;
            var columnWidths = new[] { 52d, 145d, 42d, 55d, 42d, 178d };
            var season = rows.Select(r => GetRowValue(r, "jahr")).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "-";
            var pageNumber = 0;
            PdfPage page = null!;
            XGraphics gfx = null!;
            double y = 0;

            void StartPage()
            {
                pageNumber++;
                page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
                gfx.DrawString("Arbeitsstundenübersicht", titleFont, XBrushes.DarkSlateGray, new XRect(margin, y, page.Width - margin * 2, 22), XStringFormats.TopLeft);
                y += 24;
                gfx.DrawString($"Saison {season}   |   Erstellt am {DateTime.Now:dd.MM.yyyy HH:mm}   |   Seite {pageNumber}", subtitleFont, XBrushes.DimGray, new XRect(margin, y, page.Width - margin * 2, 14), XStringFormats.TopLeft);
                y += 24;
            }

            void DrawTableHeader()
            {
                var headers = new[] { "Garten", "Mitglied", "Soll", "Geleistet", "Offen", "Wartungsvertrag" };
                var x = margin;
                for (var index = 0; index < headers.Length; index++)
                {
                    var rect = new XRect(x, y, columnWidths[index], rowHeight);
                    gfx.DrawRectangle(XBrushes.LightGray, rect);
                    gfx.DrawString(headers[index], headerFont, XBrushes.Black, rect, XStringFormats.Center);
                    x += columnWidths[index];
                }
                y += rowHeight;
            }

            void DrawGroup(string title, IEnumerable<Dictionary<string, string>> groupRows)
            {
                var orderedRows = groupRows
                    .OrderBy(r => GetGartenSortNumber(GetRowValue(r, "garten_nr")))
                    .ThenBy(r => GetRowValue(r, "garten_nr"), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => GetRowValue(r, "nachname"), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => GetRowValue(r, "vorname"), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (y + 44 > page.Height - margin)
                    StartPage();

                gfx.DrawString($"{title} ({orderedRows.Count})", groupFont, XBrushes.DarkSlateGray, new XRect(margin, y, page.Width - margin * 2, 18), XStringFormats.TopLeft);
                y += 20;
                DrawTableHeader();

                if (orderedRows.Count == 0)
                {
                    gfx.DrawString("Keine Einträge", cellFont, XBrushes.Gray, new XRect(margin + 4, y, page.Width - margin * 2, rowHeight), XStringFormats.TopLeft);
                    y += rowHeight + 10;
                    return;
                }

                foreach (var row in orderedRows)
                {
                    if (y + rowHeight > page.Height - margin)
                    {
                        StartPage();
                        gfx.DrawString(title, groupFont, XBrushes.DarkSlateGray, new XRect(margin, y, page.Width - margin * 2, 18), XStringFormats.TopLeft);
                        y += 20;
                        DrawTableHeader();
                    }

                    var values = new[]
                    {
                        GetRowValue(row, "garten_nr"),
                        string.Join(" ", new[] { GetRowValue(row, "nachname"), GetRowValue(row, "vorname") }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        GetRowValue(row, "pflichtstunden_soll"),
                        GetRowValue(row, "geleistete_stunden"),
                        GetRowValue(row, "offene_stunden"),
                        GetRowValue(row, "wartungsvertraege")
                    };

                    var x = margin;
                    for (var index = 0; index < values.Length; index++)
                    {
                        var rect = new XRect(x, y, columnWidths[index], rowHeight);
                        gfx.DrawRectangle(XPens.LightGray, rect);
                        gfx.DrawString(TruncateForCell(values[index], 60), cellFont, XBrushes.Black, new XRect(x + 3, y + 2, columnWidths[index] - 6, rowHeight - 3), XStringFormats.TopLeft);
                        x += columnWidths[index];
                    }
                    y += rowHeight;
                }

                y += 12;
            }

            // Detailzeilen werden nur für die tabellarische Ansicht angefordert. Im
            // Übersichts-PDF steht jedes Hauptmitglied dagegen genau einmal.
            var summaryRows = rows.Where(r =>
                string.IsNullOrWhiteSpace(GetRowValue(r, "zeilentyp"))
                || string.Equals(GetRowValue(r, "zeilentyp"), "Zusammenfassung", StringComparison.OrdinalIgnoreCase))
                .ToList();

            StartPage();
            DrawGroup("Stunden offen", summaryRows.Where(r => string.Equals(GetRowValue(r, "status"), "Stunden offen", StringComparison.OrdinalIgnoreCase)));
            DrawGroup("Stunden fertig", summaryRows.Where(r => string.Equals(GetRowValue(r, "status"), "Stunden fertig", StringComparison.OrdinalIgnoreCase)));
            DrawGroup("Wartungsverträge", summaryRows.Where(r => GetRowValue(r, "status").StartsWith("Wartungsvertrag", StringComparison.OrdinalIgnoreCase)));
            DrawGroup("Aufgrund Altersregelung befreit", summaryRows.Where(r => string.Equals(GetRowValue(r, "regelgrund"), "altersbefreiung", StringComparison.OrdinalIgnoreCase)));

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }

        private static string GetRowValue(Dictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }

        private static int GetGartenSortNumber(string gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.Trim().TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }

        private static List<PdfColumn> BuildEffectiveColumns(string exportKey, IReadOnlyList<AppExportColumnDefinitionRecord> columns)
        {
            var result = new List<PdfColumn>();
            var addressKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "adresse", "strasse_hsnr", "plz", "ort", "strasse", "hausnummer" };
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
                row.TryGetValue("strasse_hsnr", out var adr);
                if (string.IsNullOrWhiteSpace(adr))
                    row.TryGetValue("adresse", out adr);
                row.TryGetValue("plz", out var plz);
                row.TryGetValue("ort", out var ort);
                var line1 = adr ?? string.Empty;
                var line2 = string.Join(" ", new[] { plz, ort }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                return string.IsNullOrWhiteSpace(line2) ? line1 : line1 + "\n" + line2;
            }

            if (key == "__pdf_contact__")
            {
                row.TryGetValue("telefon", out var tel);
                row.TryGetValue("mobil", out var mob);
                if (string.IsNullOrWhiteSpace(mob))
                    row.TryGetValue("handy", out mob);
                var line1 = tel ?? string.Empty;
                var line2 = mob ?? string.Empty;
                return string.IsNullOrWhiteSpace(line2) ? line1 : line1 + "\n" + line2;
            }

            // Try multiple lookup strategies to be robust against differing keys used in remapped rows:
            // 1. direct ColumnKey
            if (!string.IsNullOrWhiteSpace(col.ColumnKey) && row.TryGetValue(col.ColumnKey, out var val1) && !string.IsNullOrWhiteSpace(val1))
                return val1;

            // 2. direct label
            if (!string.IsNullOrWhiteSpace(col.Label) && row.TryGetValue(col.Label, out var val2) && !string.IsNullOrWhiteSpace(val2))
                return val2;

            // 3. try normalized key match against any row key
            var targets = new List<string>();
            if (!string.IsNullOrWhiteSpace(col.ColumnKey)) targets.Add(col.ColumnKey);
            if (!string.IsNullOrWhiteSpace(col.Label)) targets.Add(col.Label);

            var normTargets = targets
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => NormalizeKey(t!))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (normTargets.Count > 0)
            {
                foreach (var kv in row)
                {
                    var k = kv.Key ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    var nk = NormalizeKey(k);
                    if (normTargets.Contains(nk) && !string.IsNullOrWhiteSpace(kv.Value))
                        return kv.Value;
                }
            }

            // 4. fallback to any canonical/common keys used in member export (helpful for mitgliederliste)
            var commonAliases = new[] { "mitgliedsnummer", "mitgliedsnr", "id", "name", "vorname", "email", "adresse" };
            foreach (var alias in commonAliases)
            {
                if (row.TryGetValue(alias, out var av) && !string.IsNullOrWhiteSpace(av))
                    return av;
            }

            // nothing found -> return empty string
            return string.Empty;
        }

        private static string NormalizeKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lowered = input.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder();

            foreach (var ch in lowered)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
            }

            return sb.ToString();
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

            if (string.Equals(exportKey, "jahresablesung_status", StringComparison.OrdinalIgnoreCase))
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "jahr", "Saison" },
                    { "parzelle", "Garten Nr." },
                    { "medium", "Medium" },
                    { "zaehlernummer", "Zähler" },
                    { "status", "JEA-Status" },
                    { "jae_ablesedatum", "JEA-Datum" },
                    { "jae_stand", "JEA-Stand" },
                    { "pruefstatus", "Prüfung" },
                    { "pachtbezug", "Pächter" },
                    { "pachtwechsel_im_jahr", "Wechsel" }
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
