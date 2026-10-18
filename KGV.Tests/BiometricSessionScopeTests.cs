using KGV.Core.Security;

namespace KGV.Tests;

public sealed class BiometricSessionScopeTests
{
    private static readonly Guid DemoClub = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductionClub = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Scope_is_stable_for_the_same_club_and_environment()
        => Assert.Equal(BiometricSessionScope.Create(DemoClub, "https://demo.example.test/"), BiometricSessionScope.Create(DemoClub, "https://DEMO.example.test"));

    [Fact]
    public void Demo_session_scope_cannot_match_production_environment()
        => Assert.NotEqual(BiometricSessionScope.Create(DemoClub, "https://demo.example.test"), BiometricSessionScope.Create(ProductionClub, "https://club.example.test"));

    [Fact]
    public void Same_club_id_in_two_supabase_environments_cannot_share_a_session()
        => Assert.NotEqual(BiometricSessionScope.Create(DemoClub, "https://demo.example.test"), BiometricSessionScope.Create(DemoClub, "https://club.example.test"));

    [Fact]
    public void Missing_club_or_environment_has_no_biometric_scope()
    {
        Assert.Null(BiometricSessionScope.Create(Guid.Empty, "https://demo.example.test"));
        Assert.Null(BiometricSessionScope.Create(DemoClub, " "));
    }
}
