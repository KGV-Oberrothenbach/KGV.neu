using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class InsertRecordMappingTests
{
    [Fact]
    public void TerminInsertMapping_PreservesBusinessFields()
    {
        var mapped = new TerminRecord { Titel = "Termin", Aktiv = true }.ToInsertRecord();
        Assert.Equal("Termin", mapped.Titel);
        Assert.True(mapped.Aktiv);
    }

    [Fact]
    public void BekanntmachungInsertMapping_PreservesBusinessFields()
    {
        var mapped = new BekanntmachungRecord { Titel = "Hinweis", InhaltHtml = "<p>Text</p>" }.ToInsertRecord();
        Assert.Equal("Hinweis", mapped.Titel);
        Assert.Equal("<p>Text</p>", mapped.InhaltHtml);
    }
}
