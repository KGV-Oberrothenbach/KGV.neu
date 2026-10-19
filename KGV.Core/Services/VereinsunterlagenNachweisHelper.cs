using KGV.Core.Models;

namespace KGV.Core.Services;

public static class VereinsunterlagenNachweisHelper
{
    private static readonly string[] Reihenfolge = ["satzung", "kleingartenordnung", "beitragsordnung", "arbeitsordnung"];

    public static IReadOnlyList<VereinsunterlagenNachweisSnapshotItem> CreateSnapshot(MitgliedVereinsdokumentStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var byCode = status.Dokumente.ToDictionary(item => item.DokumenttypCode, StringComparer.OrdinalIgnoreCase);
        var result = new List<VereinsunterlagenNachweisSnapshotItem>();
        foreach (var code in Reihenfolge)
        {
            if (!byCode.TryGetValue(code, out var item) || item.Status == "keine_aktuelle_fassung") throw new PreparedContractValidationException("Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt.");
            if (item.Status != "bereitgestellt" || item.VereinsdokumentFassungId is null || item.BereitstellungId is null || item.BereitgestelltAm is null || string.IsNullOrWhiteSpace(item.Fassung) || string.IsNullOrWhiteSpace(item.Bereitstellungsart)) throw new PreparedContractValidationException("Mindestens eine aktuelle Vereinsunterlage fehlt beim Mitglied.");
            result.Add(new VereinsunterlagenNachweisSnapshotItem { Dokumenttyp = item.DokumenttypCode, VereinsdokumentFassungId = item.VereinsdokumentFassungId.Value, Fassung = item.Fassung, GueltigAb = item.GueltigAb ?? DateTime.MinValue, GueltigBis = item.GueltigBis, BereitstellungId = item.BereitstellungId.Value, Bereitstellungsart = item.Bereitstellungsart, BereitgestelltAm = item.BereitgestelltAm.Value });
        }
        return result;
    }

    public static bool Equivalent(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? left, IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? right)
        => IsComplete(left) && IsComplete(right) && left!.SequenceEqual(right!);

    public static string FormatForPdf(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem> snapshot)
    {
        if (!Equivalent(snapshot, snapshot)) throw new PreparedContractValidationException("Der Vereinsunterlagen-Nachweis ist unvollständig.");
        return string.Join(Environment.NewLine, snapshot.Select(item => $"{Label(item.Dokumenttyp)} – Fassung {item.Fassung} – {(item.Bereitstellungsart == "papier" ? "Papier übergeben" : "digital bereitgestellt")} am {item.BereitgestelltAm:dd.MM.yyyy HH:mm}"));
    }

    public static bool IsComplete(IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? snapshot)
        => snapshot is not null && snapshot.Count == Reihenfolge.Length && snapshot.Select((item, index) =>
            string.Equals(item.Dokumenttyp, Reihenfolge[index], StringComparison.OrdinalIgnoreCase) &&
            item.VereinsdokumentFassungId > 0 && item.BereitstellungId > 0 &&
            !string.IsNullOrWhiteSpace(item.Fassung) && !string.IsNullOrWhiteSpace(item.Bereitstellungsart) &&
            item.GueltigAb != DateTime.MinValue && item.BereitgestelltAm != DateTime.MinValue).All(valid => valid);
    private static string Label(string code) => code.ToLowerInvariant() switch { "satzung" => "Satzung", "kleingartenordnung" => "Kleingartenordnung", "beitragsordnung" => "Beitragsordnung", "arbeitsordnung" => "Arbeitsordnung", _ => code };
}
