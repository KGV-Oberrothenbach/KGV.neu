using KGV.Core.Models;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class HomeOverviewFactoryTests
{
    [Fact]
    public void Build_ReturnsSharedAdminQuickLinks_ForAdmin()
    {
        var overview = HomeOverviewFactory.Build(UserRole.Admin);

        Assert.Collection(
            overview.QuickLinks,
            item => Assert.Equal(HomeQuickLinkKey.MemberSearch, item.Key),
            item => Assert.Equal(HomeQuickLinkKey.PlotManagement, item.Key));
    }

    [Fact]
    public void Build_ReturnsSharedAdminQuickLinks_ForVorstand()
    {
        var overview = HomeOverviewFactory.Build(UserRole.Vorstand);

        Assert.Collection(
            overview.QuickLinks,
            item => Assert.Equal(HomeQuickLinkKey.MemberSearch, item.Key),
            item => Assert.Equal(HomeQuickLinkKey.PlotManagement, item.Key));
    }

    [Fact]
    public void Build_ReturnsUserQuickLinks_ForUserContext()
    {
        var overview = HomeOverviewFactory.Build(UserRole.User);

        Assert.Collection(
            overview.QuickLinks,
            item => Assert.Equal(HomeQuickLinkKey.MyProfile, item.Key),
            item => Assert.Equal(HomeQuickLinkKey.MyWorkHours, item.Key));
    }
}
