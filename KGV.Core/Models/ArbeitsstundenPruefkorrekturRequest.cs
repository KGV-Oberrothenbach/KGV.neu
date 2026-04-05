using System;

namespace KGV.Core.Models;

public sealed class ArbeitsstundenPruefkorrekturRequest
{
    public int ArbeitsstundeId { get; set; }
    public DateTime Datum { get; set; }
    public decimal Stunden { get; set; }
    public string ArtDerArbeit { get; set; } = string.Empty;
    public string Begruendung { get; set; } = string.Empty;
    public int GeprueftVon { get; set; }
    public DateTime? GeprueftAm { get; set; }
}
