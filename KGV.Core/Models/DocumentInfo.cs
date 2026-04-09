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

        public bool CanDelete => Id > 0;

        public bool CanOpen => !string.IsNullOrWhiteSpace(DriveFileId)
            || !string.IsNullOrWhiteSpace(StoragePath);

        public string FormularDokumentTypKey
            => TryResolveFormularMetadaten(out var dokumenttyp, out _) ? dokumenttyp : string.Empty;

        public string FormularDokumentStatusKey
            => TryResolveFormularMetadaten(out _, out var status) ? status : string.Empty;

        public string FormularDokumentTypAnzeige
            => string.IsNullOrWhiteSpace(FormularDokumentTypKey)
                ? "-"
                : FormularDokumentTyp.ToDisplayName(FormularDokumentTypKey);

        public string FormularDokumentStatusAnzeige
            => string.IsNullOrWhiteSpace(FormularDokumentStatusKey)
                ? "-"
                : FormularDokumentStatus.ToDisplayName(FormularDokumentStatusKey);

        public bool IsVertragsDokument
            => FormularDokumentTypKey is FormularDokumentTyp.Mitgliedsvertrag or FormularDokumentTyp.Pachtvertrag;

        public bool CanUploadSignedContractVersion
            => IsVertragsDokument && string.Equals(FormularDokumentStatusKey, FormularDokumentStatus.Unsigniert, StringComparison.Ordinal);

        public bool CanDigitallySignContractVersion
            => CanUploadSignedContractVersion;

        private bool TryResolveFormularMetadaten(out string dokumenttyp, out string status)
        {
            foreach (var candidate in new[] { Dateiname, StoragePath, Name, Title })
            {
                if (Utilities.FormularDokumentDateiname.TryParse(candidate, out dokumenttyp, out status))
                    return true;
            }

            dokumenttyp = string.Empty;
            status = string.Empty;
            return false;
        }
    }
}
