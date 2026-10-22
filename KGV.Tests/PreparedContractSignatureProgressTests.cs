using KGV.Core.Models;
using KGV.Core.Utilities;

namespace KGV.Tests;

public sealed class PreparedContractSignatureProgressTests
{
    [Fact]
    public void Progress_counts_required_and_optional_positions_independently()
    {
        var positions = new[]
        {
            Position(true, PreparedContractSignatureStatuses.Open), Position(true, PreparedContractSignatureStatuses.Signed),
            Position(true, PreparedContractSignatureStatuses.Signed), Position(true, PreparedContractSignatureStatuses.Signed),
            Position(false, PreparedContractSignatureStatuses.Open)
        };
        var progress = PreparedContractSignatureProgress.From(positions);
        Assert.Equal(4, progress.RequiredCount); Assert.Equal(3, progress.RequiredSignedCount);
        Assert.Equal(1, progress.OptionalCount); Assert.Equal(0, progress.OptionalSignedCount); Assert.False(progress.AllRequiredSigned);
    }

    [Fact]
    public void Storage_path_is_revision_bound_and_contains_no_personal_name()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("123/4/mitglied.antrag/11111111-1111-1111-1111-111111111111.png", PreparedContractSignatureStoragePath.Create(123, 4, "mitglied.antrag", id));
    }

    [Fact]
    public void Sha256_uses_the_actual_bytes() => Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", PreparedContractSignatureStoragePath.ComputeSha256("abc"u8));

    private static PreparedContractSignatureDto Position(bool required, string status) => new() { Pflicht = required, Status = status };
}
