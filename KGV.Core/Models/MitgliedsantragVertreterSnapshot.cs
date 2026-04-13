namespace KGV.Core.Models;

public sealed class MitgliedsantragVertreterSnapshot
{
    public int? VertreterMitgliedId { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Plz { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Handy { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName => string.Join(" ", new[] { Vorname, Nachname }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
}