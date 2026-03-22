using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_startseite_arbeitseinsatz")]
public sealed class StartseiteArbeitseinsatzRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("thema")]
    public string? Thema { get; set; }

    [Column("datum")]
    public DateTime? Datum { get; set; }

    [Column("beginn")]
    public string? Beginn { get; set; }

    [Column("ende")]
    public string? Ende { get; set; }

    [Column("treffpunkt")]
    public string? Treffpunkt { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("freie_plaetze")]
    public int? FreiePlaetze { get; set; }

    [Column("angemeldet_count")]
    public int? AngemeldetCount { get; set; }

    [Column("anmeldung_moeglich")]
    public bool? AnmeldungMoeglich { get; set; }
}
