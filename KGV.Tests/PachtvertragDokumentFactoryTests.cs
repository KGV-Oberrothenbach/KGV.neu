using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace KGV.Tests;

public sealed class PachtvertragDokumentFactoryTests
{
    [Fact]
    public async Task CreateUploadRequest_AssignsPachtvertragToParzelle()
    {
        var request = await PachtvertragDokumentFactory.CreateUploadRequestAsync(
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
