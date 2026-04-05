using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace KGV.Core.Models;

[Table("arbeitsstunde_pruefverlauf")]
public sealed class ArbeitsstundenPruefverlaufRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("arbeitsstunde_id")]
    public int ArbeitsstundeId { get; set; }

    [Column("aktion")]
    public string Aktion { get; set; } = string.Empty;

    [Column("begruendung")]
    public string Begruendung { get; set; } = string.Empty;

    [Column("geprueft_von")]
    public int GeprueftVon { get; set; }

    [Column("geprueft_am")]
    public DateTime GeprueftAm { get; set; }

    [Column("vorher_snapshot")]
    public string VorherSnapshot { get; set; } = string.Empty;

    [Column("nachher_snapshot")]
    public string? NachherSnapshot { get; set; }
}
