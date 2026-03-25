using System;

namespace KGV.Core.Models
{
    public sealed class PhotoUploadTestRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string Kind { get; set; } = "ablesung";
        public string Medium { get; set; } = "strom";
        public string Anlage { get; set; } = string.Empty;
        public string Garten { get; set; } = string.Empty;
        public string? Zaehlernummer { get; set; }
        public DateTime Datum { get; set; } = DateTime.Today;
    }
}
