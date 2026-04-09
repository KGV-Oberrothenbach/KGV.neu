using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities
{
    public static class VereinsdokumentPdfBuilder
    {
        private const double PageMargin = 42;
        private const double SectionSpacing = 12;
        private const double HeaderLogoSize = 74;

        public static byte[] BuildDocument(
            string dokumentTitel,
            string formularTitel,
            string dokumentStatusAnzeige,
            DateTime ausstellungsdatum,
            IReadOnlyCollection<VereinsdokumentAbschnitt> abschnitte,
            IReadOnlyCollection<string> unterschriftFelder,
            string? introText = null)
        {
            var document = new PdfDocument();
            var effectiveFormularTitel = formularTitel ?? string.Empty;

            document.Info.Title = dokumentTitel ?? effectiveFormularTitel;
            document.Info.Subject = effectiveFormularTitel;
            document.Info.Author = VereinsdokumentBranding.VereinsName;
            document.Info.Creator = VereinsdokumentBranding.VereinsName;

            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var subtitleFont = new XFont("Arial", 11, XFontStyle.Regular);
            var sectionTitleFont = new XFont("Arial", 12, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 10.5, XFontStyle.Regular);
            var labelFont = new XFont("Arial", 10, XFontStyle.Bold);
            var smallFont = new XFont("Arial", 9, XFontStyle.Regular);
            var borderPen = new XPen(XColor.FromArgb(208, 214, 224), 0.8);
            var accentPen = new XPen(XColor.FromArgb(46, 125, 50), 1.6);

            using var graphics = XGraphics.FromPdfPage(page);
            double cursorY = PageMargin;

            DrawHeader(graphics, page, titleFont, subtitleFont, accentPen, ref cursorY);
            DrawDocumentMeta(graphics, page, titleFont, subtitleFont, bodyFont, effectiveFormularTitel, dokumentStatusAnzeige, ausstellungsdatum, introText, ref cursorY);

            var contentSections = abschnitte?
                .Where(x => x != null && x.HasContent)
                .ToList()
                ?? new List<VereinsdokumentAbschnitt>();

            foreach (var abschnitt in contentSections)
            {
                DrawSection(graphics, page, sectionTitleFont, bodyFont, labelFont, borderPen, abschnitt, ref cursorY);
            }

            DrawSignatureArea(graphics, page, labelFont, smallFont, accentPen, unterschriftFelder, ref cursorY);

            using var stream = new MemoryStream();
            document.Save(stream, false);
            return stream.ToArray();
        }

        private static void DrawHeader(
            XGraphics graphics,
            PdfPage page,
            XFont titleFont,
            XFont subtitleFont,
            XPen accentPen,
            ref double cursorY)
        {
            using var logo = XImage.FromStream(() => new MemoryStream(VereinsdokumentBranding.GetLogoBytes(), writable: false));
            graphics.DrawImage(logo, PageMargin, cursorY, HeaderLogoSize, HeaderLogoSize);

            var textX = PageMargin + HeaderLogoSize + 16;
            var textWidth = page.Width - textX - PageMargin;
            graphics.DrawString(VereinsdokumentBranding.VereinsName, titleFont, XBrushes.Black,
                new XRect(textX, cursorY + 4, textWidth, 24), XStringFormats.TopLeft);
            graphics.DrawString(VereinsdokumentBranding.VereinsRegister, subtitleFont, XBrushes.DimGray,
                new XRect(textX, cursorY + 30, textWidth, 18), XStringFormats.TopLeft);
            graphics.DrawString($"E-Mail: {VereinsdokumentBranding.VereinsEmail}", subtitleFont, XBrushes.DimGray,
                new XRect(textX, cursorY + 47, textWidth, 18), XStringFormats.TopLeft);

            cursorY += HeaderLogoSize + 10;
            graphics.DrawLine(accentPen, PageMargin, cursorY, page.Width - PageMargin, cursorY);
            cursorY += 18;
        }

        private static void DrawDocumentMeta(
            XGraphics graphics,
            PdfPage page,
            XFont titleFont,
            XFont subtitleFont,
            XFont bodyFont,
            string formularTitel,
            string dokumentStatusAnzeige,
            DateTime ausstellungsdatum,
            string? introText,
            ref double cursorY)
        {
            graphics.DrawString(formularTitel, titleFont, XBrushes.Black,
                new XRect(PageMargin, cursorY, page.Width - PageMargin * 2, 24), XStringFormats.TopLeft);
            cursorY += 28;

            graphics.DrawString(
                $"Ausstellungsdatum: {ausstellungsdatum:dd.MM.yyyy}    Dokumentstatus: {dokumentStatusAnzeige}",
                subtitleFont,
                XBrushes.DimGray,
                new XRect(PageMargin, cursorY, page.Width - PageMargin * 2, 18),
                XStringFormats.TopLeft);
            cursorY += 26;

            var intro = string.IsNullOrWhiteSpace(introText)
                ? "Dieses Vereinsdokument wird in einer standardisierten Vereinsvorlage dokumentiert."
                : introText.Trim();
            graphics.DrawString(intro, bodyFont, XBrushes.Black,
                new XRect(PageMargin, cursorY, page.Width - PageMargin * 2, 32), XStringFormats.TopLeft);
            cursorY += 36;
        }

        private static void DrawSection(
            XGraphics graphics,
            PdfPage page,
            XFont sectionTitleFont,
            XFont bodyFont,
            XFont labelFont,
            XPen borderPen,
            VereinsdokumentAbschnitt abschnitt,
            ref double cursorY)
        {
            var height = EstimateSectionHeight(graphics, sectionTitleFont, bodyFont, abschnitt);
            var sectionRect = new XRect(PageMargin, cursorY, page.Width - PageMargin * 2, height);
            graphics.DrawRectangle(XBrushes.WhiteSmoke, sectionRect);
            graphics.DrawRectangle(borderPen, sectionRect);

            var innerX = sectionRect.X + 12;
            var innerY = sectionRect.Y + 10;
            graphics.DrawString(abschnitt.Ueberschrift, sectionTitleFont, XBrushes.Black,
                new XRect(innerX, innerY, sectionRect.Width - 24, 18), XStringFormats.TopLeft);
            innerY += 24;

            foreach (var zeile in abschnitt.Zeilen)
            {
                var separatorIndex = zeile.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var label = zeile[..(separatorIndex + 1)];
                    var value = zeile[(separatorIndex + 1)..].TrimStart();
                    var labelWidth = Math.Min(130, graphics.MeasureString(label, labelFont).Width + 8);
                    graphics.DrawString(label, labelFont, XBrushes.Black,
                        new XRect(innerX, innerY, labelWidth, 16), XStringFormats.TopLeft);
                    graphics.DrawString(value, bodyFont, XBrushes.Black,
                        new XRect(innerX + labelWidth, innerY, sectionRect.Width - 24 - labelWidth, 16), XStringFormats.TopLeft);
                }
                else
                {
                    graphics.DrawString(zeile, bodyFont, XBrushes.Black,
                        new XRect(innerX, innerY, sectionRect.Width - 24, 16), XStringFormats.TopLeft);
                }

                innerY += 16;
            }

            cursorY += height + SectionSpacing;
        }

        private static void DrawSignatureArea(
            XGraphics graphics,
            PdfPage page,
            XFont labelFont,
            XFont smallFont,
            XPen accentPen,
            IReadOnlyCollection<string> unterschriftFelder,
            ref double cursorY)
        {
            var felder = unterschriftFelder?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()
                ?? new List<string>();

            if (felder.Count == 0)
                return;

            if (cursorY < page.Height - 160)
                cursorY = page.Height - 150;

            graphics.DrawString("Unterschriften", labelFont, XBrushes.Black,
                new XRect(PageMargin, cursorY, page.Width - PageMargin * 2, 16), XStringFormats.TopLeft);
            cursorY += 26;

            var spacing = 18d;
            var lineWidth = (page.Width - PageMargin * 2 - spacing) / Math.Max(1, felder.Count);
            for (var i = 0; i < felder.Count; i++)
            {
                var x = PageMargin + i * (lineWidth + spacing);
                graphics.DrawLine(accentPen, x, cursorY + 24, x + lineWidth, cursorY + 24);
                graphics.DrawString(felder[i], smallFont, XBrushes.DimGray,
                    new XRect(x, cursorY + 28, lineWidth, 14), XStringFormats.TopLeft);
            }
        }

        private static double EstimateSectionHeight(XGraphics graphics, XFont sectionTitleFont, XFont bodyFont, VereinsdokumentAbschnitt abschnitt)
        {
            var titleHeight = graphics.MeasureString(abschnitt.Ueberschrift, sectionTitleFont).Height;
            var lineHeight = graphics.MeasureString("Ag", bodyFont).Height + 3;
            return Math.Max(56, 12 + titleHeight + 8 + abschnitt.Zeilen.Count * lineHeight + 10);
        }
    }
}
