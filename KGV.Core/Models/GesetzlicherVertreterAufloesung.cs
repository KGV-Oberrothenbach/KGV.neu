namespace KGV.Core.Models;

public sealed class GesetzlicherVertreterAufloesung
{
    public int MitgliedId { get; init; }
    public bool IstMinderjaehrig { get; init; }
    public MitgliedGesetzlicherVertreterRecord? AktiveVertretung { get; init; }
    public MitgliedRecord? VertreterMitglied { get; init; }
    public GesetzlicherVertreterVorbelegung? Vorbelegung { get; init; }
    public bool HatAktivenGesetzlichenVertreter => AktiveVertretung != null && VertreterMitglied != null;
}