using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using KGV.Core.Utilities;

namespace KGV.Core.Models;

[Table("arbeitseinsatz")]
public sealed class ArbeitseinsatzRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("datum")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftPostgresDateOnlyJsonConverter))]
    [JsonConverter(typeof(PostgresDateOnlyJsonConverter))]
    public DateTime Datum { get; set; }

    [Column("start_uhrzeit")]
    public TimeSpan? StartUhrzeit { get; set; }

    [Column("end_uhrzeit")]
    public TimeSpan? EndUhrzeit { get; set; }

    [Column("treffpunkt")]
    public string? Treffpunkt { get; set; }

    [Column("max_teilnehmer")]
    public int? MaxTeilnehmer { get; set; }

    [Column("stunden_wert")]
    public decimal StundenWert { get; set; }

    [Column("sichtbar_ab")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarAb { get; set; }

    [Column("sichtbar_bis")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarBis { get; set; }

    [Column("anmeldung_bis")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? AnmeldungBis { get; set; }

    [Column("aktiv")]
    public bool Aktiv { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_demo")]
    public bool IsDemo { get; set; }
}
