namespace KGV.Core.Models;

public sealed class MitgliedVereinsdokumentStatus
{
    public IReadOnlyList<MitgliedVereinsdokumentStatusRecord> Dokumente { get; init; } = Array.Empty<MitgliedVereinsdokumentStatusRecord>();
    public IReadOnlyList<MitgliedVereinsdokumentStatusRecord> FehlendeDokumente => Dokumente.Where(item => item.Status != "bereitgestellt").ToList();
    public bool Vollstaendig => Dokumente.Count > 0 && Dokumente.All(item => item.Status == "bereitgestellt");
    public bool HatFehlendeVereinsfassungen => Dokumente.Any(item => item.Status == "keine_aktuelle_fassung");
}
