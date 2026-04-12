using System;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace KGV.Core.Utilities
{
    public static class SignedVertragsdokumentPdfBuilder
    {
        public static byte[] Build(MitgliedRecord member, DocumentInfo sourceDocument, byte[] originalPdfContent, DigitalSignatureCapture signatureCapture)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (sourceDocument == null)
                throw new ArgumentNullException(nameof(sourceDocument));
            if ((originalPdfContent?.Length ?? 0) <= 0)
                throw new InvalidOperationException("Die unsignierte Dokumentfassung konnte nicht geladen werden.");
            if (signatureCapture == null || !signatureCapture.HasContent)
                throw new InvalidOperationException("Es liegt keine digitale Signatur zum Übernehmen vor.");

            PdfSharpFontResolverInitializer.EnsureInitialized();

            using var inputStream = new MemoryStream(originalPdfContent, writable: false);
            var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
            var outputDocument = new PdfDocument();

            foreach (var page in inputDocument.Pages)
                outputDocument.AddPage(page);

            var signaturePage = outputDocument.AddPage();
            signaturePage.Width = XUnit.FromMillimeter(210);
            signaturePage.Height = XUnit.FromMillimeter(297);
            outputDocument.Info.Title = sourceDocument.Title;
            outputDocument.Info.Subject = "Digitale Signatur";
            outputDocument.Info.Author = VereinsdokumentBranding.VereinsName;
            outputDocument.Info.Creator = VereinsdokumentBranding.VereinsName;

            using var graphics = XGraphics.FromPdfPage(signaturePage);
            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var subtitleFont = new XFont("Arial", 11, XFontStyle.Regular);
            var bodyFont = new XFont("Arial", 10.5, XFontStyle.Regular);
            var labelFont = new XFont("Arial", 10, XFontStyle.Bold);
            var borderPen = new XPen(XColor.FromArgb(208, 214, 224), 0.8);
            var signaturePen = new XPen(XColor.FromArgb(33, 33, 33), 2.2);
            const double pageMargin = 42;

            double cursorY = pageMargin;
            graphics.DrawString("Digitale Signatur", titleFont, XBrushes.Black,
                new XRect(pageMargin, cursorY, signaturePage.Width - pageMargin * 2, 24), XStringFormats.TopLeft);
            cursorY += 30;

            graphics.DrawString(
                "Diese Seite ergänzt die bestehende unsignierte Dokumentfassung. Das Original bleibt unverändert im Dokumentpfad erhalten.",
                subtitleFont,
                XBrushes.DimGray,
                new XRect(pageMargin, cursorY, signaturePage.Width - pageMargin * 2, 32),
                XStringFormats.TopLeft);
            cursorY += 40;

            var signerName = string.Join(" ", new[] { member.Vorname, member.Name }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            DrawMetaLine(graphics, labelFont, bodyFont, pageMargin, ref cursorY, signaturePage.Width, "Dokument", sourceDocument.FormularDokumentTypAnzeige);
            DrawMetaLine(graphics, labelFont, bodyFont, pageMargin, ref cursorY, signaturePage.Width, "Status", FormularDokumentStatus.ToDisplayName(FormularDokumentStatus.Signiert));
            DrawMetaLine(graphics, labelFont, bodyFont, pageMargin, ref cursorY, signaturePage.Width, "Mitglied", string.IsNullOrWhiteSpace(signerName) ? $"Mitglied #{member.Id}" : signerName);
            DrawMetaLine(graphics, labelFont, bodyFont, pageMargin, ref cursorY, signaturePage.Width, "Quelle", string.IsNullOrWhiteSpace(sourceDocument.Dateiname) ? sourceDocument.Title : sourceDocument.Dateiname);
            DrawMetaLine(graphics, labelFont, bodyFont, pageMargin, ref cursorY, signaturePage.Width, "Signiert am", signatureCapture.SignedAt.ToString("dd.MM.yyyy HH:mm"));
            cursorY += 12;

            var signatureRect = new XRect(pageMargin, cursorY, signaturePage.Width - pageMargin * 2, 240);
            graphics.DrawRectangle(XBrushes.WhiteSmoke, signatureRect);
            graphics.DrawRectangle(borderPen, signatureRect);
            graphics.DrawString("Erfasste Unterschrift", labelFont, XBrushes.Black,
                new XRect(signatureRect.X + 12, signatureRect.Y + 10, signatureRect.Width - 24, 16), XStringFormats.TopLeft);

            var drawingRect = new XRect(signatureRect.X + 12, signatureRect.Y + 34, signatureRect.Width - 24, signatureRect.Height - 46);
            graphics.DrawRectangle(XBrushes.White, drawingRect);
            graphics.DrawRectangle(borderPen, drawingRect);
            DrawSignature(graphics, drawingRect, signatureCapture, signaturePen);

            using var outputStream = new MemoryStream();
            outputDocument.Save(outputStream, false);
            return outputStream.ToArray();
        }

        private static void DrawMetaLine(XGraphics graphics, XFont labelFont, XFont bodyFont, double margin, ref double cursorY, double pageWidth, string label, string? value)
        {
            graphics.DrawString($"{label}:", labelFont, XBrushes.Black,
                new XRect(margin, cursorY, 120, 16), XStringFormats.TopLeft);
            graphics.DrawString(string.IsNullOrWhiteSpace(value) ? "-" : value.Trim(), bodyFont, XBrushes.Black,
                new XRect(margin + 120, cursorY, pageWidth - margin * 2 - 120, 16), XStringFormats.TopLeft);
            cursorY += 18;
        }

        private static void DrawSignature(XGraphics graphics, XRect targetRect, DigitalSignatureCapture signatureCapture, XPen signaturePen)
        {
            var sourceWidth = Math.Max(1d, signatureCapture.CanvasWidth);
            var sourceHeight = Math.Max(1d, signatureCapture.CanvasHeight);
            var scale = Math.Min(targetRect.Width / sourceWidth, targetRect.Height / sourceHeight);
            var offsetX = targetRect.X + (targetRect.Width - sourceWidth * scale) / 2d;
            var offsetY = targetRect.Y + (targetRect.Height - sourceHeight * scale) / 2d;

            foreach (var stroke in signatureCapture.Strokes.Where(x => x?.Points?.Count > 0))
            {
                var points = stroke.Points
                    .Select(point => new XPoint(offsetX + point.X * scale, offsetY + point.Y * scale))
                    .ToArray();

                if (points.Length == 1)
                {
                    graphics.DrawEllipse(XBrushes.Black, points[0].X, points[0].Y, 2, 2);
                    continue;
                }

                for (var i = 1; i < points.Length; i++)
                    graphics.DrawLine(signaturePen, points[i - 1], points[i]);
            }
        }
    }
}