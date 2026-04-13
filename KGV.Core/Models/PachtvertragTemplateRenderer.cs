using System.Net;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class PachtvertragTemplateRenderer
{
    public static string Render(string templateHtml, PachtvertragTemplateData data)
    {
        if (string.IsNullOrWhiteSpace(templateHtml))
            throw new ArgumentException("Template HTML darf nicht leer sein.", nameof(templateHtml));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{body_class}}"] = HtmlAttribute(data.BodyClass),

            ["{{kgv_logo_data_uri}}"] = HtmlAttribute(data.KgvLogoDataUri),
            ["{{verein_name}}"] = Html(data.VereinName),
            ["{{ausstellungsdatum}}"] = Html(data.Ausstellungsdatum),

            ["{{paechter1_name}}"] = Html(data.Paechter1Name),
            ["{{paechter1_vorname}}"] = Html(data.Paechter1Vorname),
            ["{{paechter1_geburtsdatum}}"] = Html(data.Paechter1Geburtsdatum),
            ["{{paechter1_telefon}}"] = Html(data.Paechter1Telefon),
            ["{{paechter1_anschrift_mehrzeilig}}"] = HtmlMultiline(data.Paechter1AnschriftMehrzeilig),

            ["{{paechter2_name}}"] = Html(data.Paechter2Name),
            ["{{paechter2_vorname}}"] = Html(data.Paechter2Vorname),
            ["{{paechter2_geburtsdatum}}"] = Html(data.Paechter2Geburtsdatum),
            ["{{paechter2_telefon}}"] = Html(data.Paechter2Telefon),
            ["{{paechter2_anschrift_mehrzeilig}}"] = HtmlMultiline(data.Paechter2AnschriftMehrzeilig),

            ["{{parzelle_nummer}}"] = Html(data.ParzelleNummer),
            ["{{parzelle_flaeche_qm}}"] = Html(data.ParzelleFlaecheQm),

            ["{{pachtbeginn}}"] = Html(data.Pachtbeginn),
            ["{{vertragsart_text}}"] = Html(data.VertragsartText),
            ["{{pacht_pro_qm}}"] = Html(data.PachtProQm),
            ["{{jahrespacht}}"] = Html(data.Jahrespacht),
            ["{{zahlungsziel_text}}"] = Html(data.ZahlungszielText),
            ["{{bankeinzug_text}}"] = Html(data.BankeinzugText),

            ["{{altvertrag_text}}"] = Html(data.AltvertragText),
            ["{{zusatzvereinbarungen}}"] = HtmlMultiline(data.Zusatzvereinbarungen),
            ["{{anlagen_text}}"] = HtmlMultiline(data.AnlagenText),

            ["{{bankblock_mehrzeilig}}"] = HtmlMultiline(data.BankblockMehrzeilig),
            ["{{pacht_laufendes_jahr}}"] = Html(data.PachtLaufendesJahr)
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

    private static string HtmlAttribute(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string HtmlMultiline(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);
}