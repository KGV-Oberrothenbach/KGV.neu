using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_pflichtstunden_uebersicht")]
public sealed class PflichtstundenUebersichtRecord : BaseModel
{
    [PrimaryKey("mitglied_id", false)]
    [Column("mitglied_id")]
    public int MitgliedId { get; set; }

    [Column("jahr")]
    public int? Jahr { get; set; }

    [Column("saison_jahr")]
    public int? SaisonJahr { get; set; }

    [Column("pflichtstunden_soll")]
    public decimal? PflichtstundenSoll { get; set; }

    [Column("geleistete_stunden")]
    public decimal? GeleisteteStunden { get; set; }

    [Column("offene_stunden")]
    public decimal? OffeneStunden { get; set; }

    [Column("hat_wartungsvertrag")]
    public bool HatWartungsvertrag { get; set; }

    [Column("altersbefreit")]
    public bool Altersbefreit { get; set; }

    [Column("ist_befreit")]
    public bool IstBefreit { get; set; }

    [Column("regelgrund")]
    public string? Regelgrund { get; set; }
}
