namespace KGV.Core.Models;

public sealed class PachtvertragTemplateData
{
    public string Ausstellungsdatum { get; set; } = string.Empty;

    public string Paechter1Name { get; set; } = string.Empty;
    public string Paechter1Vorname { get; set; } = string.Empty;
    public string Paechter1Geburtsdatum { get; set; } = string.Empty;
    public string Paechter1Mitgliedsnummer { get; set; } = string.Empty;

    public string Paechter2Name { get; set; } = string.Empty;
    public string Paechter2Vorname { get; set; } = string.Empty;
    public string Paechter2Geburtsdatum { get; set; } = string.Empty;
    public string Paechter2Mitgliedsnummer { get; set; } = string.Empty;

    public string ParzelleNummer { get; set; } = string.Empty;
    public string ParzelleFlaecheQm { get; set; } = string.Empty;
    public string ParzelleFlaecheQmWiederholung { get; set; } = string.Empty;

    public string Pachtbeginn { get; set; } = string.Empty;
    public string Pachtende { get; set; } = string.Empty;

    public string PachtProQm { get; set; } = string.Empty;
    public string Jahrespacht { get; set; } = string.Empty;
    public string PachtzahlungFaelligBis { get; set; } = string.Empty;

    public string AltvertragDatum { get; set; } = string.Empty;

    public string BankblockMehrzeilig { get; set; } = string.Empty;
    public string PachtLaufendesJahr { get; set; } = string.Empty;

    public string UnterschriftOrt { get; set; } = "Zwickau";
    public string UnterschriftDatum { get; set; } = string.Empty;
}