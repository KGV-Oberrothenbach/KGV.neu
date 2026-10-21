using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("vereinsfunktion")]
public sealed class VereinsfunktionRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("schluessel")] public string Schluessel { get; set; } = string.Empty;
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("aktiv")] public bool Aktiv { get; set; } = true;
    [Column("erlaubt_mehrfachbesetzung")] public bool ErlaubtMehrfachbesetzung { get; set; } = true;
}

[Table("vereinsfunktion_besetzung")]
public sealed class VereinsfunktionBesetzungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("vereinsfunktion_id")] public long VereinsfunktionId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("gueltig_ab")] public DateTime GueltigAb { get; set; }
    [Column("gueltig_bis")] public DateTime? GueltigBis { get; set; }
    [Column("sort_order")] public short SortOrder { get; set; } = 10;
}

[Table("vereinsfunktion_vertretung")]
public sealed class VereinsfunktionVertretungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("vertretene_besetzung_id")] public long VertreteneBesetzungId { get; set; }
    [Column("vertreter_besetzung_id")] public long VertreterBesetzungId { get; set; }
    [Column("gueltig_ab")] public DateTime GueltigAb { get; set; }
    [Column("gueltig_bis")] public DateTime? GueltigBis { get; set; }
}

[Table("vereinsfunktion_verwendung")]
public sealed class VereinsfunktionVerwendungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("vereinsfunktion_id")] public long VereinsfunktionId { get; set; }
    [Column("verwendungszweck")] public string Verwendungszweck { get; set; } = "impressum";
    [Column("sort_order")] public short SortOrder { get; set; } = 10;
    [Column("impressum_gruppe")] public string ImpressumGruppe { get; set; } = "weitere";
    [Column("impressum_gruppe_bezeichnung")] public string ImpressumGruppeBezeichnung { get; set; } = "Weitere Funktionen";
    [Column("impressum_gruppen_sort_order")] public short ImpressumGruppenSortOrder { get; set; } = 10;
    [Column("impressum_ist_hauptkontakt")] public bool ImpressumIstHauptkontakt { get; set; }
    [Column("impressum_zeige_ladungsfaehige_anschrift")] public bool ImpressumZeigeLadungsfaehigeAnschrift { get; set; }
}
