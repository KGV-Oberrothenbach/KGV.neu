using System;

namespace KGV.Core.Models;

public sealed class ArbeitsstundenPruefaktionRequest
{
    public int ArbeitsstundeId { get; set; }
    public string Aktion { get; set; } = string.Empty;
    public string Kommentar { get; set; } = string.Empty;
    public int GeprueftVon { get; set; }
    public DateTime? GeprueftAm { get; set; }
}
