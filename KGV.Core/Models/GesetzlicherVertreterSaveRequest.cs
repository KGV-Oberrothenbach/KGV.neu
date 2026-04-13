using System;

namespace KGV.Core.Models;

public sealed class GesetzlicherVertreterSaveRequest
{
    public int MinderjaehrigesMitgliedId { get; set; }
    public int VertreterMitgliedId { get; set; }
    public DateTime GueltigAb { get; set; } = DateTime.Today;
    public string? Bemerkung { get; set; }
}