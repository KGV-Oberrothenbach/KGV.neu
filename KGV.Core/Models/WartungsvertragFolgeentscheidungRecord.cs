using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("wartungsvertrag_folgeentscheidung")]
public sealed class WartungsvertragFolgeentscheidungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("vorgaenger_zuordnung_id")] public long VorgangerZuordnungId { get; set; }
    [Column("entscheidung")] public string Entscheidung { get; set; } = string.Empty;
    [Column("entscheidungsdatum")] public DateTime Entscheidungsdatum { get; set; }
    [Column("entschieden_am")] public DateTime EntscheidenAm { get; set; }
    [Column("entschieden_von_auth_user_id")] public Guid EntschiedenVonAuthUserId { get; set; }
    [Column("vorstandsbeschlussdatum")] public DateTime? Vorstandsbeschlussdatum { get; set; }
    [Column("initiator")] public string? Initiator { get; set; }
    [Column("begruendung")] public string? Begrundung { get; set; }
    [Column("folgezuordnung_id")] public long? FolgezuordnungId { get; set; }
    [Column("kommunikationsvorgang_id")] public long? KommunikationsvorgangId { get; set; }
    [Column("zurueckgenommen_am")] public DateTime? ZurueckgenommenAm { get; set; }
    [Column("zurueckgenommen_von_auth_user_id")] public Guid? ZurueckgenommenVonAuthUserId { get; set; }
    [Column("ruecknahmebegruendung")] public string? RuecknahmeBegrundung { get; set; }
}
