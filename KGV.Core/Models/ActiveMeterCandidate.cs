namespace KGV.Core.Models;

/// <summary>
/// Verständlich darstellbarer, im gewählten Kontext aktuell wirksamer Zähler.
/// Die technische ID bleibt ausschließlich ein internes Übergabedatum an die RPC.
/// </summary>
public sealed record ActiveMeterCandidate(long Id, string Zaehlernummer, DateTime EingebautAm);
