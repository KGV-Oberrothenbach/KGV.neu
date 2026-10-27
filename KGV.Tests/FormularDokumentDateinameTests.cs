using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class FormularDokumentDateinameTests
{
    [Fact]
    public void BuildMitgliedDateiname_UsesPersistedMembershipNumberInsteadOfTechnicalId()
    {
        var member = new MitgliedRecord { Id = 92, Mitgliedsnummer = 1081, Vorname = "Erika", Name = "Musterfrau" };

        var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, FormularDokumentTyp.Mitgliedsvertrag, FormularDokumentStatus.Unsigniert, new DateTime(2026, 10, 27));

        Assert.Equal("Musterfrau_Erika-Mgl-1081-2026-10-27-mitgliedsvertrag-unsigniert.pdf", fileName);
        Assert.DoesNotContain("-92-", fileName);
        Assert.DoesNotContain("1092", fileName);
    }

    [Fact]
    public void BuildMitgliedDateiname_UsesExistingApplicationNumberWhenProvided()
    {
        var member = new MitgliedRecord { Id = 92, Mitgliedsnummer = 1081, Vorname = "Erika", Name = "Musterfrau" };

        var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, FormularDokumentTyp.Mitgliedsantrag, FormularDokumentStatus.Unsigniert, new DateTime(2026, 10, 27), 10017);

        Assert.Contains("Antrag-10017", fileName);
        Assert.DoesNotContain("-92-", fileName);
    }

    [Fact]
    public void BuildMitgliedDateiname_UsesNeutralNameWhenNoFachlicheNumberExists()
    {
        var member = new MitgliedRecord { Id = 92, Vorname = "Erika", Name = "Musterfrau" };

        var fileName = FormularDokumentDateiname.BuildMitgliedDateiname(member, FormularDokumentTyp.Mitgliedsantrag, FormularDokumentStatus.Unsigniert, new DateTime(2026, 10, 27));

        Assert.Equal("Musterfrau_Erika-2026-10-27-mitgliedsantrag-unsigniert.pdf", fileName);
        Assert.DoesNotContain("92", fileName);
    }

    [Theory]
    [InlineData(1081L, "Garten 14/West", "Pachtvertrag_Vorschau_Mgl-1081_Garten-Garten_14West.pdf")]
    [InlineData(1081L, null, "Pachtvertrag_Vorschau_Mgl-1081.pdf")]
    [InlineData(null, null, "Pachtvertrag_Vorschau.pdf")]
    public void BuildPachtvertragVorschauDateiname_UsesOnlyFachlicheValues(long? mitgliedsnummer, string? gartenNr, string expected)
    {
        var fileName = FormularDokumentDateiname.BuildPachtvertragVorschauDateiname(mitgliedsnummer, gartenNr);

        Assert.Equal(expected, fileName);
        Assert.DoesNotContain("mitgliedId", fileName);
        Assert.DoesNotContain("parzelleId", fileName);
    }

    [Theory]
    [InlineData("Mitgliedsantrag-(signiert)_2026-07-17_16-42-30.pdf")]
    [InlineData("KGV-APP/Dokumente/Mitglieder/92/Mitgliedsantrag-(signiert)_2026-07-17_16-42-30.pdf")]
    [InlineData("Mitgliedsantrag (signiert)")]
    public void TryParse_RecognizesLegacySignedMitgliedsantrag(string value)
    {
        var parsed = FormularDokumentDateiname.TryParse(value, out var dokumenttyp, out var status);

        Assert.True(parsed);
        Assert.Equal(FormularDokumentTyp.Mitgliedsantrag, dokumenttyp);
        Assert.Equal(FormularDokumentStatus.Signiert, status);
    }

    [Fact]
    public void TryParse_RecognizesExistingTechnicalIdStyleAndNewMembershipNumberStyle()
    {
        Assert.True(FormularDokumentDateiname.TryParse("Musterfrau_Erika-92-2026-10-27-mitgliedsantrag-unsigniert.pdf", out var oldType, out var oldStatus));
        Assert.Equal(FormularDokumentTyp.Mitgliedsantrag, oldType);
        Assert.Equal(FormularDokumentStatus.Unsigniert, oldStatus);

        Assert.True(FormularDokumentDateiname.TryParse("Musterfrau_Erika-Mgl-1081-2026-10-27-mitgliedsantrag-unsigniert.pdf", out var newType, out var newStatus));
        Assert.Equal(FormularDokumentTyp.Mitgliedsantrag, newType);
        Assert.Equal(FormularDokumentStatus.Unsigniert, newStatus);

        Assert.True(FormularDokumentDateiname.TryParse("Musterfrau_Erika-2026-10-27-mitgliedsantrag-unsigniert.pdf", out var neutralType, out var neutralStatus));
        Assert.Equal(FormularDokumentTyp.Mitgliedsantrag, neutralType);
        Assert.Equal(FormularDokumentStatus.Unsigniert, neutralStatus);
    }
}
