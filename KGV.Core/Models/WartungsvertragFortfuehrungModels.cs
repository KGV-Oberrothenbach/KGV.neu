using System.Text.Json.Serialization;

namespace KGV.Core.Models;

/// <summary>Server-projected state of a maintenance-contract continuation. Authorization flags are authoritative.</summary>
public sealed class WartungsvertragFortfuehrungDetail
{
    [JsonPropertyName("vorgaenger_zuordnung_id")] public long VorgaengerZuordnungId { get; init; }
    [JsonPropertyName("hauptmitglied_id")] public int HauptmitgliedId { get; init; }
    [JsonPropertyName("mitglied_name")] public string? MitgliedName { get; set; }
    [JsonPropertyName("wartungsvertrag_id")] public long WartungsvertragId { get; init; }
    [JsonPropertyName("wartungsvertrag_titel")] public string? WartungsvertragTitel { get; init; }
    [JsonPropertyName("gueltig_ab")] public DateTime GueltigAb { get; init; }
    [JsonPropertyName("gueltig_bis")] public DateTime? GueltigBis { get; init; }
    [JsonPropertyName("folgeentscheidung_id")] public long? FolgeentscheidungId { get; init; }
    [JsonPropertyName("entscheidung")] public string? Entscheidung { get; init; }
    [JsonPropertyName("entscheidungsdatum")] public DateTime? Entscheidungsdatum { get; init; }
    [JsonPropertyName("vorstandsbeschlussdatum")] public DateTime? Vorstandsbeschlussdatum { get; init; }
    [JsonPropertyName("initiator")] public string? Initiator { get; init; }
    [JsonPropertyName("begruendung")] public string? Begruendung { get; init; }
    [JsonPropertyName("folgezuordnung_id")] public long? FolgezuordnungId { get; init; }
    [JsonPropertyName("folge_wartungsvertrag_id")] public long? FolgeWartungsvertragId { get; init; }
    [JsonPropertyName("folge_wartungsvertrag_titel")] public string? FolgeWartungsvertragTitel { get; init; }
    [JsonPropertyName("folge_gueltig_ab")] public DateTime? FolgeGueltigAb { get; init; }
    [JsonPropertyName("folge_gueltig_bis")] public DateTime? FolgeGueltigBis { get; init; }
    [JsonPropertyName("kommunikationsvorgang_id")] public long? KommunikationsvorgangId { get; init; }
    [JsonPropertyName("workflow_id")] public long? WorkflowId { get; init; }
    [JsonPropertyName("workflow_art")] public string? WorkflowArt { get; init; }
    [JsonPropertyName("workflow_status")] public string? WorkflowStatus { get; init; }
    [JsonPropertyName("dokument_id")] public long? DokumentId { get; init; }
    [JsonPropertyName("zurueckgenommen_am")] public DateTime? ZurueckgenommenAm { get; init; }
    [JsonPropertyName("ruecknahmebegruendung")] public string? RuecknahmeBegruendung { get; init; }
    [JsonPropertyName("can_decide")] public bool CanDecide { get; init; }
    [JsonPropertyName("can_withdraw_nichtverlaengerung")] public bool CanWithdrawNichtverlaengerung { get; init; }
}

public sealed record WartungsvertragVerlaengerungResult(
    [property: JsonPropertyName("folgeentscheidung_id")] long FolgeentscheidungId,
    [property: JsonPropertyName("folgezuordnung_id")] long FolgezuordnungId,
    [property: JsonPropertyName("kommunikationsvorgang_id")] long KommunikationsvorgangId,
    [property: JsonPropertyName("workflow_id")] long WorkflowId,
    [property: JsonPropertyName("vertragspartei_mitglied_id")] long VertragsparteiMitgliedId);
