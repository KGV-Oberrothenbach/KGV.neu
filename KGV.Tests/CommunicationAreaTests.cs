using KGV.Core.Models;

namespace KGV.Tests;

public sealed class CommunicationAreaTests
{
    [Fact]
    public void Defines_the_supported_technical_communication_areas()
    {
        var areas = Enum.GetValues<CommunicationArea>();

        Assert.Equal(
        [
            CommunicationArea.Signatures,
            CommunicationArea.Documents,
            CommunicationArea.Board,
            CommunicationArea.MailOutbox,
            CommunicationArea.Sent,
            CommunicationArea.Print,
            CommunicationArea.History
        ], areas);
    }

    [Theory]
    [InlineData(CommunicationAreaRoutes.Signatures, CommunicationArea.Signatures)]
    [InlineData(CommunicationAreaRoutes.Documents, CommunicationArea.Documents)]
    [InlineData(CommunicationAreaRoutes.MailOutbox, CommunicationArea.MailOutbox)]
    [InlineData(CommunicationAreaRoutes.Sent, CommunicationArea.Sent)]
    [InlineData(CommunicationAreaRoutes.Print, CommunicationArea.Print)]
    [InlineData(CommunicationAreaRoutes.History, CommunicationArea.History)]
    public void Maps_each_G5b_shell_route_to_its_fixed_area(string route, CommunicationArea expectedArea)
    {
        var found = CommunicationAreaRoutes.TryGetArea(route, out var area);

        Assert.True(found);
        Assert.Equal(expectedArea, area);
    }
}
