namespace KGV.Core.Models;

public sealed class PachtvertragTemplateData
{
    public string BodyClass { get; set; } = "single";

    public string KgvLogoDataUri { get; set; } = string.Empty;
    public string VereinName { get; set; } = string.Empty;
    public string Ausstellungsdatum { get; set; } = string.Empty;

    public string Paechter1Name { get; set; } = string.Empty;
    public string Paechter1Vorname { get; set; } = string.Empty;
    public string Paechter1Geburtsdatum { get; set; } = string.Empty;
    public string Paechter1Telefon { get; set; } = string.Empty;
    public string Paechter1AnschriftMehrzeilig { get; set; } = string.Empty;

    public string Paechter2Name { get; set; } = string.Empty;
    public string Paechter2Vorname { get; set; } = string.Empty;
    public string Paechter2Geburtsdatum { get; set; } = string.Empty;
    public string Paechter2Telefon { get; set; } = string.Empty;
    public string Paechter2AnschriftMehrzeilig { get; set; } = string.Empty;

    public string ParzelleNummer { get; set; } = string.Empty;
    public string ParzelleFlaecheQm { get; set; } = string.Empty;

    public string Pachtbeginn { get; set; } = string.Empty;
    public string VertragsartText { get; set; } = string.Empty;
    public string PachtProQm { get; set; } = string.Empty;
    public string Jahrespacht { get; set; } = string.Empty;
    public string ZahlungszielText { get; set; } = string.Empty;
    public string BankeinzugText { get; set; } = string.Empty;

    public string AltvertragText { get; set; } = string.Empty;
    public string Zusatzvereinbarungen { get; set; } = string.Empty;
    public string AnlagenText { get; set; } = string.Empty;

    public string BankblockMehrzeilig { get; set; } = string.Empty;
    public string PachtLaufendesJahr { get; set; } = string.Empty;
}