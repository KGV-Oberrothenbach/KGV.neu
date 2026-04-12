using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("vereinskonfiguration")]
public sealed class VereinskonfigurationRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("aktiv")]
    public bool Aktiv { get; set; }

    [Column("kontoinhaber")]
    public string? Kontoinhaber { get; set; }

    [Column("bankname")]
    public string? Bankname { get; set; }

    [Column("iban")]
    public string? Iban { get; set; }

    [Column("bic")]
    public string? Bic { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}