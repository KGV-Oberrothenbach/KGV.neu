using KGV.Core.Models;
using KGV.ViewModels;
using System;
using System.Collections.Generic;
using Xunit;

namespace KGV.Tests;

public sealed class ExportViewModelTests
{
    [Fact]
    public void BuildMitgliederCsv_WritesHeader_AndEscapesValues()
    {
        var members = new List<MitgliedRecord>
        {
            new()
            {
                Id = 1,
                Vorname = "Max",
                Name = "Muster;mann",
                Email = "max\"quote@example.com",
                Telefon = "123",
                Handy = "456",
                Adresse = "Straße 1",
                Plz = "91234",
                Ort = "Ort",
                Aktiv = true,
                Role = "admin",
                MitgliedSeit = new DateTime(2026, 1, 2),
                MitgliedEnde = null
            }
        };

        var csv = ExportViewModel.BuildMitgliederCsv(members);

        Assert.Contains("Id;Vorname;Nachname;E-Mail;Telefon;Handy;Adresse;PLZ;Ort;Aktiv;Role;MitgliedSeit;MitgliedEnde", csv);
        Assert.Contains("\"Muster;mann\"", csv);
        Assert.Contains("\"max\"\"quote@example.com\"", csv);
        Assert.Contains(";1;admin;2026-01-02;", csv);
    }
}
