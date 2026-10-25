namespace KGV.Maui.Models;

public sealed class HistoricalDocumentRegistrationPending
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid VereinId { get; init; }
    public Guid OwnerUserId { get; init; }
    public long DokumentId { get; init; }
    public int MitgliedId { get; init; }
    public long DokumenttypId { get; init; }
    public DateTime Dokumentdatum { get; init; }
    public bool Unterzeichnet { get; init; }
    public long? PachtverhaeltnisId { get; init; }
    public long? WartungsvertragZuordnungId { get; init; }
}
