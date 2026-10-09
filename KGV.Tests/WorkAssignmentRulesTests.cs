using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class WorkAssignmentRulesTests
{
    [Theory]
    [InlineData("2026-10-17", "2026-10-15", "2026-10-31")]
    [InlineData("2027-01-01", "2026-12-30", "2027-01-15")]
    public void CreateDateDefaults_UsesRequiredCalendarOffsets(string assignmentDate, string expectedDeadline, string expectedVisibleUntil)
    {
        var defaults = WorkAssignmentRules.CreateDateDefaults(DateTime.Parse(assignmentDate));

        Assert.Equal(DateTime.Parse(expectedDeadline), defaults.SignUpDeadline);
        Assert.Equal(DateTime.Parse(expectedVisibleUntil).AddHours(23).AddMinutes(59), defaults.VisibleUntil);
    }

    [Fact]
    public void RefreshNewEntryDateDefaults_UpdatesBothAutomaticValues()
    {
        var previousDate = new DateTime(2026, 10, 9);
        var previous = WorkAssignmentRules.CreateDateDefaults(previousDate);

        var refreshed = WorkAssignmentRules.RefreshNewEntryDateDefaults(previousDate, new DateTime(2026, 10, 17), previous.VisibleUntil, previous.SignUpDeadline);

        Assert.Equal(new DateTime(2026, 10, 31, 23, 59, 0), refreshed.VisibleUntil);
        Assert.Equal(new DateTime(2026, 10, 15), refreshed.SignUpDeadline);
    }

    [Fact]
    public void RefreshNewEntryDateDefaults_PreservesManuallyChangedValues()
    {
        var previousDate = new DateTime(2026, 10, 9);
        var manualVisibleUntil = new DateTime(2026, 10, 20, 17, 30, 0);
        var manualDeadline = new DateTime(2026, 10, 7, 12, 0, 0);

        var refreshed = WorkAssignmentRules.RefreshNewEntryDateDefaults(previousDate, new DateTime(2026, 10, 17), manualVisibleUntil, manualDeadline);

        Assert.Equal(manualVisibleUntil, refreshed.VisibleUntil);
        Assert.Equal(manualDeadline, refreshed.SignUpDeadline);
    }
}
