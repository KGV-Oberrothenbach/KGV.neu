namespace KGV.Core.Models;

/// <summary>
/// Verständlich darstellbarer, im gewählten Kontext aktuell wirksamer Zähler.
/// Die technische ID bleibt ausschließlich ein internes Übergabedatum an die RPC.
/// </summary>
public sealed record ActiveMeterCandidate(
    long Id,
    string Zaehlernummer,
    DateTime EingebautAm,
    long? ParzelleId = null,
    string? Medium = null,
    bool IstHauptzaehler = false,
    string? Anlagenbereich = null)
{
    public string DisplayName => SelectableActiveMeter.BuildDisplayName(Medium, Zaehlernummer, IstHauptzaehler, Anlagenbereich);
}
