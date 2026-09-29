using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Infrastructure.Services;

/// <summary>
/// Infrastruktur-Einstieg für den Jahresabschluss. Phase 1 stellt bewusst nur
/// die Lesepfade und den Vertrag bereit; die verbindliche Berechnung folgt als
/// atomare Datenbankfunktion, bevor UI-Aktionen freigeschaltet werden.
/// </summary>
public sealed class JahresabschlussService : IJahresabschlussService
{
    private readonly ISupabaseService _supabase;

    public JahresabschlussService(ISupabaseService supabase) => _supabase = supabase;

    public Task<JahresabschlussRecord?> GetBySaisonAsync(int saisonId) =>
        _supabase.GetJahresabschlussBySaisonAsync(saisonId);

    public async Task<IReadOnlyList<JahresabschlussRechnungRecord>> GetRechnungenAsync(int saisonId) =>
        (await _supabase.GetJahresabschlussRechnungenAsync(saisonId)).ToList();

    public async Task<JahresabschlussPruefung> PruefeAsync(SaisonRecord saison)
    {
        var fehler = new List<string>();
        var warnungen = new List<string>();
        var rechnungen = await _supabase.GetJahresabschlussRechnungenAsync(saison.Id);
        foreach (var rechnung in rechnungen)
        {
            var zuordnungen = await _supabase.GetJahresabschlussRechnungZuordnungenAsync(rechnung.Id);
            var rest = rechnung.Gesamtbetrag - zuordnungen.Sum(x => x.Betrag);
            if (rest != 0) fehler.Add($"Rechnung {rechnung.Lieferant} vom {rechnung.Rechnungsdatum:dd.MM.yyyy} ist noch um {rest:N2} € nicht verteilt.");
        }

        var von = new DateTime(saison.Jahr, 1, 1);
        var bis = new DateTime(saison.Jahr, 12, 31);
        var parzellen = (await _supabase.GetAllParzellenAsync()).Where(x => x.Aktiv).ToList();
        var belegungen = await _supabase.GetAllParzellenBelegungenAsync();
        foreach (var parzelle in parzellen)
        {
            foreach (var medium in GetRelevantMedien(parzelle))
            {
                var readings = medium == "strom" ? await _supabase.GetStromAblesungenAsync(parzelle.Id) : await _supabase.GetWasserAblesungenAsync(parzelle.Id);
                var saisonReadings = readings.Where(x => x.Ablesedatum.Date >= von && x.Ablesedatum.Date <= bis).ToList();
                if (saisonReadings.Count == 0) { fehler.Add($"Garten {parzelle.GartenNr}: keine {medium}-Ablesung für {saison.Jahr}."); continue; }
                if (saisonReadings.Any(x => !AblesungPruefstatus.IsFreigegeben(x.Pruefstatus))) fehler.Add($"Garten {parzelle.GartenNr}: {medium}-Ablesung ist noch nicht freigegeben.");
                if (saisonReadings.Any(x => string.IsNullOrWhiteSpace(x.FotoDateiname) && string.IsNullOrWhiteSpace(x.FotoPfad) && string.IsNullOrWhiteSpace(x.FotoDriveFileId))) fehler.Add($"Garten {parzelle.GartenNr}: {medium}-Ablesung ohne Pflichtfoto.");
                foreach (var meter in readings.GroupBy(x => x.ZaehlerId))
                {
                    var ordered = meter.OrderBy(x => x.Ablesedatum).ToList();
                    if (ordered.Zip(ordered.Skip(1), (a, b) => b.Stand < a.Stand).Any(x => x)) fehler.Add($"Garten {parzelle.GartenNr}: rückläufiger {medium}-Zählerstand bei Zähler {meter.First().Zaehlernummer}.");
                    if (ordered.Count >= 2)
                    {
                        var consumption = ordered.Last().Stand - ordered.First().Stand;
                        if (consumption > (medium == "strom" ? 10_000m : 1_000m)) warnungen.Add($"Garten {parzelle.GartenNr}: auffälliger {medium}-Verbrauch ({consumption:N1}). Begründung in der Prüfung erforderlich.");
                    }
                }
            }
            var wechsel = belegungen.Where(x => x.ParzelleId == parzelle.Id && x.VonDatum <= bis && (x.BisDatum == null || x.BisDatum >= von)).OrderBy(x => x.VonDatum).ToList();
            if (wechsel.Count > 1) warnungen.Add($"Garten {parzelle.GartenNr}: Pächterwechsel im Saisonzeitraum – Verbrauchssegmente werden in der Berechnung geprüft.");
        }
        return new JahresabschlussPruefung { KannAbschliessen = fehler.Count == 0, Fehler = fehler, Warnungen = warnungen };
    }

    private static IEnumerable<string> GetRelevantMedien(ParzelleRecord parzelle)
    {
        if (parzelle.HatStrom) yield return "strom";
        if (parzelle.HatWasser) yield return "wasser";
    }

    public async Task<JahresabschlussBerechnungErgebnis> BerechneAsync(SaisonRecord saison)
    {
        var pruefung = await PruefeAsync(saison);
        if (!pruefung.KannAbschliessen)
            return new JahresabschlussBerechnungErgebnis { Meldung = "Berechnung nicht möglich: zuerst alle blockierenden Fehler im Prüfbericht beheben." };

        var existing = await _supabase.GetJahresabschlussBySaisonAsync(saison.Id);
        if (existing?.Status == JahresabschlussStatus.Abgeschlossen)
            return new JahresabschlussBerechnungErgebnis { Meldung = "Der Abschluss ist bereits endgültig abgeschlossen." };

        var parzellen = (await _supabase.GetAllParzellenAsync()).Where(x => x.Aktiv).ToDictionary(x => x.Id);
        var belegungen = (await _supabase.GetAllParzellenBelegungenAsync())
            .Where(x => OverlapsSaison(x, saison.Jahr)).ToList();
        var rechnungen = await _supabase.GetJahresabschlussRechnungenAsync(saison.Id);
        var positionen = new List<JahresabschlussPositionRecord>();
        foreach (var rechnung in rechnungen)
        {
            foreach (var zuordnung in await _supabase.GetJahresabschlussRechnungZuordnungenAsync(rechnung.Id))
            {
                var umlage = (await _supabase.GetUmlageartenAsync(true)).FirstOrDefault(x => x.Id == zuordnung.UmlageartId);
                if (umlage == null) return new JahresabschlussBerechnungErgebnis { Meldung = "Eine Rechnungszeile verweist auf eine nicht vorhandene Umlageart." };
                var targets = BuildTargets(umlage.Kuerzel, zuordnung, parzellen, belegungen, saison.Jahr);
                if (targets.Count == 0 && umlage.Kuerzel != "KEINE") return new JahresabschlussBerechnungErgebnis { Meldung = $"Für {umlage.Kuerzel} gibt es keine abrechnungsrelevanten Empfänger." };
                AddDistributedPositions(positionen, zuordnung.Betrag, targets, $"{umlage.Kuerzel}: {rechnung.Lieferant}");
            }
        }

        var version = (existing?.BerechnungVersion is { } value && int.TryParse(value, out var number) ? number + 1 : 1).ToString(CultureInfo.InvariantCulture);
        var saved = await _supabase.SaveJahresabschlussAsync(new JahresabschlussRecord { SaisonId = saison.Id, Status = JahresabschlussStatus.Berechnet, BerechnungVersion = version, Bemerkung = "Entwurf aus Phase 3" });
        if (saved == null) return new JahresabschlussBerechnungErgebnis { Meldung = "Abschlussentwurf konnte nicht gespeichert werden." };
        foreach (var position in positionen) position.JahresabschlussId = saved.Id;
        if (!await _supabase.ReplaceJahresabschlussPositionenAsync(saved.Id, positionen)) return new JahresabschlussBerechnungErgebnis { Meldung = "Berechnete Positionen konnten nicht gespeichert werden." };
        return new JahresabschlussBerechnungErgebnis { Erfolgreich = true, Meldung = "Entwurf wurde berechnet. Prüfen Sie Warnungen vor dem endgültigen Abschluss.", PositionsAnzahl = positionen.Count, Gesamtbetrag = positionen.Sum(x => x.Betrag) };
    }

    public async Task<JahresabschlussBerechnungErgebnis> AbschliessenAsync(SaisonRecord saison, long abgeschlossenVon)
    {
        var pruefung = await PruefeAsync(saison);
        if (!pruefung.KannAbschliessen) return new JahresabschlussBerechnungErgebnis { Meldung = "Der Abschluss ist wegen blockierender Fehler nicht möglich." };
        var abschluss = await _supabase.GetJahresabschlussBySaisonAsync(saison.Id);
        if (abschluss == null || abschluss.Status != JahresabschlussStatus.Berechnet) return new JahresabschlussBerechnungErgebnis { Meldung = "Bitte zuerst einen Berechnungsentwurf erstellen." };
        var positionen = await _supabase.GetJahresabschlussPositionenAsync(abschluss.Id);
        if (positionen.Count == 0) return new JahresabschlussBerechnungErgebnis { Meldung = "Der Berechnungsentwurf enthält keine Positionen." };
        var finalized = await _supabase.FinalizeJahresabschlussAsync(abschluss.Id, abgeschlossenVon);
        return finalized == null
            ? new JahresabschlussBerechnungErgebnis { Meldung = "Der Abschluss konnte nicht endgültig gespeichert werden." }
            : new JahresabschlussBerechnungErgebnis { Erfolgreich = true, Meldung = "Jahresabschluss endgültig abgeschlossen und gesperrt.", PositionsAnzahl = positionen.Count, Gesamtbetrag = positionen.Sum(x => x.Betrag) };
    }

    private static bool OverlapsSaison(ParzellenBelegungRecord belegung, int jahr)
    {
        var start = new DateTime(jahr, 1, 1); var end = new DateTime(jahr, 12, 31);
        return belegung.VonDatum <= end && (belegung.BisDatum == null || belegung.BisDatum >= start);
    }

    private static List<DistributionTarget> BuildTargets(string kuerzel, JahresabschlussRechnungZuordnungRecord line, IReadOnlyDictionary<int, ParzelleRecord> parzellen, IReadOnlyList<ParzellenBelegungRecord> belegungen, int jahr)
    {
        if (kuerzel == "KEINE") return new List<DistributionTarget>();
        if (kuerzel == "EINZEL")
        {
            if (line.MitgliedId is > 0) return new List<DistributionTarget> { new(line.MitgliedId.Value, line.ParzelleId, 1m) };
            var occupant = line.ParzelleId is > 0 ? belegungen.Where(x => x.ParzelleId == line.ParzelleId).OrderByDescending(x => x.BisDatum ?? DateTime.MaxValue).FirstOrDefault() : null;
            return occupant == null ? new List<DistributionTarget>() : new List<DistributionTarget> { new(occupant.MitgliedId, line.ParzelleId, 1m) };
        }
        var targets = new List<DistributionTarget>();
        foreach (var belegung in belegungen)
        {
            if (!parzellen.TryGetValue(belegung.ParzelleId, out var parzelle)) continue;
            if (kuerzel == "U-S" && !parzelle.HatStrom) continue;
            if (kuerzel == "U-W" && !parzelle.HatWasser) continue;
            var days = OverlapDays(belegung, jahr); if (days <= 0) continue;
            var weight = kuerzel == "U2" ? days * (parzelle.FlaecheQm ?? 0m) : days;
            if (weight > 0) targets.Add(new DistributionTarget(belegung.MitgliedId, belegung.ParzelleId, weight));
        }
        return targets;
    }

    private static decimal OverlapDays(ParzellenBelegungRecord belegung, int jahr)
    {
        var start = new[] { belegung.VonDatum?.Date ?? new DateTime(jahr, 1, 1), new DateTime(jahr, 1, 1) }.Max();
        var end = new[] { belegung.BisDatum?.Date ?? new DateTime(jahr, 12, 31), new DateTime(jahr, 12, 31) }.Min();
        return end < start ? 0 : (decimal)(end - start).TotalDays + 1;
    }

    private static void AddDistributedPositions(ICollection<JahresabschlussPositionRecord> output, decimal amount, IReadOnlyList<DistributionTarget> targets, string label)
    {
        var totalWeight = targets.Sum(x => x.Weight); if (totalWeight <= 0) return;
        var allocated = 0m;
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index]; var value = index == targets.Count - 1 ? amount - allocated : Math.Round(amount * target.Weight / totalWeight, 2, MidpointRounding.AwayFromZero);
            allocated += value;
            output.Add(new JahresabschlussPositionRecord { MitgliedId = target.MitgliedId, ParzelleId = target.ParzelleId, PositionTyp = label, Menge = target.Weight, Einheit = "Anteil", Betrag = value, Bemerkung = label });
        }
    }

    private sealed record DistributionTarget(long MitgliedId, long? ParzelleId, decimal Weight);

}
