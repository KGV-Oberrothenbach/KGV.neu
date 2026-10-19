using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("vereinsdokument_fassung")]
public sealed class VereinsdokumentFassungRecord : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")] public long Id { get; set; }
    [Column("dokumenttyp_code")] public string DokumenttypCode { get; set; } = string.Empty;
    [Column("fassung")] public string Fassung { get; set; } = string.Empty;
    [Column("gueltig_ab")] public DateTime GueltigAb { get; set; }
    [Column("gueltig_bis")] public DateTime? GueltigBis { get; set; }
    [Column("storage_path")] public string? StoragePath { get; set; }
    [Column("dateiname")] public string? Dateiname { get; set; }
    [Column("sha256")] public string? Sha256 { get; set; }
    [Column("erstellt_am")] public DateTime ErstelltAm { get; set; }
    [Column("erstellt_von")] public Guid? ErstelltVon { get; set; }
}
