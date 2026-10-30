using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

public sealed class PachtvertragDokumentFactoryTests
{
    [Fact]
    public void Template_data_uses_stored_membership_numbers_for_both_contract_parties()
    {
        var data = PachtvertragTemplateFactory.BuildData(new PachtvertragTemplateContext
        {
            Paechter1 = new MitgliedRecord { Id = 17, Mitgliedsnummer = 1081, Vorname = "Max", Name = "Mustermann" },
            Paechter2 = new MitgliedRecord { Id = 18, Mitgliedsnummer = 1082, Vorname = "Erika", Name = "Musterfrau" },
            ParzelleNummer = "42",
            ParzelleFlaecheQm = 300m,
            Pachtbeginn = new DateTime(2026, 4, 1),
            PachtProQm = .25m
        });

        Assert.Equal("1081", data.Paechter1Mitgliedsnummer);
        Assert.Equal("1082", data.Paechter2Mitgliedsnummer);
        Assert.DoesNotContain("17", data.Paechter1Mitgliedsnummer);
        Assert.DoesNotContain("18", data.Paechter2Mitgliedsnummer);
    }

    [Fact]
    public void Template_data_rejects_a_contract_party_without_a_membership_number()
    {
        var context = new PachtvertragTemplateContext
        {
            Paechter1 = new MitgliedRecord { Id = 17, Vorname = "Max", Name = "Mustermann" },
            ParzelleNummer = "42",
            ParzelleFlaecheQm = 300m,
            Pachtbeginn = new DateTime(2026, 4, 1),
            PachtProQm = .25m
        };

        var error = Assert.Throws<InvalidOperationException>(() => PachtvertragTemplateFactory.BuildData(context));
        Assert.Contains("Mitgliedsnummer", error.Message);
    }

    [Fact]
    public async Task CreateUploadRequest_AssignsPachtvertragToParzelle()
    {
        var request = await PachtvertragDokumentFactory.CreateUploadRequestAsync(
            hauptmitglied: new MitgliedRecord
            {
                Id = 17,
                Mitgliedsnummer = 1017,
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
                PachtProQm = 0.25m,
                PachtFaelligAm = new DateTime(2026, 3, 31)
            },
            vertragsbeginn: new DateTime(2026, 4, 1),
            altvertragDatum: null,
            gesetzlicherVertreterSnapshot: null,
            bankverbindungSnapshot: new VereinskonfigurationSnapshot
            {
                VereinName = "KGV Musterverein",
                VereinRegisterangabe = "VR 123",
                DokumentOrt = "Musterstadt",
                Kontoinhaber = "KGV Musterverein",
                Bankname = "Musterbank",
                Iban = "DE00123456780000000000",
                Bic = "MUSTDEFFXXX"
            },
            status: FormularDokumentStatus.Signiert,
            templateProvider: new CanonicalPdfTemplateProvider());

        Assert.Equal(42, request.ParzelleId);
        Assert.Null(request.MitgliedId);
        Assert.NotEmpty(request.FileContent);

        using var pdf = PdfReader.Open(new MemoryStream(request.FileContent), PdfDocumentOpenMode.InformationOnly);
        Assert.Equal(3, pdf.Pages.Count);
    }
}
