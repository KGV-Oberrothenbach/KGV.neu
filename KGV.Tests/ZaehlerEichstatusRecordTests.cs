using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class ZaehlerEichstatusRecordTests
{
    [Fact]
    public void DefektStatus_HasPriorityOverEichstatusAndIsSearchable()
    {
        var record = new ZaehlerEichstatusRecord
        {
            Defekt = true,
            Eichstatus = "ueberfaellig"
        };

        Assert.Equal("Defekt – Austausch erforderlich", record.StatusDisplay);
        Assert.Contains("Defekt – Austausch erforderlich", record.SearchText);
    }

    [Fact]
    public void Eichstatus_IsUsedWhenMeterIsNotDefective()
    {
        var record = new ZaehlerEichstatusRecord
        {
            Defekt = false,
            Eichstatus = "ueberfaellig"
        };

        Assert.Equal("Überfällig", record.StatusDisplay);
    }
}
