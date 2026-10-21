using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("impressum_kontakte")]
public sealed class ImpressumKontaktRecord : BaseModel
{
    [PrimaryKey("besetzung_id", false)] [Column("besetzung_id")] public long BesetzungId { get; set; }
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("gruppen_schluessel")] public string GruppenSchluessel { get; set; } = string.Empty;
    [Column("gruppen_bezeichnung")] public string GruppenBezeichnung { get; set; } = string.Empty;
    [Column("gruppen_sort_order")] public short GruppenSortOrder { get; set; }
    [Column("ist_hauptkontakt")] public bool IstHauptkontakt { get; set; }
    [Column("zeige_ladungsfaehige_anschrift")] public bool ZeigeLadungsfaehigeAnschrift { get; set; }
    [Column("vorname")] public string? Vorname { get; set; }
    [Column("nachname")] public string? Nachname { get; set; }
    [Column("handy")] public string? Handy { get; set; }
    [Column("adresse")] public string? Adresse { get; set; }
    [Column("plz")] public string? Plz { get; set; }
    [Column("ort")] public string? Ort { get; set; }
}
