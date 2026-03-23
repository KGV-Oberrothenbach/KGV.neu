using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("arbeitseinsatz_anmeldung")]
public sealed class ArbeitseinsatzAnmeldungRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("arbeitseinsatz_id")]
    public int ArbeitseinsatzId { get; set; }

    [Column("mitglied_id")]
    public int MitgliedId { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("angemeldet_am")]
    public DateTime AngemeldetAm { get; set; }

    [Column("bemerkung")]
    public string? Bemerkung { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
