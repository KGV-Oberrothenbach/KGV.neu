namespace KGV.Core.Models;

public sealed class MitgliedsantragTemplateData
{
    public string BodyClass { get; set; } = string.Empty;

    public string VereinName { get; set; } = string.Empty;
    public string VereinRegisterangabe { get; set; } = string.Empty;
    public string VereinEmail { get; set; } = string.Empty;

    public string Ausstellungsdatum { get; set; } = string.Empty;

    public string MitgliedName { get; set; } = string.Empty;
    public string MitgliedVorname { get; set; } = string.Empty;
    public string MitgliedGeburtsdatum { get; set; } = string.Empty;
    public string MitgliedAufnahmeAb { get; set; } = string.Empty;
    public string MitgliedAnschriftMehrzeilig { get; set; } = string.Empty;
    public string MitgliedTelefon { get; set; } = string.Empty;
    public string MitgliedMobil { get; set; } = string.Empty;
    public string MitgliedEmail { get; set; } = string.Empty;

    public string CheckWhatsapp { get; set; } = string.Empty;
    public string CheckRechnungMail { get; set; } = string.Empty;
    public string CheckInfoMail { get; set; } = string.Empty;

    public string VertreterName { get; set; } = string.Empty;
    public string VertreterVorname { get; set; } = string.Empty;
    public string VertreterAnschriftMehrzeilig { get; set; } = string.Empty;
    public string VertreterTelefon { get; set; } = string.Empty;
    public string VertreterMobil { get; set; } = string.Empty;
    public string VertreterEmail { get; set; } = string.Empty;

    public string MitgliedsbeitragJaehrlich { get; set; } = string.Empty;
    public string Aufnahmegebuehr { get; set; } = string.Empty;
    public string BeitragHinweis1 { get; set; } = string.Empty;
    public string BeitragHinweis2 { get; set; } = string.Empty;

    public string BankKontoinhaber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;
    public string BankBic { get; set; } = string.Empty;

    public string Erklaerungstext { get; set; } = string.Empty;
    public string Datenschutztext { get; set; } = string.Empty;
    public string Fussnote { get; set; } = string.Empty;
    // Optional explicit document place (Ort) to be used for PDF "dokument_ort" and
    // signature place "unterschrift_ort". Keep empty when not provided so the
    // generator does not accidentally write other fields (Verwendungszweck/Vereinsname).
    public string DokumentOrt { get; set; } = string.Empty;
    public string UnterschriftOrt { get; set; } = string.Empty;
}