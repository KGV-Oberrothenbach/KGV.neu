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

        if (context.PachtProQm <= 0)
            throw new InvalidOperationException("Pacht pro qm ist ungültig.");

        var paechter1 = context.Paechter1;
        var paechter2 = context.Paechter2;
        var ausstellungsdatum = context.Ausstellungsdatum ?? DateTime.Today;

        var jahrespacht = CalculateAnnualRent(context.ParzelleFlaecheQm, context.PachtProQm);
        var pachtLaufendesJahr = CalculateRunningYearRent(jahrespacht, context.Pachtbeginn);

        return new PachtvertragTemplateData
        {
            Ausstellungsdatum = FormatDate(ausstellungsdatum),

            Paechter1Name = Clean(paechter1.Name),
            Paechter1Vorname = Clean(paechter1.Vorname),
            Paechter1Geburtsdatum = FormatNullableDate(paechter1.Geburtsdatum),
            Paechter1Mitgliedsnummer = FormatMitgliedsnummer(paechter1),

            Paechter2Name = paechter2 == null ? string.Empty : Clean(paechter2.Name),
            Paechter2Vorname = paechter2 == null ? string.Empty : Clean(paechter2.Vorname),
            Paechter2Geburtsdatum = paechter2 == null ? string.Empty : FormatNullableDate(paechter2.Geburtsdatum),
            Paechter2Mitgliedsnummer = paechter2 == null ? string.Empty : FormatMitgliedsnummer(paechter2),

            ParzelleNummer = Clean(context.ParzelleNummer),
            ParzelleFlaecheQm = FormatArea(context.ParzelleFlaecheQm),
            ParzelleFlaecheQmWiederholung = FormatArea(context.ParzelleFlaecheQm),

            Pachtbeginn = FormatDate(context.Pachtbeginn),
            Pachtende = context.BefristetBis.HasValue ? FormatDate(context.BefristetBis.Value) : string.Empty,

            PachtProQm = FormatMoney(context.PachtProQm),
            Jahrespacht = FormatMoney(jahrespacht),
            PachtzahlungFaelligBis = CleanOrDefault(context.ZahlungszielText, $"30.11.{context.Pachtbeginn.Year}"),

            AltvertragDatum = context.AltvertragDatum.HasValue
                ? FormatDate(context.AltvertragDatum.Value)
                : string.Empty,

            BankblockMehrzeilig = BuildBankBlock(context.VereinskonfigurationSnapshot),
            PachtLaufendesJahr = FormatMoney(pachtLaufendesJahr),

            UnterschriftOrt = CleanOrDefault(context.UnterschriftOrt, "Zwickau"),
            UnterschriftDatum = FormatDate(DateTime.Today)
        };
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

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatMitgliedsnummer(MitgliedRecord member)
        => member.Id > 0 ? member.Id.ToString(CultureInfo.InvariantCulture) : string.Empty;

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

    private static string Clean(string? value)
        => (value ?? string.Empty).Trim();

    private static string CleanOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : Clean(value);
}

public sealed class PachtvertragTemplateContext
{
    public MitgliedsantragBankverbindungSnapshot? VereinskonfigurationSnapshot { get; set; }

    public MitgliedRecord? Paechter1 { get; set; }
    public MitgliedRecord? Paechter2 { get; set; }

    public string? ParzelleNummer { get; set; }
    public decimal ParzelleFlaecheQm { get; set; }

    public DateTime Pachtbeginn { get; set; }
    public DateTime? BefristetBis { get; set; }

    public decimal PachtProQm { get; set; }

    public string? ZahlungszielText { get; set; }
    public DateTime? Ausstellungsdatum { get; set; }

    public DateTime? AltvertragDatum { get; set; }

    public string? UnterschriftOrt { get; set; }
}