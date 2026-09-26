using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("parzellen_protokoll")]
public sealed class ParzellenProtokollRecord : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("parzelle_id")] public long ParzelleId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("protokoll_typ")] public string ProtokollTyp { get; set; } = string.Empty;
    [Column("protokoll_datum")] public DateTime ProtokollDatum { get; set; }
    [Column("vorstand_mitglied_id")] public long VorstandMitgliedId { get; set; }
    [Column("vorstand2_mitglied_id")] public long Vorstand2MitgliedId { get; set; }
    [Column("begleitperson_mitglied_id")] public long? BegleitpersonMitgliedId { get; set; }
    [Column("begleitperson_name")] public string? BegleitpersonName { get; set; }
    [Column("zustand_bemerkung")] public string? ZustandBemerkung { get; set; }
    [Column("vereinbarung")] public string? Vereinbarung { get; set; }
}
