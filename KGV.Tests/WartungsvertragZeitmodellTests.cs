using KGV.Core.Models;

namespace KGV.Tests;

public sealed class WartungsvertragZeitmodellTests
{
    [Theory]
    [InlineData("2026-10-21", "2026-10-21", WartungsvertragZeitstatus.Aktuell)]
    [InlineData("2026-10-20", "2026-10-21", WartungsvertragZeitstatus.Aktuell)]
    [InlineData("2026-10-22", null, WartungsvertragZeitstatus.Zukuenftig)]
    [InlineData("2026-10-20", "2026-10-20", WartungsvertragZeitstatus.Historisch)]
    public void Classify_uses_inclusive_boundaries(string start, string? end, WartungsvertragZeitstatus expected)
    {
        var validFrom = DateTime.Parse(start);
        DateTime? validUntil = end is null ? null : DateTime.Parse(end);

        Assert.Equal(expected, WartungsvertragZeitmodell.Klassifiziere(validFrom, validUntil, new DateTime(2026, 10, 21)));
    }
}
