using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("mitgliedsaufnahme_vorstand_liste")]
public sealed class MembershipBoardReviewRecord : BaseModel
{
    [PrimaryKey("bearbeitung_id", false)] [Column("bearbeitung_id")] public long BearbeitungId { get; set; }
    [Column("kommunikationsvorgang_id")] public long KommunikationsvorgangId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("final_dokument_id")] public long? FinalDokumentId { get; set; }
    [Column("antragsteller")] public string Antragsteller { get; set; } = string.Empty;
    [Column("gestartet_am")] public DateTime GestartetAm { get; set; }
    [Column("entscheidung")] public string Entscheidung { get; set; } = string.Empty;
    [Column("snapshot_personen")] public int SnapshotPersonen { get; set; }
    [Column("abgegebene_stimmen")] public int AbgegebeneStimmen { get; set; }
    [Column("ja_stimmen")] public int JaStimmen { get; set; }
    [Column("nein_stimmen")] public int NeinStimmen { get; set; }
    [Column("einstimmig_ja")] public bool EinstimmigJa { get; set; }
    [Column("konflikt")] public bool Konflikt { get; set; }
}

[Table("mitgliedsaufnahme_abstimmung_snapshot_person")]
public sealed class MembershipBoardSnapshotPersonRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("vorstandsbearbeitung_id")] public long VorstandsbearbeitungId { get; set; }
    [Column("mitglied_id")] public long MitgliedId { get; set; }
    [Column("name_snapshot")] public string NameSnapshot { get; set; } = string.Empty;
}

[Table("mitgliedsaufnahme_abstimmung_snapshot_berechtigung")]
public sealed class MembershipBoardAuthorizationSourceRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("snapshot_person_id")] public long SnapshotPersonId { get; set; }
    [Column("berechtigungsart")] public string Berechtigungsart { get; set; } = string.Empty;
    [Column("funktionsbezeichnung_snapshot")] public string FunktionsbezeichnungSnapshot { get; set; } = string.Empty;
    [Column("vertretene_funktionsbezeichnung_snapshot")] public string? VertreteneFunktionsbezeichnungSnapshot { get; set; }
}

[Table("mitgliedsaufnahme_abstimmung_stimme")]
public sealed class MembershipBoardVoteRecord : BaseModel
{
    [PrimaryKey("id", false)] [Column("id")] public long Id { get; set; }
    [Column("snapshot_person_id")] public long SnapshotPersonId { get; set; }
    [Column("version")] public int Version { get; set; }
    [Column("stimme")] public string Stimme { get; set; } = string.Empty;
    [Column("nein_begruendung")] public string? NeinBegruendung { get; set; }
    [Column("abgegeben_am")] public DateTime AbgegebenAm { get; set; }
}

public sealed record MembershipBoardVote(long PersonId, long MitgliedId, string Name, string? Stimme, string? NeinBegruendung, DateTime? AbgegebenAm, IReadOnlyList<string> Berechtigungsquellen);
public sealed record MembershipBoardReviewDetail(MembershipBoardReviewRecord Review, IReadOnlyList<MembershipBoardVote> Stimmen);
