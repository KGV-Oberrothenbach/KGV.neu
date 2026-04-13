using System;
using System.Text.Json.Serialization;
using KGV.Core.Utilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("mitglied_gesetzlicher_vertreter")]
public sealed class MitgliedGesetzlicherVertreterRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("minderjaehriges_mitglied_id")]
    public int MinderjaehrigesMitgliedId { get; set; }

    [Column("vertreter_mitglied_id")]
    public int VertreterMitgliedId { get; set; }

    [Column("gueltig_ab")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftPostgresDateOnlyJsonConverter))]
    [JsonConverter(typeof(PostgresDateOnlyJsonConverter))]
    public DateTime GueltigAb { get; set; }

    [Column("gueltig_bis")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresDateOnlyJsonConverter))]
    [JsonConverter(typeof(NullablePostgresDateOnlyJsonConverter))]
    public DateTime? GueltigBis { get; set; }

    [Column("bemerkung")]
    public string? Bemerkung { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}