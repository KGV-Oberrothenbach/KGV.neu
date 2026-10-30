using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("serienbrief_vorlage")]
public sealed class SerienbriefVorlageRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Column("betreff_vorlage")]
    [JsonPropertyName("betreff_vorlage")]
    public string BetreffVorlage { get; set; } = string.Empty;

    [Column("anrede_modus")]
    [JsonPropertyName("anrede_modus")]
    public string AnredeModus { get; set; } = "briefanrede";

    [Column("html_inhalt")]
    [JsonPropertyName("html_inhalt")]
    public string HtmlInhalt { get; set; } = string.Empty;

    [Column("grundlayout_template_key")]
    [JsonPropertyName("grundlayout_template_key")]
    public string GrundlayoutTemplateKey { get; set; } = "briefbogen-allgemein";

    [Column("grundlayout_version")]
    [JsonPropertyName("grundlayout_version")]
    public string GrundlayoutVersion { get; set; } = "v2";

    [Column("aktiv")]
    [JsonPropertyName("aktiv")]
    public bool Aktiv { get; set; }

    [Column("erstellt_am")]
    [JsonPropertyName("erstellt_am")]
    public DateTimeOffset ErstelltAm { get; set; }

    [Column("erstellt_von")]
    [JsonPropertyName("erstellt_von")]
    public Guid? ErstelltVon { get; set; }

    [Column("geaendert_am")]
    [JsonPropertyName("geaendert_am")]
    public DateTimeOffset GeaendertAm { get; set; }

    [Column("geaendert_von")]
    [JsonPropertyName("geaendert_von")]
    public Guid? GeaendertVon { get; set; }
}

[Table("serienbrief_vorlage_exportfeld")]
public sealed class SerienbriefVorlageExportfeldRecord : BaseModel
{
    [PrimaryKey("serienbrief_vorlage_id", false)]
    [Column("serienbrief_vorlage_id")]
    [JsonPropertyName("serienbrief_vorlage_id")]
    public long SerienbriefVorlageId { get; set; }

    [Column("feldschluessel")]
    [JsonPropertyName("feldschluessel")]
    public string Feldschluessel { get; set; } = string.Empty;
}

[Table("serienbrief_vorlage_freifeld")]
public sealed class SerienbriefVorlageFreifeldRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("serienbrief_vorlage_id")]
    [JsonPropertyName("serienbrief_vorlage_id")]
    public long SerienbriefVorlageId { get; set; }

    [Column("schluessel")]
    [JsonPropertyName("schluessel")]
    public string Schluessel { get; set; } = string.Empty;

    [Column("anzeigename")]
    [JsonPropertyName("anzeigename")]
    public string Anzeigename { get; set; } = string.Empty;

    [Column("typ")]
    [JsonPropertyName("typ")]
    public string Typ { get; set; } = string.Empty;

    [Column("pflicht")]
    [JsonPropertyName("pflicht")]
    public bool Pflicht { get; set; }

    [Column("sortierung")]
    [JsonPropertyName("sortierung")]
    public int Sortierung { get; set; }
}
