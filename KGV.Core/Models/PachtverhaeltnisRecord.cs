using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("pachtverhaeltnis")]
public sealed class PachtverhaeltnisRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("parzelle_id")] public long ParzelleId { get; set; }
    [Column("beendet_am")] public DateTime? BeendetAm { get; set; }
    [Column("urspruenglicher_pachtbeginn")] public DateTime? UrspruenglicherPachtbeginn { get; set; }
}

[Table("pachtvertragsfassung")]
public sealed class PachtvertragsfassungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("pachtverhaeltnis_id")] public long PachtverhaeltnisId { get; set; }
}
