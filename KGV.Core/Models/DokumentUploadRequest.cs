using System;

namespace KGV.Core.Models
{
    public sealed class DokumentUploadRequest
    {
        public int? MitgliedId { get; set; }
        public int? ParzelleId { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/octet-stream";
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
    }
}
