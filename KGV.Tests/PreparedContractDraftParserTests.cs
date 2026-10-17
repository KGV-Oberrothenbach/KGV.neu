using System.Text.Json;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedContractDraftParserTests
{
    [Fact]
    public void MembershipDraft_RoundTripsWithWebV1Names()
    {
        var draft = new PreparedMembershipApplicationDraft { MemberId = 17, StartDate = "2026-11-01", MemberFee = 42.50m, IncludeSecondaryMember = true, Representative = new LegalRepresentativeDraft { Mode = "manual", Vorname = "Erika", Nachname = "Muster", AdresseAbweichend = true, Adresse = "Gartenweg 1", Plz = "01234", Ort = "Ort" } };
        var parsed = Assert.IsType<PreparedMembershipApplicationDraft>(PreparedContractDraftParser.Parse(PreparedContractDraftParser.Serialize(draft)));
        Assert.Equal(draft, parsed);
    }

    [Fact]
    public void LeaseDraft_RoundTripsAllV1Fields()
    {
        var draft = new PreparedLeaseContractDraft { MemberId = 17, Draft = new LeaseContractDraft { StartDate = "2026-11-01", ParcelId = 23, HasPreviousContract = false, PreviousContractDate = null, IncludeSecondaryMember = true, HandoverDate = "2026-11-02", FixedUntil = "2027-10-31", GardenRulesVersion = "2026", GardenRulesHandedOverAt = "2026-11-01", AdditionalAgreements = "Zusatz", Attachment1 = true, Attachment2 = false, Attachment3 = true, Attachment4 = false, ClubSignatory = "Vorstand" } };
        var parsed = Assert.IsType<PreparedLeaseContractDraft>(PreparedContractDraftParser.Parse(PreparedContractDraftParser.Serialize(draft)));
        Assert.Equal(draft, parsed);
    }

    [Theory]
    [InlineData("{\"schema_version\":\"other\",\"document_type\":\"mitgliedsantrag\",\"memberId\":1,\"startDate\":\"2026-01-01\",\"memberFee\":1,\"includeSecondaryMember\":false}")]
    [InlineData("{\"schema_version\":\"kgv-prepared-contract/v1\",\"document_type\":\"unknown\",\"memberId\":1}")]
    [InlineData("{\"schema_version\":\"kgv-prepared-contract/v1\",\"document_type\":\"mitgliedsantrag\",\"memberId\":1,\"startDate\":\"2026-01-01\",\"memberFee\":1,\"includeSecondaryMember\":false,\"signature\":{}}")]
    public void InvalidOrSignedDraft_IsRejected(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Throws<PreparedContractValidationException>(() => PreparedContractDraftParser.Parse(document.RootElement));
    }

    [Fact]
    public void CanonicalComparison_IgnoresObjectPropertyOrder()
    {
        using var first = JsonDocument.Parse("{\"a\":1,\"nested\":{\"b\":2,\"a\":3}}");
        using var second = JsonDocument.Parse("{\"nested\":{\"a\":3,\"b\":2},\"a\":1}");
        Assert.True(PreparedContractDraftParser.AreEquivalent(first.RootElement, second.RootElement));
    }
}
