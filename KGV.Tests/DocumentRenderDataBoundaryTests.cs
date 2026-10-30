using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class DocumentRenderDataBoundaryTests
{
    [Fact]
    public void Lease_template_factory_prepares_domain_values_before_rendering()
    {
        var data = PachtvertragTemplateFactory.BuildData(new PachtvertragTemplateContext
        {
            VereinskonfigurationSnapshot = new VereinskonfigurationSnapshot
            {
                Kontoinhaber = "KGV Musterverein",
                Bankname = "Musterbank",
                Iban = "DE00123456780000000000",
                Bic = "MUSTDEFFXXX"
            },
            Paechter1 = new MitgliedRecord { Id = 7, Mitgliedsnummer = 1007, Vorname = "Max", Name = "Mustermann", Adresse = "Gartenweg 7", Plz = "08000", Ort = "Zwickau" },
            Paechter2 = new MitgliedRecord { Id = 8, Mitgliedsnummer = 1008, Vorname = "Erika", Name = "Musterfrau" },
            ZweiteParteiIstGesetzlicherVertreter = false,
            ParzelleNummer = "17",
            ParzelleFlaecheQm = 300,
            ParzelleAnlage = "Anlage Nord",
            Pachtbeginn = new DateTime(2026, 4, 1),
            PachtProQm = .25m,
            ZahlungszielText = "31.03.2026",
            UnterschriftOrt = "Zwickau"
        });

        Assert.Equal("Max Mustermann", data.Paechter1Vollname);
        Assert.Equal("Mitpächter/in", data.ZweiteParteiBezeichnung);
        Assert.Equal("75,00 €", data.Jahrespacht);
        Assert.Equal("31.03.2026", data.PachtzahlungFaelligBis);
        Assert.Equal("31.12.2027", data.ProbephaseBis);
        Assert.Equal("0,25 €/m²", data.PachtProQm);
    }

    [Fact]
    public void Renderers_accept_only_technical_render_data()
    {
        var fields = new[] { new PdfTemplateFieldSpec("neutral", "text", 1, 0, 0, 100, 20) };
        var values = new Dictionary<string, string> { ["neutral"] = "fertiger Wert" };

        var templateOperation = Assert.Single(PdfTemplateRenderer.BuildRenderPlan(fields, values));
        var signatureOperation = Assert.Single(PdfSignatureRenderer.BuildRenderPlan(new[]
        {
            (new SignaturePlaceholder { Name = "unabhaengig", Page = 1, X = 0, Y = 0, Width = 100, Height = 30 }, Capture())
        }));

        Assert.Equal("fertiger Wert", templateOperation.Text);
        Assert.Equal("unabhaengig", signatureOperation.Name);
    }

    private static DigitalSignatureCapture Capture() => new()
    {
        CanvasWidth = 100,
        CanvasHeight = 30,
        Strokes = new[] { new DigitalSignatureStroke { Points = new[] { new DigitalSignaturePoint { X = 0, Y = 0 }, new DigitalSignaturePoint { X = 100, Y = 30 } } } }
    };
}
