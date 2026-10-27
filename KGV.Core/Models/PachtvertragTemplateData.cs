namespace KGV.Core.Models;

public sealed class PachtvertragTemplateData
{
    public string Vertragsnummer { get; set; } = string.Empty;
    public string Ausstellungsdatum { get; set; } = string.Empty;

    public string Paechter1Name { get; set; } = string.Empty;
    public string Paechter1Vorname { get; set; } = string.Empty;
    public string Paechter1Geburtsdatum { get; set; } = string.Empty;
    public string Paechter1Mitgliedsnummer { get; set; } = string.Empty;
    public string Paechter1Vollname { get; set; } = string.Empty;
    public string Paechter1Anschrift { get; set; } = string.Empty;
    public string Paechter1Kontakt { get; set; } = string.Empty;

    public string Paechter2Name { get; set; } = string.Empty;
    public string Paechter2Vorname { get; set; } = string.Empty;
    public string Paechter2Geburtsdatum { get; set; } = string.Empty;
    public string Paechter2Mitgliedsnummer { get; set; } = string.Empty;
    public string Paechter2Vollname { get; set; } = string.Empty;
    public string Paechter2Anschrift { get; set; } = string.Empty;
    public string Paechter2Kontakt { get; set; } = string.Empty;
    public string ZweiteParteiBezeichnung { get; set; } = string.Empty;

    public string ParzelleNummer { get; set; } = string.Empty;
    public string ParzelleFlaecheQm { get; set; } = string.Empty;
    public string ParzelleFlaecheQmWiederholung { get; set; } = string.Empty;
    public string ParzelleAnlage { get; set; } = string.Empty;

    public string Pachtbeginn { get; set; } = string.Empty;
    public string ProbephaseBis { get; set; } = string.Empty;
    public string UebergabeDatum { get; set; } = string.Empty;

    public string PachtProQm { get; set; } = string.Empty;
    public string Jahrespacht { get; set; } = string.Empty;
    public string PachtzahlungFaelligBis { get; set; } = string.Empty;

    public string AltvertragDatum { get; set; } = string.Empty;
    public string AltvertragHinweis { get; set; } = string.Empty;
    public string VereinsunterlagenNachweis { get; set; } = string.Empty;

    public string BankblockMehrzeilig { get; set; } = string.Empty;
    public string PachtLaufendesJahr { get; set; } = string.Empty;

    public string UnterschriftOrt { get; set; } = "Zwickau";
    public string UnterschriftDatum { get; set; } = string.Empty;
    public string WeitereVereinbarungen { get; set; } = string.Empty;
    public string VereinsUnterzeichner { get; set; } = string.Empty;
    public string VereinName { get; set; } = string.Empty;
    public string Vereinsregister { get; set; } = string.Empty;
    public string VereinsFusszeile { get; set; } = string.Empty;
    public string VereinsangabenFusszeile { get; set; } = string.Empty;
    public bool Anlage1 { get; set; }
    public bool Anlage2 { get; set; }
    public bool Anlage3 { get; set; }
    public bool Anlage4 { get; set; }
}
