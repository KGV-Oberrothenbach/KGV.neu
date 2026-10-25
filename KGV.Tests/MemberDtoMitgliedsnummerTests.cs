using KGV.Core.Models;

namespace KGV.Tests;

public sealed class MemberDtoMitgliedsnummerTests
{
    [Fact]
    public void Clone_PreservesMitgliedsnummer()
    {
        var member = new MemberDTO
        {
            Id = 81,
            Mitgliedsnummer = 1081,
            Vorname = "Max",
            Nachname = "Mustermann"
        };

        var clone = member.Clone();

        Assert.Equal(81, clone.Id);
        Assert.Equal(1081, clone.Mitgliedsnummer);
        Assert.True(member.ValueEquals(clone));
    }
}
