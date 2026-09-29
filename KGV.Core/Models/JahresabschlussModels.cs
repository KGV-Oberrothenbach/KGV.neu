using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("kostenart")]
public sealed class KostenartRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("beschreibung")] public string? Beschreibung { get; set; }
    [Column("aktiv")] public bool Aktiv { get; set; } = true;
}

[Table("umlageart")]
public sealed class UmlageartRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("kuerzel")] public string Kuerzel { get; set; } = string.Empty;
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("verteilung")] public string Verteilung { get; set; } = string.Empty;
    [Column("aktiv")] public bool Aktiv { get; set; } = true;
}

[Table("jahresabschluss")]
public sealed class JahresabschlussRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("saison_id")] public int SaisonId { get; set; }
    [Column("status")] public string Status { get; set; } = JahresabschlussStatus.InBearbeitung;
    [Column("berechnung_version")] public string BerechnungVersion { get; set; } = "1";
    [Column("bemerkung")] public string? Bemerkung { get; set; }
    [Column("erstellt_am")] public DateTime? ErstelltAm { get; set; }
    [Column("erstellt_von")] public long? ErstelltVon { get; set; }
    [Column("abgeschlossen_am")] public DateTime? AbgeschlossenAm { get; set; }
    [Column("abgeschlossen_von")] public long? AbgeschlossenVon { get; set; }
}

[Table("jahresabschluss_rechnung")]
public sealed class JahresabschlussRechnungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("saison_id")] public int SaisonId { get; set; }
    [Column("lieferant")] public string Lieferant { get; set; } = string.Empty;
    [Column("rechnungsnummer")] public string? Rechnungsnummer { get; set; }
    [Column("rechnungsdatum")] public DateTime Rechnungsdatum { get; set; }
    [Column("leistungs_von")] public DateTime? LeistungsVon { get; set; }
    [Column("leistungs_bis")] public DateTime? LeistungsBis { get; set; }
    [Column("gesamtbetrag")] public decimal Gesamtbetrag { get; set; }
    [Column("bemerkung")] public string? Bemerkung { get; set; }
}

[Table("jahresabschluss_rechnung_zuordnung")]
public sealed class JahresabschlussRechnungZuordnungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("rechnung_id")] public long RechnungId { get; set; }
    [Column("kostenart_id")] public long KostenartId { get; set; }
    [Column("umlageart_id")] public long UmlageartId { get; set; }
    [Column("betrag")] public decimal Betrag { get; set; }
    [Column("parzelle_id")] public long? ParzelleId { get; set; }
    [Column("mitglied_id")] public long? MitgliedId { get; set; }
    [Column("bemerkung")] public string? Bemerkung { get; set; }
}

[Table("jahresabschluss_position")]
public sealed class JahresabschlussPositionRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("jahresabschluss_id")] public long JahresabschlussId { get; set; }
    [Column("parzelle_id")] public long? ParzelleId { get; set; }
    [Column("mitglied_id")] public long? MitgliedId { get; set; }
    [Column("position_typ")] public string PositionTyp { get; set; } = string.Empty;
    [Column("menge")] public decimal? Menge { get; set; }
    [Column("einheit")] public string? Einheit { get; set; }
    [Column("preis")] public decimal? Preis { get; set; }
    [Column("betrag")] public decimal Betrag { get; set; }
    [Column("bemerkung")] public string? Bemerkung { get; set; }
}

public sealed class JahresabschlussPruefung
{
    public bool KannAbschliessen { get; init; }
    public IReadOnlyList<string> Fehler { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnungen { get; init; } = Array.Empty<string>();
}

public sealed class JahresabschlussBerechnungErgebnis
{
    public bool Erfolgreich { get; init; }
    public string? Meldung { get; init; }
    public int PositionsAnzahl { get; init; }
    public decimal Gesamtbetrag { get; init; }
}
