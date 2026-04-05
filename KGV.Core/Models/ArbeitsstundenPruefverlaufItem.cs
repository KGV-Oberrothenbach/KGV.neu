using System;

namespace KGV.Core.Models;

public sealed class ArbeitsstundenPruefverlaufItem
{
    public long Id { get; set; }
    public int ArbeitsstundeId { get; set; }
    public string Aktion { get; set; } = string.Empty;
    public string AktionDisplay => ArbeitsstundenPruefprozess.GetAktionDisplay(Aktion);
    public string Begruendung { get; set; } = string.Empty;
    public string Kommentar => Begruendung;
    public int GeprueftVon { get; set; }
    public string GeprueftVonName { get; set; } = string.Empty;
    public DateTime GeprueftAm { get; set; }
    public ArbeitsstundenPruefSnapshot? VorherSnapshot { get; set; }
    public ArbeitsstundenPruefSnapshot? NachherSnapshot { get; set; }
    public string VorherSummary { get; set; } = string.Empty;
    public string? NachherSummary { get; set; }
}
