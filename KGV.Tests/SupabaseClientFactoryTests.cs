using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;
using KGV.Infrastructure.DependencyInjection;
using KGV.Infrastructure.Supabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Tests;

public sealed class SupabaseClientFactoryTests
{
    [Fact]
    public async Task MissingVereinskontext_RejectsDatabaseAccessDespiteLegacyConfiguration()
    {
        var vereinskontext = new VereinskontextService();
        var factory = new SupabaseClientFactory(vereinskontext);

        var urlException = Assert.Throws<InvalidOperationException>(() => factory.Url);
        Assert.Contains("ohne ausgewählten Vereinskontext", urlException.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
    }

    [Fact]
    public void DependencyInjection_RegistersFactoryAsLazySingleton()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://legacy.example.invalid",
                ["Supabase:PublishableKey"] = "legacy-publishable-key"
            })
            .Build();

        services.AddKgvServices(configuration);

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(ISupabaseClientFactory));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Null(descriptor.ImplementationInstance);
        Assert.Null(descriptor.ImplementationType);
    }

    [Fact]
    public void FactoryCreatedBeforeSelection_UsesVereinskontextSelectedLater()
    {
        var vereinskontext = new VereinskontextService();
        var factory = new SupabaseClientFactory(vereinskontext);

        vereinskontext.Setzen(CreateContext(
            " https://selected.supabase.co/ ",
            " selected-publishable-key "));

        Assert.Equal("https://selected.supabase.co/", factory.Url);
        Assert.Equal("selected-publishable-key", factory.Key);
    }

    [Fact]
    public void FactoryReadsCurrentVereinskontextAfterVereinswechsel()
    {
        var vereinskontext = new VereinskontextService();
        var factory = new SupabaseClientFactory(vereinskontext);
        vereinskontext.Setzen(CreateContext("https://first.supabase.co", "first-key"));

        Assert.Equal("https://first.supabase.co", factory.Url);

        vereinskontext.Setzen(CreateContext("https://second.supabase.co", "second-key"));

        Assert.Equal("https://second.supabase.co", factory.Url);
        Assert.Equal("second-key", factory.Key);
    }

    [Theory]
    [InlineData("", "publishable-key", "Supabase URL des Vereins fehlt")]
    [InlineData("https://selected.supabase.co", " ", "Supabase Publishable Key des Vereins fehlt")]
    public async Task IncompleteVereinsCredentials_AreRejected(string url, string key, string expectedMessage)
    {
        var vereinskontext = new VereinskontextService();
        vereinskontext.Setzen(CreateContext(url, key));
        var factory = new SupabaseClientFactory(vereinskontext);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = factory.Url);
        Assert.Contains(expectedMessage, exception.Message);

        var asyncException = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
        Assert.Contains(expectedMessage, asyncException.Message);
    }

    private static Vereinskontext CreateContext(string url, string key)
        => new(
            Guid.NewGuid(),
            "KGV-TEST",
            "Testverein",
            "Test",
            url,
            key);
}
