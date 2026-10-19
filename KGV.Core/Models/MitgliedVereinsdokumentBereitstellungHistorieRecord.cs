using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("get_mitglied_vereinsdokument_bereitstellung_historie")]
public sealed class MitgliedVereinsdokumentBereitstellungHistorieRecord : BaseModel
{
    [PrimaryKey("bereitstellung_id", false)] [Column("bereitstellung_id")] public long BereitstellungId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("vereinsdokument_fassung_id")] public long VereinsdokumentFassungId { get; set; }
    [Column("dokumenttyp_code")] public string DokumenttypCode { get; set; } = string.Empty;
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("fassung")] public string Fassung { get; set; } = string.Empty;
    [Column("bereitstellungsart")] public string Bereitstellungsart { get; set; } = string.Empty;
    [Column("bereitgestellt_am")] public DateTime BereitgestelltAm { get; set; }
    [Column("bereitgestellt_von")] public Guid? BereitgestelltVon { get; set; }
    [Column("storniert_am")] public DateTime? StorniertAm { get; set; }
    [Column("storniert_von")] public Guid? StorniertVon { get; set; }
    [Column("storno_grund")] public string? StornoGrund { get; set; }
}
