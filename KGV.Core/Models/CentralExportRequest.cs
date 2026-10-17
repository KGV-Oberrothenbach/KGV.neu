using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KGV.Core.Models
{
    /// <summary>Common client contract for the server-side export renderer.</summary>
    public sealed class CentralExportRequest
    {
        [JsonPropertyName("export_key")]
        public string ExportKey { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = "pdf";

        [JsonPropertyName("parameters")]
        public IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();

        [JsonPropertyName("columns")]
        public IReadOnlyList<string> Columns { get; set; } = new List<string>();

        [JsonPropertyName("orientation")]
        public string Orientation { get; set; } = "auto";
    }

    /// <summary>Validated response from the central PDF renderer.</summary>
    public sealed class CentralExportPdfResult
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string? FileName { get; init; }
    }
}
