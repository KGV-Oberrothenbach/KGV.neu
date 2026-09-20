using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class FormularDokumentDateinameTests
{
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
}
