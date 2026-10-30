using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class HistoricalContractRegistrationRulesTests
{
    [Fact]
    public void TwoHistoricalContractKinds_AreExplicitAndUseSeparateDocumentTypes()
    {
        Assert.Equal(2, Enum.GetValues<HistoricalContractKind>().Length);
        Assert.NotEqual(HistoricalContractDocumentTypes.MembershipAndLeaseInitialContract, HistoricalContractDocumentTypes.LeaseFollowUpContract);
    }

    [Fact]
    public void LateCaptureOfOlderContract_IsPlacedBeforeExistingNewerVersion()
    {
        var placement = HistoricalContractRegistrationRules.PlaceLeaseVersion(
            new DateOnly(1998, 4, 1),
            [new HistoricalLeaseContractVersion(2005, new DateOnly(2005, 6, 15), null)]);

        Assert.Null(placement.VorgaengerFassungId);
        Assert.Equal(2005, placement.NachfolgerFassungId);
        Assert.Equal(new DateOnly(2005, 6, 14), placement.VorgeschlagenesEndeDerNeuenFassung);
    }

    [Fact]
    public void NewerContract_ProposesDayBeforeAsPreviousEnd_AndOverrideWins()
    {
        var start = new DateOnly(2010, 6, 15);
        var defaultPlacement = HistoricalContractRegistrationRules.PlaceLeaseVersion(start,
            [new HistoricalLeaseContractVersion(1998, new DateOnly(1998, 4, 1), null)]);
        var overridden = HistoricalContractRegistrationRules.PlaceLeaseVersion(start,
            [new HistoricalLeaseContractVersion(1998, new DateOnly(1998, 4, 1), null)], new DateOnly(2010, 6, 10));

        Assert.Equal(new DateOnly(2010, 6, 14), defaultPlacement.VorgeschlagenesEndeDesVorgaengers);
        Assert.Equal(new DateOnly(2010, 6, 10), overridden.VorgeschlagenesEndeDesVorgaengers);
    }

    [Fact]
    public void Placement_OnlyUsesVersionsOfTheSelectedLeaseRelation()
    {
        var garden12 = HistoricalContractRegistrationRules.PlaceLeaseVersion(new DateOnly(2010, 6, 15),
            [new HistoricalLeaseContractVersion(12, new DateOnly(1998, 4, 1), null)]);
        var garden13 = HistoricalContractRegistrationRules.PlaceLeaseVersion(new DateOnly(2010, 6, 15), []);

        Assert.Equal(12, garden12.VorgaengerFassungId);
        Assert.Null(garden13.VorgaengerFassungId);
    }

    [Theory]
    [InlineData(false, 1, HistoricalMembershipActivitySuggestion.SuggestActive)]
    [InlineData(true, 0, HistoricalMembershipActivitySuggestion.SuggestPassive)]
    [InlineData(true, 1, HistoricalMembershipActivitySuggestion.KeepCurrentState)]
    public void MembershipActivity_IsOnlySuggested(bool active, int leases, HistoricalMembershipActivitySuggestion expected)
        => Assert.Equal(expected, HistoricalContractRegistrationRules.SuggestMembershipActivity(active, leases));

    [Fact]
    public void PdfStamp_CarriesEveryContractPartyMembershipNumber()
    {
        var text = HistoricalContractRegistrationRules.CreatePdfStampText(80037, [1048, 1042, 1048]);
        Assert.Equal("Mgl.-Nr. 1042 / 1048" + Environment.NewLine + "Vertrags-Nr. 80037", text);
    }
}
