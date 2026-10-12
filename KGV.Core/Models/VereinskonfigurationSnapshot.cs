namespace KGV.Core.Models;

/// <summary>Document-relevant club data captured for one document generation process.</summary>
public class VereinskonfigurationSnapshot
{
    public string VereinName { get; set; } = string.Empty;
    public string Kurzname { get; set; } = string.Empty;
    public string VereinRegisterangabe { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string Plz { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string VereinEmail { get; set; } = string.Empty;
    public string StandardTelefon { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Kontoinhaber { get; set; } = string.Empty;
    public string Bankname { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
    public string VerwendungszweckMitgliedsantrag { get; set; } = string.Empty;
    public string VerwendungszweckPachtvertrag { get; set; } = string.Empty;
    public string DokumentOrt { get; set; } = string.Empty;
    public string StandardHinweistext { get; set; } = string.Empty;
    public string DatenschutzText { get; set; } = string.Empty;
    public string DatenschutzVersion { get; set; } = string.Empty;
    public DateTime? DatenschutzStand { get; set; }
}
