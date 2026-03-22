using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_startseite_bekanntmachung")]
public sealed class StartseiteBekanntmachungRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("thema")]
    public string? Thema { get; set; }

    [Column("inhalt")]
    public string? Inhalt { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("veroeffentlicht_am")]
    public DateTime? VeroeffentlichtAm { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
