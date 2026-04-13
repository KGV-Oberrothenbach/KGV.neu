namespace KGV.Core.Models;

public sealed class GesetzlicherVertreterVorbelegung
{
    public int VertreterMitgliedId { get; init; }
    public string Vorname { get; init; } = string.Empty;
    public string Nachname { get; init; } = string.Empty;
    public string Adresse { get; init; } = string.Empty;
    public string Plz { get; init; } = string.Empty;
    public string Ort { get; init; } = string.Empty;
    public string Telefon { get; init; } = string.Empty;
    public string Handy { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}