using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class VereinskonfigurationSnapshotTests
{
    [Fact]
    public void FromActiveConfiguration_CopiesDocumentRelevantValuesAndNormalizesWhitespace()
    {
        var snapshot = VereinskonfigurationSnapshotFactory.FromActiveConfiguration(new VereinskonfigurationRecord
        {
            Vereinsname = " KGV Beispiel ", Registerangabe = " VR 123 ", Strasse = " Gartenweg 1 ", Plz = " 12345 ", Ort = " Beispielstadt ",
            StandardEmail = " vorstand@example.org ", Kontoinhaber = " KGV Beispiel ", Bankname = " Beispielbank ", Iban = " DE89370400440532013000 ",
            Bic = " COBADEFFXXX ", VerwendungszweckMitgliedsantrag = " Mitglied  ", VerwendungszweckPachtvertrag = " Pacht ", DokumentOrt = " Beispielstadt ",
            StandardHinweistext = " Hinweis ", DatenschutzText = " Datenschutz ", DatenschutzVersion = " v1 "
        });

        var normalized = VereinskonfigurationSnapshotFactory.NormalizeForMembershipApplication(snapshot);

        Assert.Equal("KGV Beispiel", normalized.VereinName);
        Assert.Equal("DE89370400440532013000", normalized.Iban);
        Assert.Equal("COBADEFFXXX", normalized.Bic);
        Assert.Equal("Datenschutz", normalized.DatenschutzText);
    }

    [Fact]
    public void Snapshot_IsIndependentFromLaterConfigurationChanges()
    {
        var configuration = ValidConfiguration();
        var snapshot = VereinskonfigurationSnapshotFactory.FromActiveConfiguration(configuration);
        configuration.Vereinsname = "Neu";
        configuration.Iban = "DE00123456780000000000";

        Assert.Equal("KGV Alt", snapshot.VereinName);
        Assert.Equal("DE89370400440532013000", snapshot.Iban);
    }

    [Fact]
    public void MembershipSnapshot_MissingRequiredDocumentValue_ExplainsFailure()
    {
        var snapshot = VereinskonfigurationSnapshotFactory.FromActiveConfiguration(ValidConfiguration());
        snapshot.DokumentOrt = string.Empty;

        var exception = Assert.Throws<InvalidOperationException>(() => VereinskonfigurationSnapshotFactory.NormalizeForMembershipApplication(snapshot));

        Assert.Contains("DokumentOrt", exception.Message);
    }

    private static VereinskonfigurationRecord ValidConfiguration() => new()
    {
        Vereinsname = "KGV Alt", Registerangabe = "VR 123", Strasse = "Gartenweg 1", Plz = "12345", Ort = "Beispielstadt", StandardEmail = "vorstand@example.org",
        Kontoinhaber = "KGV Alt", Bankname = "Beispielbank", Iban = "DE89370400440532013000", Bic = "COBADEFFXXX", VerwendungszweckMitgliedsantrag = "Mitglied",
        VerwendungszweckPachtvertrag = "Pacht", DokumentOrt = "Beispielstadt", StandardHinweistext = "Hinweis", DatenschutzText = "Datenschutz", DatenschutzVersion = "v1"
    };
}
