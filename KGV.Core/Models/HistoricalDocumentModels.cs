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
