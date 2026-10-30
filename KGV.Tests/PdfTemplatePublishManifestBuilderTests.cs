using System.Security.Cryptography;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf;
using Xunit;

namespace KGV.Tests;

public sealed class PdfTemplatePublishManifestBuilderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "kgv-pdf-publish-tests-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("mitgliedsantrag", 1, "mitgliedsantrag-field-spec/v1", "pdfsharpcore-ma/v1")]
    [InlineData("pachtvertrag", 2, "pachtvertrag-field-spec/v2", "top-left/v1")]
    public void Builds_a_valid_manifest_for_supported_document_contracts(string templateKey, int version, string fieldSpecVersion, string rendererVersion)
    {
        var request = CreateRequest(templateKey, version, fieldSpecVersion, rendererVersion);

        var manifest = PdfTemplatePublishManifestBuilder.Build(request);

        Assert.Equal($"v{version}", manifest.Version);
        Assert.Equal($"templates/{templateKey}/v{version}/template.pdf", manifest.PdfStoragePath);
        Assert.Equal($"templates/{templateKey}/v{version}/field-spec.json", manifest.FieldSpecStoragePath);
        Assert.Contains("begin;", manifest.ActivationSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", manifest.ToMarkdown(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_an_invalid_version(int version)
    {
        Assert.Throws<InvalidOperationException>(() => PdfTemplatePublishManifestBuilder.Build(CreateRequest(version: version)));
    }

    [Fact]
    public void Rejects_a_missing_pdf_or_an_invalid_field_spec()
    {
        var missingPdf = CreateRequest(pdfPath: Path.Combine(_directory, "missing.pdf"));
        Assert.Throws<InvalidOperationException>(() => PdfTemplatePublishManifestBuilder.Build(missingPdf));

        var invalidSpec = CreateRequest(fieldSpecJson: "not json");
        Assert.Throws<InvalidOperationException>(() => PdfTemplatePublishManifestBuilder.Build(invalidSpec));
    }

    [Fact]
    public void Rejects_field_spec_and_renderer_contract_mismatches()
    {
        var fieldSpecMismatch = CreateRequest(fieldSpecVersion: "spec/v2", expectedFieldSpecVersion: "spec/v1");
        var rendererMismatch = CreateRequest(rendererVersion: "renderer/v2", expectedRendererVersion: "renderer/v1");

        Assert.Throws<InvalidOperationException>(() => PdfTemplatePublishManifestBuilder.Build(fieldSpecMismatch));
        Assert.Throws<InvalidOperationException>(() => PdfTemplatePublishManifestBuilder.Build(rendererMismatch));
    }

    [Fact]
    public void Emits_hashes_for_the_exact_local_assets_and_never_performs_remote_work()
    {
        var request = CreateRequest();
        var manifest = PdfTemplatePublishManifestBuilder.Build(request);

        Assert.Equal(Hash(File.ReadAllBytes(request.TemplatePdfPath)), manifest.PdfSha256);
        Assert.Equal(Hash(File.ReadAllBytes(request.FieldSpecPath)), manifest.FieldSpecSha256);
        Assert.Contains("supabase storage cp", manifest.PdfStorageCommand, StringComparison.Ordinal);
        Assert.DoesNotContain("http", manifest.ToMarkdown(), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private PdfTemplatePublishRequest CreateRequest(
        string templateKey = "mitgliedsantrag",
        int version = 1,
        string fieldSpecVersion = "spec/v1",
        string rendererVersion = "renderer/v1",
        string? pdfPath = null,
        string? fieldSpecJson = null,
        string? expectedFieldSpecVersion = null,
        string? expectedRendererVersion = null)
    {
        Directory.CreateDirectory(_directory);
        var pdf = pdfPath ?? Path.Combine(_directory, "template.pdf");
        if (pdfPath == null)
        {
            using var document = new PdfDocument();
            document.AddPage();
            document.Save(pdf);
        }

        var spec = Path.Combine(_directory, "field-spec.json");
        File.WriteAllText(spec, fieldSpecJson ?? """{"schema_version":"kgv-pdf-field-spec/v1","settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"field","type":"text","page":1,"rect":{"x":0,"y":0,"w":1,"h":1}}]}""");
        return new PdfTemplatePublishRequest(templateKey, version, pdf, spec, fieldSpecVersion, rendererVersion, expectedFieldSpecVersion ?? fieldSpecVersion, expectedRendererVersion ?? rendererVersion);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
