using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

public static class DokumenttypWirkung
{
    public const string MitgliedsantragNachweis = "mitgliedsantrag_nachweis";
    public const string PachtvertragNachweis = "pachtvertrag_nachweis";
    public const string WartungsvertragNachweis = "wartungsvertrag_nachweis";
    public const string PachtbezugErforderlich = "pachtbezug_erforderlich";
    public const string WartungsvertragsbezugErforderlich = "wartungsvertragsbezug_erforderlich";
}

[Table("dokumenttyp")]
public sealed class DokumenttypRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("bezeichnung")] public string Bezeichnung { get; set; } = string.Empty;
    [Column("aktiv")] public bool Aktiv { get; set; }
    [Column("sortierung")] public int Sortierung { get; set; }
}

[Table("dokumenttyp_wirkung")]
public sealed class DokumenttypWirkungRecord : BaseModel
{
    [PrimaryKey("dokumenttyp_id", false)] [Column("dokumenttyp_id")] public long DokumenttypId { get; set; }
    [Column("wirkung")] public string Wirkung { get; set; } = string.Empty;
}

public sealed record DokumenttypItem(long Id, string Bezeichnung, bool Aktiv, int Sortierung, IReadOnlyList<string> Wirkungen);
public sealed record HistorischesDokumentRegistrierungRequest(long DokumentId, long DokumenttypId, DateTime Dokumentdatum, bool Unterzeichnet, long? PachtverhaeltnisId, long? WartungsvertragZuordnungId);
public sealed record HistorischesDokumentRegistrierungResult(long DokumentId, long DokumenttypId, DateTime Dokumentdatum, bool Unterzeichnet, long? PachtvertragsfassungId, long? WartungsvertragZuordnungId);
public sealed record HistorischeVertragsnummerResult(int Nummer, string Titel, bool PdfGestempelt);
public sealed record HistoricalContractRegistrationRequest(long DokumentId, string DokumenttypCode, DateTime Vertragsdatum, bool Unterzeichnet, IReadOnlyList<long> PachtverhaeltnisIds, IReadOnlyDictionary<long, DateTime> VorherigeEnddaten, long? MitgliedSeitKorrekturMitgliedId, DateTime? MitgliedSeitKorrekturDatum, IReadOnlyList<long> VertragsparteiMitgliedIds, IReadOnlyList<HistoricalMembershipEnd> Mitgliedschaftsenden, IReadOnlyList<HistoricalActivityStatusChange> AktivStatusAenderungen);
public sealed record HistoricalMembershipEnd(long MitgliedId, DateTime MitgliedEnde);
public sealed record HistoricalActivityStatusChange(long MitgliedId, bool Aktiv);
public sealed record HistoricalContractRegistrationResult(long DokumentId, long DokumenttypId, long PachtverhaeltnisId, long PachtvertragsfassungId, int HistorischeVertragsnummer);
/// <summary>Read-only common projection for the historical-contract editor. The
/// server determines predecessor and successor from the effective date.</summary>
public sealed record HistoricalContractHistoryEntry(long PachtverhaeltnisId, long PachtvertragsfassungId, DateOnly WirksamAb, DateOnly? GueltigBis, int? HistorischeVertragsnummer, IReadOnlyList<long> VertragsparteiMitgliedIds, bool IstVorgaenger, bool IstNachfolger, DateOnly? VorgeschlagenesVorgaengerende);

/// <summary>Die beiden fachlich zulässigen Formen eines nacherfassten Papiervertrags.</summary>
public enum HistoricalContractKind
{
    MembershipAndLeaseInitialContract = 1,
    LeaseFollowUpContract = 2
}

public static class HistoricalContractDocumentTypes
{
    public const string MembershipAndLeaseInitialContract = "historischer_ma_pv_erstvertrag";
    public const string LeaseFollowUpContract = "historischer_pv_folgevertrag";
}

public sealed record HistoricalContractParty(long MitgliedId, long Mitgliedsnummer);

/// <summary>Ein vorhandener Vertragsstand derselben Pachtbeziehung.</summary>
public sealed record HistoricalLeaseContractVersion(long PachtvertragsfassungId, DateOnly WirksamAb, DateOnly? GueltigBis);

/// <summary>
/// Ergebnis der zeitlichen Einordnung einer neuen Vertragsfassung. Es betrifft bewusst
/// nur die übergebene Pachtbeziehung und kann deshalb keine andere Parzelle verändern.
/// </summary>
public sealed record HistoricalLeaseHistoryPlacement(
    long? VorgaengerFassungId,
    DateOnly? VorgeschlagenesEndeDesVorgaengers,
    long? NachfolgerFassungId,
    DateOnly? VorgeschlagenesEndeDerNeuenFassung);

public enum HistoricalMembershipExitHandling
{
    LeaseParticipationOnly = 1,
    LeaseParticipationAndMembership = 2
}

public enum HistoricalMembershipActivitySuggestion
{
    KeepCurrentState = 0,
    SuggestActive = 1,
    SuggestPassive = 2
}

public sealed record HistoricalMembershipSinceCorrection(long MitgliedId, DateOnly BisherigesDatum, DateOnly KorrigiertesDatum)
{
    public bool IsCorrection => BisherigesDatum != KorrigiertesDatum;
}

public static class HistoricalContractRegistrationRules
{
    public static HistoricalLeaseHistoryPlacement PlaceLeaseVersion(
        DateOnly wirksamAb,
        IEnumerable<HistoricalLeaseContractVersion> existingVersions,
        DateOnly? overriddenPreviousEnd = null)
    {
        ArgumentNullException.ThrowIfNull(existingVersions);

        var ordered = existingVersions.OrderBy(version => version.WirksamAb).ToArray();
        var predecessor = ordered.LastOrDefault(version => version.WirksamAb < wirksamAb);
        var successor = ordered.FirstOrDefault(version => version.WirksamAb > wirksamAb);

        return new HistoricalLeaseHistoryPlacement(
            predecessor?.PachtvertragsfassungId,
            predecessor is null ? null : overriddenPreviousEnd ?? wirksamAb.AddDays(-1),
            successor?.PachtvertragsfassungId,
            successor is null ? null : successor.WirksamAb.AddDays(-1));
    }

    public static HistoricalMembershipActivitySuggestion SuggestMembershipActivity(
        bool isCurrentlyActive,
        int effectiveLeaseRelationCount)
    {
        if (effectiveLeaseRelationCount < 0)
            throw new ArgumentOutOfRangeException(nameof(effectiveLeaseRelationCount));

        return (isCurrentlyActive, effectiveLeaseRelationCount) switch
        {
            (false, > 0) => HistoricalMembershipActivitySuggestion.SuggestActive,
            (true, 0) => HistoricalMembershipActivitySuggestion.SuggestPassive,
            _ => HistoricalMembershipActivitySuggestion.KeepCurrentState
        };
    }

    public static string CreatePdfStampText(int historicalContractNumber, IEnumerable<long> memberNumbers)
    {
        var numbers = memberNumbers?.Distinct().OrderBy(number => number).ToArray()
            ?? throw new ArgumentNullException(nameof(memberNumbers));
        if (numbers.Length == 0)
            throw new ArgumentException("Mindestens eine Mitgliedsnummer ist erforderlich.", nameof(memberNumbers));

        return $"Mgl.-Nr. {string.Join(" / ", numbers)}{Environment.NewLine}Vertrags-Nr. {historicalContractNumber}";
    }
}

[Table("pachtverhaeltnis_beteiligung")]
public sealed class PachtverhaeltnisBeteiligungRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("pachtverhaeltnis_id")] public long PachtverhaeltnisId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("rolle")] public string Rolle { get; set; } = string.Empty;
    [Column("gueltig_ab")] public DateTime GueltigAb { get; set; }
    [Column("gueltig_bis")] public DateTime? GueltigBis { get; set; }
}

public sealed record HistorischePachtbeziehung(long PachtverhaeltnisId, long ParzelleId, string Parzellennummer, string Anlage, string Rolle, DateTime GueltigAb, DateTime? GueltigBis, DateTime? UrspruenglicherPachtbeginn, DateTime? BeendetAm);
public sealed record HistorischeWartungsvertragZuordnung(long ZuordnungId, long WartungsvertragId, string Bezeichnung, long HauptmitgliedId, DateTime GueltigAb, DateTime? GueltigBis);
public sealed record DokumentNachweisstatus(string Status, DateTime? RelevantesDatum, long? DokumentId, long? PachtvertragsfassungId = null);

[Table("v_mitglied_mitgliedsantrag_nachweisstatus")]
public sealed class MitgliedsantragNachweisstatusRecord : BaseModel
{
    [PrimaryKey("mitglied_id", false)] [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("nachweisstatus")] public string Nachweisstatus { get; set; } = string.Empty;
    [Column("dokumentdatum")] public DateTime? Dokumentdatum { get; set; }
    [Column("dokument_id")] public long? DokumentId { get; set; }
}

[Table("v_pachtverhaeltnis_pachtvertrag_nachweisstatus")]
public sealed class PachtvertragNachweisstatusRecord : BaseModel
{
    [PrimaryKey("pachtverhaeltnis_id", false)] [Column("pachtverhaeltnis_id")] public long PachtverhaeltnisId { get; set; }
    [Column("nachweisstatus")] public string Nachweisstatus { get; set; } = string.Empty;
    [Column("vertragsdatum")] public DateTime? Vertragsdatum { get; set; }
    [Column("dokument_id")] public long? DokumentId { get; set; }
    [Column("pachtvertragsfassung_id")] public long? PachtvertragsfassungId { get; set; }
}

[Table("v_wartungsvertrag_zuordnung_nachweisstatus")]
public sealed class WartungsvertragNachweisstatusRecord : BaseModel
{
    [PrimaryKey("wartungsvertrag_zuordnung_id", false)] [Column("wartungsvertrag_zuordnung_id")] public long WartungsvertragZuordnungId { get; set; }
    [Column("nachweisstatus")] public string Nachweisstatus { get; set; } = string.Empty;
    [Column("dokumentdatum")] public DateTime? Dokumentdatum { get; set; }
    [Column("dokument_id")] public long? DokumentId { get; set; }
}
