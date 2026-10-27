using KGV.Core.Calendar;
using Xunit;

namespace KGV.Tests;

public sealed class IcsCalendarDateinameTests
{
    [Fact]
    public void Build_UsesTitleAndDateWithoutTechnicalId()
    {
        var fileName = IcsCalendarDateiname.Build("Vorstandssitzung", new DateTime(2026, 11, 12));

        Assert.Equal("Vorstandssitzung_2026-11-12.ics", fileName);
        Assert.DoesNotContain("123", fileName);
    }

    [Fact]
    public void Build_SanitizesUnsafeCharactersAndLineBreaks()
    {
        var fileName = IcsCalendarDateiname.Build("  Vorstand/\r\nSitzung:*?  ", new DateTime(2026, 11, 12));

        Assert.Equal("Vorstand_Sitzung_2026-11-12.ics", fileName);
        Assert.DoesNotContain("\r", fileName);
        Assert.DoesNotContain("\n", fileName);
        Assert.DoesNotContain("/", fileName);
    }

    [Fact]
    public void Build_UsesNeutralFallbackForMissingTitle()
        => Assert.Equal("Termin_2026-11-12.ics", IcsCalendarDateiname.Build(" \r\n ", new DateTime(2026, 11, 12)));
}
