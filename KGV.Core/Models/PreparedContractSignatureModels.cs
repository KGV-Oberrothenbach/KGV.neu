using System.Text.Json.Serialization;

namespace KGV.Core.Models;

public static class PreparedContractSignatureStatuses
{
    public const string Open = "offen";
    public const string Signed = "unterschrieben";
}

public static class PreparedContractBoardSignatureKeys
{
    public const string Board1 = "vorstand_1";
    public const string Board2 = "vorstand_2";
}

public sealed record PreparedContractSignatureSlot(
    string Key,
    short SortOrder,
    bool Required,
    string SignerType,
    string DisplayLabel,
    long? MemberId = null);

public sealed record PreparedContractSignatureDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("kommunikationsvorgang_id")] public long KommunikationsvorgangId { get; init; }
    [JsonPropertyName("signatur_revision")] public int SignaturRevision { get; init; }
    [JsonPropertyName("signatur_schluessel")] public string SignaturSchluessel { get; init; } = "";
    [JsonPropertyName("sort_order")] public short SortOrder { get; init; }
    [JsonPropertyName("pflicht")] public bool Pflicht { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("unterzeichner_typ")] public string UnterzeichnerTyp { get; init; } = "";
    [JsonPropertyName("mitglied_id")] public long? MitgliedId { get; init; }
    [JsonPropertyName("unterzeichner_name_snapshot")] public string? UnterzeichnerNameSnapshot { get; init; }
    [JsonPropertyName("vereinsfunktion_id")] public long? VereinsfunktionId { get; init; }
    [JsonPropertyName("vereinsfunktion_bezeichnung_snapshot")] public string? VereinsfunktionBezeichnungSnapshot { get; init; }
    [JsonPropertyName("vereinsfunktion_besetzung_id")] public long? VereinsfunktionBesetzungId { get; init; }
    [JsonPropertyName("vertretung_id")] public long? VertretungId { get; init; }
    [JsonPropertyName("vertretene_vereinsfunktion_id")] public long? VertreteneVereinsfunktionId { get; init; }
    [JsonPropertyName("vertretene_funktion_bezeichnung_snapshot")] public string? VertreteneFunktionBezeichnungSnapshot { get; init; }
    [JsonPropertyName("vertretene_besetzung_id")] public long? VertreteneBesetzungId { get; init; }
    [JsonPropertyName("storage_bucket")] public string? StorageBucket { get; init; }
    [JsonPropertyName("storage_path")] public string? StoragePath { get; init; }
    [JsonPropertyName("signatur_sha256")] public string? SignaturSha256 { get; init; }
    [JsonPropertyName("unterzeichnet_am")] public DateTimeOffset? UnterzeichnetAm { get; init; }
    [JsonPropertyName("unterzeichnet_von_auth_user_id")] public Guid? UnterzeichnetVonAuthUserId { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record PreparedContractAuthorizedSignatoryDto
{
    [JsonPropertyName("mitglied_id")] public long MitgliedId { get; init; }
    [JsonPropertyName("unterzeichner_name")] public string Anzeigename { get; init; } = "";
    [JsonPropertyName("berechtigungsart")] public string Berechtigungsart { get; init; } = "";
    [JsonPropertyName("vereinsfunktion_id")] public long VereinsfunktionId { get; init; }
    [JsonPropertyName("vereinsfunktion_bezeichnung")] public string VereinsfunktionBezeichnung { get; init; } = "";
    [JsonPropertyName("vereinsfunktion_besetzung_id")] public long VereinsfunktionBesetzungId { get; init; }
    [JsonPropertyName("vertretung_id")] public long? VertretungId { get; init; }
    [JsonPropertyName("vertretene_vereinsfunktion_id")] public long? VertreteneVereinsfunktionId { get; init; }
    [JsonPropertyName("vertretene_funktion_bezeichnung")] public string? VertreteneFunktionBezeichnung { get; init; }
    [JsonPropertyName("vertretene_besetzung_id")] public long? VertreteneBesetzungId { get; init; }
}

public sealed record PreparedContractSignatureSaveRequest(
    long KommunikationsvorgangId,
    int SignaturRevision,
    string SignaturSchluessel,
    byte[] PngBytes,
    long? MitgliedId,
    string? UnterzeichnerNameSnapshot = null,
    long? VereinsfunktionBesetzungId = null,
    long? VertretungId = null,
    long? VertreteneBesetzungId = null,
    DateTimeOffset? UnterzeichnetAm = null);

public sealed record PreparedContractSignatureProgress(
    int RequiredCount,
    int RequiredSignedCount,
    int OptionalCount,
    int OptionalSignedCount)
{
    public bool AllRequiredSigned => RequiredCount > 0 && RequiredCount == RequiredSignedCount;

    public static PreparedContractSignatureProgress From(IReadOnlyCollection<PreparedContractSignatureDto> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var required = positions.Where(x => x.Pflicht).ToArray();
        var optional = positions.Where(x => !x.Pflicht).ToArray();
        return new(required.Length, required.Count(x => x.Status == PreparedContractSignatureStatuses.Signed), optional.Length, optional.Count(x => x.Status == PreparedContractSignatureStatuses.Signed));
    }
}
