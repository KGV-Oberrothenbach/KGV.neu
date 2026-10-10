using System;
using System.Text.Json.Serialization;
using KGV.Core.Utilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("v_startseite_termine")]
public sealed class StartseiteTerminRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("titel")]
    public string? Titel { get; set; }

    [Column("thema")]
    public string? Thema { get; set; }

    [Column("datum")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresDateOnlyJsonConverter))]
    [JsonConverter(typeof(NullablePostgresDateOnlyJsonConverter))]
    public DateTime? Datum { get; set; }

    [Column("start_uhrzeit")]
    public TimeSpan? StartUhrzeit { get; set; }

    [Column("end_uhrzeit")]
    public TimeSpan? EndUhrzeit { get; set; }

    [Column("sichtbar_ab")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarAb { get; set; }

    [Column("sichtbar_bis")]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftNullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    [JsonConverter(typeof(NullablePostgresTimestampWithoutTimeZoneJsonConverter))]
    public DateTime? SichtbarBis { get; set; }

    [Column("beginn")]
    public string? Beginn { get; set; }

    [Column("ende")]
    public string? Ende { get; set; }

    [Column("ort")]
    public string? Ort { get; set; }

    [Column("beschreibung")]
    public string? Beschreibung { get; set; }

    [Column("inhalt")]
    public string? Inhalt { get; set; }
}
