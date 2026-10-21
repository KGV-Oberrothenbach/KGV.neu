using System.Text.Json;
using System.Text.Json.Serialization;

namespace KGV.Core.Models;

public static class PreparedContractConstants
{
    public const string SchemaVersion = "kgv-prepared-contract/v1";
    public const string ContractSignatureProcess = "vertrag_signatur";
    public const string MembershipApplication = "mitgliedsantrag";
    public const string LeaseContract = "pachtvertrag";
    public const string MaintenanceContract = "wartungsvertrag";
    public const string Prepared = "vorbereitet";
    public const string ReadyForSignature = "zur_signatur";
    public const string FinalReview = "letzte_pruefung";
    public const string Completed = "erledigt";
    public const string Discarded = "verworfen";
}

public sealed record KommunikationsvorgangDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("vorgangstyp")] public string Vorgangstyp { get; init; } = "";
    [JsonPropertyName("dokumenttyp")] public string Dokumenttyp { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("mitglied_id")] public int MitgliedId { get; init; }
    [JsonPropertyName("parzelle_id")] public int? ParzelleId { get; init; }
    [JsonPropertyName("entwurf_daten")] public JsonElement EntwurfDaten { get; init; }
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("signatur_revision")] public int? SignaturRevision { get; init; }
    [JsonPropertyName("signatur_gestartet_am")] public DateTimeOffset? SignaturGestartetAm { get; init; }
    [JsonPropertyName("final_dokument_id")] public long? FinalDokumentId { get; init; }
    [JsonPropertyName("wartungsvertrag_zuordnung_id")] public long? WartungsvertragZuordnungId { get; init; }
    [JsonPropertyName("zugewiesen_an")] public Guid? ZugewiesenAn { get; init; }
    [JsonPropertyName("erstellt_am")] public DateTimeOffset ErstelltAm { get; init; }
    [JsonPropertyName("bearbeitet_am")] public DateTimeOffset BearbeitetAm { get; init; }
}

public sealed record FinalDocumentProvenance(long DocumentId, long SourceKommunikationsvorgangId, int SourceRevision);

public sealed record LegalRepresentativeDraft
{
    [JsonPropertyName("mode")] public string Mode { get; init; } = "existing";
    [JsonPropertyName("memberId")] public int? MemberId { get; init; }
    [JsonPropertyName("vorname")] public string Vorname { get; init; } = "";
    [JsonPropertyName("nachname")] public string Nachname { get; init; } = "";
    [JsonPropertyName("adresseAbweichend")] public bool AdresseAbweichend { get; init; }
    [JsonPropertyName("adresse")] public string Adresse { get; init; } = "";
    [JsonPropertyName("plz")] public string Plz { get; init; } = "";
    [JsonPropertyName("ort")] public string Ort { get; init; } = "";
}

public sealed record PreparedMembershipApplicationDraft
{
    [JsonPropertyName("schema_version")] public string SchemaVersion { get; init; } = PreparedContractConstants.SchemaVersion;
    [JsonPropertyName("document_type")] public string DocumentType { get; init; } = PreparedContractConstants.MembershipApplication;
    [JsonPropertyName("memberId")] public int MemberId { get; init; }
    [JsonPropertyName("startDate")] public string StartDate { get; init; } = "";
    [JsonPropertyName("memberFee")] public decimal MemberFee { get; init; }
    [JsonPropertyName("representative")] public LegalRepresentativeDraft? Representative { get; init; }
    [JsonPropertyName("includeSecondaryMember")] public bool IncludeSecondaryMember { get; init; }
    [JsonPropertyName("vereinsunterlagen_nachweis")] public IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? VereinsunterlagenNachweis { get; init; }
}

public sealed record VereinsunterlagenNachweisSnapshotItem
{
    [JsonPropertyName("dokumenttyp")] public string Dokumenttyp { get; init; } = "";
    [JsonPropertyName("vereinsdokument_fassung_id")] public long VereinsdokumentFassungId { get; init; }
    [JsonPropertyName("fassung")] public string Fassung { get; init; } = "";
    [JsonPropertyName("gueltig_ab")] public DateTime GueltigAb { get; init; }
    [JsonPropertyName("gueltig_bis")] public DateTime? GueltigBis { get; init; }
    [JsonPropertyName("bereitstellung_id")] public long BereitstellungId { get; init; }
    [JsonPropertyName("bereitstellungsart")] public string Bereitstellungsart { get; init; } = "";
    [JsonPropertyName("bereitgestellt_am")] public DateTime BereitgestelltAm { get; init; }
}

public sealed record LeaseContractDraft
{
    [JsonPropertyName("startDate")] public string StartDate { get; init; } = "";
    [JsonPropertyName("parcelId")] public int? ParcelId { get; init; }
    [JsonPropertyName("hasPreviousContract")] public bool? HasPreviousContract { get; init; }
    [JsonPropertyName("previousContractDate")] public string? PreviousContractDate { get; init; }
    [JsonPropertyName("includeSecondaryMember")] public bool IncludeSecondaryMember { get; init; }
    [JsonPropertyName("secondaryMemberId")] public int? SecondaryMemberId { get; init; }
    [JsonPropertyName("effectiveDate")] public string? EffectiveDate { get; init; }
    [JsonPropertyName("handoverDate")] public string HandoverDate { get; init; } = "";
    [JsonPropertyName("fixedUntil")] public string FixedUntil { get; init; } = "";
    [JsonPropertyName("gardenRulesVersion")] public string GardenRulesVersion { get; init; } = "";
    [JsonPropertyName("gardenRulesHandedOverAt")] public string GardenRulesHandedOverAt { get; init; } = "";
    [JsonPropertyName("additionalAgreements")] public string AdditionalAgreements { get; init; } = "";
    [JsonPropertyName("attachment1")] public bool Attachment1 { get; init; }
    [JsonPropertyName("attachment2")] public bool Attachment2 { get; init; }
    [JsonPropertyName("attachment3")] public bool Attachment3 { get; init; }
    [JsonPropertyName("attachment4")] public bool Attachment4 { get; init; }
    [JsonPropertyName("clubSignatory")] public string ClubSignatory { get; init; } = "";
}

public sealed record PreparedLeaseContractDraft
{
    [JsonPropertyName("schema_version")] public string SchemaVersion { get; init; } = PreparedContractConstants.SchemaVersion;
    [JsonPropertyName("document_type")] public string DocumentType { get; init; } = PreparedContractConstants.LeaseContract;
    [JsonPropertyName("memberId")] public int MemberId { get; init; }
    [JsonPropertyName("draft")] public LeaseContractDraft Draft { get; init; } = new();
    [JsonPropertyName("vereinsunterlagen_nachweis")] public IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? VereinsunterlagenNachweis { get; init; }
}

public sealed class PreparedContractValidationException : Exception
{
    public PreparedContractValidationException(string message) : base(message) { }
}
