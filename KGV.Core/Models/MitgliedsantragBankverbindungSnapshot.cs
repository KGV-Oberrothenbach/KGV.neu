namespace KGV.Core.Models;

public sealed class MitgliedsantragBankverbindungSnapshot
{
    public string VereinName { get; set; } = string.Empty;
    public string VereinRegisterangabe { get; set; } = string.Empty;
    public string VereinEmail { get; set; } = string.Empty;

    public string Kontoinhaber { get; set; } = string.Empty;
    public string Bankname { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;

    public string VerwendungszweckMitgliedsantrag { get; set; } = string.Empty;
    public string DokumentOrt { get; set; } = string.Empty;
    public string StandardHinweistext { get; set; } = string.Empty;
    public string DatenschutzText { get; set; } = string.Empty;
    public string DatenschutzVersion { get; set; } = string.Empty;
    public DateTime? DatenschutzStand { get; set; }
}