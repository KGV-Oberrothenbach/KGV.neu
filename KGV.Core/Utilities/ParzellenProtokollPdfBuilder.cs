using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities;

public static class ParzellenProtokollPdfBuilder
{
    private const double Margin = 42;

    public static byte[] Build(ParzellenProtokollPdfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        PdfSharpFontResolverInitializer.EnsureInitialized();
        var document = new PdfDocument();
        document.Info.Title = request.FormularTitel;
        document.Info.Author = VereinsdokumentBranding.VereinsName;
        document.Info.Creator = VereinsdokumentBranding.VereinsName;

        var page = AddPage(document);
        using (var gfx = XGraphics.FromPdfPage(page))
        {
            var y = DrawHeader(gfx, page);
            var title = new XFont("Arial", 17, XFontStyle.Bold);
            var label = new XFont("Arial", 9.5, XFontStyle.Bold);
            var body = new XFont("Arial", 9.5, XFontStyle.Regular);
            gfx.DrawString(request.FormularTitel, title, XBrushes.Black, new XRect(Margin, y, page.Width - Margin * 2, 24), XStringFormats.TopLeft);
            y += 30;
            DrawLines(gfx, page, label, body, ref y, "Protokolldatum", request.ProtokollDatum.ToString("dd.MM.yyyy"), "Mitglied", FullName(request.Mitglied), "Garten Nr.", request.Parzelle.DisplayName);
            DrawBox(gfx, page, label, body, ref y, "Teilnehmende", new[]
            {
                $"Vorstand 1: {FullName(request.Vorstand1)}",
                $"Vorstand 2: {FullName(request.Vorstand2)}",
                $"Begleitperson: {TextOrDash(request.Begleitperson)}"
            });
            if (request.Ablesungen.Count > 0)
                DrawBox(gfx, page, label, body, ref y, "Zählerstände", request.Ablesungen.Select(x =>
                    $"{UpperFirst(x.Medium)}: {x.Stand:0.##}  · Zähler {TextOrDash(x.Zaehlernummer)}  · {x.Ablesedatum:dd.MM.yyyy}  · {(x.Quelle == "neu_abgelesen" ? "neu abgelesen" : "letzte Ablesung übernommen")}"));
            DrawBox(gfx, page, label, body, ref y, "Anlass", new[] { TextOrDash(request.Anlass) });
            DrawBox(gfx, page, label, body, ref y, "Zustand / Feststellungen", SplitText(request.ZustandBemerkung));
            DrawBox(gfx, page, label, body, ref y, "Vereinbarungen / Fristen", SplitText(request.Vereinbarung));
            DrawSignatures(gfx, page, label, body, ref y, request);
        }

        foreach (var photo in request.Fotos.Where(x => x?.Inhalt?.Length > 0).Take(10))
            DrawPhotoPage(document, photo);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static PdfPage AddPage(PdfDocument document)
    {
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        return page;
    }

    private static double DrawHeader(XGraphics gfx, PdfPage page)
    {
        using var logo = XImage.FromStream(() => new MemoryStream(VereinsdokumentBranding.GetLogoBytes(), false));
        gfx.DrawImage(logo, Margin, Margin, 58, 58);
        var title = new XFont("Arial", 14, XFontStyle.Bold);
        var small = new XFont("Arial", 9, XFontStyle.Regular);
        gfx.DrawString(VereinsdokumentBranding.VereinsName, title, XBrushes.Black, new XRect(Margin + 70, Margin + 3, page.Width - Margin * 2 - 70, 20), XStringFormats.TopLeft);
        gfx.DrawString(VereinsdokumentBranding.VereinsRegister, small, XBrushes.DimGray, new XRect(Margin + 70, Margin + 25, page.Width - Margin * 2 - 70, 14), XStringFormats.TopLeft);
        gfx.DrawString(VereinsdokumentBranding.VereinsEmail, small, XBrushes.DimGray, new XRect(Margin + 70, Margin + 40, page.Width - Margin * 2 - 70, 14), XStringFormats.TopLeft);
        gfx.DrawLine(new XPen(XColor.FromArgb(46, 125, 50), 1.4), Margin, Margin + 68, page.Width - Margin, Margin + 68);
        return Margin + 82;
    }

    private static void DrawLines(XGraphics gfx, PdfPage page, XFont label, XFont body, ref double y, params string[] values)
    {
        for (var i = 0; i < values.Length; i += 2)
        {
            gfx.DrawString(values[i] + ":", label, XBrushes.Black, new XRect(Margin, y, 95, 15), XStringFormats.TopLeft);
            gfx.DrawString(values[i + 1], body, XBrushes.Black, new XRect(Margin + 98, y, page.Width - Margin * 2 - 98, 15), XStringFormats.TopLeft);
            y += 17;
        }
        y += 5;
    }

    private static void DrawBox(XGraphics gfx, PdfPage page, XFont label, XFont body, ref double y, string heading, IEnumerable<string> lines)
    {
        var values = lines.SelectMany(SplitText).ToList();
        if (values.Count == 0) values.Add("-");
        var height = 31 + values.Count * 13;
        gfx.DrawRectangle(XBrushes.WhiteSmoke, Margin, y, page.Width - Margin * 2, height);
        gfx.DrawRectangle(new XPen(XColor.FromArgb(208, 214, 224), .8), Margin, y, page.Width - Margin * 2, height);
        gfx.DrawString(heading, label, XBrushes.Black, new XRect(Margin + 10, y + 8, page.Width - Margin * 2 - 20, 14), XStringFormats.TopLeft);
        var lineY = y + 23;
        foreach (var line in values)
        {
            gfx.DrawString(line, body, XBrushes.Black, new XRect(Margin + 10, lineY, page.Width - Margin * 2 - 20, 13), XStringFormats.TopLeft);
            lineY += 13;
        }
        y += height + 9;
    }

    private static void DrawSignatures(XGraphics gfx, PdfPage page, XFont label, XFont body, ref double y, ParzellenProtokollPdfRequest request)
    {
        if (y > page.Height - 210) return;
        gfx.DrawString("Unterschriften", label, XBrushes.Black, new XRect(Margin, y, page.Width - Margin * 2, 16), XStringFormats.TopLeft);
        y += 20;
        var signatures = new (string Label, DigitalSignatureCapture? Capture)[]
        {
            ("Pächter/in" + (request.FormularTitel.Contains("Begehung") ? " (Kenntnisnahme)" : ""), request.PaechterSignatur),
            ("Begleitperson", request.BegleitpersonSignatur),
            ($"Vorstand 1 – {FullName(request.Vorstand1)}", request.Vorstand1Signatur),
            ($"Vorstand 2 – {FullName(request.Vorstand2)}", request.Vorstand2Signatur)
        }.Where(x => x.Capture?.HasContent == true).ToList();
        if (signatures.Count == 0) { gfx.DrawString("Noch nicht unterschrieben.", body, XBrushes.DimGray, new XRect(Margin, y, 200, 15), XStringFormats.TopLeft); return; }
        var width = (page.Width - Margin * 2 - 12) / 2;
        for (var i = 0; i < signatures.Count; i++)
        {
            var x = Margin + (i % 2) * (width + 12);
            var rowY = y + (i / 2) * 75;
            gfx.DrawRectangle(new XPen(XColor.FromArgb(208, 214, 224), .8), x, rowY, width, 64);
            gfx.DrawString(signatures[i].Label, label, XBrushes.Black, new XRect(x + 6, rowY + 5, width - 12, 12), XStringFormats.TopLeft);
            DrawSignature(gfx, new XRect(x + 6, rowY + 19, width - 12, 37), signatures[i].Capture!);
        }
    }

    private static void DrawPhotoPage(PdfDocument document, ParzellenProtokollFoto photo)
    {
        var page = AddPage(document);
        using var gfx = XGraphics.FromPdfPage(page);
        var y = DrawHeader(gfx, page);
        gfx.DrawString("Fotoanlage", new XFont("Arial", 16, XFontStyle.Bold), XBrushes.Black, new XRect(Margin, y, page.Width - Margin * 2, 20), XStringFormats.TopLeft);
        gfx.DrawString(TextOrDash(photo.Dateiname), new XFont("Arial", 9, XFontStyle.Regular), XBrushes.DimGray, new XRect(Margin, y + 24, page.Width - Margin * 2, 14), XStringFormats.TopLeft);
        try
        {
            using var image = XImage.FromStream(() => new MemoryStream(photo.Inhalt, false));
            var scale = Math.Min((page.Width - Margin * 2) / image.PixelWidth, (page.Height - y - 70) / image.PixelHeight);
            var width = image.PixelWidth * scale;
            var height = image.PixelHeight * scale;
            gfx.DrawImage(image, Margin + (page.Width - Margin * 2 - width) / 2, y + 45, width, height);
        }
        catch
        {
            gfx.DrawString("Das Foto konnte nicht in die PDF eingebettet werden.", new XFont("Arial", 10, XFontStyle.Regular), XBrushes.DarkRed, new XRect(Margin, y + 50, page.Width - Margin * 2, 16), XStringFormats.TopLeft);
        }
    }

    private static void DrawSignature(XGraphics gfx, XRect target, DigitalSignatureCapture signature)
    {
        var sx = target.Width / Math.Max(1, signature.CanvasWidth);
        var sy = target.Height / Math.Max(1, signature.CanvasHeight);
        var scale = Math.Min(sx, sy);
        foreach (var stroke in signature.Strokes.Where(x => x?.Points?.Count > 0))
        {
            var points = stroke.Points.Select(p => new XPoint(target.X + (target.Width - signature.CanvasWidth * scale) / 2 + p.X * scale, target.Y + (target.Height - signature.CanvasHeight * scale) / 2 + p.Y * scale)).ToArray();
            for (var i = 1; i < points.Length; i++) gfx.DrawLine(new XPen(XColor.FromArgb(33, 33, 33), 1.5), points[i - 1], points[i]);
        }
    }

    private static IEnumerable<string> SplitText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new[] { "-" };
        const int width = 96;
        return value.Trim().Replace("\r", string.Empty).Split('\n').SelectMany(line => line.Length <= width ? new[] { line } : Enumerable.Range(0, (line.Length + width - 1) / width).Select(i => line.Substring(i * width, Math.Min(width, line.Length - i * width))));
    }

    private static string FullName(MitgliedRecord member) => string.Join(" ", new[] { member?.Vorname, member?.Name }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    private static string TextOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string UpperFirst(string value) => string.IsNullOrWhiteSpace(value) ? "-" : char.ToUpperInvariant(value[0]) + value[1..];
}
