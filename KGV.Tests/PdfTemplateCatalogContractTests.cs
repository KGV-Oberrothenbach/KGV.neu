using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class PdfTemplateCatalogContractTests
{
    [Fact]
    public void Accepts_paths_owned_by_the_requested_template_version()
    {
        var record = Record("mitgliedsantrag", "v1");

        Assert.True(PdfTemplateCatalogContract.HasVersionedPaths(record, "mitgliedsantrag"));
        Assert.Equal("templates/mitgliedsantrag/v1/template.pdf", PdfTemplateCatalogContract.BuildPdfPath("mitgliedsantrag", "v1"));
        Assert.Equal("templates/mitgliedsantrag/v1/field-spec.json", PdfTemplateCatalogContract.BuildFieldSpecPath("mitgliedsantrag", "v1"));
    }

    [Theory]
    [InlineData("templates/mitgliedsantrag/v2/template.pdf", "templates/mitgliedsantrag/v1/field-spec.json")]
    [InlineData("templates/pachtvertrag/v1/template.pdf", "templates/mitgliedsantrag/v1/field-spec.json")]
    [InlineData("templates/mitgliedsantrag/v1/template.pdf", "templates/mitgliedsantrag/v2/field-spec.json")]
    public void Rejects_paths_that_do_not_belong_to_the_version(string pdfPath, string fieldSpecPath)
    {
        var record = new PdfTemplateCatalogRecord
        {
            TemplateKey = "mitgliedsantrag",
            Version = "v1",
            StoragePath = pdfPath,
            FieldSpecPath = fieldSpecPath
        };

        Assert.False(PdfTemplateCatalogContract.HasVersionedPaths(record, "mitgliedsantrag"));
    }

    private static PdfTemplateCatalogRecord Record(string templateKey, string version) => new()
    {
        TemplateKey = templateKey,
        Version = version,
        StoragePath = PdfTemplateCatalogContract.BuildPdfPath(templateKey, version),
        FieldSpecPath = PdfTemplateCatalogContract.BuildFieldSpecPath(templateKey, version)
    };
}
