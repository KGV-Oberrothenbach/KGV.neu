using System;

namespace KGV.Core.Models
{
    public sealed class DocumentInfo
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string Dateiname { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string DriveFileId { get; set; } = string.Empty;
        public long? Size { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
