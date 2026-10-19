using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("get_mitglied_vereinsdokument_status")]
public sealed class MitgliedVereinsdokumentStatusRecord : BaseModel
{
    [PrimaryKey("dokumenttyp_code", false)] [Column("dokumenttyp_code")] public string DokumenttypCode { get; set; } = string.Empty;
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("pflicht")] public bool Pflicht { get; set; }
    [Column("vereinsdokument_fassung_id")] public long? VereinsdokumentFassungId { get; set; }
    [Column("fassung")] public string? Fassung { get; set; }
    [Column("gueltig_ab")] public DateTime? GueltigAb { get; set; }
    [Column("gueltig_bis")] public DateTime? GueltigBis { get; set; }
    [Column("storage_path")] public string? StoragePath { get; set; }
    [Column("dateiname")] public string? Dateiname { get; set; }
    [Column("bereitstellung_id")] public long? BereitstellungId { get; set; }
    [Column("bereitstellungsart")] public string? Bereitstellungsart { get; set; }
    [Column("bereitgestellt_am")] public DateTime? BereitgestelltAm { get; set; }
    [Column("bereitgestellt_von")] public Guid? BereitgestelltVon { get; set; }
    [Column("status")] public string Status { get; set; } = string.Empty;
}
