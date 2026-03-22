using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

public sealed class UserRolesTests
{
    [Theory]
    [InlineData("admin", UserRole.Admin)]
    [InlineData(" Admin ", UserRole.Admin)]
    [InlineData("VORSTAND", UserRole.Vorstand)]
    [InlineData("user", UserRole.User)]
    [InlineData(null, UserRole.User)]
    [InlineData("unbekannt", UserRole.User)]
    public void Parse_ReturnsExpectedRole_ForStorageValues(string? input, UserRole expected)
    {
        var result = UserRoles.Parse(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UserRole.Admin, UserRoles.Admin)]
    [InlineData(UserRole.Vorstand, UserRoles.Vorstand)]
    [InlineData(UserRole.User, UserRoles.User)]
    public void ToStorageValue_ReturnsNormalizedStorageValue(UserRole role, string expected)
    {
        var result = UserRoles.ToStorageValue(role);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AssignableRoles_ReturnsStableSharedRoleOrder()
    {
        Assert.Collection(
            UserRoles.AssignableRoles,
            item => Assert.Equal(UserRoles.Admin, item),
            item => Assert.Equal(UserRoles.Vorstand, item),
            item => Assert.Equal(UserRoles.User, item));
    }
}
