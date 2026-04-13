using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("vereinskonfiguration")]
public sealed class VereinskonfigurationRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("vereinsname")]
    public string? Vereinsname { get; set; }

    [Column("kurzname")]
    public string? Kurzname { get; set; }

    [Column("registerangabe")]
    public string? Registerangabe { get; set; }

    [Column("strasse")]
    public string? Strasse { get; set; }

    [Column("plz")]
    public string? Plz { get; set; }

    [Column("ort")]
    public string? Ort { get; set; }

    [Column("standard_email")]
    public string? StandardEmail { get; set; }

    [Column("standard_telefon")]
    public string? StandardTelefon { get; set; }

    [Column("website")]
    public string? Website { get; set; }

    [Column("aktiv")]
    public bool Aktiv { get; set; }

    [Column("kontoinhaber")]
    public string? Kontoinhaber { get; set; }

    [Column("bankname")]
    public string? Bankname { get; set; }

    [Column("iban")]
    public string? Iban { get; set; }

    [Column("bic")]
    public string? Bic { get; set; }

    [Column("verwendungszweck_mitgliedsantrag")]
    public string? VerwendungszweckMitgliedsantrag { get; set; }

    [Column("verwendungszweck_pachtvertrag")]
    public string? VerwendungszweckPachtvertrag { get; set; }

    [Column("dokument_ort")]
    public string? DokumentOrt { get; set; }

    [Column("standard_hinweistext")]
    public string? StandardHinweistext { get; set; }

    [Column("datenschutz_text")]
    public string? DatenschutzText { get; set; }

    [Column("datenschutz_version")]
    public string? DatenschutzVersion { get; set; }

    [Column("datenschutz_stand")]
    public DateTime? DatenschutzStand { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}