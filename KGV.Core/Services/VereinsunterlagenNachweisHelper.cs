using KGV.Core.Models;

namespace KGV.Core.Services;

public static class VereinsunterlagenNachweisHelper
{
    private static readonly IReadOnlyDictionary<string, string[]> Pflichtunterlagen = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [PreparedContractConstants.MembershipApplication] = ["satzung"],
        [PreparedContractConstants.LeaseContract] = ["kleingartenordnung", "beitragsordnung", "arbeitsordnung"]
    };

    public static IReadOnlyList<string> RequiredFor(string documentType)
        => Pflichtunterlagen.TryGetValue(documentType, out var required)
            ? required
            : throw new PreparedContractValidationException($"Für Vertragsart '{documentType}' sind keine Pflichtunterlagen definiert.");

    public static IReadOnlyList<VereinsunterlagenNachweisSnapshotItem> CreateSnapshot(MitgliedVereinsdokumentStatus status, string documentType)
    {
        ArgumentNullException.ThrowIfNull(status);
        var byCode = status.Dokumente.ToDictionary(item => item.DokumenttypCode, StringComparer.OrdinalIgnoreCase);
        var result = new List<VereinsunterlagenNachweisSnapshotItem>();
        foreach (var code in RequiredFor(documentType))
        {
            if (!byCode.TryGetValue(code, out var item) || item.Status == "keine_aktuelle_fassung") throw new PreparedContractValidationException("Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt.");
            if (item.Status != "bereitgestellt" || item.VereinsdokumentFassungId is null || item.BereitstellungId is null || item.BereitgestelltAm is null || string.IsNullOrWhiteSpace(item.Fassung) || string.IsNullOrWhiteSpace(item.Bereitstellungsart)) throw new PreparedContractValidationException("Mindestens eine aktuelle Vereinsunterlage fehlt beim Mitglied.");
            result.Add(new VereinsunterlagenNachweisSnapshotItem { Dokumenttyp = item.DokumenttypCode, VereinsdokumentFassungId = item.VereinsdokumentFassungId.Value, Fassung = item.Fassung, GueltigAb = item.GueltigAb ?? DateTime.MinValue, GueltigBis = item.GueltigBis, BereitstellungId = item.BereitstellungId.Value, Bereitstellungsart = item.Bereitstellungsart, BereitgestelltAm = item.BereitgestelltAm.Value });
        }
        return result;
    }

    public static bool Equivalent(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? left, IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? right, string documentType)
        => IsComplete(left, documentType) && IsComplete(right, documentType) && left!.SequenceEqual(right!);

    public static string FormatForPdf(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem> snapshot, string documentType)
    {
        if (!IsComplete(snapshot, documentType)) throw new PreparedContractValidationException("Der Vereinsunterlagen-Nachweis ist unvollständig.");
        return string.Join(Environment.NewLine, snapshot.Select(item => $"{Label(item.Dokumenttyp)} – {(string.IsNullOrWhiteSpace(item.Fassung) ? "Papierfassung ausgehändigt" : $"Fassung {item.Fassung}")} – {(item.Bereitstellungsart == "papier" ? "Papier übergeben" : "digital bereitgestellt")} am {item.BereitgestelltAm:dd.MM.yyyy HH:mm}"));
    }

    public static bool IsComplete(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? snapshot, string documentType)
    {
        var required = RequiredFor(documentType);
        return snapshot is not null && snapshot.Count == required.Count && snapshot.Select((item, index) =>
            string.Equals(item.Dokumenttyp, required[index], StringComparison.OrdinalIgnoreCase) &&
            item.Bereitstellungsart is "digital" or "papier" && item.BereitgestelltAm != DateTime.MinValue &&
            (item.Bereitstellungsart == "papier" || (item.VereinsdokumentFassungId > 0 && item.BereitstellungId > 0 && !string.IsNullOrWhiteSpace(item.Fassung) && item.GueltigAb != DateTime.MinValue))).All(valid => valid);
    }
    private static string Label(string code) => code.ToLowerInvariant() switch { "satzung" => "Satzung", "kleingartenordnung" => "Kleingartenordnung", "beitragsordnung" => "Beitragsordnung", "arbeitsordnung" => "Arbeitsordnung", _ => code };
}
