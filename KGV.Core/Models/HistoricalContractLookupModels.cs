using System.Text.Json.Serialization;

namespace KGV.Core.Models;

public sealed record HistoricalContractLookupMember(
    int Id, long? Mitgliedsnummer, string? Vorname, string? Name,
    [property: JsonPropertyName("mitglied_seit")] DateTime? MitgliedSeit, bool Aktiv);

public sealed record HistoricalContractLookupLease(
    long PachtverhaeltnisId, long ParzelleId, string? GartenNr, string? Anlage,
    string Rolle, DateTime GueltigAb, DateTime? GueltigBis,
    DateTime? UrspruenglicherPachtbeginn, DateTime? BeendetAm);

public sealed record HistoricalContractDocumentLink(long DocumentId, string? Title, int? Number, DateTime? Date, string? MimeType);
