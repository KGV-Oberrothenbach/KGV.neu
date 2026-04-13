using System.Net;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class MitgliedsantragTemplateRenderer
{
    public static string Render(string templateHtml, MitgliedsantragTemplateData data)
    {
        if (string.IsNullOrWhiteSpace(templateHtml))
            throw new ArgumentException("Template HTML darf nicht leer sein.", nameof(templateHtml));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{body_class}}"] = Raw(data.BodyClass),

            ["{{verein_name}}"] = Html(data.VereinName),
            ["{{verein_registerangabe}}"] = Html(data.VereinRegisterangabe),
            ["{{verein_email}}"] = Html(data.VereinEmail),

            ["{{ausstellungsdatum}}"] = Html(data.Ausstellungsdatum),

            ["{{mitglied_name}}"] = Html(data.MitgliedName),
            ["{{mitglied_vorname}}"] = Html(data.MitgliedVorname),
            ["{{mitglied_geburtsdatum}}"] = Html(data.MitgliedGeburtsdatum),
            ["{{mitglied_aufnahme_ab}}"] = Html(data.MitgliedAufnahmeAb),
            ["{{mitglied_anschrift_mehrzeilig}}"] = HtmlMultiline(data.MitgliedAnschriftMehrzeilig),
            ["{{mitglied_telefon}}"] = Html(data.MitgliedTelefon),
            ["{{mitglied_mobil}}"] = Html(data.MitgliedMobil),
            ["{{mitglied_email}}"] = Html(data.MitgliedEmail),

            ["{{check_whatsapp}}"] = Raw(CheckMark(data.CheckWhatsapp)),
            ["{{check_rechnung_mail}}"] = Raw(CheckMark(data.CheckRechnungMail)),
            ["{{check_info_mail}}"] = Raw(CheckMark(data.CheckInfoMail)),

            ["{{vertreter_name}}"] = Html(data.VertreterName),
            ["{{vertreter_vorname}}"] = Html(data.VertreterVorname),
            ["{{vertreter_anschrift_mehrzeilig}}"] = HtmlMultiline(data.VertreterAnschriftMehrzeilig),
            ["{{vertreter_telefon}}"] = Html(data.VertreterTelefon),
            ["{{vertreter_mobil}}"] = Html(data.VertreterMobil),
            ["{{vertreter_email}}"] = Html(data.VertreterEmail),

            ["{{mitgliedsbeitrag_jaehrlich}}"] = Html(data.MitgliedsbeitragJaehrlich),
            ["{{aufnahmegebuehr}}"] = Html(data.Aufnahmegebuehr),
            ["{{beitrag_hinweis_1}}"] = Html(data.BeitragHinweis1),
            ["{{beitrag_hinweis_2}}"] = Html(data.BeitragHinweis2),

            ["{{bank_kontoinhaber}}"] = Html(data.BankKontoinhaber),
            ["{{bank_name}}"] = Html(data.BankName),
            ["{{bank_iban}}"] = Html(data.BankIban),
            ["{{bank_bic}}"] = Html(data.BankBic),

            ["{{erklaerungstext}}"] = HtmlMultiline(data.Erklaerungstext),
            ["{{datenschutztext}}"] = HtmlMultiline(data.Datenschutztext),
            ["{{fussnote}}"] = Html(data.Fussnote),
        };

        var result = templateHtml;

        foreach (var pair in values)
        {
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string Html(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string HtmlMultiline(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);

    private static string Raw(string? value)
        => value ?? string.Empty;

    private static string CheckMark(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim();

        return normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("1", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("ja", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("x", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("✓", StringComparison.OrdinalIgnoreCase)
            ? "✓"
            : string.Empty;
    }
}