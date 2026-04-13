using System.Globalization;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class PachtvertragTemplateFactory
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");

    public static PachtvertragTemplateData BuildData(PachtvertragTemplateContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.VereinskonfigurationSnapshot == null)
            throw new InvalidOperationException("Vereinskonfiguration fehlt für den Pachtvertrag.");

        if (context.Paechter1 == null)
            throw new InvalidOperationException("Pächter 1 fehlt für den Pachtvertrag.");

        if (string.IsNullOrWhiteSpace(context.ParzelleNummer))
            throw new InvalidOperationException("Parzellennummer fehlt für den Pachtvertrag.");

        if (context.ParzelleFlaecheQm <= 0)
            throw new InvalidOperationException("Parzellenfläche fehlt oder ist ungültig.");

        if (context.PachtProQm < 0)
            throw new InvalidOperationException("Pacht pro qm ist ungültig.");

        var config = context.VereinskonfigurationSnapshot;
        var paechter1 = context.Paechter1;
        var paechter2 = context.Paechter2;

        var bodyClass = paechter2 == null ? "single" : "dual";
        var ausstellungsdatum = context.Ausstellungsdatum ?? DateTime.Today;

        var jahrespacht = CalculateAnnualRent(context.ParzelleFlaecheQm, context.PachtProQm);
        var pachtLaufendesJahr = CalculateRunningYearRent(jahrespacht, context.Pachtbeginn);

        return new PachtvertragTemplateData
        {
            BodyClass = bodyClass,
            KgvLogoDataUri = context.KgvLogoDataUri ?? string.Empty,
            VereinName = CleanOrDefault(config.VereinName, VereinsdokumentBranding.VereinsName),
            Ausstellungsdatum = FormatDate(ausstellungsdatum),

            Paechter1Name = Clean(paechter1.Name),
            Paechter1Vorname = Clean(paechter1.Vorname),
            Paechter1Geburtsdatum = FormatNullableDate(paechter1.Geburtsdatum),
            Paechter1Telefon = BuildPhoneText(
                context.Paechter1TelefonOverride,
                paechter1.Telefon,
                paechter1.Handy),
            Paechter1AnschriftMehrzeilig = BuildAddress(
                context.Paechter1AdresseOverride,
                paechter1.Adresse,
                paechter1.Plz,
                paechter1.Ort),

            Paechter2Name = paechter2 == null ? string.Empty : Clean(paechter2.Name),
            Paechter2Vorname = paechter2 == null ? string.Empty : Clean(paechter2.Vorname),
            Paechter2Geburtsdatum = paechter2 == null ? string.Empty : FormatNullableDate(paechter2.Geburtsdatum),
            Paechter2Telefon = paechter2 == null
                ? string.Empty
                : BuildPhoneText(
                    context.Paechter2TelefonOverride,
                    paechter2.Telefon,
                    paechter2.Handy),
            Paechter2AnschriftMehrzeilig = paechter2 == null
                ? string.Empty
                : BuildAddress(
                    context.Paechter2AdresseOverride,
                    paechter2.Adresse,
                    paechter2.Plz,
                    paechter2.Ort),

            ParzelleNummer = Clean(context.ParzelleNummer),
            ParzelleFlaecheQm = FormatArea(context.ParzelleFlaecheQm),

            Pachtbeginn = FormatDate(context.Pachtbeginn),
            VertragsartText = BuildContractTypeText(context),
            PachtProQm = FormatMoney(context.PachtProQm) + " / m²",
            Jahrespacht = FormatMoney(jahrespacht),
            ZahlungszielText = CleanOrDefault(context.ZahlungszielText, "30. November des laufenden Jahres"),
            BankeinzugText = context.BankeinzugVereinbart ? "wird" : "wird nicht",

            AltvertragText = CleanOrDefault(context.AltvertragText, "kein abweichender Altvertrag hinterlegt"),
            Zusatzvereinbarungen = CleanOrDefault(context.Zusatzvereinbarungen, "Keine zusätzlichen Vereinbarungen."),
            AnlagenText = BuildAnlagenText(context, config),

            BankblockMehrzeilig = BuildBankBlock(config),
            PachtLaufendesJahr = FormatMoney(pachtLaufendesJahr)
        };
    }

    public static string CreateRenderedHtml(string templateHtml, PachtvertragTemplateContext context)
    {
        var data = BuildData(context);
        return PachtvertragTemplateRenderer.Render(templateHtml, data);
    }

    private static decimal CalculateAnnualRent(decimal areaQm, decimal pachtProQm)
    {
        var annual = areaQm * pachtProQm;
        return Math.Round(annual, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateRunningYearRent(decimal annualRent, DateTime pachtbeginn)
    {
        var restMonate = Math.Clamp(13 - pachtbeginn.Month, 1, 12);
        var result = annualRent * restMonate / 12m;
        return Math.Round(result, 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildContractTypeText(PachtvertragTemplateContext context)
    {
        if (context.BefristetBis.HasValue)
            return $"befristet bis {FormatDate(context.BefristetBis.Value)}";

        return "unbefristet";
    }

    private static string BuildAddress(string? overrideAddress, string? adresse, string? plz, string? ort)
    {
        if (!string.IsNullOrWhiteSpace(overrideAddress))
            return NormalizeMultiline(overrideAddress);

        var line1 = Clean(adresse);
        var line2 = string.Join(" ", new[] { Clean(plz), Clean(ort) }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        return NormalizeMultiline(string.Join(Environment.NewLine, new[] { line1, line2 }.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static string BuildPhoneText(string? overridePhone, string? telefon, string? handy)
    {
        if (!string.IsNullOrWhiteSpace(overridePhone))
            return Clean(overridePhone);

        var parts = new[]
        {
            Clean(telefon),
            Clean(handy)
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(" / ", parts);
    }

    private static string BuildAnlagenText(PachtvertragTemplateContext context, MitgliedsantragBankverbindungSnapshot config)
    {
        if (!string.IsNullOrWhiteSpace(context.AnlagenText))
            return NormalizeMultiline(context.AnlagenText);

        var vereinName = CleanOrDefault(config.VereinName, VereinsdokumentBranding.VereinsName);
        var lines = new List<string>
        {
            "Rahmenkleingartenordnung",
            $"Satzung des {vereinName}",
            $"Kleingartenordnung des {vereinName}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildBankBlock(MitgliedsantragBankverbindungSnapshot config)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(config.Kontoinhaber))
            lines.Add(Clean(config.Kontoinhaber));

        if (!string.IsNullOrWhiteSpace(config.Bankname))
            lines.Add(Clean(config.Bankname));

        if (!string.IsNullOrWhiteSpace(config.Iban))
            lines.Add(FormatIban(config.Iban));

        if (!string.IsNullOrWhiteSpace(config.Bic))
            lines.Add(Clean(config.Bic));

        if (!string.IsNullOrWhiteSpace(config.DokumentOrt))
        {
            lines.Add(string.Empty);
            lines.Add(Clean(config.DokumentOrt));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatDate(DateTime value)
        => value.ToString("dd.MM.yyyy", DeCulture);

    private static string FormatNullableDate(DateTime? value)
        => value.HasValue ? FormatDate(value.Value) : string.Empty;

    private static string FormatMoney(decimal value)
        => string.Format(DeCulture, "{0:N2} €", value);

    private static string FormatArea(decimal value)
        => value.ToString("0.##", DeCulture);

    private static string FormatIban(string? iban)
    {
        var normalized = new string((iban ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return string.Join(" ",
            Enumerable.Range(0, (normalized.Length + 3) / 4)
                .Select(i => normalized.Substring(i * 4, Math.Min(4, normalized.Length - i * 4))));
    }

    private static string NormalizeMultiline(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static string Clean(string? value)
        => (value ?? string.Empty).Trim();

    private static string CleanOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : Clean(value);
}

public sealed class PachtvertragTemplateContext
{
    public string? KgvLogoDataUri { get; set; }

    public MitgliedsantragBankverbindungSnapshot? VereinskonfigurationSnapshot { get; set; }

    public MitgliedRecord? Paechter1 { get; set; }
    public MitgliedRecord? Paechter2 { get; set; }

    public string? Paechter1TelefonOverride { get; set; }
    public string? Paechter2TelefonOverride { get; set; }

    public string? Paechter1AdresseOverride { get; set; }
    public string? Paechter2AdresseOverride { get; set; }

    public string? ParzelleNummer { get; set; }
    public decimal ParzelleFlaecheQm { get; set; }

    public DateTime Pachtbeginn { get; set; }
    public DateTime? BefristetBis { get; set; }

    public decimal PachtProQm { get; set; }

    public string? ZahlungszielText { get; set; }
    public bool BankeinzugVereinbart { get; set; }

    public DateTime? Ausstellungsdatum { get; set; }

    public string? AltvertragText { get; set; }
    public string? Zusatzvereinbarungen { get; set; }
    public string? AnlagenText { get; set; }
}