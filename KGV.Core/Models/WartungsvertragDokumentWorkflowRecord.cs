using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("wartungsvertrag_dokument_workflow")]
public sealed class WartungsvertragDokumentWorkflowRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("wartungsvertrag_zuordnung_id")]
    public long WartungsvertragZuordnungId { get; set; }

    [Column("kommunikationsvorgang_id")]
    public long? KommunikationsvorgangId { get; set; }

    [Column("dokument_id")]
    public long? DokumentId { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("fehlermeldung")]
    public string? Fehlermeldung { get; set; }

    [Column("versuche")]
    public int Versuche { get; set; }
}
