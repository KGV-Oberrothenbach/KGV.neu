using System;
using System.Text;
using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class PdfTemplateFieldSpecParserTests
{
    [Fact]
    public void Parses_canonical_spec() => Assert.Single(PdfTemplateFieldSpecParser.Parse(Bytes("""{"schema_version":"kgv-pdf-field-spec/v1","coordinate_system":"top-left-points","settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"a","type":"text","page":1,"rect":{"x":0,"y":0,"w":1,"h":1}}]}""")));

    [Fact]
    public void Normalizes_legacy_multiline() => Assert.Equal("multiline", Assert.Single(PdfTemplateFieldSpecParser.Parse(Bytes("""{"settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"a","type":"text","flags":{"multiline":true},"page":1,"rect":{"x":0,"y":0,"w":1,"h":1}}]}"""))).Kind);

    [Theory]
    [InlineData("\"schema_version\":\"unknown\",")]
    [InlineData("\"coordinate_system\":\"bottom-left\",")]
    [InlineData("")]
    public void Rejects_invalid_top_level(string prefix) => Assert.Throws<InvalidOperationException>(() => PdfTemplateFieldSpecParser.Parse(Bytes($$"""{ {{prefix}}"settings":{"coordinateOrigin":"topLeft"},"fields":[]}""")));

    [Theory]
    [InlineData("", "text", 1, 0, 0, 1, 1)]
    [InlineData("a", "unknown", 1, 0, 0, 1, 1)]
    [InlineData("a", "text", 0, 0, 0, 1, 1)]
    [InlineData("a", "text", 1, -1, 0, 1, 1)]
    [InlineData("a", "text", 1, 0, 0, 0, 1)]
    public void Rejects_invalid_field(string name, string type, int page, double x, double y, double w, double h) => Assert.Throws<InvalidOperationException>(() => PdfTemplateFieldSpecParser.Parse(Bytes($$"""{"settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"{{name}}","type":"{{type}}","page":{{page}},"rect":{"x":{{x}},"y":{{y}},"w":{{w}},"h":{{h}}}},{"name":"{{name}}","type":"{{type}}","page":{{page}},"rect":{"x":{{x}},"y":{{y}},"w":{{w}},"h":{{h}}}}]}""")));

    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
}
