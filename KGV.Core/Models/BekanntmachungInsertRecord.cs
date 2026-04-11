using System;
using System.Text.Json.Serialization;
using KGV.Core.Utilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("bekanntmachung")]
public sealed class BekanntmachungInsertRecord : BaseModel
{
    [Column("titel")]
    public string? Titel { get; set; }

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

    [Column("aktiv")]
    public bool Aktiv { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}