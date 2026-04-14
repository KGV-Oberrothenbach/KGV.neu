using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KGV.Core.Utilities
{
    public static class MitgliedsantragDokumentFactory
    {
        private const string TemplateResourceName = "KGV.Core.Templates.MitgliedsantragTemplate.html";
        private const double PageMargin = 34d;
        private const double SectionSpacing = 10d;
        private const double HeaderLogoSize = 62d;

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, 0m, beginnDatum, null, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, string? status = null)
            => CreateUploadRequest(member, mitgliedsbeitrag, 0m, beginnDatum, gesetzlicherVertreterSnapshot, null, status);

        public static DokumentUploadRequest CreateUploadRequest(MitgliedRecord member, decimal mitgliedsbeitrag, decimal aufnahmegebuehr, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot? bankverbindungSnapshot, string? status = null)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (mitgliedsbeitrag < 0m)
                throw new InvalidOperationException("Der Mitgliedsbeitrag darf nicht negativ sein.");
            if (aufnahmegebuehr < 0m)
                throw new InvalidOperationException("Die Aufnahmegebühr darf nicht negativ sein.");
            if (bankverbindungSnapshot == null)
                throw new InvalidOperationException("Es ist keine aktive Vereinskonfiguration mit vollständigen Mitgliedsantragsdaten hinterlegt.");

            var normalizedStatus = FormularDokumentStatus.Normalize(status);
            var dokumenttyp = FormularDokumentTyp.Mitgliedsantrag;
            var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, dokumenttyp, normalizedStatus, DateTime.Today);
            var title = FormularDokumentDateiname.BuildTitel(dokumenttyp, normalizedStatus);
            var templateData = BuildTemplateData(member, mitgliedsbeitrag, aufnahmegebuehr, beginnDatum.Date, gesetzlicherVertreterSnapshot, bankverbindungSnapshot);
            var renderedHtml = MitgliedsantragTemplateRenderer.Render(LoadTemplateHtml(), templateData);
            EnsureTemplateFullyRendered(renderedHtml);
            var content = BuildTemplatePdf(title, renderedHtml);

            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = title,
                FileName = fileName,
                MimeType = "application/pdf",
                FileContent = content
            };
        }

        private static MitgliedsantragTemplateData BuildTemplateData(MitgliedRecord member, decimal mitgliedsbeitrag, decimal aufnahmegebuehr, DateTime beginnDatum, MitgliedsantragVertreterSnapshot? gesetzlicherVertreterSnapshot, MitgliedsantragBankverbindungSnapshot bankverbindungSnapshot)
        {
            var istMinderjaehrig = gesetzlicherVertreterSnapshot != null;
            var beitragsjahr = beginnDatum.Year;
            var erklaerungsteile = new[]
            {
                $"Ich beantrage die Mitgliedschaft im {ResolveVereinName(bankverbindungSnapshot)}.",
                "Die gemachten Angaben sind nach meinem Kenntnisstand vollständig und richtig.",
                Safe(bankverbindungSnapshot.StandardHinweistext)
            };
            var datenschutzMeta = BuildDatenschutzMeta(bankverbindungSnapshot);
            var datenschutzteile = new[]
            {
                Safe(bankverbindungSnapshot.DatenschutzText),
                datenschutzMeta
            };

            return new MitgliedsantragTemplateData
            {
                BodyClass = istMinderjaehrig ? "minor" : string.Empty,
                VereinName = ResolveVereinName(bankverbindungSnapshot),
                VereinRegisterangabe = Safe(bankverbindungSnapshot.VereinRegisterangabe),
                VereinEmail = Safe(bankverbindungSnapshot.VereinEmail),
                Ausstellungsdatum = FormatDate(DateTime.Today),
                MitgliedName = Safe(member.Name),
                MitgliedVorname = Safe(member.Vorname),
                MitgliedGeburtsdatum = FormatDate(member.Geburtsdatum),
                MitgliedAufnahmeAb = FormatDate(beginnDatum),
                MitgliedAnschriftMehrzeilig = BuildAddressMultiline(member.Adresse, member.Plz, member.Ort),
                MitgliedTelefon = SafeContact(member.Telefon),
                MitgliedMobil = SafeContact(member.Handy),
                MitgliedEmail = SafeContact(member.Email),
                CheckWhatsapp = member.WhatsappEinwilligung ? "true" : string.Empty,
                CheckRechnungMail = member.EmailRechnungEinwilligung ? "true" : string.Empty,
                CheckInfoMail = member.EmailInfoEinwilligung ? "true" : string.Empty,
                VertreterName = gesetzlicherVertreterSnapshot == null ? string.Empty : Safe(gesetzlicherVertreterSnapshot.Nachname),
                VertreterVorname = gesetzlicherVertreterSnapshot == null ? string.Empty : Safe(gesetzlicherVertreterSnapshot.Vorname),
                VertreterAnschriftMehrzeilig = gesetzlicherVertreterSnapshot == null ? string.Empty : BuildAddressMultiline(gesetzlicherVertreterSnapshot.Adresse, gesetzlicherVertreterSnapshot.Plz, gesetzlicherVertreterSnapshot.Ort),
                VertreterTelefon = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Telefon),
                VertreterMobil = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Handy),
                VertreterEmail = gesetzlicherVertreterSnapshot == null ? string.Empty : SafeContact(gesetzlicherVertreterSnapshot.Email),
                MitgliedsbeitragJaehrlich = FormatCurrency(mitgliedsbeitrag),
                Aufnahmegebuehr = FormatCurrency(aufnahmegebuehr),
                BeitragHinweis1 = BuildBeitragsHinweis(beitragsjahr, mitgliedsbeitrag, beginnDatum),
                BeitragHinweis2 = $"Die einmalige Aufnahmegebühr beträgt {FormatCurrency(aufnahmegebuehr)}.",
                BankKontoinhaber = Safe(bankverbindungSnapshot.Kontoinhaber),
                BankName = Safe(bankverbindungSnapshot.Bankname),
                BankIban = Safe(bankverbindungSnapshot.Iban),
                BankBic = Safe(bankverbindungSnapshot.Bic),
                Erklaerungstext = JoinParagraphs(erklaerungsteile),
                Datenschutztext = JoinParagraphs(datenschutzteile),
                Fussnote = BuildFussnote(bankverbindungSnapshot)
            };
        }

        private static byte[] BuildTemplatePdf(string dokumentTitel, string renderedHtml)
            => MitgliedsantragHtmlPdfRenderer.Build(dokumentTitel, renderedHtml);

        private static void DrawHeader(PdfDocument document, PdfPage page, XGraphics graphics, XFont titleFont, XFont subtitleFont, XFont labelFont, XFont bodyFont, XPen borderPen, XPen accentPen, MitgliedsantragTemplateData data, ref double cursorY)
        {
            using var logo = XImage.FromStream(() => new MemoryStream(VereinsdokumentBranding.GetLogoBytes(), writable: false));
            var contentWidth = page.Width.Point - PageMargin * 2;
            var docBoxWidth = 145d;
            var textWidth = contentWidth - docBoxWidth - 18d;
            graphics.DrawImage(logo, PageMargin, cursorY + 2, HeaderLogoSize, HeaderLogoSize);

            var textX = PageMargin + HeaderLogoSize + 14d;
            var textY = cursorY + 4d;
            graphics.DrawString(data.VereinName, titleFont, XBrushes.Black, new XRect(textX, textY, textWidth - HeaderLogoSize - 14d, 24), XStringFormats.TopLeft);
            graphics.DrawString(data.VereinRegisterangabe, subtitleFont, XBrushes.DimGray, new XRect(textX, textY + 24, textWidth, 16), XStringFormats.TopLeft);
            graphics.DrawString($"E-Mail: {data.VereinEmail}", subtitleFont, XBrushes.DimGray, new XRect(textX, textY + 40, textWidth, 16), XStringFormats.TopLeft);

            var docBoxX = page.Width.Point - PageMargin - docBoxWidth;
            var docBoxRect = new XRect(docBoxX, cursorY, docBoxWidth, 58d);
            graphics.DrawRectangle(XBrushes.WhiteSmoke, docBoxRect);
            graphics.DrawRectangle(borderPen, docBoxRect);
            graphics.DrawString("Ausstellungsdatum", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 8, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
            graphics.DrawString(data.Ausstellungsdatum, bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 22, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);
            graphics.DrawString("Dokument", labelFont, XBrushes.DimGray, new XRect(docBoxRect.X + 10, docBoxRect.Y + 36, docBoxRect.Width - 20, 12), XStringFormats.TopLeft);
            graphics.DrawString("Mitgliedsantrag", bodyFont, XBrushes.Black, new XRect(docBoxRect.X + 10, docBoxRect.Y + 49, docBoxRect.Width - 20, 14), XStringFormats.TopLeft);

            cursorY += Math.Max(HeaderLogoSize, docBoxRect.Height) + 8d;
            graphics.DrawLine(accentPen, PageMargin, cursorY, page.Width.Point - PageMargin, cursorY);
            cursorY += 14d;
            graphics.DrawString("Mitgliedsantrag", titleFont, XBrushes.Black, new XRect(PageMargin, cursorY, contentWidth, 24), XStringFormats.TopLeft);
            cursorY += 24d;
            graphics.DrawString("Antrag auf Aufnahme in den Kleingartenverein Oberrothenbach e.V.", subtitleFont, XBrushes.DimGray, new XRect(PageMargin, cursorY, contentWidth, 16), XStringFormats.TopLeft);
            cursorY += 22d;
        }

        private static void DrawLead(XGraphics graphics, XFont bodyFont, MitgliedsantragTemplateData data, ref double cursorY)
        {
            graphics.DrawString("Hiermit beantrage ich die Aufnahme in den Kleingartenverein Oberrothenbach e.V.", bodyFont, XBrushes.Black, new XRect(PageMargin, cursorY, 520, 16), XStringFormats.TopLeft);
            cursorY += 24d;
        }

        private static void DrawPersonSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont boxFont, XFont bodyFont, XPen borderPen, XPen accentPen, MitgliedsantragTemplateData data, ref double cursorY, bool vertreter)
        {
            var title = vertreter ? "2. Gesetzliche Vertretung" : "1. Angaben zur antragstellenden Person";
            var fields = vertreter
                ? new[]
                {
                    new TemplateField("Name", data.VertreterName),
                    new TemplateField("Vorname", data.VertreterVorname),
                    new TemplateField("Anschrift", data.VertreterAnschriftMehrzeilig, true, true),
                    new TemplateField("Telefon", data.VertreterTelefon),
                    new TemplateField("Mobil", data.VertreterMobil),
                    new TemplateField("E-Mail", data.VertreterEmail, true)
                }
                : new[]
                {
                    new TemplateField("Name", data.MitgliedName),
                    new TemplateField("Vorname", data.MitgliedVorname),
                    new TemplateField("Geburtsdatum", data.MitgliedGeburtsdatum),
                    new TemplateField("Aufnahme ab", data.MitgliedAufnahmeAb),
                    new TemplateField("Anschrift", data.MitgliedAnschriftMehrzeilig, true, true),
                    new TemplateField("Telefon", data.MitgliedTelefon),
                    new TemplateField("Mobil", data.MitgliedMobil),
                    new TemplateField("E-Mail", data.MitgliedEmail, true),
                    new TemplateField("Kommunikation", BuildCommunicationValue(data), true, true)
                };

            var estimatedHeight = EstimateFieldSectionHeight(fields);
            EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
            var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, title, ref cursorY, estimatedHeight - SectionSpacing);
            DrawFieldGrid(graphics, labelFont, boxFont, bodyFont, borderPen, sectionRect, fields);
            cursorY = sectionRect.Bottom + SectionSpacing;
        }

        private static void DrawPaymentSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont boxFont, XFont bodyFont, XFont cardValueFont, XPen borderPen, XPen accentPen, MitgliedsantragTemplateData data, ref double cursorY)
        {
            const double estimatedHeight = 158d;
            EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
            var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, "3. Beiträge und Zahlungen", ref cursorY, estimatedHeight - SectionSpacing);
            var innerRect = new XRect(sectionRect.X + 12, sectionRect.Y + 34, sectionRect.Width - 24, sectionRect.Height - 46);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 251, 246)), innerRect);
            graphics.DrawRectangle(borderPen, innerRect);

            var cardGap = 10d;
            var cardWidth = (innerRect.Width - cardGap) / 2d;
            DrawPaymentCard(graphics, labelFont, cardValueFont, borderPen, new XRect(innerRect.X + 6, innerRect.Y + 8, cardWidth - 6, 48), "Mitgliedsbeitrag jährlich", data.MitgliedsbeitragJaehrlich);
            DrawPaymentCard(graphics, labelFont, cardValueFont, borderPen, new XRect(innerRect.X + cardWidth + cardGap, innerRect.Y + 8, cardWidth - 6, 48), "Aufnahmegebühr", data.Aufnahmegebuehr);
            graphics.DrawString(data.BeitragHinweis1, bodyFont, XBrushes.Black, new XRect(innerRect.X + 6, innerRect.Y + 66, innerRect.Width - 12, 18), XStringFormats.TopLeft);
            graphics.DrawString(data.BeitragHinweis2, bodyFont, XBrushes.Black, new XRect(innerRect.X + 6, innerRect.Y + 86, innerRect.Width - 12, 28), XStringFormats.TopLeft);
            cursorY = sectionRect.Bottom + SectionSpacing;
        }

        private static void DrawBankSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont labelFont, XFont boxFont, XPen borderPen, MitgliedsantragTemplateData data, ref double cursorY)
        {
            var fields = new[]
            {
                new TemplateField("Kontoinhaber", data.BankKontoinhaber),
                new TemplateField("Bank", data.BankName),
                new TemplateField("IBAN", data.BankIban),
                new TemplateField("BIC", data.BankBic)
            };
            var estimatedHeight = EstimateFieldSectionHeight(fields, 2, false);
            EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
            var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, "4. Bankverbindung des Vereins", ref cursorY, estimatedHeight - SectionSpacing);
            DrawFieldGrid(graphics, labelFont, boxFont, boxFont, borderPen, sectionRect, fields, false);
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

        private static void DrawSignatureSection(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont sectionTitleFont, XFont bodyFont, XFont boxFont, XPen borderPen, XPen accentPen, string title, MitgliedsantragTemplateData data, bool datenschutz, ref double cursorY)
        {
            var istMinderjaehrig = string.Equals(data.BodyClass, "minor", StringComparison.Ordinal);
            var estimatedHeight = istMinderjaehrig ? 236d : 190d;
            if (datenschutz)
                estimatedHeight += 24d;

            EnsureSpace(document, ref page, ref graphics, ref cursorY, estimatedHeight);
            var sectionRect = DrawSectionShell(graphics, page, borderPen, XBrushes.WhiteSmoke, sectionTitleFont, title, ref cursorY, estimatedHeight - SectionSpacing);
            var metaTop = sectionRect.Y + 36d;
            var metaWidth = (sectionRect.Width - 32d) / 2d;
            DrawLineField(graphics, bodyFont, accentPen, sectionRect.X + 12d, metaTop, metaWidth, "Ort");
            DrawLineField(graphics, bodyFont, accentPen, sectionRect.X + 20d + metaWidth, metaTop, metaWidth, "Datum");

            var boxTop = metaTop + 34d;
            var gap = 10d;
            var topBoxWidth = istMinderjaehrig ? (sectionRect.Width - 24d - gap) / 2d : sectionRect.Width - 24d;
            DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d, boxTop, topBoxWidth, 76d), datenschutz ? "Unterschrift Antragsteller/in zur Datenschutzerklärung" : "Unterschrift Antragsteller/in");
            if (istMinderjaehrig)
            {
                DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d + topBoxWidth + gap, boxTop, topBoxWidth, 76d), datenschutz ? "Unterschrift gesetzliche/r Vertreter/in zur Datenschutzerklärung" : "Unterschrift gesetzliche/r Vertreter/in");
            }

            if (!datenschutz)
            {
                var bottomWidth = istMinderjaehrig ? topBoxWidth : sectionRect.Width - 24d;
                DrawSignatureBox(graphics, boxFont, borderPen, new XRect(sectionRect.X + 12d, boxTop + 94d, bottomWidth, 76d), "Für den Verein");
            }
            else
            {
                graphics.DrawString("Mit der Unterschrift wird bestätigt, dass die Datenschutzerklärung zur Kenntnis genommen wurde.", bodyFont, XBrushes.DimGray, new XRect(sectionRect.X + 12d, boxTop + 94d, sectionRect.Width - 24d, 22d), XStringFormats.TopLeft);
            }

            cursorY = sectionRect.Bottom + SectionSpacing;
        }

        private static void DrawFooter(PdfDocument document, ref PdfPage page, ref XGraphics graphics, XFont footerFont, MitgliedsantragTemplateData data, ref double cursorY)
        {
            EnsureSpace(document, ref page, ref graphics, ref cursorY, 24d);
            graphics.DrawString(data.Fussnote, footerFont, XBrushes.DimGray, new XRect(PageMargin, cursorY, page.Width.Point - PageMargin * 2, 18), XStringFormats.TopCenter);
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

        private static void DrawFieldGrid(XGraphics graphics, XFont labelFont, XFont valueFont, XFont bodyFont, XPen borderPen, XRect sectionRect, IReadOnlyList<TemplateField> fields, bool tallCommunication = true)
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
                    var height = GetFieldHeight(field, bodyFont, tallCommunication);
                    DrawField(graphics, labelFont, valueFont, borderPen, new XRect(contentX, currentY, contentWidth, height), field);
                    currentY += height + gapY;
                    index++;
                    continue;
                }

                var secondField = index + 1 < fields.Count && !fields[index + 1].FullWidth ? fields[index + 1] : null;
                var firstHeight = GetFieldHeight(field, bodyFont, tallCommunication);
                var secondHeight = secondField == null ? firstHeight : GetFieldHeight(secondField, bodyFont, tallCommunication);
                var rowHeight = Math.Max(firstHeight, secondHeight);
                DrawField(graphics, labelFont, valueFont, borderPen, new XRect(contentX, currentY, columnWidth, rowHeight), field);
                if (secondField != null)
                    DrawField(graphics, labelFont, valueFont, borderPen, new XRect(contentX + columnWidth + gapX, currentY, columnWidth, rowHeight), secondField);
                currentY += rowHeight + gapY;
                index += secondField == null ? 1 : 2;
            }
        }

        private static void DrawField(XGraphics graphics, XFont labelFont, XFont valueFont, XPen borderPen, XRect rect, TemplateField field)
        {
            graphics.DrawString(field.Label, labelFont, XBrushes.DimGray, new XRect(rect.X, rect.Y, rect.Width, 12), XStringFormats.TopLeft);
            var boxRect = new XRect(rect.X, rect.Y + 14d, rect.Width, rect.Height - 14d);
            graphics.DrawRectangle(XBrushes.White, boxRect);
            graphics.DrawRectangle(borderPen, boxRect);
            var valueY = boxRect.Y + 8d;
            foreach (var line in SplitLines(string.IsNullOrWhiteSpace(field.Value) ? "-" : field.Value))
            {
                graphics.DrawString(line, valueFont, XBrushes.Black, new XRect(boxRect.X + 8d, valueY, boxRect.Width - 16d, 14d), XStringFormats.TopLeft);
                valueY += 13d;
            }
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

        private static double EstimateFieldSectionHeight(IReadOnlyList<TemplateField> fields, int columns = 2, bool tallCommunication = true)
        {
            const double gapY = 8d;
            double height = 42d;
            int index = 0;
            while (index < fields.Count)
            {
                var field = fields[index];
                if (field.FullWidth)
                {
                    height += GetFieldHeight(field, null, tallCommunication) + gapY;
                    index++;
                    continue;
                }

                var secondField = index + 1 < fields.Count && !fields[index + 1].FullWidth ? fields[index + 1] : null;
                var rowHeight = Math.Max(GetFieldHeight(field, null, tallCommunication), secondField == null ? 0d : GetFieldHeight(secondField, null, tallCommunication));
                height += rowHeight + gapY;
                index += secondField == null ? 1 : Math.Min(columns, 2);
            }
            return height + 4d;
        }

        private static double GetFieldHeight(TemplateField field, XFont? bodyFont, bool tallCommunication)
        {
            var lines = Math.Max(1, SplitLines(string.IsNullOrWhiteSpace(field.Value) ? "-" : field.Value).Count);
            var baseHeight = 14d + Math.Max(field.Large ? 42d : 28d, lines * 13d + 14d);
            if (tallCommunication && string.Equals(field.Label, "Kommunikation", StringComparison.Ordinal))
                return Math.Max(baseHeight, 66d);
            return baseHeight;
        }

        private static List<string> SplitLines(string? text)
            => (text ?? string.Empty)
                .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(x => string.IsNullOrWhiteSpace(x) ? string.Empty : x.Trim())
                .ToList();

        private static string BuildCommunicationValue(MitgliedsantragTemplateData data)
        {
            var values = new List<string>();
            if (string.Equals(data.CheckWhatsapp, "true", StringComparison.OrdinalIgnoreCase))
                values.Add("✓ WhatsApp");
            if (string.Equals(data.CheckRechnungMail, "true", StringComparison.OrdinalIgnoreCase))
                values.Add("✓ Rechnung per Mail");
            if (string.Equals(data.CheckInfoMail, "true", StringComparison.OrdinalIgnoreCase))
                values.Add("✓ Info per Mail");
            return values.Count == 0 ? "Keine Einwilligungen hinterlegt." : string.Join("\n", values);
        }

        private static string LoadTemplateHtml()
        {
            var assembly = typeof(MitgliedsantragDokumentFactory).Assembly;
            using var stream = assembly.GetManifestResourceStream(TemplateResourceName)
                ?? throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage ist nicht im Projekt eingebunden.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void EnsureTemplateFullyRendered(string renderedHtml)
        {
            if (string.IsNullOrWhiteSpace(renderedHtml))
                throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage konnte nicht gerendert werden.");
            if (renderedHtml.Contains("{{", StringComparison.Ordinal) || renderedHtml.Contains("}}", StringComparison.Ordinal))
                throw new InvalidOperationException("Die Mitgliedsantrag-HTML-Vorlage enthält noch nicht aufgelöste Platzhalter.");
        }

        private static string BuildAddressMultiline(string? adresse, string? plz, string? ort)
        {
            var street = SafeContact(adresse);
            var city = string.Join(" ", new[] { plz, ort }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
            if (string.IsNullOrWhiteSpace(city))
                return street;
            if (string.IsNullOrWhiteSpace(street))
                return city;
            return $"{street}\n{city}";
        }

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatCurrency(decimal value)
            => MitgliedsantragBeitragHelper.NormalizeBeitrag(value).ToString("0.00 €", CultureInfo.GetCultureInfo("de-DE"));

        private static string Safe(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string SafeContact(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string ResolveVereinName(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var value = Safe(snapshot.VereinName);
            return string.IsNullOrWhiteSpace(value) ? VereinsdokumentBranding.VereinsName : value;
        }

        private static string BuildDatenschutzMeta(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(snapshot.DatenschutzVersion))
                parts.Add($"Version {snapshot.DatenschutzVersion.Trim()}");
            if (snapshot.DatenschutzStand.HasValue)
                parts.Add($"Stand {FormatDate(snapshot.DatenschutzStand)}");
            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string BuildBeitragsHinweis(int beitragsjahr, decimal jahresbeitrag, DateTime beginnDatum)
        {
            if (beginnDatum.Year != beitragsjahr)
                return $"Der Mitgliedsbeitrag beträgt für das Beitragsjahr {beitragsjahr} {FormatCurrency(jahresbeitrag)} und ist jährlich zu zahlen.";

            var monate = 12 - beginnDatum.Month + 1; // inkl. Eintrittsmonat
            if (monate < 1) monate = 1;
            var anteil = MitgliedsantragBeitragHelper.NormalizeBeitrag(jahresbeitrag * ((decimal)monate / 12m));
            return $"Jahresbeitrag: {FormatCurrency(jahresbeitrag)} · Aufnahmejahr ({monate} Monate: {beginnDatum:dd.MM.yyyy}–31.12.{beitragsjahr}): anteiliger Beitrag {FormatCurrency(anteil)}.";
        }

        private static string BuildFussnote(MitgliedsantragBankverbindungSnapshot snapshot)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(snapshot.DokumentOrt))
                parts.Add($"Dokument-Ort: {snapshot.DokumentOrt.Trim()}");
            if (!string.IsNullOrWhiteSpace(snapshot.VerwendungszweckMitgliedsantrag))
                parts.Add($"Verwendungszweck: {snapshot.VerwendungszweckMitgliedsantrag.Trim()}");
            return parts.Count == 0 ? "Mitgliedsantrag des Vereins" : string.Join(" · ", parts);
        }

        private static string JoinParagraphs(IEnumerable<string?> parts)
            => string.Join("\n\n", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        private sealed record TemplateField(string Label, string Value, bool FullWidth = false, bool Large = false);
    }
}
