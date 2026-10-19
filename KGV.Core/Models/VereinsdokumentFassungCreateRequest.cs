namespace KGV.Core.Models;
public sealed class VereinsdokumentFassungCreateRequest
{
    public string DokumenttypCode { get; set; } = string.Empty;
    public string Fassung { get; set; } = string.Empty;
    public DateOnly GueltigAb { get; set; }
    public string Dateiname { get; set; } = string.Empty;
    public byte[] PdfInhalt { get; set; } = Array.Empty<byte>();
}
