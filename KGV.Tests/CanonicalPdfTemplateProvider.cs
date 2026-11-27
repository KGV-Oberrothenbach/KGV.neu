using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Tests;

/// <summary>Loads the versioned source assets used for publish and regression checks, never embedded runtime fallbacks.</summary>
internal sealed class CanonicalPdfTemplateProvider : IPdfTemplateProvider
{
    private readonly string _leaseVersion;
    public CanonicalPdfTemplateProvider(string leaseVersion = "v9") { _leaseVersion = leaseVersion; }

    public Task<PdfTemplateAsset> GetActiveAsync(PdfTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var (version, fieldSpecVersion, rendererVersion) = request.TemplateKey switch
        {
            "mitgliedsantrag" => ("v5", "mitgliedsantrag-field-spec/v5", "top-left/v1"),
            "pachtvertrag" => (_leaseVersion, $"pachtvertrag-field-spec/{_leaseVersion}", "top-left/v1"),
            _ => throw new InvalidOperationException($"Unexpected template key '{request.TemplateKey}'.")
        };

        if (!request.SupportsFieldSpecVersion(fieldSpecVersion) || request.ExpectedRendererVersion != rendererVersion)
            throw new InvalidOperationException("The requested template contract does not match the canonical source asset.");

        var directory = Path.Combine(AppContext.BaseDirectory, "Templates", request.TemplateKey, version);
        return Task.FromResult(new PdfTemplateAsset
        {
            TemplateKey = request.TemplateKey,
            Version = version,
            FieldSpecVersion = fieldSpecVersion,
            RendererVersion = rendererVersion,
            PdfBytes = File.ReadAllBytes(Path.Combine(directory, "template.pdf")),
            Fields = PdfTemplateFieldSpecParser.Parse(File.ReadAllBytes(Path.Combine(directory, "field-spec.json")))
        });
    }
}
