using System;
using System.Text.Json.Serialization;
using KGV.Core.Utilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_startseite_bekanntmachungen")]
public sealed class StartseiteBekanntmachungRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("bekanntmachung_id")]
    public int? BekanntmachungId { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("betreff")]
    public string? Betreff { get; set; }

    [Column("thema")]
    public string? Thema { get; set; }

    [Column("inhalt")]
    public string? Inhalt { get; set; }

    [Column("text")]
    public string? Text { get; set; }

    [Column("inhalt_html")]
    public string? InhaltHtml { get; set; }

    [Column("sichtbar_ab")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarAb { get; set; }
    [Column("sichtbar_bis")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarBis { get; set; }
    [Column("sort_order")]
    public int? SortOrder { get; set; }
    [Column("created_at")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? CreatedAt { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("kurztext")]
    public string? Kurztext { get; set; }

    [Column("veroeffentlicht_am")]
    public DateTime? VeroeffentlichtAm { get; set; }

    [Column("datum")]
    public DateTime? Datum { get; set; }

    [Column("erstellt_am")]
    public DateTime? ErstelltAm { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
