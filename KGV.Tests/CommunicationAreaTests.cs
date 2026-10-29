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
}
