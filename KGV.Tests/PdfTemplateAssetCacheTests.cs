using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class PdfTemplateAssetCacheTests
{
    [Fact]
    public async Task Reuses_a_successfully_loaded_immutable_asset()
    {
        var cache = new PdfTemplateAssetCache();
        var loads = 0;
        var key = Key();

        var first = await cache.GetOrLoadAsync(key, _ => Task.FromResult(Load(ref loads, "v1")));
        var second = await cache.GetOrLoadAsync(key, _ => Task.FromResult(Load(ref loads, "v1")));

        Assert.Same(first, second);
        Assert.Equal(1, loads);
    }

    [Fact]
    public async Task Keeps_versions_and_compatibility_contracts_in_separate_entries()
    {
        var cache = new PdfTemplateAssetCache();
        var loads = 0;

        await cache.GetOrLoadAsync(Key(version: "v1"), _ => Task.FromResult(Load(ref loads, "v1")));
        await cache.GetOrLoadAsync(Key(version: "v2", pdfChecksum: "pdf-v2"), _ => Task.FromResult(Load(ref loads, "v2")));
        await cache.GetOrLoadAsync(Key(rendererVersion: "renderer/v2"), _ => Task.FromResult(Load(ref loads, "renderer-v2")));
        await cache.GetOrLoadAsync(Key(fieldSpecVersion: "spec/v2"), _ => Task.FromResult(Load(ref loads, "spec-v2")));

        Assert.Equal(4, loads);
    }

    [Theory]
    [InlineData("checksum")]
    [InlineData("network")]
    public async Task Does_not_cache_failed_loads_and_allows_a_retry(string failureKind)
    {
        var cache = new PdfTemplateAssetCache();
        var loads = 0;
        var key = Key();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrLoadAsync(key, _ =>
        {
            loads++;
            throw new InvalidOperationException(failureKind);
        }));

        var asset = await cache.GetOrLoadAsync(key, _ => Task.FromResult(Load(ref loads, "recovered")));

        Assert.Equal("recovered", asset.Version);
        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task Coalesces_parallel_loads_for_the_same_immutable_asset()
    {
        var cache = new PdfTemplateAssetCache();
        var loads = 0;
        var completion = new TaskCompletionSource<PdfTemplateAsset>(TaskCreationOptions.RunContinuationsAsynchronously);
        var key = Key();

        var requests = Enumerable.Range(0, 8)
            .Select(_ => cache.GetOrLoadAsync(key, _ =>
            {
                Interlocked.Increment(ref loads);
                return completion.Task;
            }))
            .ToArray();

        completion.SetResult(Asset("v1"));
        var assets = await Task.WhenAll(requests);

        Assert.Equal(1, loads);
        Assert.All(assets, asset => Assert.Equal("v1", asset.Version));
    }

    [Fact]
    public async Task Retains_an_old_immutable_asset_when_a_new_active_version_uses_a_new_key()
    {
        var cache = new PdfTemplateAssetCache();
        var v1Loads = 0;
        var v2Loads = 0;
        var v1 = Key(version: "v1", pdfChecksum: "pdf-v1");
        var v2 = Key(version: "v2", pdfChecksum: "pdf-v2");

        await cache.GetOrLoadAsync(v1, _ => Task.FromResult(Load(ref v1Loads, "v1")));
        var activeAsset = await cache.GetOrLoadAsync(v2, _ => Task.FromResult(Load(ref v2Loads, "v2")));
        await cache.GetOrLoadAsync(v1, _ => Task.FromResult(Load(ref v1Loads, "v1")));

        Assert.Equal("v2", activeAsset.Version);
        Assert.Equal(1, v1Loads);
        Assert.Equal(1, v2Loads);
    }

    private static PdfTemplateAssetCacheKey Key(
        string version = "v1",
        string pdfChecksum = "pdf-v1",
        string fieldSpecVersion = "spec/v1",
        string rendererVersion = "renderer/v1")
        => new("mitgliedsantrag", version, pdfChecksum, "field-spec-v1", fieldSpecVersion, rendererVersion);

    private static PdfTemplateAsset Load(ref int loads, string version)
    {
        loads++;
        return Asset(version);
    }

    private static PdfTemplateAsset Asset(string version) => new()
    {
        TemplateKey = "mitgliedsantrag",
        Version = version,
        PdfBytes = [1],
        Fields = [new PdfTemplateFieldSpec("field", "text", 1, 0, 0, 1, 1)]
    };
}
