using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class SaisonverwaltungHelperTests
{
    [Fact]
    public void Next_season_keeps_due_month_and_day_but_uses_the_new_year()
    {
        var proposal = SaisonverwaltungHelper.CreateNextSaisonProposal(new[]
        {
            new SaisonRecord { Jahr = 2026, PachtFaelligAm = new DateTime(2026, 3, 31) }
        }, new DateTime(2026, 8, 1));

        Assert.Equal(2027, proposal.Jahr);
        Assert.Equal(new DateTime(2027, 3, 31), proposal.PachtFaelligAm);
    }

    [Fact]
    public void Next_season_preserves_a_custom_due_month_and_day()
    {
        var proposal = SaisonverwaltungHelper.CreateNextSaisonProposal(new[]
        {
            new SaisonRecord { Jahr = 2027, PachtFaelligAm = new DateTime(2027, 4, 15) }
        }, new DateTime(2027, 8, 1));

        Assert.Equal(2028, proposal.Jahr);
        Assert.Equal(new DateTime(2028, 4, 15), proposal.PachtFaelligAm);
    }
}
