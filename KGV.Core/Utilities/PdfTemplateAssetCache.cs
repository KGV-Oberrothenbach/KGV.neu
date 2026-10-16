using System.Collections.Concurrent;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

/// <summary>
/// Process-local single-flight cache for assets that have already passed the
/// catalog, checksum and field-spec validation in the template provider.
/// </summary>
public sealed class PdfTemplateAssetCache
{
    private readonly ConcurrentDictionary<PdfTemplateAssetCacheKey, Lazy<Task<PdfTemplateAsset>>> _entries = new();

    public bool Contains(PdfTemplateAssetCacheKey key) => _entries.ContainsKey(key);

    public async Task<PdfTemplateAsset> GetOrLoadAsync(
        PdfTemplateAssetCacheKey key,
        Func<CancellationToken, Task<PdfTemplateAsset>> loader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(loader);

        var candidate = new Lazy<Task<PdfTemplateAsset>>(
            () => loader(CancellationToken.None),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var entry = _entries.GetOrAdd(key, candidate);

        try
        {
            return await entry.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                _entries.TryRemove(key, out _);
            throw;
        }
    }
}

/// <summary>Identity of a published immutable asset and its compatibility contract.</summary>
public sealed record PdfTemplateAssetCacheKey(
    string TemplateKey,
    string Version,
    string PdfChecksum,
    string FieldSpecChecksum,
    string FieldSpecVersion,
    string RendererVersion)
{
    public static PdfTemplateAssetCacheKey From(PdfTemplateCatalogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new PdfTemplateAssetCacheKey(
            record.TemplateKey.Trim(),
            record.Version.Trim(),
            record.Checksum.Trim(),
            record.FieldSpecChecksum.Trim(),
            record.FieldSpecVersion.Trim(),
            record.RendererVersion.Trim());
    }
}
