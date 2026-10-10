using KGV.Core.Models;
using Xunit;

namespace KGV.Tests;

public sealed class InsertRecordMappingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TerminInsertMapping_PreservesDemoScope(bool isDemo)
        => Assert.Equal(isDemo, new TerminRecord { IsDemo = isDemo }.ToInsertRecord().IsDemo);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BekanntmachungInsertMapping_PreservesDemoScope(bool isDemo)
        => Assert.Equal(isDemo, new BekanntmachungRecord { IsDemo = isDemo }.ToInsertRecord().IsDemo);
}
