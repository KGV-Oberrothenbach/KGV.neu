namespace KGV.Core.Models;

public sealed class MitgliedVereinsunterlagenBereitstellungRequest
{
    public long MitgliedId { get; init; }
    public IReadOnlyList<long> VereinsdokumentFassungIds { get; init; } = Array.Empty<long>();
    public DateTime BereitgestelltAm { get; init; }
}
