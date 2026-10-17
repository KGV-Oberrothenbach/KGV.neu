using System.Text.Json;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedLeaseContractMapperTests
{
    [Fact]
    public void Full_roundtrip_preserves_v1_fields()
    {
        var request = FullRequest();

        var first = PreparedLeaseContractMapper.ToPreparedDraft(request);
        var serialized = PreparedContractDraftParser.Serialize(first);
        var parsed = Assert.IsType<PreparedLeaseContractDraft>(PreparedContractDraftParser.Parse(serialized));
        var result = PreparedLeaseContractMapper.ToPreparedDraft(PreparedLeaseContractMapper.ToDocumentRequest(parsed, false));

        Assert.True(PreparedContractDraftParser.AreEquivalent(
            PreparedContractDraftParser.Serialize(first),
            PreparedContractDraftParser.Serialize(result)));
    }

    [Fact]
    public void No_previous_contract_clears_previous_contract_date()
    {
        var request = FullRequest();
        request.HasPreviousContract = false;
        request.AltvertragDatum = new DateTime(2022, 2, 3);

        var draft = PreparedLeaseContractMapper.ToPreparedDraft(request);

        Assert.False(draft.Draft.HasPreviousContract);
        Assert.Null(draft.Draft.PreviousContractDate);
    }

    [Fact]
    public void Previous_contract_requires_date()
    {
        var request = FullRequest();
        request.AltvertragDatum = null;

        Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractMapper.ToPreparedDraft(request));
    }

    [Fact]
    public void Previous_contract_decision_is_required()
    {
        var request = FullRequest();
        request.HasPreviousContract = null;

        Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractMapper.ToPreparedDraft(request));
    }

    [Fact]
    public void Minor_forces_secondary_member_false()
    {
        var request = FullRequest();
        request.IstMinderjaehrig = true;
        request.IncludeSecondaryMember = true;

        var draft = PreparedLeaseContractMapper.ToPreparedDraft(request);

        Assert.False(draft.Draft.IncludeSecondaryMember);
    }

    [Fact]
    public void Handover_defaults_to_start_date()
    {
        var request = FullRequest();
        request.UebergabeDatum = null;

        var draft = PreparedLeaseContractMapper.ToPreparedDraft(request);

        Assert.Equal(draft.Draft.StartDate, draft.Draft.HandoverDate);
    }

    [Fact]
    public void Optional_values_use_empty_v1_values()
    {
        var request = FullRequest();
        request.BefristetBis = null;
        request.KleingartenordnungFassung = null;
        request.KleingartenordnungAusgehaendigtAm = null;
        request.WeitereVereinbarungen = null;
        request.VereinsUnterzeichner = null;

        var draft = PreparedLeaseContractMapper.ToPreparedDraft(request);

        Assert.Equal("", draft.Draft.FixedUntil);
        Assert.Equal("", draft.Draft.GardenRulesVersion);
        Assert.Equal("", draft.Draft.GardenRulesHandedOverAt);
        Assert.Equal("", draft.Draft.AdditionalAgreements);
        Assert.Equal("", draft.Draft.ClubSignatory);
    }

    [Fact]
    public void Dates_are_exact_iso_dates()
    {
        var draft = PreparedLeaseContractMapper.ToPreparedDraft(FullRequest());

        Assert.Equal("2027-01-02", draft.Draft.StartDate);
        Assert.Equal("2020-03-04", draft.Draft.PreviousContractDate);
        Assert.Equal("2027-05-06", draft.Draft.HandoverDate);
        Assert.Equal("2028-07-08", draft.Draft.FixedUntil);
        Assert.Equal("2026-09-10", draft.Draft.GardenRulesHandedOverAt);
    }

    [Fact]
    public void Invalid_member_id_throws()
    {
        var request = FullRequest();
        request.MitgliedId = 0;

        Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractMapper.ToPreparedDraft(request));
    }

    [Fact]
    public void Invalid_parcel_id_throws()
    {
        var request = FullRequest();
        request.ParzelleId = 0;

        Assert.Throws<PreparedContractValidationException>(() => PreparedLeaseContractMapper.ToPreparedDraft(request));
    }

    [Fact]
    public void Serialized_prepared_structure_contains_no_runtime_or_signature_properties()
    {
        var request = FullRequest();
        request.Status = "vorbereitet";
        request.IstMinderjaehrig = true;
        var serialized = PreparedContractDraftParser.Serialize(PreparedLeaseContractMapper.ToPreparedDraft(request));

        var propertyNames = PropertyNames(serialized).ToArray();
        foreach (var forbidden in new[] { "Status", "IstMinderjaehrig", "VereinskonfigurationSnapshot", "Signature", "Signatur", "PDF", "StoragePath" })
            Assert.DoesNotContain(propertyNames, name => string.Equals(name, forbidden, StringComparison.OrdinalIgnoreCase));
    }

    private static PachtvertragDokumentRequest FullRequest() => new()
    {
        MitgliedId = 42,
        ParzelleId = 17,
        Vertragsbeginn = new DateTime(2027, 1, 2, 14, 30, 0),
        HasPreviousContract = true,
        AltvertragDatum = new DateTime(2020, 3, 4, 23, 59, 0),
        UebergabeDatum = new DateTime(2027, 5, 6, 8, 0, 0),
        BefristetBis = new DateTime(2028, 7, 8, 12, 0, 0),
        KleingartenordnungFassung = "Fassung 2026",
        KleingartenordnungAusgehaendigtAm = new DateTime(2026, 9, 10, 16, 0, 0),
        WeitereVereinbarungen = "Zusatzvereinbarung",
        Anlage1 = true,
        Anlage2 = false,
        Anlage3 = true,
        Anlage4 = true,
        VereinsUnterzeichner = "Vorstand",
        IncludeSecondaryMember = true
    };

    private static IEnumerable<string> PropertyNames(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
            {
                yield return property.Name;
                foreach (var name in PropertyNames(property.Value)) yield return name;
            }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
                foreach (var name in PropertyNames(item)) yield return name;
    }
}
