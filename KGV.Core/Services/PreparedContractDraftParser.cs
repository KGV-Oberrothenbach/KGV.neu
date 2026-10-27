using System.Text.Json;
using KGV.Core.Models;

namespace KGV.Core.Services;

public static class PreparedContractDraftParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = false };

    public static object Parse(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid("Der gespeicherte Vertragsentwurf ist ungültig.");
        if (ContainsSignatureData(value)) throw Invalid("Der gespeicherte Vertragsentwurf darf keine Signaturdaten enthalten.");
        var version = GetString(value, "schema_version");
        var type = GetString(value, "document_type");
        if (version != PreparedContractConstants.SchemaVersion || (type != PreparedContractConstants.MembershipApplication && type != PreparedContractConstants.LeaseContract && type != PreparedContractConstants.MaintenanceContract))
            throw Invalid("Die Version des gespeicherten Vertragsentwurfs wird nicht unterstützt.");
        if (!HasNumber(value, "memberId")) throw Invalid("Der gespeicherte Vertragsentwurf enthält kein Mitglied.");
        if (type == PreparedContractConstants.MembershipApplication)
        {
            if (!HasString(value, "startDate") || !HasNumber(value, "memberFee") || !HasBoolean(value, "includeSecondaryMember")) throw Invalid("Der gespeicherte Mitgliedsantrag ist unvollständig.");
            var membership = JsonSerializer.Deserialize<PreparedMembershipApplicationDraft>(value.GetRawText(), Options)!;
            if (membership.Representative is not null && membership.IncludeSecondaryMember)
                throw Invalid("Ein minderjähriges Mitglied kann kein Nebenmitglied im Mitgliedsantrag haben.");
            return membership;
        }
        if (type == PreparedContractConstants.MaintenanceContract)
        {
            if (!HasNumberOrNull(value, "wartungsvertrag_zuordnung_id") || !HasNumber(value, "wartungsvertrag_id") || !HasString(value, "gueltig_ab") || !HasStringOrNull(value, "gueltig_bis"))
                throw Invalid("Der gespeicherte Wartungsvertrag ist unvollständig.");
            return JsonSerializer.Deserialize<PreparedMaintenanceContractDraft>(value.GetRawText(), Options)!;
        }
        if (!value.TryGetProperty("draft", out var lease) || lease.ValueKind != JsonValueKind.Object || !HasStringOrNull(lease, "previousContractDate") ||
            !HasString(lease, "startDate") || !HasNumber(lease, "parcelId") || !HasBoolean(lease, "hasPreviousContract") || !HasBoolean(lease, "includeSecondaryMember") ||
            !HasString(lease, "handoverDate") || !HasString(lease, "fixedUntil") || !HasString(lease, "gardenRulesVersion") || !HasString(lease, "gardenRulesHandedOverAt") || !HasString(lease, "additionalAgreements") ||
            !HasBoolean(lease, "attachment1") || !HasBoolean(lease, "attachment2") || !HasBoolean(lease, "attachment3") || !HasBoolean(lease, "attachment4") || !HasString(lease, "clubSignatory"))
            throw Invalid("Der gespeicherte Pachtvertrag ist unvollständig.");
        return JsonSerializer.Deserialize<PreparedLeaseContractDraft>(value.GetRawText(), Options)!;
    }

    public static JsonElement Serialize(object draft)
    {
        var json = JsonSerializer.SerializeToElement(draft, Options);
        Parse(json);
        return json;
    }

    public static bool AreEquivalent(JsonElement left, JsonElement right) => Canonical(left) == Canonical(right);
    public static string Canonical(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x.Name) + ":" + Canonical(x.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(Canonical)) + "]",
        _ => value.GetRawText()
    };

    private static bool ContainsSignatureData(JsonElement value) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Any(x => x.Name.Contains("signature", StringComparison.OrdinalIgnoreCase) || ContainsSignatureData(x.Value)) || value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(ContainsSignatureData);
    private static string? GetString(JsonElement value, string name) => value.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    private static bool HasString(JsonElement value, string name) => GetString(value, name) is not null;
    private static bool HasStringOrNull(JsonElement value, string name) => value.TryGetProperty(name, out var item) && (item.ValueKind == JsonValueKind.String || item.ValueKind == JsonValueKind.Null);
    private static bool HasNumber(JsonElement value, string name) => value.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number;
    private static bool HasNumberOrNull(JsonElement value, string name) => value.TryGetProperty(name, out var item) && (item.ValueKind == JsonValueKind.Number || item.ValueKind == JsonValueKind.Null);
    private static bool HasBoolean(JsonElement value, string name) => value.TryGetProperty(name, out var item) && (item.ValueKind is JsonValueKind.True or JsonValueKind.False);
    private static PreparedContractValidationException Invalid(string message) => new(message);
}
