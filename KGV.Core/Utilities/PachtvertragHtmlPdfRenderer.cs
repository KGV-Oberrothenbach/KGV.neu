using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities;

public static class PachtvertragHtmlPdfRenderer
{
    private const double PageMargin = 34d;
    private const double SectionSpacing = 8d;
    private const double HeaderLogoSize = 58d;

    public static byte[] Build(string dokumentTitel, string renderedHtml)
    {
        if (string.IsNullOrWhiteSpace(renderedHtml))
            throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage konnte nicht gerendert werden.");

        var template = Parse(renderedHtml);
        PdfSharpFontResolverInitializer.EnsureInitialized();

        var document = new PdfDocument();
        document.Info.Title = dokumentTitel;
        document.Info.Subject = "Pachtvertrag";
        document.Info.Author = template.VereinName;
        document.Info.Creator = template.VereinName;
        document.Info.Keywords = "PachtvertragTemplateHtml";

        DrawPage1(document, template);
        DrawTextPage(document, template.Page2Sections);
        DrawTextPage(document, template.Page3Sections);
        DrawPage4(document, template);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void DrawPage1(PdfDocument document, PachtvertragPdfTemplate template)
    {
        var page = AddPage(document);
        using var graphics = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 19, XFontStyle.Bold);
        var subtitleFont = new XFont("Arial", 10.5, XFontStyle.Regular);
        var sectionTitleFont = new XFont("Arial", 11.2, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9.6, XFontStyle.Regular);
        var labelFont = new XFont("Arial", 8.8, XFontStyle.Bold);
        var borderPen = new XPen(XColor.FromArgb(216, 222, 229), 0.8);
        var accentPen = new XPen(XColor.FromArgb(47, 93, 58), 1.6);
        double cursorY = PageMargin;

        DrawHeader(page, graphics, titleFont, subtitleFont, labelFont, bodyFont, borderPen, accentPen, template, ref cursorY);
        DrawPartyBoxes(graphics, page, labelFont, bodyFont, borderPen, sectionTitleFont, template.Parties, ref cursorY);
        foreach (var section in template.Page1Sections)
            DrawTextSection(graphics, page, sectionTitleFont, bodyFont, borderPen, section, ref cursorY);

        DrawPaymentSection(graphics, page, sectionTitleFont, labelFont, bodyFont, borderPen, template.Payment, ref cursorY);
    }

    private static void DrawTextPage(PdfDocument document, IReadOnlyList<TextSectionData> sections)
    {
        var page = AddPage(document);
        using var graphics = XGraphics.FromPdfPage(page);
        var sectionTitleFont = new XFont("Arial", 11, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9.4, XFontStyle.Regular);
        var borderPen = new XPen(XColor.FromArgb(216, 222, 229), 0.8);
        double cursorY = PageMargin;

        foreach (var section in sections)
            DrawTextSection(graphics, page, sectionTitleFont, bodyFont, borderPen, section, ref cursorY);
    }

    private static void DrawPage4(PdfDocument document, PachtvertragPdfTemplate template)
    {
        var page = AddPage(document);
        using var graphics = XGraphics.FromPdfPage(page);
        var sectionTitleFont = new XFont("Arial", 11, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9.6, XFontStyle.Regular);
        var labelFont = new XFont("Arial", 8.8, XFontStyle.Bold);
        var boxFont = new XFont("Arial", 10.2, XFontStyle.Regular);
        var costFont = new XFont("Arial", 13, XFontStyle.Bold);
        var footerFont = new XFont("Arial", 9, XFontStyle.Regular);
        var borderPen = new XPen(XColor.FromArgb(216, 222, 229), 0.8);
        var accentPen = new XPen(XColor.FromArgb(47, 93, 58), 1.4);
        double cursorY = PageMargin;

        DrawTextSection(graphics, page, sectionTitleFont, bodyFont, borderPen, template.Page4AdditionalSection, ref cursorY);
        DrawBankAndCostSection(graphics, page, sectionTitleFont, bodyFont, labelFont, costFont, borderPen, template.CostSection, ref cursorY);
        DrawSignatureSection(graphics, page, sectionTitleFont, bodyFont, boxFont, borderPen, accentPen, template.SignatureSection, template.IsDual, ref cursorY);
        DrawFinalNote(graphics, page, footerFont, template.FinalNote, ref cursorY);
    }

    private static PachtvertragPdfTemplate Parse(string renderedHtml)
    {
        var document = XDocument.Parse(NormalizeHtml(renderedHtml), LoadOptions.PreserveWhitespace);
        var body = document.Root?.Element("body") ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keinen body.");
        var pages = body.Elements().Where(x => HasClass(x, "page")).ToList();
        if (pages.Count < 4)
            throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält nicht alle erwarteten Seiten.");

        var isDual = HasClass(body, "dual");
        var page1 = pages[0];
        var page4 = pages[3];
        var titleBlock = GetDescendantByClass(page1, "title-block") ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keinen Titelblock.");
        var logoText = GetDescendantByClass(titleBlock, "logo-text") ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keine Titelinformationen.");
        var contractMeta = GetDescendantByClass(titleBlock, "contract-meta") ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keine Dokumentmetadaten.");
        var metaValues = GetDescendantsByClass(contractMeta, "meta-value").Select(GetText).ToList();
        var parties = GetDescendantsByClass(page1, "box")
            .Where(x => IsVisibleForMode(x, isDual))
            .Select(ParseInfoBox)
            .ToList();
        var page1Sections = page1.Elements().Where(x => HasClass(x, "section")).ToList();
        if (page1Sections.Count < 3)
            throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält nicht alle erwarteten Abschnitte auf Seite 1.");

        var page4Sections = page4.Elements().Where(x => HasClass(x, "section")).ToList();
        if (page4Sections.Count < 2)
            throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält nicht alle erwarteten Abschnitte auf Seite 4.");

        return new PachtvertragPdfTemplate(
            IsDual: isDual,
            LogoBytes: DecodeImageDataUri((string?)GetDescendantByClass(titleBlock, "logo-box")?.Element("img")?.Attribute("src")),
            Titel: GetText(GetDescendantByClass(logoText, "title")),
            Untertitel: GetText(GetDescendantByClass(logoText, "subtitle")),
            VereinName: GetText(contractMeta.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, "span", StringComparison.OrdinalIgnoreCase))),
            Vertragsintro: GetText(GetDescendantByClass(contractMeta, "subtitle")),
            DokumentName: metaValues.ElementAtOrDefault(0) ?? string.Empty,
            Ausstellungsdatum: metaValues.ElementAtOrDefault(1) ?? string.Empty,
            Parties: parties,
            Page1Sections: new[] { ParseTextSection(page1Sections[0], isDual), ParseTextSection(page1Sections[1], isDual) },
            Payment: ParsePaymentSection(page1Sections[2], isDual),
            Page2Sections: ParseTextSections(pages[1], isDual),
            Page3Sections: ParseTextSections(pages[2], isDual),
            Page4AdditionalSection: ParseTextSection(page4Sections[0], isDual),
            CostSection: ParseCostSection(page4Sections[1]),
            SignatureSection: ParseSignatureSection(page4, isDual),
            FinalNote: GetText(GetDescendantByClass(page4, "final-note")));
    }

    private static IReadOnlyList<TextSectionData> ParseTextSections(XElement page, bool isDual)
        => page.Elements()
            .Where(x => HasClass(x, "section") && IsVisibleForMode(x, isDual))
            .Select(x => ParseTextSection(x, isDual))
            .ToList();

    private static InfoBoxData ParseInfoBox(XElement box)
    {
        var fields = GetDescendantsByClass(box, "field")
            .Select(field => new InfoFieldData(
                GetText(GetDescendantByClass(field, "label")),
                GetText(GetDescendantByClass(field, "value"))))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .ToList();

        return new InfoBoxData(GetText(GetDescendantByClass(box, "box-title")), fields);
    }

    private static TextSectionData ParseTextSection(XElement section, bool isDual)
        => new(GetText(GetDescendantByClass(section, "section-title")), ExtractContentLines(GetDescendantByClass(section, "section-body"), isDual));

    private static PaymentSectionData ParsePaymentSection(XElement section, bool isDual)
    {
        var body = GetDescendantByClass(section, "section-body") ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keinen Zahlungsabschnitt.");
        var cards = GetDescendantsByClass(body, "field")
            .Where(x => IsVisibleForMode(x, isDual))
            .Select(field => new InfoFieldData(
                GetText(GetDescendantByClass(field, "label")),
                GetText(GetDescendantByClass(field, "value"))))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .ToList();
        var lines = body.Elements()
            .Where(x => HasClass(x, "paragraph") && IsVisibleForMode(x, isDual))
            .Select(GetText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        return new PaymentSectionData(GetText(GetDescendantByClass(section, "section-title")), cards, lines);
    }

    private static CostSectionData ParseCostSection(XElement section)
    {
        return new CostSectionData(
            GetText(GetDescendantByClass(section, "section-title")),
            GetText(GetDescendantByClass(section, "bank-box")),
            GetText(GetDescendantByClass(section, "cost-label")),
            GetText(GetDescendantByClass(section, "cost-value")),
            GetText(GetDescendantByClass(section, "small-note")));
    }

    private static SignatureSectionData ParseSignatureSection(XElement page, bool isDual)
    {
        var section = page.Elements().FirstOrDefault(x => HasClass(x, "signature-section"))
            ?? throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält keinen Signaturabschnitt.");
        var labels = GetDescendantsByClass(section, "signature-label")
            .Where(x => IsVisibleForMode(x, isDual))
            .Select(GetText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (labels.Count < 2)
            throw new InvalidOperationException("Die Pachtvertrag-HTML-Vorlage enthält nicht alle erwarteten Signaturfelder.");

        var topLabels = labels.Take(isDual ? 2 : 1).ToList();
        var bottomLabel = labels.Last();
        return new SignatureSectionData(GetText(GetDescendantByClass(section, "signature-head")), topLabels, bottomLabel);
    }

    private static IReadOnlyList<string> ExtractContentLines(XElement? body, bool isDual)
    {
        var lines = new List<string>();
        if (body == null)
            return lines;

        foreach (var child in body.Elements())
        {
            if (!IsVisibleForMode(child, isDual))
                continue;

            if (HasClass(child, "paragraph") || HasClass(child, "small-note"))
            {
                var text = GetText(child);
                if (!string.IsNullOrWhiteSpace(text))
                    lines.Add(text);
                continue;
            }

            if (HasClass(child, "list"))
            {
                foreach (var item in child.Elements().Where(x => string.Equals(x.Name.LocalName, "li", StringComparison.OrdinalIgnoreCase)))
                {
                    var text = GetText(item);
                    if (!string.IsNullOrWhiteSpace(text))
                        lines.Add($"• {text}");
                }
                continue;
            }

            if (HasClass(child, "field-grid-2") || HasClass(child, "field-grid-3"))
            {
                foreach (var field in GetDescendantsByClass(child, "field").Where(x => IsVisibleForMode(x, isDual)))
                {
                    var label = GetText(GetDescendantByClass(field, "label"));
                    var value = GetText(GetDescendantByClass(field, "value"));
                    if (!string.IsNullOrWhiteSpace(label) || !string.IsNullOrWhiteSpace(value))
                        lines.Add(string.IsNullOrWhiteSpace(label) ? value : $"{label}: {value}");
                }
                continue;
            }

            if (HasClass(child, "field"))
            {
                var label = GetText(GetDescendantByClass(child, "label"));
                var value = GetText(GetDescendantByClass(child, "value"));
                if (!string.IsNullOrWhiteSpace(label) || !string.IsNullOrWhiteSpace(value))
                    lines.Add(string.IsNullOrWhiteSpace(label) ? value : $"{label}: {value}");
                continue;
            }
        }

        return lines;
    }

    private static void DrawHeader(PdfPage page, XGraphics graphics, XFont titleFont, XFont subtitleFont, XFont labelFont, XFont bodyFont, XPen borderPen, XPen accentPen, PachtvertragPdfTemplate template, ref double cursorY)
    {
        var logoBytes = template.LogoBytes?.Length > 0 ? template.LogoBytes : VereinsdokumentBranding.GetLogoBytes();
        using var logo = XImage.FromStream(() => new MemoryStream(logoBytes, writable: false));
        var contentWidth = page.Width.Point - PageMargin * 2;
        var docBoxWidth = 145d;
        var textWidth = contentWidth - docBoxWidth - 18d;
        graphics.DrawImage(logo, PageMargin, cursorY + 2, HeaderLogoSize, HeaderLogoSize);

        var textX = PageMargin + HeaderLogoSize + 14d;
        var textY = cursorY + 2d;
        graphics.DrawString(template.Titel, titleFont, XBrushes.Black, new XRect(textX, textY, textWidth, 24), XStringFormats.TopLeft);
        graphics.DrawString(template.Untertitel, subtitleFont, XBrushes.DimGray, new XRect(textX, textY + 24d, textWidth, 18d), XStringFormats.TopLeft);
        graphics.DrawString(template.Vertragsintro, bodyFont, XBrushes.Black, new XRect(textX, textY + 40d, textWidth, 30d), XStringFormats.TopLeft);

        var docBoxX = page.Width.Point - PageMargin - docBoxWidth;
        var docBoxRect = new XRect(docBoxX, cursorY, docBoxWidth, 58d);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, docBoxRect);
        graphics.DrawRectangle(borderPen, docBoxRect);
        graphics.DrawString("Dokument", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 8, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
        graphics.DrawString(template.DokumentName, bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 22, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);
        graphics.DrawString("Ausstellungsdatum", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 36, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
        graphics.DrawString(template.Ausstellungsdatum, bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 49, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);

        cursorY += Math.Max(HeaderLogoSize + 12d, 82d);
        graphics.DrawLine(accentPen, PageMargin, cursorY, page.Width.Point - PageMargin, cursorY);
        cursorY += 12d;
    }

    private static void DrawPartyBoxes(XGraphics graphics, PdfPage page, XFont labelFont, XFont bodyFont, XPen borderPen, XFont titleFont, IReadOnlyList<InfoBoxData> boxes, ref double cursorY)
    {
        if (boxes.Count == 0)
            return;

        const double gap = 10d;
        var contentWidth = page.Width.Point - PageMargin * 2;
        var boxWidth = boxes.Count > 1 ? (contentWidth - gap) / 2d : contentWidth;
        var heights = boxes.Select(box => EstimateInfoBoxHeight(box)).ToList();
        var rowHeight = heights.Count == 0 ? 0d : heights.Max();

        for (var i = 0; i < boxes.Count; i++)
        {
            var x = PageMargin + i * (boxWidth + gap);
            DrawInfoBox(graphics, titleFont, labelFont, bodyFont, borderPen, new XRect(x, cursorY, boxWidth, rowHeight), boxes[i]);
        }

        cursorY += rowHeight + SectionSpacing;
    }

    private static void DrawInfoBox(XGraphics graphics, XFont titleFont, XFont labelFont, XFont bodyFont, XPen borderPen, XRect rect, InfoBoxData box)
    {
        graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(box.Title, titleFont, XBrushes.Black, new XRect(rect.X + 10d, rect.Y + 8d, rect.Width - 20d, 16d), XStringFormats.TopLeft);
        var currentY = rect.Y + 28d;
        foreach (var field in box.Fields)
        {
            graphics.DrawString(field.Label, labelFont, XBrushes.DimGray, new XRect(rect.X + 10d, currentY, rect.Width - 20d, 12d), XStringFormats.TopLeft);
            currentY += 12d;
            var lines = SplitLines(field.Value);
            foreach (var line in lines)
            {
                graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(rect.X + 10d, currentY, rect.Width - 20d, 12d), XStringFormats.TopLeft);
                currentY += 11d;
            }
            currentY += 6d;
        }
    }

    private static double EstimateInfoBoxHeight(InfoBoxData box)
    {
        var height = 34d;
        foreach (var field in box.Fields)
            height += 18d + Math.Max(1, SplitLines(field.Value).Count) * 11d;
        return height + 6d;
    }

    private static void DrawTextSection(XGraphics graphics, PdfPage page, XFont sectionTitleFont, XFont bodyFont, XPen borderPen, TextSectionData section, ref double cursorY)
    {
        var width = page.Width.Point - PageMargin * 2;
        var height = EstimateTextSectionHeight(graphics, bodyFont, width - 24d, section.Lines);
        var rect = new XRect(PageMargin, cursorY, width, height);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(section.Title, sectionTitleFont, XBrushes.Black, new XRect(rect.X + 12d, rect.Y + 9d, rect.Width - 24d, 16d), XStringFormats.TopLeft);
        graphics.DrawLine(borderPen, rect.X, rect.Y + 28d, rect.Right, rect.Y + 28d);

        var currentY = rect.Y + 36d;
        foreach (var paragraph in section.Lines)
        {
            var wrapped = WrapText(graphics, bodyFont, paragraph, rect.Width - 24d);
            if (wrapped.Count == 0)
            {
                currentY += 6d;
                continue;
            }

            foreach (var line in wrapped)
            {
                graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(rect.X + 12d, currentY, rect.Width - 24d, 12d), XStringFormats.TopLeft);
                currentY += 11.5d;
            }
            currentY += 3.5d;
        }

        cursorY = rect.Bottom + SectionSpacing;
    }

    private static double EstimateTextSectionHeight(XGraphics graphics, XFont font, double width, IReadOnlyList<string> paragraphs)
    {
        double textHeight = 8d;
        foreach (var paragraph in paragraphs)
        {
            var wrapped = WrapText(graphics, font, paragraph, width);
            textHeight += Math.Max(1, wrapped.Count) * 11.5d + 3.5d;
        }
        return 28d + textHeight + 8d;
    }

    private static void DrawPaymentSection(XGraphics graphics, PdfPage page, XFont sectionTitleFont, XFont labelFont, XFont bodyFont, XPen borderPen, PaymentSectionData section, ref double cursorY)
    {
        var width = page.Width.Point - PageMargin * 2;
        var paragraphHeight = section.Lines.Sum(line => Math.Max(1, WrapText(graphics, bodyFont, line, width - 24d).Count) * 11.5d + 3.5d);
        var rect = new XRect(PageMargin, cursorY, width, 92d + paragraphHeight);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(section.Title, sectionTitleFont, XBrushes.Black, new XRect(rect.X + 12d, rect.Y + 9d, rect.Width - 24d, 16d), XStringFormats.TopLeft);
        graphics.DrawLine(borderPen, rect.X, rect.Y + 28d, rect.Right, rect.Y + 28d);

        const double gap = 8d;
        var cardWidth = (rect.Width - 24d - gap * 2) / 3d;
        var cardX = rect.X + 12d;
        for (var i = 0; i < section.Cards.Count; i++)
        {
            var cardRect = new XRect(cardX + i * (cardWidth + gap), rect.Y + 36d, cardWidth, 36d);
            graphics.DrawRectangle(XBrushes.White, cardRect);
            graphics.DrawRectangle(borderPen, cardRect);
            graphics.DrawString(section.Cards[i].Label, labelFont, XBrushes.DimGray, new XRect(cardRect.X + 6d, cardRect.Y + 6d, cardRect.Width - 12d, 10d), XStringFormats.TopLeft);
            graphics.DrawString(section.Cards[i].Value, bodyFont, XBrushes.Black, new XRect(cardRect.X + 6d, cardRect.Y + 18d, cardRect.Width - 12d, 12d), XStringFormats.TopLeft);
        }

        var currentY = rect.Y + 80d;
        foreach (var paragraph in section.Lines)
        {
            foreach (var line in WrapText(graphics, bodyFont, paragraph, rect.Width - 24d))
            {
                graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(rect.X + 12d, currentY, rect.Width - 24d, 12d), XStringFormats.TopLeft);
                currentY += 11.5d;
            }
            currentY += 3.5d;
        }

        cursorY = rect.Bottom + SectionSpacing;
    }

    private static void DrawBankAndCostSection(XGraphics graphics, PdfPage page, XFont sectionTitleFont, XFont bodyFont, XFont labelFont, XFont costFont, XPen borderPen, CostSectionData section, ref double cursorY)
    {
        var width = page.Width.Point - PageMargin * 2;
        var rect = new XRect(PageMargin, cursorY, width, 120d);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(section.Title, sectionTitleFont, XBrushes.Black, new XRect(rect.X + 12d, rect.Y + 9d, rect.Width - 24d, 16d), XStringFormats.TopLeft);
        graphics.DrawLine(borderPen, rect.X, rect.Y + 28d, rect.Right, rect.Y + 28d);

        var leftRect = new XRect(rect.X + 12d, rect.Y + 38d, rect.Width - 24d - 176d, 70d);
        var rightRect = new XRect(leftRect.Right + 10d, rect.Y + 38d, 166d, 70d);
        graphics.DrawRectangle(XBrushes.White, leftRect);
        graphics.DrawRectangle(borderPen, leftRect);
        graphics.DrawRectangle(XBrushes.White, rightRect);
        graphics.DrawRectangle(borderPen, rightRect);

        var lineY = leftRect.Y + 8d;
        foreach (var line in SplitLines(section.BankText))
        {
            graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(leftRect.X + 8d, lineY, leftRect.Width - 16d, 12d), XStringFormats.TopLeft);
            lineY += 11.5d;
        }

        graphics.DrawString(section.CostLabel, labelFont, XBrushes.DimGray, new XRect(rightRect.X + 8d, rightRect.Y + 8d, rightRect.Width - 16d, 12d), XStringFormats.TopLeft);
        graphics.DrawString(section.CostValue, costFont, XBrushes.Black, new XRect(rightRect.X + 8d, rightRect.Y + 24d, rightRect.Width - 16d, 18d), XStringFormats.TopLeft);

        var noteY = rect.Y + 112d;
        foreach (var line in WrapText(graphics, bodyFont, section.Note, rect.Width - 24d))
        {
            graphics.DrawString(line, bodyFont, XBrushes.DimGray, new XRect(rect.X + 12d, noteY, rect.Width - 24d, 12d), XStringFormats.TopLeft);
            noteY += 11d;
        }

        cursorY = Math.Max(rect.Bottom, noteY + 4d) + SectionSpacing;
    }

    private static void DrawSignatureSection(XGraphics graphics, PdfPage page, XFont sectionTitleFont, XFont bodyFont, XFont boxFont, XPen borderPen, XPen accentPen, SignatureSectionData section, bool isDual, ref double cursorY)
    {
        var width = page.Width.Point - PageMargin * 2;
        var rect = new XRect(PageMargin, cursorY, width, isDual ? 165d : 158d);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(section.Title, sectionTitleFont, XBrushes.Black, new XRect(rect.X + 12d, rect.Y + 9d, rect.Width - 24d, 16d), XStringFormats.TopLeft);
        graphics.DrawLine(borderPen, rect.X, rect.Y + 28d, rect.Right, rect.Y + 28d);

        var metaTop = rect.Y + 38d;
        var metaWidth = (rect.Width - 32d) / 2d;
        DrawLineField(graphics, bodyFont, accentPen, rect.X + 12d, metaTop, metaWidth, "Ort");
        DrawLineField(graphics, bodyFont, accentPen, rect.X + 20d + metaWidth, metaTop, metaWidth, "Datum");

        var gap = 10d;
        var topWidth = isDual ? (rect.Width - 24d - gap) / 2d : rect.Width - 24d;
        var topY = metaTop + 30d;
        DrawSignatureBox(graphics, boxFont, borderPen, new XRect(rect.X + 12d, topY, topWidth, 70d), section.TopLabels.FirstOrDefault() ?? "Unterschrift Pächter 1");
        if (isDual)
            DrawSignatureBox(graphics, boxFont, borderPen, new XRect(rect.X + 12d + topWidth + gap, topY, topWidth, 70d), section.TopLabels.Skip(1).FirstOrDefault() ?? "Unterschrift Pächter 2");

        var landlordWidth = isDual ? topWidth : rect.Width - 24d;
        DrawSignatureBox(graphics, boxFont, borderPen, new XRect(rect.X + 12d, topY + 88d, landlordWidth, 70d), section.BottomLabel);
        cursorY = rect.Bottom + SectionSpacing;
    }

    private static void DrawFinalNote(XGraphics graphics, PdfPage page, XFont font, string text, ref double cursorY)
    {
        foreach (var line in WrapText(graphics, font, text, page.Width.Point - PageMargin * 2))
        {
            graphics.DrawString(line, font, XBrushes.DimGray, new XRect(PageMargin, cursorY, page.Width.Point - PageMargin * 2, 12d), XStringFormats.TopLeft);
            cursorY += 10.8d;
        }
    }

    private static void DrawLineField(XGraphics graphics, XFont font, XPen accentPen, double x, double y, double width, string label)
    {
        graphics.DrawString(label, font, XBrushes.Black, new XRect(x, y, width, 12), XStringFormats.TopLeft);
        graphics.DrawLine(accentPen, x, y + 20d, x + width, y + 20d);
    }

    private static void DrawSignatureBox(XGraphics graphics, XFont boxFont, XPen borderPen, XRect rect, string label)
    {
        var canvasRect = new XRect(rect.X, rect.Y, rect.Width, 50d);
        graphics.DrawRectangle(XBrushes.White, canvasRect);
        graphics.DrawRectangle(borderPen, canvasRect);
        graphics.DrawString(label, boxFont, XBrushes.DimGray, new XRect(rect.X, rect.Y + 54d, rect.Width, 16d), XStringFormats.TopLeft);
    }

    private static PdfPage AddPage(PdfDocument document)
    {
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        return page;
    }

    private static List<string> WrapText(XGraphics graphics, XFont font, string? text, double maxWidth)
    {
        var lines = new List<string>();
        foreach (var rawLine in SplitLinesPreserveEmpty(text))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = rawLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var current = words[0];
            for (var i = 1; i < words.Length; i++)
            {
                var candidate = current + " " + words[i];
                if (graphics.MeasureString(candidate, font).Width <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = words[i];
            }
            lines.Add(current);
        }
        return lines;
    }

    private static List<string> SplitLines(string? text)
        => SplitLinesPreserveEmpty(text).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

    private static List<string> SplitLinesPreserveEmpty(string? text)
        => (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(x => x.Trim())
            .ToList();

    private static string NormalizeHtml(string html)
    {
        var normalized = html
            .Replace("<!DOCTYPE html>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "<br />", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "<br />", StringComparison.OrdinalIgnoreCase);
        normalized = Regex.Replace(normalized, "<img([^>]*?)(?<!/)>", "<img$1 />", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return normalized.Trim();
    }

    private static byte[]? DecodeImageDataUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var separatorIndex = value.IndexOf(",", StringComparison.Ordinal);
        if (separatorIndex < 0 || separatorIndex >= value.Length - 1)
            return null;

        var base64 = value[(separatorIndex + 1)..];
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }

    private static XElement? GetDescendantByClass(XElement? element, string className)
        => element?.Descendants().FirstOrDefault(x => HasClass(x, className));

    private static IReadOnlyList<XElement> GetDescendantsByClass(XElement? element, string className)
        => element == null
            ? Array.Empty<XElement>()
            : element.Descendants().Where(x => HasClass(x, className)).ToList();

    private static bool HasClass(XElement? element, string className)
    {
        if (element == null)
            return false;

        var value = (string?)element.Attribute("class");
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => string.Equals(x, className, StringComparison.Ordinal));
    }

    private static bool IsVisibleForMode(XElement element, bool isDual)
        => isDual || !element.AncestorsAndSelf().Any(x => HasClass(x, "dual-only"));

    private static string GetText(XElement? element)
    {
        if (element == null)
            return string.Empty;

        var builder = new StringBuilder();
        AppendText(element.Nodes(), builder);
        return NormalizeText(builder.ToString());
    }

    private static void AppendText(IEnumerable<XNode> nodes, StringBuilder builder)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement element when string.Equals(element.Name.LocalName, "br", StringComparison.OrdinalIgnoreCase):
                    builder.Append('\n');
                    break;
                case XElement element:
                    AppendText(element.Nodes(), builder);
                    break;
            }
        }
    }

    private static string NormalizeText(string value)
    {
        var lines = SplitLinesPreserveEmpty(value)
            .Select(NormalizeSingleLine)
            .ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines).Trim();
    }

    private static string NormalizeSingleLine(string value)
    {
        var parts = (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim());
        return string.Join(" ", parts);
    }

    private sealed record PachtvertragPdfTemplate(
        bool IsDual,
        byte[]? LogoBytes,
        string Titel,
        string Untertitel,
        string VereinName,
        string Vertragsintro,
        string DokumentName,
        string Ausstellungsdatum,
        IReadOnlyList<InfoBoxData> Parties,
        IReadOnlyList<TextSectionData> Page1Sections,
        PaymentSectionData Payment,
        IReadOnlyList<TextSectionData> Page2Sections,
        IReadOnlyList<TextSectionData> Page3Sections,
        TextSectionData Page4AdditionalSection,
        CostSectionData CostSection,
        SignatureSectionData SignatureSection,
        string FinalNote);

    private sealed record InfoBoxData(string Title, IReadOnlyList<InfoFieldData> Fields);
    private sealed record InfoFieldData(string Label, string Value);
    private sealed record TextSectionData(string Title, IReadOnlyList<string> Lines);
    private sealed record PaymentSectionData(string Title, IReadOnlyList<InfoFieldData> Cards, IReadOnlyList<string> Lines);
    private sealed record CostSectionData(string Title, string BankText, string CostLabel, string CostValue, string Note);
    private sealed record SignatureSectionData(string Title, IReadOnlyList<string> TopLabels, string BottomLabel);
}
