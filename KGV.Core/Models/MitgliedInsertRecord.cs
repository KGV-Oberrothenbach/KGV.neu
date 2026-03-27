using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("mitglied")]
public sealed class MitgliedInsertRecord : BaseModel
{
    [Column("hauptmitglied_id")]
    public int? HauptmitgliedId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("vorname")]
    public string Vorname { get; set; } = string.Empty;

    [Column("adresse")]
    public string? Adresse { get; set; }

    [Column("plz")]
    public string? Plz { get; set; }

    [Column("ort")]
    public string? Ort { get; set; }

    [Column("telefon")]
    public string? Telefon { get; set; }

    [Column("handy")]
    public string? Handy { get; set; }

    [Column("email")]
    public string? Email { get; set; }
}