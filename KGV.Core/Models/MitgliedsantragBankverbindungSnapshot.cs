namespace KGV.Core.Models;

public sealed class MitgliedsantragBankverbindungSnapshot
{
    public string Kontoinhaber { get; set; } = string.Empty;
    public string Bankname { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
}