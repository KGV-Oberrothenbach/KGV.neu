using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("mitglied")]
public sealed class MitgliedInsertRecord : BaseModel
{
    [Column("hauptmitglied_id")]
    public int? HauptmitgliedId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("vorname")]
    public string Vorname { get; set; } = string.Empty;

    [Column("adresse")]
    public string? Adresse { get; set; }

    [Column("plz")]
    public string? Plz { get; set; }

    [Column("ort")]
    public string? Ort { get; set; }

    [Column("telefon")]
    public string? Telefon { get; set; }

    [Column("handy")]
    public string? Handy { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("geburtsdatum")]
    public DateTime? Geburtsdatum { get; set; }

    [Column("bemerkung")]
    public string? Bemerkung { get; set; }

    [Column("whatsapp_einwilligung")]
    public bool WhatsappEinwilligung { get; set; }

    [Column("email_rechnung_einwilligung")]
    public bool EmailRechnungEinwilligung { get; set; }

    [Column("email_info_einwilligung")]
    public bool EmailInfoEinwilligung { get; set; }

    [Column("arbeitsstunden_altersregel_typ")]
    public string ArbeitsstundenAltersregelTyp { get; set; } = "keine";

    [Column("mitglied_seit")]
    public DateTime? MitgliedSeit { get; set; }

    [Column("mitglied_ende")]
    public DateTime? MitgliedEnde { get; set; }

    [Column("aktiv")]
    public bool Aktiv { get; set; } = true;
}