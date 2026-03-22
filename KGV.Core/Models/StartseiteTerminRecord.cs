using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_startseite_termine")]
public sealed class StartseiteTerminRecord : BaseModel
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

    [Column("ort")]
    public string? Ort { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("inhalt")]
    public string? Inhalt { get; set; }
}
