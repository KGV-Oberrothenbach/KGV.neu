using System.Text.Json.Serialization;

namespace KGV.Core.Models;

public sealed class MitgliedVereinsunterlagenBereitstellungResult
{
    [JsonPropertyName("success")] public bool Success { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("diagnostic_code")] public string DiagnosticCode { get; init; } = string.Empty;
    [JsonPropertyName("bereitgestellt_am")] public DateTime? BereitgestelltAm { get; init; }
    public static MitgliedVereinsunterlagenBereitstellungResult Ok(DateTime time, string message) => new() { Success = true, BereitgestelltAm = time, Message = message, DiagnosticCode = "OK" };
    public static MitgliedVereinsunterlagenBereitstellungResult Fail(string message, string code) => new() { Success = false, Message = message, DiagnosticCode = code };
}
