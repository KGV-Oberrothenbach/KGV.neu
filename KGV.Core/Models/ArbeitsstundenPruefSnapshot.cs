using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace KGV.Core.Models;

public sealed class ArbeitsstundenPruefSnapshot
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("mitglied_id")]
    public int MitgliedId { get; set; }

    [JsonPropertyName("saison_id")]
    public int SaisonId { get; set; }

    [JsonPropertyName("datum")]
    public DateTime Datum { get; set; }

    [JsonPropertyName("stunden")]
    public decimal Stunden { get; set; }

    [JsonPropertyName("art_der_arbeit")]
    public string ArtDerArbeit { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("freigegeben")]
    public bool Freigegeben { get; set; }

    [JsonPropertyName("genehmigt_von")]
    public int? GenehmigtVon { get; set; }

    [JsonPropertyName("genehmigt_am")]
    public DateTime? GenehmigtAm { get; set; }

    public static ArbeitsstundenPruefSnapshot FromRecord(ArbeitsstundeRecord record)
    {
        return new ArbeitsstundenPruefSnapshot
        {
            Id = record.Id,
            MitgliedId = record.MitgliedId,
            SaisonId = record.SaisonId,
            Datum = record.Datum,
            Stunden = record.Stunden,
            ArtDerArbeit = record.ArtDerArbeit,
            Status = ArbeitsstundenPruefprozess.NormalizeStatus(record.Status, record.Freigegeben),
            Freigegeben = record.Freigegeben,
            GenehmigtVon = record.GenehmigtVon,
            GenehmigtAm = record.GenehmigtAm
        };
    }

    public string ToSummary(string? mitgliedDisplayName = null)
    {
        var status = ArbeitsstundenPruefprozess.BuildStatusDisplay(Status, Freigegeben);
        var memberPart = string.IsNullOrWhiteSpace(mitgliedDisplayName) ? string.Empty : $"{mitgliedDisplayName} · ";
        return $"{memberPart}{Datum:dd.MM.yyyy} · {Stunden.ToString("0.##", CultureInfo.CurrentCulture)} h · {ArtDerArbeit} · {status}";
    }
}
