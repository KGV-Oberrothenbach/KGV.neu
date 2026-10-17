using KGV.Core.Models;
using KGV.Core.Services;
using System.Text.Json;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationMapperTests
{
    [Fact]
    public void AdultRequest_RoundTripsV1WithoutRepresentative()
    {
        var request = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = new DateTime(2027, 1, 1, 18, 0, 0), Mitgliedsbeitrag = 42m, NebenmitgliedEinbeziehen = true, Aufnahmegebuehr = 12m, Status = "x", NebenmitgliedId = 5 };
        var draft = PreparedMembershipApplicationMapper.ToPreparedDraft(request, false);
        Assert.Equal("2027-01-01", draft.StartDate); Assert.Null(draft.Representative); Assert.Equal(42m, PreparedMembershipApplicationMapper.ToDocumentRequest(draft, false).Mitgliedsbeitrag);
    }

    [Fact]
    public void MinorExistingRepresentative_RoundTrips()
    {
        var request = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = new DateTime(2027, 1, 1), Mitgliedsbeitrag = 1, GesetzlicherVertreterAusBestehendemMitglied = true, GesetzlicherVertreterMitgliedId = 9 };
        var draft = PreparedMembershipApplicationMapper.ToPreparedDraft(request, true);
        Assert.Equal("existing", draft.Representative!.Mode); Assert.Equal(9, PreparedMembershipApplicationMapper.ToDocumentRequest(draft, true).GesetzlicherVertreterMitgliedId);
    }

    [Fact]
    public void ExistingRepresentativeWithoutMemberId_IsRejected()
    {
        var request = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = new DateTime(2026, 12, 31), GesetzlicherVertreterAusBestehendemMitglied = true };
        Assert.Throws<PreparedContractValidationException>(() => PreparedMembershipApplicationMapper.ToPreparedDraft(request, true));
    }

    [Fact]
    public void PersistedJson_ContainsNoLegacyOrSignatureFields()
    {
        var request = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = new DateTime(2026, 12, 31), Mitgliedsbeitrag = 1, Aufnahmegebuehr = 2, Status = "x", IstMinderjaehrig = true, NebenmitgliedId = 3, VereinskonfigurationSnapshot = new VereinskonfigurationSnapshot() };
        var json = JsonSerializer.Serialize(PreparedMembershipApplicationMapper.ToPreparedDraft(request, false));
        foreach (var forbidden in new[] { "Aufnahmegebuehr", "Status", "IstMinderjaehrig", "NebenmitgliedId", "VereinskonfigurationSnapshot", "signature", "signatures", "base64", "dataUrl" }) Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "12345", "Ort")]
    [InlineData("Weg 1", "", "Ort")]
    [InlineData("Weg 1", "12345", "")]
    public void ManualRepresentative_WithMissingRequiredDifferentAddressPart_IsRejected(string address, string postalCode, string city)
    {
        var request = new MitgliedsantragDokumentRequest
        {
            MitgliedId = 7,
            BeginnDatum = new DateTime(2026, 12, 31),
            GesetzlicherVertreterAdresseAbweichend = true,
            GesetzlicherVertreterSnapshot = new MitgliedsantragVertreterSnapshot
            {
                Vorname = "Ada", Nachname = "Muster", Adresse = address, Plz = postalCode, Ort = city
            }
        };

        Assert.Throws<PreparedContractValidationException>(() => PreparedMembershipApplicationMapper.ToPreparedDraft(request, true));
    }

    [Fact]
    public void ManualRepresentative_RoundTripsAndRejectsIncompleteData()
    {
        var request = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = new DateTime(2026, 12, 31), Mitgliedsbeitrag = 12, GesetzlicherVertreterAdresseAbweichend = true, GesetzlicherVertreterSnapshot = new MitgliedsantragVertreterSnapshot { Vorname = "Ada", Nachname = "Muster", Adresse = "Weg 1", Plz = "12345", Ort = "Ort" } };
        var draft = PreparedMembershipApplicationMapper.ToPreparedDraft(request, true);
        Assert.Equal("manual", draft.Representative!.Mode); Assert.Null(draft.Representative.MemberId); Assert.Equal("2026-12-31", draft.StartDate);
        Assert.Equal("Ada", PreparedMembershipApplicationMapper.ToDocumentRequest(draft, true).GesetzlicherVertreterSnapshot!.Vorname);
        var invalid = new MitgliedsantragDokumentRequest { MitgliedId = 7, BeginnDatum = request.BeginnDatum, Mitgliedsbeitrag = 12, GesetzlicherVertreterSnapshot = new MitgliedsantragVertreterSnapshot { Vorname = "", Nachname = "Muster" } };
        Assert.Throws<PreparedContractValidationException>(() => PreparedMembershipApplicationMapper.ToPreparedDraft(invalid, true));
    }
}
