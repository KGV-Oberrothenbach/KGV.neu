using System;
using System.Text.Json.Serialization;
using KGV.Core.Utilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("wartungsvertrag_zuordnungen")]
public sealed class WartungsvertragZuordnungInsertRecord : BaseModel
{
    [Column("wartungsvertrag_id")]
    public long WartungsvertragId { get; set; }

    [Column("hauptmitglied_id")]
    public long HauptmitgliedId { get; set; }

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