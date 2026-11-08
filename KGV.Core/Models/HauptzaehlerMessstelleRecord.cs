using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("hauptzaehler_messstelle")]
public sealed class HauptzaehlerMessstelleRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("medium")]
    public string Medium { get; set; } = string.Empty;

    [Column("anlagenbereich")]
    public string Anlagenbereich { get; set; } = string.Empty;

    [Column("rfid_tag_uid")]
    public string? RfidTagUid { get; set; }
}
