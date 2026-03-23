using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using KGV.Core.Utilities;

namespace KGV.Core.Models;

[Table("termin")]
public sealed class TerminRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("datum")]
    [JsonConverter(typeof(PostgresDateOnlyJsonConverter))]
    public DateTime Datum { get; set; }

    [Column("start_uhrzeit")]
    public TimeSpan? StartUhrzeit { get; set; }

    [Column("end_uhrzeit")]
    public TimeSpan? EndUhrzeit { get; set; }

    [Column("sichtbar_ab")]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarAb { get; set; }

    [Column("sichtbar_bis")]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarBis { get; set; }

    [Column("aktiv")]
    public bool Aktiv { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
