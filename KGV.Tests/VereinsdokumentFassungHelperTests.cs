using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class VereinsdokumentFassungHelperTests
{
    [Fact]
    public void Valid_version_is_resolved_by_stichtag_and_future_version_is_not_current()
    {
        var versions = new[]
        {
            new VereinsdokumentFassungRecord { DokumenttypCode = "satzung", Fassung = "2026", GueltigAb = new DateTime(2026, 1, 1), GueltigBis = new DateTime(2026, 12, 31) },
            new VereinsdokumentFassungRecord { DokumenttypCode = "satzung", Fassung = "2027", GueltigAb = new DateTime(2027, 1, 1) }
        };
        Assert.Equal("2026", VereinsdokumentFassungHelper.GetValidClubDocumentVersion(versions, "satzung", new DateTime(2026, 6, 1))?.Fassung);
        Assert.Equal("2027", VereinsdokumentFassungHelper.GetValidClubDocumentVersion(versions, "satzung", new DateTime(2027, 1, 1))?.Fassung);
        Assert.True(VereinsdokumentFassungHelper.IsFuture(versions[1], new DateTime(2026, 6, 1)));
    }

    [Fact]
    public void Storage_path_requires_pdf_and_is_deterministic()
    {
        Assert.Equal("vereinsunterlagen/satzung/01.01.2027/Satzung.pdf", VereinsdokumentFassungHelper.CreateStoragePath("satzung", "01.01.2027", "Satzung.pdf"));
        Assert.Throws<InvalidOperationException>(() => VereinsdokumentFassungHelper.CreateStoragePath("satzung", "v1", "Satzung.docx"));
    }
}
