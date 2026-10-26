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
        return new PachtvertragTemplateData
        {
            Ausstellungsdatum = FormatDate(ausstellungsdatum),

            Paechter1Name = Clean(paechter1.Name),
            Paechter1Vorname = Clean(paechter1.Vorname),
            Paechter1Geburtsdatum = FormatNullableDate(paechter1.Geburtsdatum),
            Paechter1Mitgliedsnummer = FormatMitgliedsnummer(paechter1),
            Paechter1Vollname = BuildFullName(paechter1),
            Paechter1Anschrift = BuildAddress(paechter1),
            Paechter1Kontakt = BuildContact(paechter1),

            Paechter2Name = paechter2 == null ? string.Empty : Clean(paechter2.Name),
            Paechter2Vorname = paechter2 == null ? string.Empty : Clean(paechter2.Vorname),
            Paechter2Geburtsdatum = paechter2 == null ? string.Empty : FormatNullableDate(paechter2.Geburtsdatum),
            Paechter2Mitgliedsnummer = paechter2 == null ? string.Empty : FormatMitgliedsnummer(paechter2),
            Paechter2Vollname = paechter2 == null ? string.Empty : BuildFullName(paechter2),
            Paechter2Anschrift = paechter2 == null ? string.Empty : BuildAddress(paechter2),
            Paechter2Kontakt = paechter2 == null ? string.Empty : BuildContact(paechter2),
            ZweiteParteiBezeichnung = paechter2 == null ? string.Empty : context.ZweiteParteiIstGesetzlicherVertreter ? "Gesetzliche/r Vertreter/in" : "Mitpächter/in",

            ParzelleNummer = Clean(context.ParzelleNummer),
            ParzelleFlaecheQm = FormatArea(context.ParzelleFlaecheQm),
            ParzelleFlaecheQmWiederholung = FormatArea(context.ParzelleFlaecheQm),
            ParzelleAnlage = Clean(context.ParzelleAnlage),

            Pachtbeginn = FormatDate(context.Pachtbeginn),
            ProbephaseBis = new DateTime(context.Pachtbeginn.Year + 1, 12, 31).ToString("dd.MM.yyyy", DeCulture),
            UebergabeDatum = FormatDate(context.UebergabeDatum ?? context.Pachtbeginn),

            PachtProQm = string.Format(DeCulture, "{0:N2} €/m²", context.PachtProQm),
            Jahrespacht = FormatMoney(jahrespacht),
            PachtzahlungFaelligBis = Clean(context.ZahlungszielText),

            AltvertragDatum = context.AltvertragDatum.HasValue
                ? FormatDate(context.AltvertragDatum.Value)
                : string.Empty,
            AltvertragHinweis = context.AltvertragDatum.HasValue
                ? $"Dieser Pachtvertrag ersetzt die bisherige Vertragsurkunde vom {FormatDate(context.AltvertragDatum.Value)}. Das bestehende Pachtverhältnis und dessen ursprünglicher Beginn bleiben hiervon unberührt."
                : string.Empty,
            VereinsunterlagenNachweis = Clean(context.VereinsunterlagenNachweis),

            UnterschriftOrt = CleanOrDefault(context.UnterschriftOrt, "Zwickau"),
            UnterschriftDatum = FormatDate(DateTime.Today),
            WeitereVereinbarungen = Clean(context.WeitereVereinbarungen),
            VereinsUnterzeichner = string.Empty,
            VereinName = Clean(context.VereinskonfigurationSnapshot?.VereinName),
            Vereinsregister = Clean(context.VereinskonfigurationSnapshot?.VereinRegisterangabe),
            VereinsFusszeile = BuildClubAddress(context.VereinskonfigurationSnapshot),
            VereinsangabenFusszeile = BuildClubFooter(context.VereinskonfigurationSnapshot)
        };
    }

    private static decimal CalculateAnnualRent(decimal areaQm, decimal pachtProQm)
    {
        var annual = areaQm * pachtProQm;
        return Math.Round(annual, 2, MidpointRounding.AwayFromZero);
    }

    private static string FormatMitgliedsnummer(MitgliedRecord member)
    {
        if (member.Mitgliedsnummer <= 0)
            throw new InvalidOperationException("Für eine Vertragspartei des Pachtvertrags fehlt eine gültige Mitgliedsnummer.");

        return member.Mitgliedsnummer.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildFullName(MitgliedRecord member)
        => string.Join(" ", new[] { member.Vorname, member.Name }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));

    private static string BuildAddress(MitgliedRecord member)
    {
        var street = Clean(member.Adresse);
        var location = string.Join(" ", new[] { Clean(member.Plz), Clean(member.Ort) }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.Join(", ", new[] { street, location }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildContact(MitgliedRecord member)
    {
        var phone = string.Join(" / ", new[] { Clean(member.Telefon), Clean(member.Handy) }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var email = Clean(member.Email);

        return string.Join(" | ", new[] { phone, email }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildClubAddress(VereinskonfigurationSnapshot? configuration)
    {
        if (configuration == null)
            return string.Empty;

        return string.Join(", ", new[]
        {
            Clean(configuration.Strasse),
            string.Join(" ", new[] { Clean(configuration.Plz), Clean(configuration.Ort) }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildClubFooter(VereinskonfigurationSnapshot? configuration)
    {
        if (configuration == null)
            return string.Empty;

        return string.Join(Environment.NewLine, new[]
        {
            Clean(configuration.VereinName),
            Clean(configuration.Strasse),
            string.Join(" ", new[] { Clean(configuration.Plz), Clean(configuration.Ort) }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            Clean(configuration.VereinRegisterangabe)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatDate(DateTime value)
        => value.ToString("dd.MM.yyyy", DeCulture);

    private static string FormatNullableDate(DateTime? value)
        => value.HasValue ? FormatDate(value.Value) : string.Empty;

    private static string FormatMoney(decimal value)
        => string.Format(DeCulture, "{0:N2} €", value);

    private static string FormatArea(decimal value)
        => value.ToString("0.##", DeCulture);

    private static string Clean(string? value)
        => (value ?? string.Empty).Trim();

    private static string CleanOrDefault(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : Clean(value);
}

public sealed class PachtvertragTemplateContext
{
    public VereinskonfigurationSnapshot? VereinskonfigurationSnapshot { get; set; }

    public MitgliedRecord? Paechter1 { get; set; }
    public MitgliedRecord? Paechter2 { get; set; }
    public bool ZweiteParteiIstGesetzlicherVertreter { get; set; }

    public string? ParzelleNummer { get; set; }
    public decimal ParzelleFlaecheQm { get; set; }
    public string? ParzelleAnlage { get; set; }

    public DateTime Pachtbeginn { get; set; }
    public DateTime? BefristetBis { get; set; }
    public DateTime? UebergabeDatum { get; set; }
    public string? VereinsunterlagenNachweis { get; set; }
    public string? KleingartenordnungFassung { get; set; }
    public DateTime? KleingartenordnungAusgehaendigtAm { get; set; }
    public string? WeitereVereinbarungen { get; set; }
    public bool Anlage1 { get; set; }
    public bool Anlage2 { get; set; }
    public bool Anlage3 { get; set; }
    public bool Anlage4 { get; set; }
    public string? VereinsUnterzeichner { get; set; }

    public decimal PachtProQm { get; set; }

    public string? ZahlungszielText { get; set; }
    public DateTime? Ausstellungsdatum { get; set; }

    public DateTime? AltvertragDatum { get; set; }

    public string? UnterschriftOrt { get; set; }
}
