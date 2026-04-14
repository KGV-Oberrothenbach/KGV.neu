using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities;

public static class MitgliedsantragHtmlPdfRenderer
{
    private const double PageMargin = 34d;
    private const double SectionSpacing = 10d;
    private const double HeaderLogoSize = 62d;

    public static byte[] Build(string dokumentTitel, string renderedHtml)
    {
        if (string.IsNullOrWhiteSpace(renderedHtml))
            throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage konnte nicht gerendert werden.");

        var template = Parse(renderedHtml);
        PdfSharpFontResolverInitializer.EnsureInitialized();

        var document = new PdfDocument();
        document.Info.Title = dokumentTitel;
        document.Info.Subject = "Mitgliedsantrag";
        document.Info.Author = template.VereinName;
        document.Info.Creator = template.VereinName;
        document.Info.Keywords = "MitgliedsantragTemplateHtml";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        var graphics = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 17, XFontStyle.Bold);
        var subtitleFont = new XFont("Arial", 10, XFontStyle.Regular);
        var sectionTitleFont = new XFont("Arial", 10, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9.5, XFontStyle.Regular);
        var labelFont = new XFont("Arial", 9, XFontStyle.Bold);
        var boxFont = new XFont("Arial", 10.5, XFontStyle.Regular);
        var cardValueFont = new XFont("Arial", 15, XFontStyle.Bold);
        var footerFont = new XFont("Arial", 8.5, XFontStyle.Regular);
        var borderPen = new XPen(XColor.FromArgb(216, 222, 229), 0.8);
        var accentPen = new XPen(XColor.FromArgb(47, 93, 58), 1.6);

        double cursorY = PageMargin;
        DrawHeader(page, graphics, titleFont, subtitleFont, labelFont, bodyFont, borderPen, accentPen, template, ref cursorY);
        DrawLead(graphics, bodyFont, template.LeadText, ref cursorY);
        DrawPersonSection(document, ref page, ref graphics, sectionTitleFont, labelFont, boxFont, bodyFont, borderPen, template.PersonSectionTitle, template.PersonFields, ref cursorY);

        if (template.IstMinderjaehrig && template.GesetzlicherVertreterFields.Count > 0)
            DrawPersonSection(document, ref page, ref graphics, sectionTitleFont, labelFont, boxFont, bodyFont, borderPen, template.GesetzlicherVertreterSectionTitle, template.GesetzlicherVertreterFields, ref cursorY);

        DrawPaymentSection(document, ref page, ref graphics, sectionTitleFont, labelFont, bodyFont, cardValueFont, borderPen, template, ref cursorY);
        DrawBankSection(document, ref page, ref graphics, sectionTitleFont, labelFont, boxFont, borderPen, template.BankSectionTitle, template.BankFields, ref cursorY);
        DrawTextSection(document, ref page, ref graphics, sectionTitleFont, bodyFont, borderPen, template.ErklaerungTitle, template.ErklaerungText, ref cursorY);
        DrawTextSection(document, ref page, ref graphics, sectionTitleFont, bodyFont, borderPen, template.DatenschutzTitle, template.DatenschutzText, ref cursorY);
        DrawSignatureSection(document, ref page, ref graphics, sectionTitleFont, bodyFont, boxFont, borderPen, accentPen, template.SignatureSection, template.IstMinderjaehrig, false, ref cursorY);
        DrawSignatureSection(document, ref page, ref graphics, sectionTitleFont, bodyFont, boxFont, borderPen, accentPen, template.DatenschutzSignatureSection, template.IstMinderjaehrig, true, ref cursorY);
        DrawFooter(document, ref page, ref graphics, footerFont, template.FooterText, ref cursorY);

        graphics.Dispose();

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static MitgliedsantragPdfTemplate Parse(string renderedHtml)
    {
        var document = XDocument.Parse(NormalizeHtml(renderedHtml), LoadOptions.PreserveWhitespace);
        var body = document.Root?.Element("body") ?? throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält keinen body.");
        var page = body.Elements().FirstOrDefault(x => HasClass(x, "page")) ?? throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält keine Seite.");
        var istMinderjaehrig = HasClass(body, "minor");

        var header = page.Elements().FirstOrDefault(x => HasClass(x, "header")) ?? throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält keinen Header.");
        var clubMetaLines = SplitLines(GetText(GetDescendantByClass(header, "club-meta")));
        var emailLine = clubMetaLines.FirstOrDefault(x => x.StartsWith("E-Mail:", StringComparison.CurrentCultureIgnoreCase));
        var sectionElements = page.Elements().Where(x => HasClass(x, "section")).ToList();
        if (sectionElements.Count < 6)
            throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält nicht alle erwarteten Abschnitte.");

        return new MitgliedsantragPdfTemplate(
            IstMinderjaehrig: istMinderjaehrig,
            VereinName: GetText(GetDescendantByClass(header, "club-name")),
            VereinRegisterangabe: string.Join("\n", clubMetaLines.Where(x => !string.Equals(x, emailLine, StringComparison.Ordinal)).Where(x => !string.IsNullOrWhiteSpace(x))),
            VereinEmail: string.IsNullOrWhiteSpace(emailLine) ? string.Empty : emailLine["E-Mail:".Length..].Trim(),
            Ausstellungsdatum: GetText(GetDescendantsByClass(header, "value").FirstOrDefault()),
            DokumentName: GetText(GetDescendantsByClass(header, "value").Skip(1).FirstOrDefault()),
            Titel: GetText(GetDescendantByClass(header, "title")?.Elements().FirstOrDefault(x => string.Equals(x.Name.LocalName, "h1", StringComparison.OrdinalIgnoreCase))),
            Untertitel: GetText(GetDescendantByClass(header, "title-sub")),
            LeadText: GetText(GetDescendantByClass(page, "lead")),
            PersonSectionTitle: GetSectionTitle(sectionElements[0]),
            PersonFields: ParseFields(sectionElements[0], false),
            GesetzlicherVertreterSectionTitle: GetSectionTitle(sectionElements[1]),
            GesetzlicherVertreterFields: istMinderjaehrig ? ParseFields(sectionElements[1], true) : Array.Empty<PdfField>(),
            PaymentSectionTitle: GetSectionTitle(sectionElements[2]),
            MitgliedsbeitragLabel: GetText(GetDescendantByClass(GetDescendantsByClass(sectionElements[2], "payment-card").FirstOrDefault(), "payment-label")),
            MitgliedsbeitragWert: GetText(GetDescendantByClass(GetDescendantsByClass(sectionElements[2], "payment-card").FirstOrDefault(), "payment-value")),
            AufnahmegebuehrLabel: GetText(GetDescendantByClass(GetDescendantsByClass(sectionElements[2], "payment-card").Skip(1).FirstOrDefault(), "payment-label")),
            AufnahmegebuehrWert: GetText(GetDescendantByClass(GetDescendantsByClass(sectionElements[2], "payment-card").Skip(1).FirstOrDefault(), "payment-value")),
            PaymentHints: GetDescendantsByClass(sectionElements[2], "payment-text").Select(GetText).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            BankSectionTitle: GetSectionTitle(sectionElements[3]),
            BankFields: ParseFields(sectionElements[3], false),
            ErklaerungTitle: GetSectionTitle(sectionElements[4]),
            ErklaerungText: GetText(GetDescendantByClass(sectionElements[4], "declaration")),
            DatenschutzTitle: GetSectionTitle(sectionElements[5]),
            DatenschutzText: GetText(GetDescendantByClass(sectionElements[5], "declaration")),
            SignatureSection: ParseSignatureSection(page, false, istMinderjaehrig),
            DatenschutzSignatureSection: ParseSignatureSection(page, true, istMinderjaehrig),
            FooterText: GetText(GetDescendantByClass(page, "footer-note")));
    }

    private static SignatureSectionData ParseSignatureSection(XElement page, bool datenschutz, bool istMinderjaehrig)
    {
        var sections = page.Elements().Where(x => HasClass(x, "signature-section")).ToList();
        var section = datenschutz
            ? sections.Skip(1).FirstOrDefault()
            : sections.FirstOrDefault();

        if (section == null)
            throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält nicht alle Signaturabschnitte.");

        var labels = GetDescendantsByClass(section, "signature-label")
            .Where(x => istMinderjaehrig || !HasClassInSelfOrAncestors(x, "guardian-only"))
            .Select(GetText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var topLabels = labels.Take(istMinderjaehrig ? 2 : 1).ToList();
        var bottomLabel = datenschutz ? null : labels.Skip(topLabels.Count).FirstOrDefault();

        return new SignatureSectionData(
            GetText(GetDescendantByClass(section, "signature-head")),
            topLabels,
            bottomLabel,
            datenschutz ? GetText(GetDescendantByClass(section, "privacy-note")) : string.Empty);
    }

    private static IReadOnlyList<PdfField> ParseFields(XElement section, bool istGesetzlicherVertreter)
    {
        var fields = new List<PdfField>();
        foreach (var field in GetDescendantsByClass(section, "field"))
        {
            var label = GetText(GetDescendantByClass(field, "field-label"));
            if (string.IsNullOrWhiteSpace(label))
                continue;

            var checkboxGrid = GetDescendantByClass(field, "checkbox-grid");
            if (checkboxGrid != null)
            {
                var checkItems = GetDescendantsByClass(checkboxGrid, "checkbox-item")
                    .Select(item =>
                    {
                        var spans = item.Elements().Where(x => string.Equals(x.Name.LocalName, "span", StringComparison.OrdinalIgnoreCase)).ToList();
                        var isChecked = spans.Count > 0 && !string.IsNullOrWhiteSpace(GetText(spans[0]));
                        var itemLabel = spans.Count > 1 ? GetText(spans[1]) : GetText(item);
                        return new CheckItem(itemLabel, isChecked);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Label))
                    .ToList();

                fields.Add(new PdfField(label, string.Empty, true, false, checkItems));
                continue;
            }

            var valueElement = GetDescendantByClass(field, "field-value");
            fields.Add(new PdfField(
                label,
                GetText(valueElement),
                HasClass(field, "full"),
                valueElement != null && HasClass(valueElement, "large"),
                null));
        }

        return istGesetzlicherVertreter
            ? fields.Where(x => !string.IsNullOrWhiteSpace(x.Value) || x.CheckItems?.Count > 0).ToList()
            : fields;
    }

    private static void DrawHeader(PdfPage page, XGraphics graphics, XFont titleFont, XFont subtitleFont, XFont labelFont, XFont bodyFont, XPen borderPen, XPen accentPen, MitgliedsantragPdfTemplate template, ref double cursorY)
    {
        using var logo = XImage.FromStream(() => new MemoryStream(VereinsdokumentBranding.GetLogoBytes(), writable: false));
        var contentWidth = page.Width.Point - PageMargin * 2;
        var docBoxWidth = 145d;
        var textWidth = contentWidth - docBoxWidth - 18d;
        graphics.DrawImage(logo, PageMargin, cursorY + 2, HeaderLogoSize, HeaderLogoSize);

        var textX = PageMargin + HeaderLogoSize + 14d;
        var textY = cursorY + 4d;
        graphics.DrawString(template.VereinName, titleFont, XBrushes.Black, new XRect(textX, textY, textWidth - HeaderLogoSize - 14d, 24), XStringFormats.TopLeft);
        graphics.DrawString(template.VereinRegisterangabe, subtitleFont, XBrushes.DimGray, new XRect(textX, textY + 24, textWidth, 16), XStringFormats.TopLeft);
        graphics.DrawString($"E-Mail: {template.VereinEmail}", subtitleFont, XBrushes.DimGray, new XRect(textX, textY + 40, textWidth, 16), XStringFormats.TopLeft);

        var docBoxX = page.Width.Point - PageMargin - docBoxWidth;
        var docBoxRect = new XRect(docBoxX, cursorY, docBoxWidth, 58d);
        graphics.DrawRectangle(XBrushes.WhiteSmoke, docBoxRect);
        graphics.DrawRectangle(borderPen, docBoxRect);
        graphics.DrawString("Ausstellungsdatum", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 8, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
        graphics.DrawString(template.Ausstellungsdatum, bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 22, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);
        graphics.DrawString("Dokument", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 36, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
        graphics.DrawString(template.DokumentName, bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 49, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);

        cursorY += Math.Max(HeaderLogoSize, docBoxRect.Height) + 8d;
        graphics.DrawLine(accentPen, PageMargin, cursorY, page.Width.Point - PageMargin, cursorY);
        cursorY += 14d;
        graphics.DrawString(template.Titel, titleFont, XBrushes.Black, new XRect(PageMargin, cursorY, contentWidth, 24), XStringFormats.TopLeft);
        cursorY += 24d;
        graphics.DrawString(template.Untertitel, subtitleFont, XBrushes.DimGray, new XRect(PageMargin, cursorY, contentWidth, 16), XStringFormats.TopLeft);
        cursorY += 22d;
    }

    private static void DrawLead(XGraphics graphics, XFont bodyFont, string leadText, ref double cursorY)
    {
        graphics.DrawString(leadText, bodyFont, XBrushes.Black, new XRect(PageMargin, cursorY, 520, 16), XStringFormats.TopLeft);
        cursorY += 24d;
    }

    private static void DrawPersonSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont valueFont, XFont bodyFont, XPen borderPen, string title, IReadOnlyList<PdfField> fields, ref double cursorY)
    {
        var estimatedHeight = EstimateFieldSectionHeight(fields);
        EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
        var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, title, ref cursorY, estimatedHeight - SectionSpacing);
        DrawFieldGrid(graphics, labelFont, valueFont, bodyFont, borderPen, sectionRect, fields);
        cursorY = sectionRect.Bottom + SectionSpacing;
    }

    private static void DrawPaymentSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont bodyFont, XFont cardValueFont, XPen borderPen, MitgliedsantragPdfTemplate template, ref double cursorY)
    {
        var estimatedHeight = 126d + template.PaymentHints.Count * 18d;
        EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
        var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, template.PaymentSectionTitle, ref cursorY, estimatedHeight - SectionSpacing);
        var innerRect = new XRect(sectionRect.X + 12, sectionRect.Y + 34, sectionRect.Width - 24, sectionRect.Height - 46);
        graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 251, 246)), innerRect);
        graphics.DrawRectangle(borderPen, innerRect);

        var cardGap = 10d;
        var cardWidth = (innerRect.Width - cardGap) / 2d;
        DrawPaymentCard(graphics, labelFont, cardValueFont, borderPen, new XRect(innerRect.X + 6, innerRect.Y + 8, cardWidth - 6, 48), template.MitgliedsbeitragLabel, template.MitgliedsbeitragWert);
        DrawPaymentCard(graphics, labelFont, cardValueFont, borderPen, new XRect(innerRect.X + cardWidth + cardGap, innerRect.Y + 8, cardWidth - 6, 48), template.AufnahmegebuehrLabel, template.AufnahmegebuehrWert);

        var hintY = innerRect.Y + 66;
        foreach (var hint in template.PaymentHints)
        {
            graphics.DrawString(hint, bodyFont, XBrushes.Black, new XRect(innerRect.X + 6, hintY, innerRect.Width - 12, 20), XStringFormats.TopLeft);
            hintY += 18d;
        }

        cursorY = sectionRect.Bottom + SectionSpacing;
    }

    private static void DrawBankSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont valueFont, XPen borderPen, string title, IReadOnlyList<PdfField> fields, ref double cursorY)
    {
        var estimatedHeight = EstimateFieldSectionHeight(fields, 2, false);
        EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
        var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, title, ref cursorY, estimatedHeight - SectionSpacing);
        DrawFieldGrid(graphics, labelFont, valueFont, valueFont, borderPen, sectionRect, fields, false);
        cursorY = sectionRect.Bottom + SectionSpacing;
    }

    private static void DrawTextSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont bodyFont, XPen borderPen, string title, string text, ref double cursorY)
    {
        var lines = SplitLines(text);
        var estimatedHeight = 58d + lines.Count * 13d;
        EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
        var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, title, ref cursorY, estimatedHeight - SectionSpacing);
        var innerX = sectionRect.X + 12d;
        var lineY = sectionRect.Y + 36d;
        foreach (var line in lines)
        {
            graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(innerX, lineY, sectionRect.Width - 24, 14), XStringFormats.TopLeft);
            lineY += 14d;
        }

        cursorY = sectionRect.Bottom + SectionSpacing;
    }

    private static void DrawSignatureSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont bodyFont, XFont boxFont, XPen borderPen, XPen accentPen, SignatureSectionData section, bool istMinderjaehrig, bool datenschutz, ref double cursorY)
    {
        var estimatedHeight = istMinderjaehrig ? 236d : 190d;
        if (datenschutz)
            estimatedHeight += 24d;

        EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
        var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, section.Title, ref cursorY, estimatedHeight - SectionSpacing);
        var metaTop = sectionRect.Y + 36d;
        var metaWidth = (sectionRect.Width - 32d) / 2d;
        DrawLineField(graphics, bodyFont, accentPen, sectionRect.X + 12d, metaTop, metaWidth, "Ort");
        DrawLineField(graphics, bodyFont, accentPen, sectionRect.X + 20d + metaWidth, metaTop, metaWidth, "Datum");

        var boxTop = metaTop + 34d;
        var gap = 10d;
        var topBoxWidth = istMinderjaehrig ? (sectionRect.Width - 24d - gap) / 2d : sectionRect.Width - 24d;
        var primaryLabel = section.TopLabels.FirstOrDefault() ?? "Unterschrift Antragsteller/in";
        DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d, boxTop, topBoxWidth, 76d), primaryLabel);
        if (istMinderjaehrig)
        {
            var secondaryLabel = section.TopLabels.Skip(1).FirstOrDefault() ?? (datenschutz ? "Unterschrift gesetzliche/r Vertreter/in zur Datenschutzerklärung" : "Unterschrift gesetzliche/r Vertreter/in");
            DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d + topBoxWidth + gap, boxTop, topBoxWidth, 76d), secondaryLabel);
        }

        if (!datenschutz)
        {
            var bottomWidth = istMinderjaehrig ? topBoxWidth : sectionRect.Width - 24d;
            DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d, boxTop + 94d, bottomWidth, 76d), string.IsNullOrWhiteSpace(section.BottomLabel) ? "Für den Verein" : section.BottomLabel);
        }
        else if (!string.IsNullOrWhiteSpace(section.Note))
        {
            graphics.DrawString(section.Note, bodyFont, XBrushes.DimGray, new XRect(sectionRect.X + 12d, boxTop + 94d, sectionRect.Width - 24d, 22d), XStringFormats.TopLeft);
        }

        cursorY = sectionRect.Bottom + SectionSpacing;
    }

    private static void DrawFooter(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont footerFont, string footerText, ref double cursorY)
    {
        EnsureSpace(document, ref page, ref graphics, ref cursorY, 24d);
        graphics.DrawString(footerText, footerFont, XBrushes.DimGray, new XRect(PageMargin, cursorY, page.Width.Point - PageMargin * 2, 18), XStringFormats.TopCenter);
        cursorY += 18d;
    }

    private static XRect DrawSectionShell(XGraphics graphics, PdfPage page, XPen borderPen, XBrush backgroundBrush, XFont sectionTitleFont, string title, ref double cursorY, double height)
    {
        var rect = new XRect(PageMargin, cursorY, page.Width.Point - PageMargin * 2, height);
        graphics.DrawRectangle(backgroundBrush, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawRectangle(XBrushes.Transparent, rect.X, rect.Y + 28d, rect.Width, 0.01d);
        graphics.DrawString(title, sectionTitleFont, XBrushes.Black, new XRect(rect.X + 12, rect.Y + 9, rect.Width - 24, 16), XStringFormats.TopLeft);
        graphics.DrawLine(borderPen, rect.X, rect.Y + 28d, rect.X + rect.Width, rect.Y + 28d);
        return rect;
    }

    private static void DrawFieldGrid(XGraphics graphics, XFont labelFont, XFont valueFont, XFont bodyFont, XPen borderPen, XRect sectionRect, IReadOnlyList<PdfField> fields, bool tallCommunication = true)
    {
        const double gapX = 8d;
        const double gapY = 8d;
        var contentX = sectionRect.X + 12d;
        var contentY = sectionRect.Y + 40d;
        var contentWidth = sectionRect.Width - 24d;
        var columnWidth = (contentWidth - gapX) / 2d;
        double currentY = contentY;
        int index = 0;

        while (index < fields.Count)
        {
            var field = fields[index];
            if (field.FullWidth)
            {
                var height = GetFieldHeight(field, tallCommunication);
                DrawField(graphics, labelFont, valueFont, bodyFont, borderPen, new XRect(contentX, currentY, contentWidth, height), field);
                currentY += height + gapY;
                index++;
                continue;
            }

            var secondField = index + 1 < fields.Count && !fields[index + 1].FullWidth ? fields[index + 1] : null;
            var firstHeight = GetFieldHeight(field, tallCommunication);
            var secondHeight = secondField == null ? firstHeight : GetFieldHeight(secondField, tallCommunication);
            var rowHeight = Math.Max(firstHeight, secondHeight);
            DrawField(graphics, labelFont, valueFont, bodyFont, borderPen, new XRect(contentX, currentY, columnWidth, rowHeight), field);
            if (secondField != null)
                DrawField(graphics, labelFont, valueFont, bodyFont, borderPen, new XRect(contentX + columnWidth + gapX, currentY, columnWidth, rowHeight), secondField);

            currentY += rowHeight + gapY;
            index += secondField == null ? 1 : 2;
        }
    }

    private static void DrawField(XGraphics graphics, XFont labelFont, XFont valueFont, XFont bodyFont, XPen borderPen, XRect rect, PdfField field)
    {
        graphics.DrawString(field.Label, labelFont, XBrushes.DimGray, new XRect(rect.X, rect.Y, rect.Width, 12), XStringFormats.TopLeft);
        var boxRect = new XRect(rect.X, rect.Y + 14d, rect.Width, rect.Height - 14d);
        graphics.DrawRectangle(XBrushes.White, boxRect);
        graphics.DrawRectangle(borderPen, boxRect);

        if (field.CheckItems?.Count > 0)
        {
            var itemY = boxRect.Y + 8d;
            foreach (var item in field.CheckItems)
            {
                DrawCheckItem(graphics, bodyFont, borderPen, boxRect.X + 8d, itemY, boxRect.Width - 16d, item);
                itemY += 16d;
            }

            return;
        }

        var valueY = boxRect.Y + 8d;
        foreach (var line in SplitLines(string.IsNullOrWhiteSpace(field.Value) ? "-" : field.Value))
        {
            graphics.DrawString(line, valueFont, XBrushes.Black, new XRect(boxRect.X + 8d, valueY, boxRect.Width - 16d, 14d), XStringFormats.TopLeft);
            valueY += 13d;
        }
    }

    private static void DrawCheckItem(XGraphics graphics, XFont font, XPen borderPen, double x, double y, double width, CheckItem item)
    {
        var boxRect = new XRect(x, y + 1d, 11d, 11d);
        graphics.DrawRectangle(XBrushes.White, boxRect);
        graphics.DrawRectangle(borderPen, boxRect);
        if (item.Checked)
            graphics.DrawString("✓", font, XBrushes.Black, new XRect(boxRect.X + 1d, boxRect.Y - 1d, boxRect.Width, boxRect.Height + 2d), XStringFormats.Center);

        graphics.DrawString(item.Label, font, XBrushes.Black, new XRect(x + 16d, y, width - 16d, 14d), XStringFormats.TopLeft);
    }

    private static void DrawPaymentCard(XGraphics graphics, XFont labelFont, XFont valueFont, XPen borderPen, XRect rect, string label, string value)
    {
        graphics.DrawRectangle(XBrushes.White, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.DrawString(label, labelFont, XBrushes.DimGray, new XRect(rect.X + 8d, rect.Y + 8d, rect.Width - 16d, 12d), XStringFormats.TopLeft);
        graphics.DrawString(value, valueFont, XBrushes.Black, new XRect(rect.X + 8d, rect.Y + 22d, rect.Width - 16d, 20d), XStringFormats.TopLeft);
    }

    private static void DrawLineField(XGraphics graphics, XFont font, XPen accentPen, double x, double y, double width, string label)
    {
        graphics.DrawString(label, font, XBrushes.Black, new XRect(x, y, width, 12), XStringFormats.TopLeft);
        graphics.DrawLine(accentPen, x, y + 22d, x + width, y + 22d);
    }

    private static void DrawSignatureBox(XGraphics graphics, XFont boxFont, XPen borderPen, XRect rect, string label)
    {
        var canvasRect = new XRect(rect.X, rect.Y, rect.Width, 54d);
        graphics.DrawRectangle(XBrushes.White, canvasRect);
        graphics.DrawRectangle(borderPen, canvasRect);
        graphics.DrawString(label, boxFont, XBrushes.DimGray, new XRect(rect.X, rect.Y + 58d, rect.Width, 18d), XStringFormats.TopLeft);
    }

    private static void EnsureSpace(PdfDocument document, ref PdfPage page, ref XGraphics graphics, ref double cursorY, double neededHeight)
    {
        if (cursorY + neededHeight <= page.Height.Point - PageMargin)
            return;

        graphics.Dispose();
        page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        graphics = XGraphics.FromPdfPage(page);
        cursorY = PageMargin;
    }

    private static double EstimateFieldSectionHeight(IReadOnlyList<PdfField> fields, int columns = 2, bool tallCommunication = true)
    {
        const double gapY = 8d;
        double height = 42d;
        int index = 0;
        while (index < fields.Count)
        {
            var field = fields[index];
            if (field.FullWidth)
            {
                height += GetFieldHeight(field, tallCommunication) + gapY;
                index++;
                continue;
            }

            var secondField = index + 1 < fields.Count && !fields[index + 1].FullWidth ? fields[index + 1] : null;
            var rowHeight = Math.Max(GetFieldHeight(field, tallCommunication), secondField == null ? 0d : GetFieldHeight(secondField, tallCommunication));
            height += rowHeight + gapY;
            index += secondField == null ? 1 : Math.Min(columns, 2);
        }

        return height + 4d;
    }

    private static double GetFieldHeight(PdfField field, bool tallCommunication)
    {
        if (field.CheckItems?.Count > 0)
            return Math.Max(78d, 24d + field.CheckItems.Count * 16d);

        var lines = Math.Max(1, SplitLines(string.IsNullOrWhiteSpace(field.Value) ? "-" : field.Value).Count);
        var baseHeight = 14d + Math.Max(field.Large ? 42d : 28d, lines * 13d + 14d);
        if (tallCommunication && string.Equals(field.Label, "Kommunikation", StringComparison.Ordinal))
            return Math.Max(baseHeight, 66d);
        return baseHeight;
    }

    private static string NormalizeHtml(string html)
        => html
            .Replace("<!DOCTYPE html>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "<br />", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "<br />", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static string GetSectionTitle(XElement section)
        => GetText(GetDescendantByClass(section, "section-title"));

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

    private static bool HasClassInSelfOrAncestors(XElement element, string className)
        => element.AncestorsAndSelf().Any(x => HasClass(x, className));

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
        var lines = SplitLines(value)
            .Select(NormalizeSingleLine)
            .ToList();

        return string.Join("\n", lines).Trim();
    }

    private static string NormalizeSingleLine(string value)
    {
        var parts = (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim());
        return string.Join(" ", parts);
    }

    private static List<string> SplitLines(string? text)
        => (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

    private sealed record MitgliedsantragPdfTemplate(
        bool IstMinderjaehrig,
        string VereinName,
        string VereinRegisterangabe,
        string VereinEmail,
        string Ausstellungsdatum,
        string DokumentName,
        string Titel,
        string Untertitel,
        string LeadText,
        string PersonSectionTitle,
        IReadOnlyList<PdfField> PersonFields,
        string GesetzlicherVertreterSectionTitle,
        IReadOnlyList<PdfField> GesetzlicherVertreterFields,
        string PaymentSectionTitle,
        string MitgliedsbeitragLabel,
        string MitgliedsbeitragWert,
        string AufnahmegebuehrLabel,
        string AufnahmegebuehrWert,
        IReadOnlyList<string> PaymentHints,
        string BankSectionTitle,
        IReadOnlyList<PdfField> BankFields,
        string ErklaerungTitle,
        string ErklaerungText,
        string DatenschutzTitle,
        string DatenschutzText,
        SignatureSectionData SignatureSection,
        SignatureSectionData DatenschutzSignatureSection,
        string FooterText);

    private sealed record PdfField(string Label, string Value, bool FullWidth = false, bool Large = false, IReadOnlyList<CheckItem>? CheckItems = null);
    private sealed record CheckItem(string Label, bool Checked);
    private sealed record SignatureSectionData(string Title, IReadOnlyList<string> TopLabels, string? BottomLabel, string Note);
}
