using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class PachtvertragDokumentFactoryTests
{
    [Fact]
    public void CreateUploadRequest_AssignsPachtvertragToParzelle()
    {
        var request = PachtvertragDokumentFactory.CreateUploadRequest(
            hauptmitglied: new MitgliedRecord
            {
                Id = 17,
                Vorname = "Max",
                Name = "Mustermann",
                Geburtsdatum = new DateTime(1980, 1, 1)
            },
            nebenmitglied: null,
            parzelle: new ParzelleRecord
            {
                Id = 42,
                GartenNr = "42",
                FlaecheQm = 300m
            },
            saison: new SaisonRecord
            {
                Jahr = 2026,
                PachtProQm = 0.25m
            },
            vertragsbeginn: new DateTime(2026, 4, 1),
            altvertragDatum: null,
            gesetzlicherVertreterSnapshot: null,
            bankverbindungSnapshot: new MitgliedsantragBankverbindungSnapshot
            {
                VereinName = "KGV Musterverein",
                Kontoinhaber = "KGV Musterverein",
                Bankname = "Musterbank",
                Iban = "DE00123456780000000000",
                Bic = "MUSTDEFFXXX"
            },
            status: FormularDokumentStatus.Signiert);

        Assert.Equal(42, request.ParzelleId);
        Assert.Null(request.MitgliedId);
        Assert.NotEmpty(request.FileContent);
    }
}
