using KGV.Core.Utilities;

namespace KGV.Tests;

public sealed class SichtbarerDateinameTests
{
    [Fact]
    public void Foto_UsesOnlyBusinessValuesWithoutTechnicalOrAuthUuid()
    {
        var fileName = SichtbarerDateiname.BuildFoto("Ablesung", "Garten-63", "Strom", new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero));

        Assert.Equal("2026-10-27_Ablesung_Garten-63_Strom.jpg", fileName);
        Assert.DoesNotMatch(@"(?i)[0-9a-f]{8}-[0-9a-f]{4}-", fileName);
        Assert.DoesNotContain("auth_user", fileName.ToLowerInvariant());
    }

    [Fact]
    public void Pdf_PrefersOriginalDocumentNameAndRemovesUuid()
    {
        var fileName = SichtbarerDateiname.BuildPdf("Satzung 2026.pdf", "Vereinsunterlage");

        Assert.Equal("Satzung_2026.pdf", fileName);
    }

    [Fact]
    public void Pdf_UsesTitleWhenOriginalNameIsTechnicalUuidCache()
    {
        var fileName = SichtbarerDateiname.BuildPdf("kgv-pdf-11111111-1111-1111-1111-111111111111.pdf", "Mitgliedsantrag 2026");

        Assert.Equal("Mitgliedsantrag_2026.pdf", fileName);
        Assert.DoesNotMatch(@"(?i)[0-9a-f]{8}-[0-9a-f]{4}-", fileName);
    }

    [Fact]
    public void Pdf_UsesNeutralFallbackWhenNoBusinessNameExists()
        => Assert.Equal("KGV_Dokument.pdf", SichtbarerDateiname.BuildPdf(null, " \r\n "));
}
