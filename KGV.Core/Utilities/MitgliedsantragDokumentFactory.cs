using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using KGV.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Xml.Linq;
using System.Diagnostics;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.Annotations;

namespace KGV.Core.Utilities
{
    public static class MitgliedsantragDokumentFactory
    {
        private const string PdfTemplateResourceName = "KGV.Core.Templates.Mitgliedsantrag_Vorlage_Formularfelder.pdf";
        private const double PageMargin = 24d;
        private const double SectionSpacing = 8d;
        private const double HeaderLogoSize = 48d;

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

            // Load embedded PDF form template and fill AcroForm fields.
            var pdfTemplate = LoadTemplatePdfBytes();
            if (pdfTemplate == null || pdfTemplate.Length == 0)
                throw new InvalidOperationException("Die Mitgliedsantrag-PDF-Vorlage ist nicht im Projekt eingebunden oder nicht auffindbar.");

            var filledPdf = FillPdfForm(pdfTemplate, templateData);

            return new DokumentUploadRequest
            {
                MitgliedId = member.Id,
                Titel = title,
                FileName = fileName, // keep .pdf
                MimeType = "application/pdf",
                FileContent = filledPdf
            };
        }

        private static string DecodePdfFieldName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw ?? string.Empty;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '\\' && i + 1 < raw.Length)
                {
                    int j = i + 1;
                    int end = Math.Min(j + 3, raw.Length);
                    int val = 0;
                    int digits = 0;
                    while (j < end && raw[j] >= '0' && raw[j] <= '7')
                    {
                        val = val * 8 + (raw[j] - '0');
                        j++; digits++;
                    }
                    if (digits > 0)
                    {
                        sb.Append((char)val);
                        i = j - 1;
                        continue;
                    }
                    sb.Append(raw[i + 1]);
                    i++;
                    continue;
                }
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        private static (string onState, string offState) DetectOnOffStateFromElements(object? elements)
        {
            if (elements == null) return ("Yes", "Off");
            try
            {
                var dump = elements.ToString() ?? string.Empty;
                var rx = new System.Text.RegularExpressions.Regex(@"/([A-Za-z0-9_]+)");
                var matches = rx.Matches(dump).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
                var preferred = new[] { "Yes", "On", "1" };
                var on = matches.FirstOrDefault(t => preferred.Contains(t)) ?? matches.FirstOrDefault(t => !string.Equals(t, "Off", StringComparison.OrdinalIgnoreCase)) ?? "Yes";
                var off = matches.FirstOrDefault(t => string.Equals(t, "Off", StringComparison.OrdinalIgnoreCase)) ?? "Off";
                return (on, off);
            }
            catch { return ("Yes", "Off"); }
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



        private static byte[] LoadTemplateDocxBytes()
        {
            throw new NotSupportedException("LoadTemplateDocxBytes is no longer supported. The member application uses a PDF form template.");
        }

        private static byte[] LoadTemplatePdfBytes()
        {
            var assembly = typeof(MitgliedsantragDokumentFactory).Assembly;
            var stream = assembly.GetManifestResourceStream(PdfTemplateResourceName)
                         ?? throw new InvalidOperationException("Die Mitgliedsantrag-PDF-Vorlage ist nicht im Projekt eingebunden.");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static byte[] FillPdfForm(byte[] pdfBytes, MitgliedsantragTemplateData data)
        {
            // Try to open PDF and set AcroForm fields by name. This relies on PdfSharpCore's AcroForm support.
            using var inMs = new MemoryStream();
            inMs.Write(pdfBytes, 0, pdfBytes.Length);
            inMs.Position = 0;

            using var document = PdfReader.Open(inMs, PdfDocumentOpenMode.Modify);

            var form = document.AcroForm;
            if (form == null)
                throw new InvalidOperationException("Die PDF-Vorlage enthält kein AcroFormular (keine Formularfelder).");

            // Map expected field names to values (match the corrected PDF template field list)
            // Compute beitragsmonate and mitgliedsbeitrag_anteilig from the formatted strings
            int beitragsmonate = 0;
            decimal jahresbeitrag = 0m;
            decimal mitgliedsbeitragAnteilig = 0m;
            try
            {
                // parse AufnahmeAb date (expected format dd.MM.yyyy)
                if (DateTime.TryParseExact(data.MitgliedAufnahmeAb, "dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var beginnDate))
                {
                    beitragsmonate = 12 - beginnDate.Month + 1;
                    if (beitragsmonate < 1) beitragsmonate = 1;
                }
                // parse yearly contribution like "90,00 €"
                var style = NumberStyles.Currency | NumberStyles.AllowThousands;
                decimal.TryParse(data.MitgliedsbeitragJaehrlich, style, CultureInfo.GetCultureInfo("de-DE"), out jahresbeitrag);
                mitgliedsbeitragAnteilig = Math.Round(jahresbeitrag * ((decimal)beitragsmonate / 12m), 2);
            }
            catch { beitragsmonate = 0; jahresbeitrag = 0m; mitgliedsbeitragAnteilig = 0m; }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ausstellungsdatum"] = data.Ausstellungsdatum,
                ["dokument_ort"] = (data.Fussnote ?? string.Empty),
                ["mitglied_name"] = data.MitgliedName,
                ["mitglied_vorname"] = data.MitgliedVorname,
                ["mitglied_geburtsdatum"] = data.MitgliedGeburtsdatum,
                ["mitglied_aufnahme_ab"] = data.MitgliedAufnahmeAb,
                ["mitglied_telefon"] = data.MitgliedTelefon,
                ["mitglied_mobil"] = data.MitgliedMobil,
                ["mitglied_email"] = data.MitgliedEmail,
                ["mitglied_anschrift_mehrzeilig"] = data.MitgliedAnschriftMehrzeilig,
                ["check_whatsapp"] = string.Equals(data.CheckWhatsapp, "true", StringComparison.OrdinalIgnoreCase) ? "true" : string.Empty,
                ["check_rechnung_mail"] = string.Equals(data.CheckRechnungMail, "true", StringComparison.OrdinalIgnoreCase) ? "true" : string.Empty,
                ["check_info_mail"] = string.Equals(data.CheckInfoMail, "true", StringComparison.OrdinalIgnoreCase) ? "true" : string.Empty,
                ["vertreter_name"] = data.VertreterName,
                ["vertreter_vorname"] = data.VertreterVorname,
                ["vertreter_geburtsdatum"] = string.Empty,
                ["vertreter_telefon"] = data.VertreterTelefon,
                ["vertreter_mobil"] = data.VertreterMobil,
                ["vertreter_email"] = data.VertreterEmail,
                ["vertreter_anschrift_mehrzeilig"] = data.VertreterAnschriftMehrzeilig,
                ["mitgliedsbeitrag_jaehrlich"] = data.MitgliedsbeitragJaehrlich,
                ["mitgliedsbeitrag_anteilig"] = mitgliedsbeitragAnteilig.ToString("0.00 €", CultureInfo.GetCultureInfo("de-DE")),
                ["beitragsmonate"] = beitragsmonate.ToString(),
                ["aufnahmegebuehr"] = data.Aufnahmegebuehr,
                ["bank_kontoinhaber"] = data.BankKontoinhaber,
                ["bank_name"] = data.BankName,
                ["bank_iban"] = data.BankIban,
                ["bank_bic"] = data.BankBic,
                ["unterschrift_ort"] = string.Empty,
                ["unterschrift_datum"] = data.Ausstellungsdatum,
                ["unterschrift_antragsteller"] = string.Empty,
                ["unterschrift_vertreter"] = string.Empty,
                ["unterschrift_verein"] = string.Empty,
                ["datenschutz_unterschrift_antragsteller"] = string.Empty,
                ["datenschutz_unterschrift_vertreter"] = string.Empty
            };

            // Iterate fields and attempt to set values using reflection to remain compatible with PdfSharpCore internals.
            var logLines = new List<string>();
            var fieldNames = new List<string>();
            foreach (var fieldObj in form.Fields)
            {
                // local helpers
                static string DecodePdfFieldNameLocal(string raw)
                {
                    if (string.IsNullOrEmpty(raw)) return raw ?? string.Empty;
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < raw.Length; i++)
                    {
                        if (raw[i] == '\\' && i + 1 < raw.Length)
                        {
                            int j = i + 1;
                            int end = Math.Min(j + 3, raw.Length);
                            int val = 0;
                            int digits = 0;
                            while (j < end && raw[j] >= '0' && raw[j] <= '7')
                            {
                                val = val * 8 + (raw[j] - '0');
                                j++; digits++;
                            }
                            if (digits > 0)
                            {
                                sb.Append((char)val);
                                i = j - 1;
                                continue;
                            }
                            sb.Append(raw[i + 1]);
                            i++;
                            continue;
                        }
                        sb.Append(raw[i]);
                    }
                    return sb.ToString();
                }

                static (string onState, string offState) DetectOnOffStateFromElementsLocal(object? elements)
                {
                    if (elements == null) return ("Yes", "Off");
                    try
                    {
                        var dump = elements.ToString() ?? string.Empty;
                        var rx = new System.Text.RegularExpressions.Regex(@"/([A-Za-z0-9_]+)");
                        var matches = rx.Matches(dump).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
                        var preferred = new[] { "Yes", "On", "1" };
                        var on = matches.FirstOrDefault(t => preferred.Contains(t)) ?? matches.FirstOrDefault(t => !string.Equals(t, "Off", StringComparison.OrdinalIgnoreCase)) ?? "Yes";
                        var off = matches.FirstOrDefault(t => string.Equals(t, "Off", StringComparison.OrdinalIgnoreCase)) ?? "Off";
                        return (on, off);
                    }
                    catch { return ("Yes", "Off"); }
                }
                try
                {
                    var fieldName = string.Empty;
                    try
                    {
                        var nameProp = fieldObj.GetType().GetProperty("Name");
                        if (nameProp != null)
                        {
                            fieldName = nameProp.GetValue(fieldObj)?.ToString() ?? string.Empty;
                        }
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(fieldName))
                    {
                        try
                        {
                            var elementsProp = fieldObj.GetType().GetProperty("Elements");
                            var elements = elementsProp?.GetValue(fieldObj);
                            var getString = elements?.GetType().GetMethod("GetString", new[] { typeof(string) });
                            var t = getString?.Invoke(elements, new object[] { "/T" })?.ToString();
                            fieldName = t ?? string.Empty;
                        }
                        catch { }
                    }

                    if (string.IsNullOrWhiteSpace(fieldName))
                        continue;

                    // Resolve value: prefer exact field name, otherwise try decoded PDF name
                    string value;
                    if (!map.TryGetValue(fieldName, out var tmp))
                    {
                        var decoded = DecodePdfFieldNameLocal(fieldName);
                        if (!string.IsNullOrWhiteSpace(decoded) && map.TryGetValue(decoded, out var tmp2))
                            value = tmp2 ?? string.Empty;
                        else
                            value = string.Empty;
                    }
                    else
                    {
                        value = tmp ?? string.Empty;
                    }

                    fieldNames.Add(fieldName);
                    var decodedForLog = DecodePdfFieldNameLocal(fieldName);
                    logLines.Add($"TemplateFieldRaw='{fieldName}' Decoded='{decodedForLog}' MappingHasKey={map.ContainsKey(decodedForLog)}");

                    // Try to set a Value property
                    var type = fieldObj.GetType();
                    var setSuccess = false;

                    // Try to set Value property (text fields)
                    try
                    {
                        var valueProp = type.GetProperty("Value");
                        if (valueProp != null)
                        {
                            var pdfStringType = typeof(PdfSharpCore.Pdf.PdfString);
                            var ctor = pdfStringType.GetConstructor(new[] { typeof(string) });
                            var pdfStr = ctor?.Invoke(new object[] { value ?? string.Empty });
                            valueProp.SetValue(fieldObj, pdfStr);
                            setSuccess = true;
                        }
                    }
                    catch { }

                    // Try Checked property for checkboxes/radio
                    try
                    {
                        var checkedProp = type.GetProperty("Checked");
                        if (!setSuccess && checkedProp != null)
                        {
                            var isChecked = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
                            checkedProp.SetValue(fieldObj, isChecked);
                            setSuccess = true;
                        }
                    }
                    catch { }

                    // Fallback: write into Elements /V and for buttons also set /AS
                    try
                    {
                        var elementsProp = type.GetProperty("Elements");
                        var elements = elementsProp?.GetValue(fieldObj);
                        var setString = elements?.GetType().GetMethod("SetString", new[] { typeof(string), typeof(string) });
                        // write raw value into /V for text fields
                        setString?.Invoke(elements, new object[] { "/V", value ?? string.Empty });

                        // also set appearance state for buttons: detect on/off state and use exact token
                        try
                        {
                            var ft = (elements?.GetType().GetMethod("GetString", new[] { typeof(string) })?.Invoke(elements, new object[] { "/FT" }) ?? string.Empty)?.ToString();
                            if (!string.IsNullOrWhiteSpace(ft) && ft.Contains("Btn"))
                            {
                                var (onState, offState) = DetectOnOffStateFromElements(elements);
                                var isChecked = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
                                var useState = isChecked ? onState : offState;
                                try { setString?.Invoke(elements, new object[] { "/AS", useState }); } catch { }
                                try { setString?.Invoke(elements, new object[] { "/V", useState }); } catch { }
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
                catch
                {
                    // ignore individual field failures but continue
                }
            }

            // write discovered template field names and mapping keys for diagnosis
            try
            {
                var basePath = Environment.CurrentDirectory;
                var f1 = Path.Combine(basePath, "Mitgliedsantrag_fieldnames_log.txt");
                File.AppendAllLines(f1, new[] { "--- Template field names: " + DateTime.UtcNow.ToString("o") }.Concat(fieldNames));
                var f2 = Path.Combine(basePath, "Mitgliedsantrag_mapping_keys.txt");
                File.AppendAllLines(f2, new[] { "--- Mapping keys: " + DateTime.UtcNow.ToString("o") }.Concat(map.Keys));
            }
            catch { }

            // Ensure the form is marked as needing appearance updates
            try
            {
                form.Elements.SetBoolean("/NeedAppearances", true);
            }
            catch
            {
                // ignore
            }

            // Save intermediate PDF (after setting field dictionary values) so we can inspect written /V and /AS programmatically
            try
            {
                using var inspectMs = new MemoryStream();
                document.Save(inspectMs);
                var inspectBytes = inspectMs.ToArray();
                var inspectPath = Path.Combine(Environment.CurrentDirectory, "Mitgliedsantrag_afterset.pdf");
                File.WriteAllBytes(inspectPath, inspectBytes);

                // Re-open read-only to inspect field dictionaries
                using var inspectDoc = PdfReader.Open(new MemoryStream(inspectBytes), PdfDocumentOpenMode.ReadOnly);
                var inspectForm = inspectDoc.AcroForm;
                var inspectLines = new List<string> { "--- AfterSet inspection: " + DateTime.UtcNow.ToString("o") };
                if (inspectForm != null)
                {
                    foreach (var f in inspectForm.Fields)
                    {
                        try
                        {
                            var name = string.Empty;
                            try { name = f.GetType().GetProperty("Name")?.GetValue(f)?.ToString() ?? string.Empty; } catch { }
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                try { name = f.GetType().GetProperty("Elements")?.GetValue(f)?.ToString() ?? string.Empty; } catch { }
                            }
                            var elements = f.GetType().GetProperty("Elements")?.GetValue(f);
                            string v = string.Empty;
                            try { v = elements?.GetType().GetMethod("GetString", new[] { typeof(string) })?.Invoke(elements, new object[] { "/V" })?.ToString() ?? string.Empty; } catch { }
                            string asv = string.Empty;
                            try { asv = elements?.GetType().GetMethod("GetString", new[] { typeof(string) })?.Invoke(elements, new object[] { "/AS" })?.ToString() ?? string.Empty; } catch { }
                            inspectLines.Add($"Field: {name} /V='{v}' /AS='{asv}'");
                        }
                        catch { }
                    }
                }
                else
                {
                    inspectLines.Add("No AcroForm in intermediate PDF");
                }
                var inspectTxt = Path.Combine(Environment.CurrentDirectory, "Mitgliedsantrag_afterset_inspect.txt");
                File.AppendAllLines(inspectTxt, inspectLines);
            }
            catch
            {
                // ignore inspection failures
            }

            // Flatten fields into page content so values are visually present in all viewers.
            try
            {
                FlattenFormFieldsToPageContent(document, map);
            }
            catch
            {
                // if flattening fails, proceed with saved PDF (viewers may still render via NeedAppearances)
            }

            using var outMs = new MemoryStream();
            document.Save(outMs);
            return outMs.ToArray();
        }

        private static void FlattenFormFieldsToPageContent(PdfDocument document, Dictionary<string, string> map)
        {
            if (document == null) return;
            var logLines = new List<string>();
            // New flattening strategy: iterate pages and page.Annotations directly and draw values from mapping
            var baseFont = new XFont("Arial", 10, XFontStyle.Regular);
            var checkFont = new XFont("Arial", 12, XFontStyle.Regular);
            var pages = document.Pages;
            for (int pi = 0; pi < pages.Count; pi++)
            {
                var page = pages[pi];
                var annots = page.Annotations;
                if (annots == null || annots.Count == 0) continue;
                // collect annotations to remove after drawing
                var toRemove = new List<PdfAnnotation>();
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                for (int ai = annots.Count - 1; ai >= 0; ai--)
                {
                    try
                    {
                        var annotItem = annots[ai];
                        var annot = annotItem as PdfAnnotation;
                        if (annot == null) continue;
                        var elements = annot.GetType().GetProperty("Elements")?.GetValue(annot);
                        if (elements == null) continue;
                        // read /T
                        string rawName = string.Empty;
                        try { rawName = elements.GetType().GetMethod("GetString", new[] { typeof(string) })?.Invoke(elements, new object[] { "/T" })?.ToString() ?? string.Empty; } catch { }
                        if (string.IsNullOrWhiteSpace(rawName)) continue;
                        var name = DecodePdfFieldName(rawName);
                        // get value from mapping
                        map.TryGetValue(name, out var value);

                        // read rect
                        string rectString = null;
                        try
                        {
                            var getArray = elements.GetType().GetMethod("GetArray", new[] { typeof(string) });
                            var rectArray = getArray?.Invoke(elements, new object[] { "/Rect" });
                            if (rectArray != null) rectString = rectArray.ToString();
                        }
                        catch { }
                        if (rectString == null) continue;
                        var nums = rectString.Replace("[", "").Replace("]", "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (nums.Length < 4) continue;
                        if (!double.TryParse(nums[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var llx)) continue;
                        if (!double.TryParse(nums[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var lly)) continue;
                        if (!double.TryParse(nums[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var urx)) continue;
                        if (!double.TryParse(nums[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var ury)) continue;
                        var rect = new XRect(llx, page.Height.Point - ury, urx - llx, ury - lly);

                        // draw
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (rect.Width < 24 && rect.Height < 24 && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("Yes", StringComparison.OrdinalIgnoreCase) || value.Equals("On", StringComparison.OrdinalIgnoreCase)))
                            {
                                var centerX = rect.X + rect.Width / 2.0;
                                var centerY = rect.Y + rect.Height / 2.0 - 2;
                                gfx.DrawString("✓", checkFont, XBrushes.Black, new XPoint(centerX - 6, centerY + 6));
                                logLines.Add($"ANNOT CHECK: {name} at page={pi} rect={rect.X:F0},{rect.Y:F0},{rect.Width:F0}x{rect.Height:F0}");
                            }
                            else
                            {
                                var lines = SplitLines(value);
                                var y = rect.Y + 3;
                                foreach (var line in lines)
                                {
                                    gfx.DrawString(line, baseFont, XBrushes.Black, new XRect(rect.X + 3, y, rect.Width - 6, rect.Height - 6), XStringFormats.TopLeft);
                                    y += baseFont.GetHeight();
                                    if (y > rect.Y + rect.Height) break;
                                }
                                logLines.Add($"ANNOT TEXT: {name}='{value}' page={pi} rect={rect.X:F0},{rect.Y:F0},{rect.Width:F0}x{rect.Height:F0}");
                            }
                        }

                        // schedule removal
                        toRemove.Add(annot);
                    }
                    catch { }
                }

                // remove annotations
                foreach (var a in toRemove)
                {
                    try { if (page.Annotations.Contains(a)) page.Annotations.Remove(a); } catch { }
                }
            }

            // Write flatten log if any
            try
            {
                if (logLines.Count > 0)
                {
                    var outPath = Path.Combine(Environment.CurrentDirectory, "Mitgliedsantrag_flatten_log.txt");
                    File.AppendAllLines(outPath, new[] { "--- Flatten run: " + DateTime.UtcNow.ToString("o") }.Concat(logLines));
                }
            }
            catch { }

            // (old widget-level removal logic removed; annotations already removed per-page)

            // Finally, remove AcroForm fields dictionary so the PDF is no longer an interactive form
            try
            {
                var acro = document.AcroForm;
                if (acro != null)
                {
                    // Clear fields
                    var fieldsProp = acro.GetType().GetProperty("Fields");
                    if (fieldsProp != null)
                    {
                        var fields = fieldsProp.GetValue(acro) as System.Collections.IList;
                        fields?.Clear();
                    }

                    // Remove AcroForm dictionary entry
                    document.Internals.Catalog.Elements.Remove("/AcroForm");
                }
            }
            catch
            {
                // ignore
            }
        }

        private static int GetPageIndex(PdfDocument document, PdfPage page)
        {
            for (int i = 0; i < document.Pages.Count; i++)
                if (document.Pages[i] == page) return i;
            return -1;
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
