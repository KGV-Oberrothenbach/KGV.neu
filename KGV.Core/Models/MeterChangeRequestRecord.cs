using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace KGV.Core.Models;
[Table("zaehlerwechsel_antrag")]
public sealed class MeterChangeRequestRecord : BaseModel
{
 [PrimaryKey("id",false)][Column("id")] public long Id {get;set;}
 [Column("antragsteller_mitglied_id")] public long AntragstellerMitgliedId {get;set;}
 [Column("parzelle_id")] public long ParzelleId {get;set;}
 [Column("medium")] public string Medium {get;set;}="";
 [Column("alter_zaehler_id")] public long AlterZaehlerId {get;set;}
 [Column("ausbaudatum")] public DateTime Ausbaudatum {get;set;}
 [Column("endstand")] public decimal Endstand {get;set;}
 [Column("neue_zaehlernummer")] public string NeueZaehlernummer {get;set;}="";
 [Column("eichdatum")] public DateTime Eichdatum {get;set;}
 [Column("einbaudatum")] public DateTime Einbaudatum {get;set;}
 [Column("anfangsstand")] public decimal Anfangsstand {get;set;}
 [Column("ausbau_foto_pfad")] public string? AusbauFotoPfad {get;set;}
 [Column("einbau_foto_pfad")] public string? EinbauFotoPfad {get;set;}
 [Column("eingereicht_am")] public DateTime EingereichtAm {get;set;}
 [Column("status")] public string Status {get;set;}="";
 [Column("pruefkommentar")] public string? Pruefkommentar {get;set;}
}
